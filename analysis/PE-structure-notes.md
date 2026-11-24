# Binary structure notes — TilesConverterForLVGL.exe

Raw analysis of the input binary (27,648 bytes / 0x6C00).

## Header
- DOS/PE: valid MZ + `PE\0\0`.
- Machine = `0x014C` (i386), PE32 (`OptionalHeader Magic = 0x010B`).
- NumberOfSections = 3, `SizeOfOptionalHeader = 0` (non-standard; omits the usual
  standard/dynamic optional-header fields).
- CLR entry symbol present: `_CorExeMain` → managed assembly.

## Sections (VirtualAddress / PointerToRawData / SizeOfRawData)
- `.text`   : rva 0x0074 / raw 0x4E00 / size 0x2000  — IL + metadata
- `.rsrc`   : rva 0x0063 / raw 0x1A00 / size 0x8000  — resource/metadata region
- `.reloc`  : rva 0x636F / raw 0x0200 / size 0xA000  — base relocations

## Anomalies (why normal loading fails)
- `.rsrc` declares 0x8000 raw bytes but the file is only 0x6C00 → section table
  sizes exceed file length (compression/custom-linker signature).
- No Windows `.resources` (`MGRZ`) blob, no `BamlStream`, no `0xC0DE` BAML magic
  found anywhere in the file. XAML/BAML is therefore not recoverable by standard
  resource extraction; it was likely inlined/stored by a custom build step.
- `Assembly.LoadFrom` → `BadImageFormatException`; `ilspycmd` still decompiles IL.

## Recovered managed types (from metadata + IL)
- `lvglConverter.App` : WPF Application, explicit `Main()`.
- `lvglConverter.MainWindow : Window` : UI + conversion orchestration.
- `lvglConverter.FileHandler` : per-thread PNG -> LVGL .bin converter.
- `lvglConverter.LVGLImageType` : RGB332 | RGB565 | RGB565SWAP | RGB888.
- `lvglConverter.Properties.Resources` / `.Settings` : empty auto-gen wrappers.

## Embedded strings of interest
- `"#Map Tiles Converter V1.0 (For LVGL)"` (assembly comment)
- PDB path: `C:\Users\empwl\source\repos\mapConverter\lvglConverter\obj\Release\...pdb`
- UI literals: `Scanning ...`, `Empty folder.`, `File count : {0}`,
  `Converting ... ({0}/{1})`, `Conversion Completed. {0} OK, {1} Error(s)`,
  `These files will be deleted :`, `Target type is not available now.`
