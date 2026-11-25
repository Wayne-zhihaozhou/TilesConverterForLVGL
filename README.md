# lvglConverter — C/C++ 版

把原 WPF/CLI 程序 **TilesConverterForLVGL.exe**（逆向自 .NET 程序集 lvglConverter）重写成 **C/C++**，用于把 PNG 地图瓦片转换成 LVGL 嵌入式显示用的二进制图像 (.bin)。

> 原版是 C# (.NET Framework 4.7.2)，用 System.Drawing.Bitmap.GetPixel() 逐像素读取 + List<> 分配，处理速度慢。本版用直接读缓冲区的 PNG 解码 + 紧凑打包循环 + 多线程，速度显著提升（见「性能」节）。
> 原 C# 源码已全面移除（仅保留 analysis/ 逆向分析笔记）。

## 目录结构

    src/
      lvgl_converter.cpp   主程序：参数解析、递归取瓦片、RGB565 打包、多线程、输出
      png_decoder.c/.h     PNG 解码封装（内联 stb_image，无需外部 zlib）
      stb_image.h          单文件公共域 PNG 解码库（已随项目附带）
      stb_image_write.h    单文件公共域图像写入库（仅测试用）
    build.bat              MSVC 构建脚本 (Visual Studio 2022 BuildTools)
    Makefile               gcc/clang 便携构建 (POSIX / Cygwin / MSYS)

## 编译

### Windows (MSVC，推荐)

需要安装「C++ desktop development」工作负载（含 cl.exe）。

    build.bat          # -> bin\Release\lvglConverter.exe
    build.bat clean    # 清理构建产物

### Linux / macOS / 有 gcc 或 clang 的环境

    make               # -> bin/Release/lvglConverter
    make clean

## 用法

    lvglConverter <inputFolder> [options]

    参数:
      <inputFolder>    必填。包含「编号子文件夹」的根目录，里面存放 PNG 地图瓦片。
    选项:
      --type <T>       目标像素格式：RGB565SWAP（默认）或 RGB565。RGB332 / RGB888 为占位格式，不可用。
      --ignore-existing   若某瓦片的 .bin 已存在且大小为规定长度(131076 字节)，则跳过不重写。
      -h, --help       打印帮助并退出。

    退出码: 0 = 全部成功, 1 = 部分失败, 2 = 用法/参数错误。

示例：

    bin\Release\lvglConverter.exe "C:\tiles" --type RGB565SWAP --ignore-existing
## 输入目录规则

递归收集 *.png，但只进入名称能解析为 **≥0 整数** 的子文件夹。因此 `0`、`1`、`42` 有效；
`number`、`layer`、`-1`(负数) 无效。每个被转换的 .png 在同目录生成同名 .bin。

## .bin 格式（每片）

    [ uint32 小端头信息 ][ RGB565 像素数据 ]

* 头信息：
num = (((height << 11) | width) << 10) | 4，小端写入 4 字节。
* 每个像素为 5-6-5 打包的 ushort：r5=R>>3, g6=G>>2, b5=B>>3，word=(r5<<11)|(g6<<5)|b5。
  * RGB565：字节小端（低字节在前）。
  * RGB565SWAP：字节大端（高字节在前）。

> --ignore-existing 中「规定长度」131076 = 256*256*2 + 4，是原程序写死的判定值；仅当已有 .bin 恰好为此尺寸时才跳过。

## 性能

在同样数据集（12 个缩放级别 × 30 片 = **360 片**，每片 256×256，约 3300 万像素）、--type RGB565SWAP：

| 版本 | 耗时 |
| --- | --- |
| 原版 C# (.NET) | ~0.59 s |
| **本版 C/C++** | **~0.07 s** |

约为原版的 **8× 以上**。主要提升来源：1) stb_image 直接解码到缓冲区，取代逐像素 GetPixel()；2) 打包在单一紧凑、可自动向量化循环中，使用扁平缓冲区，避免 List<> 分配；3) std::thread + 原子进度计数，取代轮询休眠。

## 正确性验证

已对已知颜色瓦片做逐字节比对：RGB565 与 RGB565SWAP 输出均与原版算法逐像素一致；编号子文件夹递归规则、--ignore-existing 跳过、以及全部退出码 (0/1/2) 均已核对无误。

## CLI 与原版的一致性

C++ 版在 CLI 上与原版**基本一致**：各选项、退出码与目录规则均与原程序相同，仅 --help 文本中的默认格式说明随之更新。**唯一行为差异**：默认像素格式由原版的 RGB565 改为 **RGB565SWAP**（本次需求变更），即不带 --type 时输出大端字节序。此外额外支持两个可选写法（不改变原有行为）：

* -help：等价于 --help（原版只接受 -h / --help）。
* --type rgb565_swap：RGB565SWAP 的下划线写法，等价于 gb565swap。

## 说明 / 注意

* 逻辑（头信息打包、RGB565 字节序、并行编排、忽略已存在文件的判定）与原版完全一致。
* 默认 --type 为 RGB565SWAP（按需求从原版的 RGB565 变更；故中文说明中「默认 RGB565SWAP」现正确）。
* RGB332 / RGB888 仍返回不可用（退出码 2），与原版保持一致。
