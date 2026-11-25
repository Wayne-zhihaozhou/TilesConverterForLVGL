using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace lvglConverter
{
    /// <summary>
    /// Console entry point for the LVGL map-tile converter.
    /// Replaces the original WPF UI: parses command-line arguments, collects PNG tiles
    /// from a folder tree of numbered subfolders, and converts each tile to an LVGL .bin.
    /// Conversion logic is delegated to FileHandler (unchanged from the recovered source).
    /// </summary>
    public static class Program
    {
        // Partition count mirrors the original UI (up to 16 worker threads).
        private const int MaxThreads = 16;

        public static int Main(string[] args)
        {
            Options options;
            try
            {
                options = ParseArgs(args);
            }
            catch (CliException ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                Console.Error.WriteLine();
                PrintUsage(Console.Error);
                return 2;
            }

            if (options.ShowHelp)
            {
                PrintUsage(Console.Out);
                return 0;
            }

            // Validate input folder.
            if (!Directory.Exists(options.FolderPath))
            {
                Console.Error.WriteLine($"Error: Folder not found: {options.FolderPath}");
                return 2;
            }

            // Collect tiles (same recursive rule as the original GetTileFiles).
            List<string> tileFiles = new List<string>();
            GetTileFiles(options.FolderPath, tileFiles);
            if (tileFiles.Count == 0)
            {
                Console.Error.WriteLine($"Error: No PNG map tiles found under '{options.FolderPath}'.");
                return 1;
            }

            // Reject formats the original left unimplemented.
            if (options.Type == LVGLImageType.RGB332 || options.Type == LVGLImageType.RGB888)
            {
                Console.Error.WriteLine($"Error: Target type '{options.Type}' is not available.");
                return 2;
            }

            Console.WriteLine($"LVGL map-tile converter");
            Console.WriteLine($"  Folder      : {options.FolderPath}");
            Console.WriteLine($"  Type        : {options.Type}");
            Console.WriteLine($"  Ignore exist: {(options.IgnoreExisting ? "yes" : "no")}");
            Console.WriteLine($"  Tiles found : {tileFiles.Count}");
            Console.WriteLine();

            int ok = ConvertTiles(tileFiles, options);

            // Report.
            Console.WriteLine();
            if (ok == tileFiles.Count)
            {
                Console.WriteLine($"Conversion completed. All {tileFiles.Count} tiles converted.");
                return 0;
            }
            Console.WriteLine($"Conversion completed. {ok} OK, {tileFiles.Count - ok} error(s).");
            return 1;
        }

        // Convert the tile list using up to MaxThreads workers (one FileHandler each),
        // matching the original's parallel orchestration but reporting via the console.
        private static int ConvertTiles(List<string> tileFiles, Options options)
        {
            int num = Math.Min(MaxThreads, tileFiles.Count);
            int per = tileFiles.Count / num;
            int remainder = tileFiles.Count - per * num;

            List<FileHandler> handlers = new List<FileHandler>();
            for (int i = 0; i < num; i++)
            {
                int start = i * per;
                int length = per + ((i == num - 1) ? remainder : 0);
                FileHandler handler = new FileHandler(tileFiles, start, length, options.Type, options.IgnoreExisting);
                handlers.Add(handler);
                new Thread(handler.Start).Start();
            }

            while (true)
            {
                bool allDone = true;
                int handled = 0;
                foreach (FileHandler h in handlers)
                {
                    handled += h.HandledCount;
                    allDone &= h.IsCompleted;
                }
                Console.Write($"\r  Converting ... ({handled}/{tileFiles.Count})");
                if (allDone) break;
                Thread.Sleep(20);
            }
            Console.WriteLine(); // clear progress line
            int ok = 0;
            foreach (FileHandler h in handlers) ok += h.HandledCount;
            return ok;
        }

        // Recursive PNG collection under numeric directories (faithful to original).
        private static void GetTileFiles(string folderPath, List<string> path)
        {
            foreach (string file in Directory.GetFiles(folderPath))
            {
                FileInfo info = new FileInfo(file);
                if (info.Extension.ToLower().IndexOf("png") > 0)
                    path.Add(file);
            }
            foreach (string dir in Directory.GetDirectories(folderPath))
            {
                int n = -1;
                if (int.TryParse(new DirectoryInfo(dir).Name, out n) && n >= 0)
                    GetTileFiles(dir, path);
            }
        }

        private static void PrintUsage(TextWriter w)
        {
            w.WriteLine("lvglConverter - convert PNG map tiles into LVGL binary images (.bin)");
            w.WriteLine();
            w.WriteLine("Usage:");
            w.WriteLine("  lvglConverter <inputFolder> [options]");
            w.WriteLine();
            w.WriteLine("Arguments:");
            w.WriteLine("  <inputFolder>    Folder containing numbered subfolders of PNG map tiles (required).");
            w.WriteLine();
            w.WriteLine("Options:");
            w.WriteLine("  --type <T>       Target pixel format: RGB565 (default) or RGB565SWAP.");
            w.WriteLine("                   RGB332 / RGB888 are placeholders and not available.");
            w.WriteLine("  --ignore-existing   Skip tiles whose .bin already exists with the expected size.");
            w.WriteLine("  -h, --help       Show this help and exit.");
            w.WriteLine();
            w.WriteLine("Exit codes: 0 = all converted, 1 = some errors, 2 = usage/argument error.");
        }

        private static Options ParseArgs(string[] args)
        {
            Options o = new Options { Type = LVGLImageType.RGB565 };
            int positionalCount = 0;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                switch (a)
                {
                    case "-h":
                    case "--help":
                        o.ShowHelp = true;
                        break;
                    case "--type":
                        if (i + 1 >= args.Length) throw new CliException("--type requires a value.");
                        o.Type = ParseType(args[++i]);
                        break;
                    case "--ignore-existing":
                        o.IgnoreExisting = true;
                        break;
                    default:
                        if (a.StartsWith("--")) throw new CliException($"Unknown option: {a}");
                        if (positionalCount == 0)
                        {
                            o.FolderPath = a;
                            positionalCount++;
                        }
                        else
                        {
                            throw new CliException($"Unexpected argument: {a}");
                        }
                        break;
                }
            }
            if (!o.ShowHelp && positionalCount == 0)
                throw new CliException("Missing required <inputFolder> argument.");
            return o;
        }

        private static LVGLImageType ParseType(string value)
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "rgb332": return LVGLImageType.RGB332;
                case "rgb565": return LVGLImageType.RGB565;
                case "rgb565swap": return LVGLImageType.RGB565SWAP;
                case "rgb888": return LVGLImageType.RGB888;
                default: throw new CliException($"Unknown type '{value}'. Use RGB565 or RGB565SWAP.");
            }
        }

        private sealed class Options
        {
            public string FolderPath;
            public LVGLImageType Type = LVGLImageType.RGB565;
            public bool IgnoreExisting;
            public bool ShowHelp;
        }

        private sealed class CliException : Exception
        {
            public CliException(string message) : base(message) { }
        }
    }
}
