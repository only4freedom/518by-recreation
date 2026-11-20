# 518by Replica

一个从零编写的 Windows 播音软件复刻项目。此仓库不包含原软件的二进制文件、图标或反编译代码。

## 当前阶段

首个原型围绕已确认的 518 超市播音功能搭建：

- 本机 SAPI 语音枚举、试听和语速/音量调整
- 播音内容队列，增删、排序、立即播报和循环播报
- 背景音乐目录选择与播放
- XML 设置保存
- GitHub Actions 分别构建 x86 与 x64 Windows 可执行文件

原程序静态证据显示它是 32 位原生 Windows PE，使用 BASS 音频组件，并包含本机 HuiHui SAPI 语音、Edge TTS/Python 组件和远端 TTS 线索。与安装包一样的完整功能需要分阶段复刻；线上语音、模板变量、时间表、音频混音和数据导入格式仍需继续确认。

逆向证据来源、可信度和未确定项见 [`docs/reconstruction-notes.md`](docs/reconstruction-notes.md)。

## 构建

GitHub Actions 工作流在推送到 `main`、创建版本标签或手动运行时构建两个架构，并将 EXE 上传为可下载的 workflow artifacts。

程序以 .NET Framework 3.5 / WinForms 编写，目标是 Windows 7 及更高版本。Windows 7 通常已包含 .NET Framework 3.5，因此构建产物是单个 EXE，无需另外下载程序依赖 DLL；若系统精简或关闭了 .NET 3.5 Windows 功能，需要先启用该功能。实际兼容性仍需在 Win7 x86 与 x64 真机/虚拟机上验证。

## 数据

设置保存在 `%APPDATA%\518byReplica\settings.xml`。音频和播音文本均由用户自行选择或输入。


