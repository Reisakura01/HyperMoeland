# Microsoft Store 上架资料（Islora）

本文件是 Partner Center 提交时的**照填清单**：文案、字段、截图拍摄方法、审核备注。
复制粘贴即可，不用再想措辞。

- 产品名（已保留）：**Islora**
- 提交包：`artifacts/store/Reisakura.Islora-1.3.0.0.msix`
- 隐私政策：`docs/privacy.md`（启用 GitHub Pages 后为 `https://reisakura01.github.io/Islora/privacy.html`）

> ⚠️ 全程避免出现 `Dynamic Island` / `灵动岛` / `超级岛` / `iOS` / `Apple` / `小米` 等商标词。
> 下面文案已按此写好，改的时候也请守着这条。

---

## 一、字段速填

| Partner Center 字段 | 填什么 |
|---|---|
| 产品名称 | `Islora` |
| 类别 | **个性设置（Personalization）**；若后台无此项则选 **实用工具（Utilities & tools）** |
| 版权与商标信息 | `Copyright © 2026 Reisakura01. Windows is a registered trademark of Microsoft Corporation.` |
| 支持联系人 | `https://github.com/Reisakura01/Islora/issues` |
| 支持邮箱 | `zx138913@gmail.com` |
| 隐私政策 URL | `https://reisakura01.github.io/Islora/privacy.html` |
| 网站（可选） | `https://github.com/Reisakura01/Islora` |
| 附加系统要求 | 见下方「附加系统要求」 |
| 搜索词 | 见下方「搜索词」 |

---

## 二、商店描述

### 简体中文

```
Islora 把 Windows 11 的屏幕顶部变成一条会呼吸的状态胶囊。

空闲时它是一枚显示时间的小胶囊；音乐响起，它变成迷你封面与曲名；收到通知，它短暂让位给
「谁说了什么」，几秒后自动复原。轻点展开成卡片，再点收回，全程不抢焦点、不进任务栏，
不打断你手上的工作。

【它会自己跟着你走】
· 音乐在放，卡片就是播放器：封面、曲名、上一首 / 播放暂停 / 下一首，进度条可点击跳转
· 真实音频频谱：直接采集系统正在播放的声音做实时频谱，霓虹背景随节奏律动
· 系统通知：读取通知正文，在胶囊上显示发送人与内容
· 硬件状态：CPU 与内存占用率环形小组件，电量百分比与插拔电提示
· 日夜自动切换：默认 6:00–19:00 浅色，其余时间深色，时间点可自定义
· 全屏自动隐藏：看视频、玩游戏时自动让位，退出全屏立刻恢复
· 中英双语界面，一键切换

【它不做的事】
不采集、不上传任何数据；没有账号、没有广告、没有统计分析。音频只在本机实时计算，
不录音、不保存。通知内容仅用于本机显示。

【顺手的小细节】
可拖到屏幕左 / 中 / 右并吸附；多显示器自动跟随；始终置顶却从不抢焦点；
随应用附带可选的包身份注册脚本，开启后通知改走系统官方事件订阅，实时且零轮询。
```

### English

```
Islora turns the top of your Windows 11 screen into a living status capsule.

Idle, it is a small pill showing the time. Start some music and it becomes a mini album cover with
the track title. A notification arrives and it briefly makes room for who said what, then restores
itself. Tap to expand into a card, tap again to collapse — it never steals focus, never sits in your
taskbar, and never interrupts what you are doing.

WHAT IT FOLLOWS ON ITS OWN
· Music: the card becomes a player — artwork, title, previous / play-pause / next, with a seekable
  progress bar
· Real audio spectrum: captures what your system is actually playing and draws a live spectrum,
  with the neon backdrop pulsing to the beat
· System notifications: reads the notification body and shows sender and message on the capsule
· Hardware: CPU and memory ring widgets, battery percentage and charging indicator
· Automatic day/night theme: light from 6:00 to 19:00, dark the rest of the time — configurable
· Auto-hide in full screen: steps aside for videos and games, returns the moment you exit
· Bilingual interface (Chinese / English), switchable in one click

WHAT IT DOES NOT DO
No data collection, no uploads, no account, no ads, no analytics. Audio is processed locally in real
time and is never recorded or stored. Notification content is used only for on-device display.

NICE DETAILS
Drag it to the left / center / right and it snaps into place. Follows your active monitor. Always on
top, yet never steals focus. Ships with an optional package-identity script that switches
notification listening to the official system event subscription — real time, zero polling.
```

