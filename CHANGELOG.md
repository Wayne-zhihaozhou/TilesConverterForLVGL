# 更新日志

本项目遵循 [Keep-a-Changelog](https://keepachangelog.com/zh-CN/v1.1.0/) 风格。

## [未发布]

### 新增
- 增加 `LICENSE`（MIT）与 `NOTICE.md`（stb 第三方归属声明），满足开源发布要求。
- 重写 `README.md`：补充输出格式的位布局、解码公式、Python 校验实现、故障排查等，
  使项目「拿到手即可直接使用」。

## [1.0.0]

### 变更
- 默认像素格式由 `RGB565` 改为 `RGB565SWAP`（大端），并同步文档。

## [1.0.0-beta]

### 修复 / 维护
- 清理失效文档与 Makefile、测试残留脚本。
- 修复文档错误，补充 CLI 用法与原版一致性说明。

## [1.0.0-alpha]

### 重大变更
- 将项目从 C# 重写为 C/C++，大幅提升处理性能（多线程 + stb_image 批量解码）。
- WPF UI 改为命令行工具（CLI）。
- 通过逆向 `TilesConverterForLVGL.exe` 重建 `lvglConverter` 源码。
