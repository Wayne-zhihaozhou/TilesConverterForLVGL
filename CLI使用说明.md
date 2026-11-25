# lvglConverter CLI 使用说明

`lvglConverter` 是一个命令行工具,把 **PNG 地图瓦片**转换成 LVGL 嵌入式显示使用的
二进制图像文件(`.bin`)。它是原 WPF 程序 `TilesConverterForLVGL.exe` 的命令行版本,
转换逻辑与原程序完全一致。

---

## 1. 快速开始

```bash
# 编译(首次)
dotnet build -c Release

# 使用(默认 RGB565SWAP)
bin\Release\net472\lvglConverter.exe "C:\path\to\tiles"

# 指定格式 + 跳过已存在文件
bin\Release\net472\lvglConverter.exe "C:\path\to\tiles" --type RGB565SWAP --ignore-existing
```

运行后会在控制台打印扫描结果、转换进度和最终统计。

---

## 2. 用法

```
lvglConverter <inputFolder> [options]
```

### 参数

| 参数 | 说明 |
| --- | --- |
| `<inputFolder>` | **必填**。包含「编号子文件夹」的根目录,里面存放 PNG 地图瓦片。 |

### 选项

| 选项 | 说明 |
| --- | --- |
| `--type <T>` | 目标像素格式:`RGB565SWAP`(默认)或 `RGB565`。`RGB332` / `RGB888` 为占位格式,不可用。 |
| `--ignore-existing` | 若某瓦片的 `.bin` 已存在且大小为规定长度(131076 字节),则跳过不重写。 |
| `-h`, `--help` | 打印帮助并退出。 |

> 参数顺序任意,例如 `--type RGB565SWAP "C:\tiles"` 也可以。

---

## 3. 输入目录结构(重要)

工具会**递归**收集 PNG,但只处理位于「数字命名子文件夹」下的瓦片:

```
tiles\
├── 0\            ← 数字文件夹,会被扫描
│   ├── 0.png
│   └── 1.png
├── 1\            ← 数字文件夹,会被扫描
│   └── 0.png
└── number\       ← 非数字文件夹,被忽略
    └── 0.png     ← 不会被转换
```

判定规则:子文件夹名称能解析为 **≥0 的整数** 才进入递归。因此 `0`、`1`、`42` 有效,
`number`、`layer`、`-1`(负数)无效。

---

## 4. 输出

* 每个被转换的 `.png` 会在**同目录**下生成一个同名 `.bin`。
* `.bin` 结构:`[ uint32 小端头信息 ][ RGB565 像素数据 ]`。
  * 头信息:把高×宽及标志位打包进一个 little-endian `uint`(含固定标志位)。
  * 每个像素为一个 `ushort` 的 5-6-5 打包值。
* `RGB565`:像素字节按小端写入;`RGB565SWAP`:按大端写入(高字节在前)。

---

## 5. 退出码

| 退出码 | 含义 |
| --- | --- |
| `0` | 所有瓦片全部转换成功。 |
| `1` | 部分失败(控制台会列出失败/未处理的文件)。 |
| `2` | 用法或参数错误(缺参数、未知选项、不可用格式等)。 |

在脚本中可用 `%EXITCODE%`(批处理)或 `$LASTEXITCODE`(PowerShell)判断结果。

---

## 6. 示例

```bash
# 基本转换(默认 RGB565SWAP)
lvglConverter "D:\map\tiles"

# RGB565 交换字节序
lvglConverter "D:\map\tiles" --type RGB565SWAP

# 跳过已生成且大小正确的 .bin(增量/断点续跑)
lvglConverter "D:\map\tiles" --ignore-existing

# 组合用法
lvglConverter "D:\map\tiles" --type RGB565SWAP --ignore-existing
```

---

## 7. 常见问题

* **提示「No PNG map tiles found」**:根目录下没有数字命名的子文件夹,或其中没有 `.png`。请检查目录结构(见第 3 节)。
* **某些文件失败/未处理**:控制台会在结尾列出这些文件路径。常见原因是 PNG 损坏或无法被 `System.Drawing` 读取;这类文件不会生成 `.bin`。
* **想跳过已存在的 .bin**:加 `--ignore-existing`,但仅当已有 `.bin` 恰好为 131076 字节时才跳过(这是原程序的判定口径)。
* **RGB332 / RGB888 能用吗?**:不能,这两个是占位格式,选择后会以退出码 2 报错。

---

## 8. 环境要求

* 目标框架 `.NET Framework 4.7.2`,需在能运行 .NET Framework 的 Windows 上执行。
* 首次 `dotnet build` 会自动下载 4.7.2 引用包(需联网)。