---

## 三、产品功能（最多 20 条，每条 ≤ 200 字符）

| # | 中文 | English |
|---|---|---|
| 1 | 顶部常驻胶囊：空闲显示时间，播放音乐时显示封面与曲名 | Always-on-top capsule: shows the time when idle, artwork and track title while playing |
| 2 | 真实音频频谱：采集系统实际播放的声音，霓虹随音量律动 | Real audio spectrum from what your system is playing, with neon reacting to the level |
| 3 | 系统通知上岛：显示发送人与正文，几秒后自动复原 | System notifications on the capsule: sender and body, restoring automatically |
| 4 | 媒体控制：上一首 / 播放暂停 / 下一首，进度条可点击跳转 | Media controls: previous / play-pause / next, plus a seekable progress bar |
| 5 | 系统小组件：CPU 与内存占用率环形进度 | System widgets: CPU and memory usage rings |
| 6 | 电量百分比与插拔电提示 | Battery percentage with charging indicator |
| 7 | 日夜自动切换，时间点可自定义 | Automatic day/night theme with configurable hours |
| 8 | 全屏自动隐藏、多显示器跟随、始终置顶不抢焦点 | Auto-hide in full screen, follows the active monitor, always on top without stealing focus |
| 9 | 中英双语界面 | Bilingual interface (Chinese / English) |
| 10 | 不采集数据、无广告、无统计分析 | No data collection, no ads, no analytics |
| 11 | 可选包身份：通知改走系统官方事件订阅，实时零轮询 | Optional package identity: notifications switch to the official event subscription |

---

## 四、搜索词（最多 7 个，每个 ≤ 30 字符）

`Islora` · `状态胶囊` · `桌面美化` · `媒体控制` · `系统监控` · `通知助手` · `desktop widget`

---

## 五、本次更新说明（What's new，首次提交）

```
首个 Microsoft Store 版本。

· 顶部胶囊：时钟 / 媒体 / 通知 / 电量
· 真实音频频谱与霓虹律动（WASAPI 环回 + 1024 点 FFT）
· CPU / 内存环形小组件
· 中英双语界面
· 可选的包身份注册，通知走官方事件订阅

自包含发布：无需另外安装 .NET 运行时。
```

---

## 六、附加系统要求

```
操作系统：Windows 11（build 22000 或更高，64 位）
处理器：x64
内存：建议 4 GB 以上（实测占用约 180 MB）
无需额外硬件；运行时已自包含，不需要单独安装 .NET 运行时
```

---

## 七、年龄分级（IARC 问卷）

按应用实际情况回答，预期结果是**最低分级（3+ / Everyone）**：

| 问题方向 | 答案 |
|---|---|
| 暴力 / 恐怖 / 性 / 赌博 / 毒品 / 粗话 | 全部「无」 |
| 用户生成内容 / 社交功能 | 无 |
| 是否会收集或分享个人信息 | **否**（全部本机处理，详见隐私政策） |
| 是否包含广告 | 否 |
| 是否包含内购 | 否 |
| 是否允许用户交互 / 位置共享 | 否 |

> 唯一需要留意的是：应用会读取系统通知内容用于**本机显示**。
> 若问卷有「访问设备上的个人信息」类问题，如实说明「仅本机显示、不存储、不传输」，并指向隐私政策。

---

## 八、审核备注（Notes for certification，务必填）

```
测试方法：
1. 启动应用后，屏幕顶部中央会出现一枚胶囊（默认显示时间）。
2. 单击胶囊展开成卡片，再次单击收回。
3. 托盘图标右键菜单提供「测试通知」，可立即看到通知在胶囊上显示的效果。
4. 播放任意音频（例如浏览器里的视频），卡片上会出现实时频谱条，霓虹背景随音量律动。
5. 托盘右键 →「打开设置」可切换主题、语言与系统小组件开关。

关于受限能力 userNotificationListener：
本应用用它读取系统通知的发送方与正文，仅用于在屏幕顶部的胶囊上显示几秒，
供用户不切换窗口就能看到通知。通知内容不会写入磁盘，不会上传，也不会共享给任何第三方。
用户可随时在「设置 → 隐私和安全性 → 通知」中撤销该权限，撤销后应用其余功能不受影响。

关于窗口行为：
这是一个常驻屏幕顶部的状态胶囊，刻意不进入任务栏、不抢占焦点；
前台窗口全屏（视频、游戏）时会自动隐藏，退出全屏后恢复。
应用提供托盘菜单，可从中「退出」，符合桌面应用的正常退出方式。
```

