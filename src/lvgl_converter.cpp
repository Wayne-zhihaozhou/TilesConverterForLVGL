/* lvgl_converter.cpp
 * Fast C++ port of the reverse-engineered TilesConverterForLVGL tool.
 *
 * Converts PNG map tiles found under numbered subfolders into LVGL binary images:
 *   [ uint32 LE header ][ RGB565 pixel data ]
 *
 * Header  : num = (((height << 11) | width) << 10) | 4;   written little-endian.
 * Pixel   : r5=R>>3, g6=G>>2, b5=B>>3; word=(r5<<11)|(g6<<5)|b5.
 *           RGB565      -> word little-endian (low byte first)
 *           RGB565SWAP  -> word big-endian     (high byte first)
 *
 * Speed wins over the original C# build:
 *   - stb_image decodes PNGs straight into a buffer (no per-pixel GetPixel()).
 *   - packing runs in one tight, auto-vectorised loop over flat buffers.
 *   - workers run on std::thread with an atomic progress counter (no busy poll).
 */
#if defined(_MSC_VER)
#define _CRT_SECURE_NO_WARNINGS
#endif

#include <cstdio>
#include <cstdint>
#include <cstring>
#include <cstdlib>
#include <cctype>
#include <string>
#include <vector>
#include <thread>
#include <atomic>
#include <chrono>
#include <filesystem>
#include "png_decoder.h"

namespace fs = std::filesystem;

enum class ImgType { RGB332, RGB565, RGB565SWAP, RGB888 };

struct Options {
    std::string folderPath;
    ImgType     type         = ImgType::RGB565;   // matches the original executable's default
    bool        ignoreExisting = false;
    bool        showHelp     = false;
};

static const uintmax_t EXPECTED_BIN_SIZE = 131076ULL;   // 256x256x2 + 4 (original hard-coded check)
static const int       MAX_WORKERS       = 16;

struct Task { size_t start; size_t len; int ok = 0; };

/* ---- argument parsing -------------------------------------------------- */

static ImgType parseType(const std::string &v)
{
    std::string s = v;
    for (auto &c : s) c = (char)std::tolower((unsigned char)c);
    if (s == "rgb332")             return ImgType::RGB332;
    if (s == "rgb565")             return ImgType::RGB565;
    if (s == "rgb565swap" || s == "rgb565_swap") return ImgType::RGB565SWAP;
    if (s == "rgb888")             return ImgType::RGB888;
    throw std::runtime_error("Unknown type '" + v + "'. Use RGB565 or RGB565SWAP.");
}

static void printUsage(std::FILE *w)
{
    std::fprintf(w, "lvglConverter - convert PNG map tiles into LVGL binary images (.bin)\n\n");
    std::fprintf(w, "Usage:\n");
    std::fprintf(w, "  lvglConverter <inputFolder> [options]\n\n");
    std::fprintf(w, "Arguments:\n");
    std::fprintf(w, "  <inputFolder>    Folder containing numbered subfolders of PNG map tiles (required).\n\n");
    std::fprintf(w, "Options:\n");
    std::fprintf(w, "  --type <T>       Target pixel format: RGB565 (default) or RGB565SWAP.\n");
    std::fprintf(w, "                   RGB332 / RGB888 are placeholders and not available.\n");
    std::fprintf(w, "  --ignore-existing   Skip tiles whose .bin already exists with the expected size.\n");
    std::fprintf(w, "  -h, --help       Show this help and exit.\n\n");
    std::fprintf(w, "Exit codes: 0 = all converted, 1 = some errors, 2 = usage/argument error.\n");
}

