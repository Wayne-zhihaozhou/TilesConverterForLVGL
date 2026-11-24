# lvglConverter — Reverse-Engineered Source

Reconstructed C# source of the Windows tool **`TilesConverterForLVGL.exe`**
(alassembly name `lvglConverter`), a WPF utility that converts PNG map tiles into
the binary image format consumed by **[LVGL](https://lvgl.io/)** embedded displays.

> Original project path (from embedded PDB string):
> `C:\Users\empwl\source\repos\mapConverter\lvglConverter` · Copyright © 2021.

---

## What the tool does

Given a folder tree of numbered subfolders holding PNG map tiles, it writes one
`.bin` file per tile in an LVGL-friendly packed format:

* Recursively collects `*.png` files under numeric directories (`GetTileFiles`).
* Rasterises each tile with `System.Drawing.Bitmap.GetPixel`.
* Packs pixels into **RGB565** (or RGB565-SWAP) and prepends a 4-byte header.
* Runs the work on up to 16 background threads with a progress UI.

### Binary container format (per tile)

```
[ uint32 LE header ][ pixel data ]
```

Header packs height/width plus flags into one little-endian `uint`:

```
num = height;
num <<= 11;
num |= width;
num <<= 2 + 3 + 5;   // == <<= 10
num |= 4;            // flag bit
// written bytes 0..3 (LE)
```

Each pixel is 5-6-5 packed into one `ushort`:

```
r5 = R >> 3;  g6 = G >> 2;  b5 = B >> 3;
word = (r5 << 11) | (g6 << 5) | b5;
RGB565      : word written little-endian
RGB565SWAP  : word written big-endian   (high byte first)
```

`RGB332` / `RGB888` are exposed in the UI enum but return `null` (not implemented).

---

## Reverse-engineering process

1. **File type.** `TilesConverterForLVGL.exe` is a 27,648-byte PE32
   (`Machine = i386`, `OptionalHeader Magic = 0x10B`). It carries the CLR entry
   symbol `_CorExeMain` and .NET metadata streams (`#Strings/#Blob/#GUID/#US`),
   so it is a **managed** assembly, not native code.
2. **Anomaly.** The PE is deliberately non-standard: `SizeOfOptionalHeader = 0`,
   odd section VirtualAddresses/RawSizes (`.rsrc` declares 0x8000 bytes in a
   0x6C00-byte file), and it has **no** `.resources` (`MGRZ`) blob, no standalone
   `BamlStream`, and no `0xC0DE` BAML signature. Loading it with
   `Assembly.LoadFrom` throws `BadImageFormatException`. This points to a custom /
   obfuscated build rather than a normal Visual Studio output.
3. **Decompilation.** `ilspycmd` (ILSpy 11.2) decompiled the IL cleanly despite the
   malformed PE, yielding full method bodies for `App`, `MainWindow`, and
   `FileHandler`.
4. **Resources / XAML.** UI markup (`MainWindow.xaml`) is referenced via
   `/lvglConverter;component/mainwindow.xaml` but its BAML was not cleanly
   extractable from the abnormal resource layout, so the XAML below is
   **reconstructed** from the decompiled code-behind control set and event wiring.

### Tools used

* `ilspycmd` 11.2 (headless ILSpy) — IL → C#.
* Custom PE/resource scanner (`C:\0_Code\_retools`, `System.Reflection` + manual
  header parsing) — to characterise the binary and hunt for BAML/.resources.

---

## Recovered project layout

```
TilesConverterForLVGL-Reverse/
├── lvglConverter.csproj        # net472 WPF Exe (matches original target)
├── src/
│   ├── App.xaml.cs             # entry point (faithful: explicit static Main)
│   ├── MainWindow.xaml         # reconstructed UI (x:Names match code-behind)
│   ├── MainWindow.xaml.cs      # recovered code-behind
│   ├── FileHandler.cs          # per-thread tile -> .bin conversion
│   ├── LVGLImageType.cs        # RGB332/RGB565/RGB565SWAP/RGB888 enum
│   └── Properties/
│       ├── Resources.Designer.cs
│       └── Settings.Designer.cs
└── analysis/
    └── decompiled-sources/
        └── TilesConverterForLVGL.ilspycmd.cs   # raw ilspycmd dump (reference)
```

## Build & run

```bash
dotnet build -c Release
# -> bin\Release\net472\lvglConverter.exe
```

Requires the .NET Framework 4.7.2 reference packs (auto-downloaded on first build).

---

## Fidelity notes / caveats

* **Logic:** `FileHandler` pixel/header packing and `MainWindow` orchestration are
  reproduced faithfully; rebuilt binary reproduces the original's user strings.
* **Interpolated strings:** ilspycmd un-interpolates `$"..."` into concatenations;
  these have been restored to interpolated form to match the likely original source
  (verified against composite strings still present in the original resource blob).
* **XAML:** reconstructed from the recovered control set + wiring, not byte-extracted.
  Exact pixel layout / button labels may differ from the original visual.
* **Assembly metadata** (Company/Product/Description) is omitted — it was cosmetic.
* `RGB332`/`RGB888` intentionally return `null`, matching original behaviour.
