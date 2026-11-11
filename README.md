# 喵时 MeowTimer 🐱

![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS-0078D4)
![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/license-MIT-3DA639)

一个很轻的桌面倒计时 / 番茄钟。没有窗口边框，可以拖到屏幕任意角落，平时就安安静静趴着显示时间。

点击数字就能开始，不用去找图标。

## 两个平台

| 平台 | 技术 | 功能完整度 |
| --- | --- | --- |
| **Windows** | WPF | 完整（倒计时 / 时钟 / 番茄钟 / 专注循环 / 热键 / 托盘 / 统计） |
| **macOS** | Avalonia | 核心（倒计时 / 主题 / 音效 / 悬浮窗） |

macOS 版是独立实现，两者共用配置格式和计时逻辑，但界面代码不通用。macOS 版目前还没有：番茄钟与专注循环、全局热键、托盘常驻、完成统计。

macOS 版长这样（和 Windows 版几乎一致）：

<img src="dist/ui-mac.png" width="300" />

<p align="center">
  <img src="dist/ui-main.png" width="420" alt="主界面" />
</p>

## 它长这样

| 编辑模式（可拖拽缩放） | 无背景 · 只留数字 |
| --- | --- |
| <img src="dist/ui-edit.png" width="360" /> | <img src="dist/ui-nobg.png" width="360" /> |

| 设置面板（跟随主题换色） | 深色主题 |
| --- | --- |
| <img src="dist/ui-settings-theme.png" width="360" /> | <img src="dist/ui-settings-dark.png" width="360" /> |

## 功能

**计时**
- 倒计时（精确到秒）、时钟、番茄钟、专注/休息循环四种模式
- 番茄钟与循环都可以设时 / 分 / 秒，自动进入下一阶段
- 计时结束闪烁提醒、提前提醒

**窗口**
- 无边框圆角，拖动任意位置，位置自动记住
- 编辑模式下拖右下角小圆点，整体等比缩放
- 可锁定位置，防止误碰
- 置顶 / 不置顶可选

**外观**
- 6 套配色：樱花粉、奶油布丁、薄荷苏打、夜空紫、落日橘、极简黑
- 其中夜空紫、极简黑是完整深色模式（设置面板也是暗色）
- 背景可选渐变 / 纯色（8 色可选）/ 完全无背景，不透明度 0–100%
- 文字颜色独立于主题，浅色字自动配深描边
- 字号 18–130，字体可选
- 猫咪、进度条、状态文字都能单独隐藏

**声音**
- 7 种内置音效（全部程序实时合成，不占体积）
- 可指定任意文件夹当音效库，支持 mp3 / wav / m4a / wma / flac / aac
- 音频文件直接拖进设置面板就自动加入

**其他**
- 8 个全局热键
- 托盘图标可显示剩余时间 / CPU / 内存
- 完成统计
- 开机自启、单实例运行
- 中英文界面

## 下载

### Windows

**[⬇ 下载 MeowTimer.exe](https://github.com/chenxuan169/MeowTimer/releases/latest/download/MeowTimer.exe)** — 约 380 KB，双击就能用

需要 [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)（大多数电脑已经装了）。

### macOS

在 [Releases](https://github.com/chenxuan169/MeowTimer/releases) 里按芯片选：

| 你的 Mac | 下载 |
| --- | --- |
| Apple Silicon（M1 / M2 / M3 / M4） | `MeowTimer-arm64.zip` |
| Intel 芯片 | `MeowTimer-x64.zip` |

解压后把 `MeowTimer.app` 拖进「应用程序」文件夹即可，**不需要装 .NET**（运行时已打包进去）。

> 应用没有 Apple 开发者签名，首次打开会提示「无法验证开发者」。
> 解决办法：在访达里**右键**点 MeowTimer.app → 选「打开」→ 再点一次「打开」。
> 或者在终端执行：`xattr -dr com.apple.quarantine /Applications/MeowTimer.app`

macOS 包由 GitHub Actions 在真 Mac 上自动构建（见 `.github/workflows/mac-build.yml`），保证可执行权限和芯片架构正确。

详细说明见 [使用说明.md](dist/使用说明.md)。

## 快捷键

| 快捷键 | 功能 |
| --- | --- |
| `Ctrl+Alt+P` | 开始 / 暂停 |
| `Ctrl+Alt+R` | 重置 |
| `Alt+Shift+T` | 设置倒计时 |
| `Ctrl+Alt+S` | 秒表 / 时钟 |
| `Ctrl+Alt+H` | 显示 / 隐藏 |
| `Ctrl+Alt+O` | 切换置顶 |
| `Ctrl+Alt+D` | 番茄钟 |
| `Ctrl+Alt+B` | 专注 · 休息循环 |

热键都能在设置里改。

## 自己编译

需要 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

**Windows 版**

```bash
cd MeowTimer

dotnet run                       # 调试运行

dotnet publish -c Release -r win-x64 --self-contained false ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:DebugType=none -o ../dist
```

**macOS 版**

```bash
cd MeowTimer.Mac

dotnet run                       # 在任意平台都能预览界面

# Apple Silicon
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o build/arm64

# Intel
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o build/x64

./scripts/make-app.sh 1.5.3      # 打成 .app + zip
```

## 技术栈

**Windows**：纯 .NET 8 + WPF，零第三方依赖。
**macOS**：.NET 8 + [Avalonia 11](https://avaloniaui.net/)，跨平台 XAML。

两边共用：

- 音效用代码实时合成波形（正弦波 + 包络），不占体积
- 配置文件格式一致（`%LOCALAPPDATA%\MeowTimer\` / `~/.local/share/MeowTimer/`）
- 计时与时长解析逻辑同源（`MeowTimer/Models`、`Services/ConfigService.cs` 等文件被 Mac 项目直接引用）

## 目录

```
MeowTimer/            Windows 版（WPF）
├── Models/           数据模型（配置、主题、统计）
├── Services/         配置、音效合成、主题、本地化、热键
├── Views/            设置、统计、输入等子窗口
└── MainWindow.xaml   主窗口

MeowTimer.Mac/        macOS 版（Avalonia）
├── Services/         主题、音效播放（afplay）
├── Views/            主窗口、设置、计时输入
└── （Models 与 ConfigService 直接从 Windows 版引用）

scripts/make-app.sh   打包 .app 的脚本
.github/workflows/    macOS 自动构建
```

## License

MIT