static Options parseArgs(int argc, char **argv)
{
    Options o;
    int positionalCount = 0;
    for (int i = 1; i < argc; ++i) {
        std::string a = argv[i];
        if (a == "-h" || a == "--help") {
            o.showHelp = true;
        } else if (a == "--type") {
            if (i + 1 >= argc) throw std::runtime_error("--type requires a value.");
            o.type = parseType(argv[++i]);
        } else if (a == "--ignore-existing") {
            o.ignoreExisting = true;
        } else if (!a.empty() && a[0] == '-') {
            /* Accept both single and double dash forms for friendliness. */
            std::string inner = a.substr(1);
            if (inner == "h" || inner == "help") { o.showHelp = true; continue; }
            throw std::runtime_error("Unknown option: " + a);
        } else {
            if (positionalCount == 0) { o.folderPath = a; ++positionalCount; }
            else throw std::runtime_error("Unexpected argument: " + a);
        }
    }
    if (!o.showHelp && positionalCount == 0)
        throw std::runtime_error("Missing required <inputFolder> argument.");
    return o;
}

/* ---- tile collection --------------------------------------------------- */

/* A subfolder is traversed only when its name parses as an integer >= 0,
 * matching the original int.TryParse(name) && n >= 0 rule. */
static bool isNumericDir(const std::string &name)
{
    if (name.empty()) return false;
    const char *s = name.c_str();
    char *end = nullptr;
    long long v = std::strtoll(s, &end, 10);
    if (end != s + name.size()) return false;      /* must consume the whole name */
    return v >= 0 && v <= 2147483647LL;
}

static bool hasPngExtension(const std::string &ext)
{
    /* ext includes the leading dot, e.g. ".png"; compare case-insensitively. */
    return ext.size() == 4 &&
           (ext[1] == 'p' || ext[1] == 'P') &&
           (ext[2] == 'n' || ext[2] == 'N') &&
           (ext[3] == 'g' || ext[3] == 'G');
}

static void collectTiles(const fs::path &dir, std::vector<std::string> &out)
{
    std::error_code ec;
    for (fs::directory_iterator it(dir, ec), end; !ec && it != end; it.increment(ec)) {
        const fs::directory_entry &entry = *it;
        fs::file_status st = entry.status(ec);
        if (st.type() == fs::file_type::directory) {
            if (isNumericDir(entry.path().filename().string()))
                collectTiles(entry.path(), out);
        } else if (st.type() == fs::file_type::regular) {
            if (hasPngExtension(entry.path().extension().string()))
                out.push_back(entry.path().string());
        }
    }
}

/* ---- per-tile conversion ---------------------------------------------- */

static bool writeBin(const std::string &outPath, const unsigned char *data, size_t size)
{
    std::FILE *f = std::fopen(outPath.c_str(), "wb");
    if (!f) return false;
    bool ok = std::fwrite(data, 1, size, f) == size && std::fclose(f) == 0;
    if (!ok) std::remove(outPath.c_str());
    return ok;
}

static bool convertOne(const std::string &inPath, const Options &opt)
{
    fs::path p(inPath);
    fs::path outPath = p.parent_path() / (p.filename().stem().string() + ".bin");

    /* Faithful to the original: only skip when ignore-existing is set AND the
     * output exists AND its size equals the hard-coded expected length. */
    if (opt.ignoreExisting) {
        std::error_code ec;
        uintmax_t sz = fs::file_size(outPath, ec);
        if (!ec && sz == EXPECTED_BIN_SIZE) return true;
    }

    int w = 0, h = 0;
    unsigned char *rgba = png_load_rgba(inPath.c_str(), &w, &h);
    if (!rgba) return false;

    const size_t px = (size_t)w * (size_t)h;
    std::vector<unsigned char> buf(4 + px * 2);
    unsigned char *out = buf.data();

    /* Header: little-endian uint32. */
    uint32_t num = ((uint32_t)h << 11) | (uint32_t)w;
    num = (num << 10) | 4u;
    out[0] = (unsigned char)(num & 0xFF);
    out[1] = (unsigned char)((num >> 8) & 0xFF);
    out[2] = (unsigned char)((num >> 16) & 0xFF);
    out[3] = (unsigned char)((num >> 24) & 0xFF);

    const bool swap = (opt.type == ImgType::RGB565SWAP);
    unsigned char *pix = out + 4;
    const unsigned char *src = rgba;
    for (size_t i = 0; i < px; ++i) {
        uint16_t word = ((src[0] >> 3) << 11) | ((src[1] >> 2) << 5) | (src[2] >> 3);
        if (swap) { pix[0] = (unsigned char)(word >> 8); pix[1] = (unsigned char)(word & 0xFF); }
        else      { pix[0] = (unsigned char)(word & 0xFF); pix[1] = (unsigned char)((word >> 8) & 0xFF); }
        pix += 2; src += 4;
    }
    png_free(rgba);

    return writeBin(outPath.string(), out, buf.size());
}

