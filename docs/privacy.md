# 隐私政策 · Privacy Policy

**HyperMoeland**（Windows 11 桌面应用）· 最后更新：2026-09-19

---

## 简体中文

### 一句话概括

HyperMoeland 是一个**完全在本机运行**的桌面小工具。它**不收集、不存储、不上传**任何个人信息，
没有账号体系，没有遥测、统计分析或广告，也不包含任何第三方 SDK。

### 应用会读取哪些信息，用途是什么

| 信息 | 用途 | 是否离开本机 |
|---|---|---|
| **系统通知内容**（发送方与正文） | 在你调节音量/播放音乐时，把新通知显示在屏幕顶部的「岛」上，几秒后自动消失 | 否 |
| **媒体会话信息**（曲名、歌手、专辑封面、播放状态） | 在岛上显示当前播放内容，并提供上一首/播放暂停/下一首控制 | 否 |
| **系统音频输出**（WASAPI 环回采集） | 实时计算频谱，驱动岛上的频谱条与霓虹亮度 | 否。**不录音、不保存、不传输**，数据仅在内存中用于逐帧计算 |
| **硬件状态**（CPU 占用率、物理内存占用率、电池电量） | 在展开卡片上显示系统小组件与电量 | 否 |
| **应用设置**（主题、语言、开机自启、动画速度等） | 记住你的偏好 | 否。保存在本机 `%LOCALAPPDATA%\HyperMoeland\settings.json` |

### 通知访问权限

阅读系统通知需要在 Windows 中授予「通知访问权限」（设置 → 隐私和安全性 → 通知）。
应用只读取通知的发送方与正文用于**本机显示**，不会将通知写入磁盘或发送到任何地方。
你可以在上述系统设置里**随时撤销**该权限；撤销后应用的其他功能不受影响，仅不再显示通知。

### 网络访问

应用对网络的唯一使用是：**启动时查询本项目 GitHub 仓库是否有新版本**（请求 `api.github.com`），
以便在托盘提示你更新。该请求只发送常规 HTTP 请求头，不包含任何个人标识或使用数据。

> 从 **Microsoft Store** 安装的版本**不会**执行该检查——更新由 Microsoft Store 负责，
> 即该版本完全不产生任何网络请求。

### 数据共享与第三方

不共享、不转让、不售卖任何数据。应用不集成第三方分析、崩溃上报或广告组件。

### 数据留存与删除

- 应用运行期间的信息（通知、媒体信息、音频采样）**只在内存中短暂存在**，不落盘。
- 唯一的持久化数据是上表的「应用设置」。
- 想彻底清除：删除 `%LOCALAPPDATA%\HyperMoeland` 文件夹即可；卸载应用不会自动删除它（这是为了让你重装后保留偏好），你可以手动删除。

### 儿童隐私

本应用不面向儿童，也不收集任何年龄相关的个人信息。

### 政策变更

如本政策有变更，会更新本文档并同步到本页面。

### 联系方式

有任何隐私相关问题，请通过以下方式联系：

- GitHub Issues：<https://github.com/Reisakura01/HyperMoeland/issues>
- 电子邮件：zx138913@gmail.com

---

## English

### Summary

HyperMoeland is a desktop widget that runs **entirely on your device**. It collects **no** personal
data, has no accounts, no telemetry, no analytics, no ads, and bundles no third-party SDKs.

### What the app reads, and why

| Data | Purpose | Leaves your device? |
|---|---|---|
| **System notifications** (sender and body) | Briefly show the notification on the on-screen "island" | No |
| **Media session info** (title, artist, artwork, playback state) | Show what's playing and provide playback controls | No |
| **System audio output** (WASAPI loopback) | Compute a live spectrum for the visualization | No. **Not recorded, not stored, not transmitted** — processed in memory, frame by frame |
| **Hardware status** (CPU load, memory load, battery level) | Show the system widgets and battery indicator | No |
| **App settings** (theme, language, autostart, animation speed) | Remember your preferences | No — stored locally in `%LOCALAPPDATA%\HyperMoeland\settings.json` |

### Notification access

Reading system notifications requires the "Notification access" permission in Windows
(Settings → Privacy & security → Notifications). The app uses notification sender and body
**only for on-device display**; notifications are never written to disk or sent anywhere.
You can revoke this permission at any time in Windows Settings; the rest of the app keeps working,
it simply stops showing notifications.

### Network access

The only network request the app ever makes is checking this project's GitHub repository for a
newer release at startup (`api.github.com`), so the tray can tell you an update exists. The request
carries no personal identifiers or usage data.

> The **Microsoft Store** build does **not** perform this check — the Store handles updates, so that
> build makes no network requests at all.

### Sharing and third parties

No data is shared, transferred, or sold. There are no analytics, crash-reporting, or advertising
components.

### Retention and deletion

- Runtime data (notifications, media info, audio samples) exists **only in memory** and is never persisted.
- The only persisted data is the app settings listed above.
- To erase everything, delete the `%LOCALAPPDATA%\HyperMoeland` folder. Uninstalling does not remove
  it automatically (so your preferences survive a reinstall) — you may delete it manually.

### Children's privacy

This app is not directed at children and collects no age-related personal information.

### Changes

Any change to this policy will be reflected in this document and on this page.

### Contact

- GitHub Issues: <https://github.com/Reisakura01/HyperMoeland/issues>
- Email: zx138913@gmail.com