### 受限能力说明（提交时后台会要求单独填写）

上传包后会出现验证警告：

> The following restricted capabilities require approval before you can use them in your app:
> **runFullTrust, userNotificationListener**.

这是**警告不是错误**（包已验证通过，可继续提交），但提交时必须为每个受限能力填写用途说明。
若后台没有单独的「受限能力说明」输入框，就把下面两段一并写进**审核备注**。

**① runFullTrust —— 例行声明，桌面应用都需要**

```
Islora is a desktop application built with .NET/WPF and packaged as a full-trust MSIX
(desktop bridge). Like any packaged Win32 application, it runs at medium integrity level
outside the AppContainer, so it must declare the runFullTrust restricted capability.
This is the standard requirement for packaged desktop apps; the app does not use it to
perform any privileged operation that the signed-in user could not already perform.
```

**② userNotificationListener —— 需要说明理由的那个**

```
Islora is a status capsule that stays at the top of the screen. It uses the User Notification
Listener capability for exactly one purpose: to read the sender and body of Windows toast
notifications so it can display them briefly on the capsule (about five seconds), letting the
user see who said what without switching windows.

 - Notification content is used only for that immediate on-device display. It is never written
   to disk, never uploaded, and never shared with any third party. It is discarded as soon as
   the on-screen message disappears.
 - No notification data is logged, aggregated, or analyzed, and the app contains no telemetry.
 - Access is requested through UserNotificationListener.RequestAccessAsync(). The app works only
   after the user explicitly grants notification access in Windows Settings; if the user declines
   or later revokes it (Settings → Privacy & security → Notifications), the app keeps working
   normally and simply stops showing notifications.
 - Reviewers can verify the display path at any time from the tray icon's context menu
   ("Test notification"), which shows a locally generated sample message.
 - Reading notifications is the core feature of this app: it is a notification-display utility,
   and without this capability the app cannot fulfil its stated purpose.

The app does not collect, transmit, or sell any user data. See the privacy policy:
https://reisakura01.github.io/Islora/privacy.html
```

### 如果 userNotificationListener 没被批准

它决定的是**打包（Store）版能否读取系统通知**。一旦被拒，两条路：

1. **去掉该能力重新提交**：应用照常运行，只是不再显示系统通知（其余功能不受影响）。
   出包时把 `packaging/store/AppxManifest.xml.template` 里那行
   `<rescap:Capability Name="userNotificationListener" />` 删掉即可。
2. 继续争取：补充更具体的说明（例如说明这是应用的核心功能、数据完全不外传）。

> `runFullTrust` 几乎不会出问题——它是所有 Win32/WPF 打成 MSIX 的应用的标配声明。

---

## 九、截图拍摄指南

### 硬性要求

- **桌面应用：至少 1 张，分辨率不低于 1366×768**，最多 10 张
- 推荐 **1920×1080**（16:9），全部同尺寸同比例
- 格式 PNG 或 JPEG，**不要加设备边框、不要加文字水印**（文字需按语言本地化，索性不加）

> 现有 `docs/preview.png` 只有 920×500、`docs/pill.png` 464×92，**都不达标**，必须重拍。

### 拍摄前准备（5 分钟）

1. 换一张**干净的壁纸**：纯色或柔和渐变最好，别用花哨图片，否则胶囊和频谱条会被淹没
2. **关掉所有窗口**，桌面图标可以隐藏（右键桌面 → 查看 → 取消「显示桌面图标」）
3. 切到**深色主题**：设置 → 外观 → 模式 → 强制深色。霓虹效果在深色下最明显
4. 播放一首歌（浏览器或任意播放器），让卡片有封面、曲名和频谱可看
5. 用 `Win + Shift + S`（截图工具）或 `Win + PrtScn` 截图，**注意先确认没有通知弹窗和私人信息**

### 建议拍这 5 张

| # | 画面 | 怎么摆出来 |
|---|---|---|
| 1 | **胶囊态**：只在顶部中央显示时间胶囊，桌面干净 | 不播放任何媒体，等胶囊回到时钟状态 |
| 2 | **媒体卡片**：封面 + 曲名 + 控制按钮 + 频谱条 + 进度 | 播放音乐后单击胶囊展开 |
| 3 | **系统小组件**：时钟卡上的 CPU / 内存环形 + 电量 | 暂停全部媒体（QQ音乐/浏览器都退出），单击胶囊展开 |
| 4 | **通知上岛**：胶囊显示「📩 应用 · 发送人：内容」 | 托盘图标右键 →「测试通知」，趁它显示的 5 秒内截图 |
| 5 | **设置窗口**（可选）：主题 / 语言 / 小组件开关 | 托盘右键 →「打开设置」 |