/* ---- orchestration ----------------------------------------------------- */

static int convertTiles(const std::vector<std::string> &files, const Options &opt)
{
    const size_t n = files.size();
    int numWorkers = (int)(n < (size_t)MAX_WORKERS ? n : (size_t)MAX_WORKERS);
    if (numWorkers < 1) numWorkers = 1;

    size_t per = n / (size_t)numWorkers;
    size_t rem = n - per * (size_t)numWorkers;

    std::vector<Task> tasks(numWorkers);
    for (int i = 0; i < numWorkers; ++i) {
        tasks[i].start = i * per;
        tasks[i].len   = per + ((i == numWorkers - 1) ? rem : 0);
    }

    std::atomic<size_t> doneCount{0};

    auto worker = [&](Task &t) {
        for (size_t i = 0; i < t.len; ++i) {
            if (convertOne(files[t.start + i], opt)) t.ok++;
            doneCount.fetch_add(1, std::memory_order_relaxed);
        }
    };

    std::vector<std::thread> ths;
    ths.reserve(numWorkers);
    for (int i = 0; i < numWorkers; ++i) ths.emplace_back(worker, std::ref(tasks[i]));

    /* Live progress line, cleared when done. */
    std::thread monitor([&] {
        while (doneCount.load(std::memory_order_relaxed) < n) {
            std::fprintf(stdout, "\r  Converting ... (%zu/%zu)", doneCount.load(), n);
            std::fflush(stdout);
            std::this_thread::sleep_for(std::chrono::milliseconds(20));
        }
        std::fprintf(stdout, "\n");
    });

    for (auto &t : ths) t.join();
    monitor.join();

    int ok = 0;
    for (const Task &t : tasks) ok += t.ok;
    return ok;
}

/* ---- entry point ------------------------------------------------------- */

int main(int argc, char **argv)
{
    Options opt;
    try {
        opt = parseArgs(argc, argv);
    } catch (const std::exception &ex) {
        std::fprintf(stderr, "Error: %s\n\n", ex.what());
        printUsage(stderr);
        return 2;
    }

    if (opt.showHelp) { printUsage(stdout); return 0; }

    std::error_code ec;
    if (!fs::exists(opt.folderPath, ec) || !fs::is_directory(opt.folderPath, ec)) {
        std::fprintf(stderr, "Error: Folder not found: %s\n", opt.folderPath.c_str());
        return 2;
    }

    std::vector<std::string> files;
    collectTiles(opt.folderPath, files);
    if (files.empty()) {
        std::fprintf(stderr, "Error: No PNG map tiles found under '%s'.\n", opt.folderPath.c_str());
        return 1;
    }

    if (opt.type == ImgType::RGB332 || opt.type == ImgType::RGB888) {
        std::fprintf(stderr, "Error: Target type '%s' is not available.\n",
                     opt.type == ImgType::RGB332 ? "RGB332" : "RGB888");
        return 2;
    }

    std::printf("LVGL map-tile converter\n");
    std::printf("  Folder      : %s\n", opt.folderPath.c_str());
    std::printf("  Type        : %s\n", opt.type == ImgType::RGB565SWAP ? "RGB565SWAP" : "RGB565");
    std::printf("  Ignore exist: %s\n", opt.ignoreExisting ? "yes" : "no");
    std::printf("  Tiles found : %zu\n\n", files.size());

    int ok = convertTiles(files, opt);

    std::printf("\n");
    if (ok == (int)files.size()) {
        std::printf("Conversion completed. All %zu tiles converted.\n", files.size());
        return 0;
    }
    std::printf("Conversion completed. %d OK, %zu error(s).\n", ok, files.size() - (size_t)ok);
    return 1;
}