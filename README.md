# HyperMoeland

> **Hyper 灵动岛** —— Windows 11 上的 iOS 风格 Dynamic Island（灵动岛）实现。
> 顶部居中的深/浅胶囊常驻显示，点击展开成卡片，自动跟随系统媒体、通知、电量。

用 **.NET 10 / WPF** 实现，不依赖 WinUI，API 稳定、好维护。

---

## ✨ 功能

| 类别 | 说明 |
|---|---|
| 🕐 **时钟** | 无媒体时胶囊显示当前时间（`HH:mm:ss`）；展开卡片显示完整时间/日期 |
| 🎵 **媒体（SMTC）** | 播放时胶囊显示迷你封面 + 曲目；展开成大面板：封面 + 标题/歌手 + 控制按钮（喜欢/上一首/播放暂停/下一首） |
| 🎚️ **音频频谱可视化** | **WASAPI 环回采集**系统实际播放的声音 → 1024 点 FFT → 6 频段频谱条 + 霓虹背景随音量律动（不是假节拍，是真实音频） |
| 📊 **播放进度** | 进度条跟随播放前进 + `已播 / 总时长`；**可点击进度条跳转**（源支持时） |
| 📩 **通知** | 读取系统 Toast **正文**（发送人 + 消息），胶囊暂时显示「📩 App · 发送人：内容」，几秒后复原 |
| 🔐 **包身份（可选）** | 通过 `packaging/identity` 注册**稀疏包身份**，通知改用官方事件订阅（实时、零轮询），并解锁更多系统能力 |
| 🔋 **电量** | 展开卡片显示电量百分比 + 插拔电闪电指示 |
| 🧩 **系统小组件** | 展开卡片显示 **CPU 占用率 / 内存占用率**（环形进度 + 悬停显示 `已用 / 总量` GB），可在设置中关闭 |
| 🌗 **日夜自动切换** | 默认 6:00–19:00 浅色胶囊，其余夜间深色胶囊；随时可改 |
| 🌐 **中英双语** | 设置内可切换中文 / English，即时生效并持久化 |
| 🖥️ **全屏自动隐藏** | 前台窗口全屏（视频/游戏）时自动隐藏岛，退出全屏恢复 |
| 🖥️ **多显示器跟随** | 自动跟随前台窗口所在屏幕 |
| 📌 **始终置顶** | 周期性强制置顶（不抢焦点），不被其它窗口遮挡 |
| 📌 **系统托盘** | 托盘菜单提供**设置 / 开机自启 / 测试通知 / 退出**（岛窗口不进任务栏） |
| 🖱️ **拖动吸附** | 按住胶囊可拖动，松手吸附到 左/中/右（顶部对齐），轻点展开/收回 |

> ⚠️ **进度条说明**：进度条依赖媒体源向 Windows 上报播放时间轴（SMTC Timeline）。
> **Spotify、系统媒体播放器、多数浏览器（Chrome/Edge）会上报** → 进度条正常显示；
> 而 **网易云等部分客户端不上报**（时间轴为 0）→ 进度行自动隐藏，属源的限制。

---

## 🖼️ 截图

| 展开卡片（媒体面板） | 紧凑胶囊 |
|---|---|
| ![卡片](docs/preview.png) | ![胶囊](docs/pill.png) |

<!-- 把截图放到 docs/ 下：preview.png（卡片）、pill.png（胶囊），README 会自动引用 -->

---

## 🛠️ 技术栈

- **.NET 10** / **WPF**（无 WinUI）
- **Windows SDK 10.0.26100**（TFM `net10.0-windows10.0.26100.0`）
- **WinRT：SMTC**（媒体会话）、**UserNotificationListener**（通知）、**Battery**（电量）
- **WASAPI 环回采集 + 手写 1024 点 FFT**（音频频谱，纯 COM 互操作，无第三方库）
- **Win32/DWM P/Invoke**（窗口样式、不抢焦点、显示器、前台窗口、CPU/内存）
- **Inno Setup 6**（安装包）+ **MSIX 稀疏包身份**（可选，启用官方通知事件订阅）

---

## 🚀 构建与运行

### 方式一：下载安装包（推荐给普通用户）