### 隐私检查（截图是公开的，务必过一遍）

- [ ] 浏览器标签、聊天窗口、文件名、邮箱、真实姓名 —— 全部不可见
- [ ] 播放的歌曲名如果不是你自己的作品，最好换成无版权或自己熟悉的曲子（避免奇怪的关联）
- [ ] 任务栏时间/日期是否介意公开
- [ ] 托盘图标里有没有暴露其它私人软件

### 想省事的话

如果你愿意，我可以让应用停在某个状态（比如展开成卡片），然后**只截取岛窗口所在的那一小块区域**
（不含桌面其它内容），再放到 1920×1080 的纯色底上导出 PNG。
这样绝对不含隐私，缺点是画面里没有「真实使用场景」的氛围感。
要的话说一声，并把你想用的底色（深色 / 浅色 / 指定色值）告诉我。

---

## 十、定价与可用性（Pricing and availability）逐项填写

这一页有四大块，**从上到下**依次是 Markets → Visibility → Schedule → Pricing。
后台界面是英文，下表「选这个」一列给的就是界面上的英文原文，照着点即可。

| # | 区块 | 字段 | 选这个 | 为什么 |
|---|---|---|---|---|
| 1 | **Markets** | 市场 | **保持默认：所有可用市场**（`Your app will be offered in all possible markets`） | 应用免费、无地区特定内容、界面中英双语，没有需要排除的市场。⚠️ 若手点过 `Select markets`，务必确认**中国**在列表里 |
| 2 | **Visibility** | Discoverability | **`Make this product available and discoverable in the Store`**（默认项） | 只有这一项能被搜索、浏览、进入推荐列表 |
| 3 | **Visibility** | Audience | **`Public`**（默认项） | 公开发布。`Private audience` 是按邮箱名单限定（上限约 1 万），只适合内测 |
| 4 | **Schedule** | Release | **`as soon as possible`**（默认项） | 认证通过后自动上架 |
| 5 | **Schedule** | Stop acquisition | **留空** | 不设下架日期 |
| 6 | **Pricing** | Base price | **`Free`** | MIT 协议、永久免费 |
| 7 | **Pricing** | Free trial / Sale price | **不动** | 只有付费应用才出现这些选项 |
| 8 | **Pricing** | 价格变更排期 / 各市场单独定价 | **不动** | 免费应用无需配置 |

### 三个必须注意的点

1. **不要选 `Make this product available but not discoverable in the Store`。**
   选了之后商店里搜不到、浏览不到（只能靠直链访问），而且按官方文档，
   **Schedule 区块会直接不可配置**——连上线时间都没法设。
2. **免费应用不需要税务 / 收款资料。**
   后台若提示补 tax / payout 信息，那是给付费应用用的
   （微软官方措辞是 "before you can charge money"）。走免费路线可以忽略，也省掉一堆合规填表。
3. **发布之后，Schedule 里的 Release 选项会消失**（已发布，不再可改）。
   若想把上线时间卡在某个具体时刻（比如配合宣传），**必须在第一次提交时就选 `at <日期时间>`**；
   否则用默认的「越快越好」。时间口径还有 `UTC`（全球同时上线）/ `Local`（各市场本地时间）之分。

### 这一页**不**管的两件事

- **支持哪些 Windows 版本**：由包内清单的 `TargetDeviceFamily MinVersion="10.0.22000.0"` 决定，
  商店会自动只向 **Windows 11** 用户展示，本页没有开关可调（我们的包就是这个设置，符合预期）。
- Schedule 里设的日期**只对 Windows 10 / 11 的客户生效**（官方注明）。

### 照抄版（最省事）

```
Markets           → 默认（所有可用市场）
Discoverability   → Make this product available and discoverable in the Store
Audience          → Public
Release           → as soon as possible
Stop acquisition  → 留空
Base price        → Free
其余所有选项       → 不动
```

### 来源

- [Pricing and availability 页面说明](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/price-and-availability)
- [配置发布时间（Schedule）](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/configure-release-schedule)
