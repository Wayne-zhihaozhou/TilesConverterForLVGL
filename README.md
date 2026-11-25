# lvglConverter — PNG 地图瓦片 → LVGL 二进制图像转换器

`lvglConverter` 是一个跨平台友好的命令行工具，把 **PNG 地图瓦片（map tiles）**
转换成嵌入式 GUI 框架 [LVGL](https://lvgl.io/) 可直接加载的二进制图像文件（`.bin`）。

它是逆向还原自 `TilesConverterForLVGL` 工具的纯 C/C++ 重写版本：不依赖任何 GUI、
不依赖 .NET，仅用标准 C++17 + 一个内联的公共领域 PNG 解码器即可编译运行。

---

## 目录

- [它做什么](#它做什么)
- [特性](#特性)
- [环境要求](#环境要求)
- [编译](#编译)
- [快速开始](#快速开始)
- [完整用法](#完整用法)
- [输入目录结构规则](#输入目录结构规则)
- [输出文件格式（重要）](#输出文件格式重要)
- [退出码](#退出码)
- [示例](#示例)
- [增量 / 断点续跑](#增量--断点续跑)
- [常见问题与故障排查](#常见问题与故障排查)
- [项目结构](#项目结构)
- [许可证与第三方声明](#许可证与第三方声明)
- [与原版工具的一致性](#与原版工具的一致性)

---

## 它做什么

给定一个包含「编号子文件夹」的根目录，工具会：

1. **递归**扫描所有「名称为 ≥0 整数」的子文件夹；
2. 收集其中的 `.png` 瓦片；
3. 把每个 PNG 解码成 RGBA，打包成 LVGL 需要的 RGB565 二进制数据；
4. 在 **PNG 同目录**下生成一个同名 `.bin`。

输出文件结构为：

```
[ uint32 小端头信息 ][ RGB565 像素数据... ]
```

> 典型用途：把设计好的地图/图标切片（PNG）批量转换成 LVGL `lv_image_set_data()` 能直接
> 使用的二进制资源，省去在代码里手写图像数组。

---

## 特性

- **零运行时依赖**：只链接 C/C++ 标准库与一个内联的公共领域 PNG 解码器（stb_image）。
- **高性能**：
  - 用 stb_image 一次性把 PNG 解码进缓冲（无逐像素 `GetPixel`）；
  - RGB565 打包为单一紧凑、可自动向量化循环；
  - 多线程并行处理（`std::thread` + 原子进度计数器，非轮询）。
- **参数灵活**：参数顺序任意；支持 `-h`/`--help`、`-help` 等写法。
- **可脚本化**：通过退出码（0 / 1 / 2）让批处理或 CI 判断结果。

---

## 环境要求

| 项目 | 要求 |
| --- | --- |
| 操作系统 | Windows（官方用 MSVC 构建；代码为标准 C++17，其他编译器亦可） |
| 编译器 | Microsoft Visual Studio **2022 BuildTools**（需勾选「C++ 桌面开发」工作负载，含 `cl.exe`） |
| 架构 | x64 |
| 依赖 | 无外部 NuGet/包；`src/stb_image.h`、`src/stb_image_write.h` 已随仓库附带（公共领域） |

> 若没有 VS BuildTools，可改用 MinGW-w64 (`g++ -std=c++17`) 或 CMake + 任意 C++17 编译器。
> `build.bat` 仅针对 MSVC；换编译器时按下方「项目结构」自行编译两个源文件即可。

---

## 编译

仓库自带一键构建脚本 `build.bat`：

```bat
REM 首次编译 -> 生成 bin\Release\lvglConverter.exe
build.bat

REM 清理构建产物（可选）
build.bat clean
```

- 成功时输出 `Built: <ROOT>\bin\Release\lvglConverter.exe`，退出码 `0`。
- 若提示 `VsDevCmd.bat not found`，说明未安装「C++ 桌面开发」工作负载或 VS 路径不同，
  请重新运行 Visual Studio Installer 补装该 workload（见[故障排查](#故障排查)）。

编译产物：

| 文件 | 说明 |
| --- | --- |
| `bin\Release\lvglConverter.exe` | 最终可执行文件 |
| `build\*.obj` | 中间目标文件（`.gitignore` 已忽略） |

---

## 快速开始

```bat
REM 1) 编译（首次）
build.bat

REM 2) 转换（默认格式 RGB565SWAP）
bin\Release\lvglConverter.exe "C:\path\to\tiles"

REM 3) 指定格式 + 跳过已存在的正确输出
bin\Release\lvglConverter.exe "C:\path\to\tiles" --type RGB565SWAP --ignore-existing
```

运行后会在控制台打印：扫描结果、转换进度（`Converting ... (k/n)`）与最终统计。

---

## 完整用法

```
lvglConverter <inputFolder> [options]
```

### 参数

| 参数 | 是否必填 | 说明 |
| --- | --- | --- |
| `<inputFolder>` | **是** | 包含「编号子文件夹」的根目录，内部存放 PNG 地图瓦片。 |

### 选项

| 选项 | 说明 |
| --- | --- |
| `--type <T>` | 目标像素格式：`RGB565SWAP`（默认）或 `RGB565`。`RGB332` / `RGB888` 为占位格式，不可用。 |
| `--ignore-existing` | 若某瓦片的 `.bin` 已存在且大小恰为规定长度（131076 字节），则跳过不重写。 |
| `-h`, `--help` | 打印帮助并退出（退出码 0）。 |

**参数灵活性说明：**

- 参数顺序任意，例如 `--type RGB565SWAP "C:\tiles"` 与先给目录等价。
- `-help` 等价于 `--help`（单/双横线都接受）。
- `--type rgb565_swap` 是 `RGB565SWAP` 的下划线写法，等价于 `rgb565swap` / `RGB565SWAP`。

> 大小写不敏感：`--type rgb565swap`、`RGB565Swap` 均可识别。

---

## 输入目录结构规则（重要）

工具会**递归**收集 PNG，但**只处理位于「数字命名子文件夹」下的瓦片**。

```
tiles\
├── 0\            ← 数字文件夹，会被扫描
│   ├── 0.png
│   └── 1.png
├── 1\            ← 数字文件夹，会被扫描
│   └── 0.png
└── number\       ← 非数字文件夹，被忽略
    └── 0.png     ← 不会被转换（不生成 .bin）
```

**判定规则（精确）：** 子文件夹名称必须能被完整解析为 **`[0, 2147483647]` 范围内的整数**
（即 `strtoll` 消耗整个字符串且结果非负、不溢出）。因此：

- ✅ 有效：`0`、`1`、`42`
- ❌ 无效：`number`、`layer`、`-1`（负数）、`v1`、`0x10`、空名等

非数字文件夹及其内容会被**完全跳过**，不会生成任何 `.bin`。

---

## 输出文件格式（重要）

每个被转换的 PNG 会在 **同目录**下生成同名 `.bin`，结构为：

```
偏移 0   : uint32 little-endian 头信息
偏移 4   : RGB565 像素数据，每像素 2 字节，共 width*height 个
总大小   : 4 + width*height*2 字节
```

### 头信息（uint32，小端）

```
num = (((height << 11) | width) << 10) | 4      // 写入时取 little-endian
```

- 低 3 位固定为 `100`（即常量 `4`）。
- `width` 占 11 位，`height` 占 11 位。

**解码公式**（从已有 `.bin` 反推尺寸）：

```python
num = int.from_bytes(data[:4], "little")
flag = num & 0x7            # 应为 4
M    = num >> 10
w    = M & 0x7FF            # width
h    = (M >> 11) & 0x7FF    # height
```

### 像素打包（RGB565）

每个像素按 R5G6B5 压缩：

```
r5 = R >> 3      # 5 bit
g6 = G >> 2      # 6 bit
b5 = B >> 3      # 5 bit
word = (r5 << 11) | (g6 << 5) | b5        # 0 .. 0xFFFF
```

字节序由 `--type` 决定：

| `--type` | 写入方式 | 说明 |
| --- | --- | --- |
| `RGB565`（默认原始值） | **小端**：先写低字节 `(word & 0xFF)`，再写高字节 `(word>>8)&0xFF` | 与原版一致 |
| `RGB565SWAP`（本工具默认） | **大端**：先写高字节 `(word>>8)&0xFF`，再写低字节 `word&0xFF` | LVGL 常见端序 |

> 示例：纯白像素 (255,255,255) → `word = 0xFFFF` → RGB565SWAP 写入 `[0xFF, 0xFF]`。
> 示例：纯红像素 (255,0,0) → `word = 0xF800` → RGB565 小端写入 `[0x00, 0xF8]`，
> RGB565SWAP 大端写入 `[0xF8, 0x00]`。

### 参考实现（Python，可用于校验互操作性）

```python
import struct

def encode(w, h, rgba_bytes, swap=False):
    num = (((h << 11) | w) << 10) | 4
    out = bytearray(struct.pack("<I", num))
    for i in range(0, len(rgba_bytes), 4):
        R, G, B = rgba_bytes[i], rgba_bytes[i+1], rgba_bytes[i+2]
        word = ((R >> 3) << 11) | ((G >> 2) << 5) | (B >> 3)
        hi, lo = (word >> 8) & 0xFF, word & 0xFF
        out += bytes([hi, lo]) if swap else bytes([lo, hi])
    return bytes(out)
```

---

## 退出码

| 退出码 | 含义 |
| --- | --- |
| `0` | 所有瓦片全部转换成功。 |
| `1` | 部分失败（控制台会列出失败/未处理的文件；或未发现任何 PNG 瓦片）。 |
| `2` | 用法或参数错误（缺参数、未知选项、不可用格式、目录不存在等）。 |

在脚本中判断结果：

```bat
bin\Release\lvglConverter.exe "C:\tiles"
if %EXITCODE% equ 0 ( echo OK ) else ( echo FAIL code=%EXITCODE% )
```

```powershell
& bin\Release\lvglConverter.exe "C:\tiles"
if ($LASTEXITCODE -eq 0) { "OK" } else { "FAIL $LASTEXITCODE" }
```

---

## 示例

```bat
REM 基本转换（默认 RGB565SWAP）
lvglConverter "D:\map\tiles"

RGB565SWAP（大端）
lvglConverter "D:\map\tiles" --type RGB565SWAP

RGB565（小端，与原版一致）
lvglConverter "D:\map\tiles" --type RGB565

跳过已生成且大小正确的 .bin（增量 / 断点续跑）
lvglConverter "D:\map\tiles" --ignore-existing

组合用法
lvglConverter "D:\map\tiles" --type RGB565SWAP --ignore-existing

查看帮助
lvglConverter --help
```

---

## 增量 / 断点续跑

`--ignore-existing` 配合「规定大小 131076 字节」的校验，可实现：

- **增量构建**：只重写缺失或尺寸不对的输出。
- **断点续跑**：上次中断后再次运行，不会覆盖已正确生成的文件。

> 注意：该「规定大小」源自原始工具对 256×256 瓦片的硬编码检查
> （`4 + 256*256*2 = 131076`）。它**不**按实际图片尺寸动态判断——仅用于快速跳过。

---

## 常见问题与故障排查

**Q: 运行报 `VsDevCmd.bat not found` / 编译失败？**
A: 未安装「C++ 桌面开发」工作负载，或 VS 安装在非默认路径。请重新运行
Visual Studio Installer → 修改 → 勾选 **C++ 桌面开发 (C++ desktop development)**；
若装在别处，把 `build.bat` 中 VsDevCmd.bat 的路径改成实际位置。

**Q: 我的子文件夹没被转换？**
A: 检查文件夹名是否为纯整数（如 `0`、`1`）。`number`、`layer`、`-1` 会被忽略。

**Q: `.bin` 大小不对 / LVGL 显示异常？**
A: 确认 `--type` 的端序与 LVGL 期望一致；用上方「参考实现」核对头信息与像素字节。
可用 `xxd`/十六进制查看器检查前 4 字节是否为小端头信息。

**Q: 想用其他编译器（非 MSVC）？**
A: 代码为标准 C++17，仅依赖 `<filesystem>`、`<thread>`。用 g++/clang：
```
g++ -std=c++17 -O2 src/png_decoder.c src/lvgl_converter.cpp -o lvglConverter
```

**Q: 处理大量瓦片时内存/线程数？**
A: 最多启动 16 个工作线程（`MAX_WORKERS`），按文件数量自动折半；每个任务按需分配缓冲，
无全局大缓存。

---

## 项目结构

```
TilesConverterForLVGL-Reverse/
├── src/
│   ├── lvgl_converter.cpp   # 主程序：参数解析、递归收集、多线程转换
│   ├── png_decoder.c        # stb_image 内联封装（RGBA 解码）
│   ├── png_decoder.h        # 解码接口声明
│   ├── stb_image.h          # 公共领域 PNG/JPEG 解码器 (v2.34, Sean Barrett)
│   └── stb_image_write.h    # 公共领域写入器 (Sean Barrett，随附)
├── build.bat                # MSVC 一键构建（支持 `clean`）
├── LICENSE                  # MIT
├── NOTICE.md                # 第三方（stb）归属声明
└── README.md
```

---

## 许可证与第三方声明

- **本项目源码**：采用 **MIT License**（见 `LICENSE`），可自由使用、修改、分发。
- **stb_image / stb_image_write**：Sean Barrett 的公共领域软件，随仓库附带，
  遵循其公共领域/Unlicense 条款（详见 `NOTICE.md`）。

发布前请将 `LICENSE` 中的版权持有者替换为你的真实姓名或仓库名称。

---

## 与原版工具的一致性

- 输出头信息公式、RGB565 打包算法、编号子文件夹递归规则均与原
  `TilesConverterForLVGL` 保持一致。
- 默认像素格式本工具为 `RGB565SWAP`（大端），原始工具为小端 `RGB565`；
  用 `--type RGB565` 可获得与原版逐字节一致的输出。