到 [Releases](https://github.com/Reisakura01/HyperMoeland/releases) 下载
`HyperMoeland-1.3.0-beta.1-setup.exe`，双击安装即可：

- 装到 `%LOCALAPPDATA%\Programs\HyperMoeland`，**每位用户安装，不需要管理员权限**
- 安装界面支持**简体中文 / English**
- 安装前会检查 .NET 10 桌面运行时（缺失时给出下载地址）
- 卸载走「设置 → 应用」，会一并清理快捷方式与安装目录

### 方式二：从源码编译

1. 安装 **VS2026 Community**（或 VS2022+），勾选 **.NET 10 SDK** 与 **Windows SDK 10.0.26100**；
2. 用 VS 打开 `HyperMoeland.sln`；
3. **Ctrl+Shift+B** 编译，或直接 **F5** 运行。

命令行发布 / 打包安装程序：

```powershell
# 发布（框架依赖）
dotnet publish HyperMoeland/HyperMoeland.csproj -c Release -o dist

# 打 Inno Setup 安装包（需先装 Inno Setup 6）
pwsh -File packaging/installer/Build-Installer.ps1
```

运行后：顶部中央出现胶囊（显示时间），点击展开成卡片，再点收回；
播放音乐时胶囊显示封面+曲名；到 19:00 自动切深色，早 6:00 切回浅色。
首次运行通知功能会弹"允许访问通知"的系统询问，点**允许**即可。

---

## 🏗️ 项目结构

```
HyperMoeland/
├── HyperMoeland.sln
├── README.md
├── 项目目录.md
├── .gitignore
├── packaging/
│   ├── identity/                    # 稀疏包身份（MSIX）：证书/打包/注册/移除脚本
│   ├── installer/                   # Inno Setup 安装包：脚本 + 中文词条 + 一键构建
│   └── store/                       # 完整 MSIX 包：Store 提交与本地测试打包
└── HyperMoeland/
    ├── HyperMoeland.csproj   # net10.0-windows10.0.26100.0 + UseWPF + UseWindowsForms
    ├── App.xaml(.cs)                # 入口 + 主题画刷默认值
    ├── MainWindow.xaml(.cs)         # 岛窗口：透明置顶 + 动画 + 所有服务接线
    ├── Core/
    │   ├── IslandState.cs           # 状态枚举 + 胶囊/卡片尺寸常量
    │   └── IslandController.cs      # 紧凑/展开状态机
    ├── Models/
    │   ├── MediaSessionInfo.cs      # 媒体会话快照（曲名/歌手/封面/播放状态）
    │   └── AppSettings.cs           # 设置（主题/自启/语言/霓虹节拍/小组件开关）
    ├── Theme/
    │   ├── AppTheme.cs              # Day / Night 枚举
    │   ├── ThemeManager.cs          # 画刷资源动态切换（深浅胶囊）
    │   └── ThemeScheduler.cs        # 按小时自动切换日夜
    ├── Interop/
    │   ├── NativeMethods.cs         # Win32/DWM P/Invoke（窗口样式、显示器、前台窗口、包身份）
    │   ├── MicaController.cs        # 应用窗口样式 + 暗色模式 + 不抢焦点
    │   ├── MonitorHelper.cs         # 主屏 / 指定屏工作区
    │   ├── WasapiLoopbackCapture.cs # WASAPI 环回采集（系统正在播放的声音）
    │   └── SystemInfo.cs            # GetSystemTimes + GlobalMemoryStatusEx
    ├── Services/
    │   ├── MediaService.cs          # SMTC 媒体会话（信息/控制/进度，含时间平滑外推）
    │   ├── NotificationService.cs   # 系统通知监听（事件订阅 / 轮询双模）+ 读取 Toast 正文
    │   ├── PackageContext.cs        # 运行形态判定：无身份 / 稀疏包 / 完整包（Store）
    │   ├── AudioService.cs          # 1024 点 FFT → 6 频段频谱 + 音量包络
    │   ├── SystemMonitorService.cs  # CPU / 内存采样（小组件数据源）
    │   ├── BatteryService.cs        # 电量百分比
    │   ├── ForegroundWatcher.cs     # 全屏检测 + 多显示器跟随
    │   └── TrayIcon.cs              # 系统托盘（设置/开机自启/测试通知/退出）
    └── Views/
        ├── CompactPill.xaml(.cs)    # 紧凑胶囊（时钟/媒体封面/通知）
        ├── ExpandedCard.xaml(.cs)   # 展开卡片（时钟卡+小组件 / 媒体大面板+频谱）
        ├── SystemWidgets.xaml(.cs)  # CPU / 内存环形小组件
        └── SettingsWindow.xaml(.cs) # 设置窗口
```

---

## ⚙️ 自定义

**日间 / 夜间时间段**：改 `ThemeManager` 调用的 `ThemeScheduler` 的 `DayStartHour` / `NightStartHour`，
例如 7:00–19:00：

```csharp
private readonly ThemeScheduler _themeScheduler = new()
{
    DayStartHour = 7,
    NightStartHour = 19,
};
```

**胶囊 / 卡片尺寸**：改 `Core/IslandState.cs` 里的 `IslandMetrics` 常量（紧凑胶囊 / 时钟卡 / 媒体大面板）。

**深浅配色**：改 `Theme/ThemeManager.cs` 的四支画刷（前景 / 次要 / 边框 / 覆盖）。

---

## 📦 已知局限

- **进度条**：仅当媒体源上报时间轴时显示（见上"进度条说明"）。
- **点通知直接打开 App**：WinRT 的 `AppInfo` 无 `Launch`，跨 Win32/UWP 打开不可靠，未实现。
- **计时器/通话/打车等实时活动**：依赖对应 App 开放标准接口，多数不上报，未做。

---

## 📄 许可

本项目采用 **MIT License**，详见 [LICENSE](LICENSE)。

隐私政策见 [docs/privacy.md](docs/privacy.md)。

---

*Built with .NET 10 + WPF · 纯 Windows 11 桌面实现 · 无第三方运行时依赖*
