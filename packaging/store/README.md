# Islora 完整 MSIX 包（Microsoft Store / 本地测试）

把 `dotnet publish` 的产物打成**完整 MSIX 包**，用于：
- 提交 Microsoft Store（Store 免费代为签名，并自动提供 CDN 与更新）
- 本地安装、验证「打包形态」下的行为

与 [`../identity`](../identity/README.md)（稀疏包）的区别：

| | 稀疏包（identity） | 完整包（store） |
|---|---|---|
| 用途 | 给包**外**的 exe 授予包身份 | 真正分发（Store 提交 / 本地安装） |
| 应用文件 | 不进包（`AllowExternalContent` 指向真实目录） | 打进包内 |
| 开始菜单 | `AppListEntry="none"` | 有正常入口，从开始菜单启动 |
| 开机自启 | HKCU `Run` 键 | 清单 `windows.startupTask` |
| 签名 | 自签证书（需自行导入受信任人） | 提交用未签名包，Store 代签 |

## Store 身份

Partner Center「产品管理 → 产品标识」给出的三个值（已在后台把产品名改为 Islora 后的实际取值）：

| 清单节点 | 值 |
|---|---|
| `Package/Identity/Name` → `<Identity Name>` | `Reisakura.Islora` |
| `Package/Identity/Publisher` → `<Identity Publisher>` | `CN=0070A02F-1245-460C-935F-426185146220` |
| `Package/Properties/PublisherDisplayName` | `Reisakura` |

由 Name + Publisher 推导的**包系列名**（按 `Name_ + Base32(SHA256(UTF16LE(Publisher)))[0..12]` 计算，
算法已用 `CN=MoeOrigin Team → 6c96f9ngvaz36` 与实装包核对过，实测装包结果同样吻合）：

```
Reisakura.Islora_5250pqc4cqtpj
```

提交包一条命令（自包含 + 未签名，Store 会代为签名）：

```powershell
pwsh -File packaging/store/Build-StorePackage.ps1 -NoSign -SelfContained `
    -PackageName "Reisakura.Islora" `
    -Publisher "CN=0070A02F-1245-460C-935F-426185146220" `
    -PublisherDisplayName "Reisakura"
```

产物：`artifacts/store/Reisakura.Islora-<清单版本>.msix`（当前 `1.3.0.0`，约 78 MB）。
上传到 Partner Center 的「程序包」页即可，**不要**用自签证书签名后再上传（Store 会自己签）。

## 构建

```powershell
# 本地测试用（自签证书签名）
pwsh -File packaging/store/Build-StorePackage.ps1

# 提交 Store 用（未签名 + Partner Center 身份三件套）
pwsh -File packaging/store/Build-StorePackage.ps1 -NoSign `
    -PackageName "12345Reisakura01.Islora" `
    -Publisher "CN=1A2B3C4D-0000-0000-0000-000000000000" `
    -PublisherDisplayName "MoeOrigin Team"

# 自包含版（用户无需安装 .NET 运行时；需要能访问 nuget.org）
pwsh -File packaging/store/Build-StorePackage.ps1 -SelfContained
```

产物在 `artifacts/store/`：`<包名>-<版本>.msix`，以及中间目录 `package/`（含生成的清单与图标）。

## 本地安装 / 卸载

```powershell
# 安装（证书已在「受信任人」时无需管理员）
pwsh -File packaging/store/Install-StorePackage.ps1

# 卸载（只卸载 StoreTest 包，不会误删 identity 的稀疏包）
pwsh -File packaging/store/Install-StorePackage.ps1 -Uninstall
```

> 自签证书要能安装，必须位于「本地计算机 → 受信任人」。首次需管理员执行：
> `Import-Certificate -FilePath dist\identity\MoeOrigin.Islora.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople`
> （或直接跑 `packaging/identity/Install-Identity.ps1`，它会一并处理）

> ⚠️ 要用 **Store 身份**（`CN=0070A02F-…`）的包本地安装，签名证书的 Subject 必须与清单里的
> Publisher 完全一致，即需要一张 `CN=0070A02F-1245-460C-935F-426185146220` 的自签证书并导入
> 受信任人（需管理员）。现有的 `CN=MoeOrigin Team` 证书签不了这个包。

## 版本号与 Store 提交

**清单版本 = 应用版本的 major.minor.build + 固定的第 4 段 `0`。**

| 应用版本 | 清单版本 | 说明 |
|---|---|---|
| `1.3.0-beta.2` | `1.3.0.0` | 预发布序号**不进**版本号 |
| `1.3.0` | `1.3.0.0` | 与上面同一个包版本 |
| `1.3.1` | `1.3.1.0` | 再次提交时必须提升第 3 段 |

### 第 4 段必须为 0（硬性要求）

[官方文档](https://learn.microsoft.com/en-us/windows/apps/publishing/publish-your-app/package-version-numbering)原文：

> For Windows 10 or Windows 11 packages, **the last (fourth) section of the version number is
> reserved for Store use and must be left as 0** when you build your package.

填非 0 会在提交时被拒，报错原文是
`Apps are not allowed to have a Version with a revision number other than zero specified in the app manifest`。
所以**预发布序号（`-beta.2` 里的 `2`）绝不能写进版本号**——早期版本的本脚本曾把它算进第 4 段
（产出 `1.3.0.2`），那种包提交必被拒。现在脚本会强制把第 4 段归零。

### 其余约束

- 各段取值 0..65535，且**第 1 段不能为 0**（脚本已做校验并会报错）；
- Store 要求**每次提交的包版本严格大于上一次**。由于同一 `X.Y.Z` 的 beta 与稳定版都会映射到
  同一个包版本，**同一 `X.Y.Z` 只能提交一次**；
- 因此再次提交的正确做法是提升第 3 段：提交过 `1.3.0.0` 之后，下一次用应用版本 `1.3.1`（→ `1.3.1.0`）。

## 清单声明了什么

`AppxManifest.xml.template` 相对稀疏包清单的关键差异：

| 声明 | 原因 |
|---|---|
| `EntryPoint="Windows.FullTrustApplication"` | WPF 桌面应用的标准入口点（稀疏包那份用的是 `uap10:RuntimeBehavior="win32App"`） |
| `rescap:Capability Name="runFullTrust"` | 桌面应用全信任运行 |
| `rescap:Capability Name="userNotificationListener"` | 读取系统通知。**必须写在 `rescap:` 命名空间**，写成 `uap:Capability` 会直接被清单校验拒绝 |
| `uap5:Extension Category="windows.startupTask"` | 打包应用不能用 HKCU `Run` 实现开机自启 |
| `TargetDeviceFamily MinVersion="10.0.22000.0"` | 云母（Mica）等效果要求 Windows 11 |
| `ProcessorArchitecture="x64"` | 与发布 RID 一致（稀疏包用的是 `neutral`） |

## 本地验证结论

### 完整包形态（测试身份，1.3.0-beta.1）

在 Windows 11（build 26100）上装包后从**开始菜单入口**启动，实测：

| 项目 | 结果 |
|---|---|
| 包注册 | `MoeOrigin.Islora.StoreTest_1.3.0.1_x64__6c96f9ngvaz36`，安装到 `C:\Program Files\WindowsApps\...` |
| 开始菜单 | 入口已注册（`Get-StartApps` 可见），AUMID 启动正常 |
| 完整包识别 | `IsFullPackage=True`、`FamilyName` 正确、exe 路径落在 `InstalledLocation` 内 |
| 通知能力 | `ok=True mode=event-subscription` —— 受限能力 `userNotificationListener` 生效，通知走官方事件订阅而非轮询 |
| 开机自启 | `StartupTask State=Enabled`，由应用按设置调用 `RequestEnableAsync()` 打开 |
| 设置读写 | 仍读写真实路径 `%LOCALAPPDATA%\Islora\settings.json`，**未被虚拟化**，用户既有设置直接沿用 |
| 稳定性 | 进程持续存活，事件日志无崩溃记录 |

由此确认：**不需要**在完整包里声明 `unvirtualizedResources`，设置存储路径无需改动。

### 正式 Store 包（真实身份，1.3.0-beta.2 → 清单 1.3.0.0）

用与清单 Publisher 同名的自签证书签名后本地安装（证书已在受信任人，无需管理员），实测：

| 项目 | 结果 |
|---|---|
| 包全名 | `Reisakura.Islora_1.3.0.0_x64__5250pqc4cqtpj` |
| 包系列名 | `Reisakura.Islora_5250pqc4cqtpj`（与上面推导的预测值完全一致） |
| 开始菜单 | `Reisakura.Islora_5250pqc4cqtpj!Islora` |
| 启动 | 从 AUMID 启动正常，进程运行于 `C:\Program Files\WindowsApps\Reisakura.Islora_1.3.0.0_x64__5250pqc4cqtpj\Islora.exe`，无崩溃记录 |
| 卸载 | 验证后已卸载，避免与将来真正的 Store 版（同包系列名）冲突 |

## 提交 Store 前还需要做的事

1. ~~Partner Center 身份三件套~~ —— 后台产品名已改为 Islora，打包脚本已按真实身份出包。
2. **受限能力说明**：`userNotificationListener` 属受限能力，提交时需要在审核备注里说明用途
   （Islora 用它把系统通知显示在岛的胶囊上，不落盘、不外传）。
3. **截图**：至少 1 张，≥1366×768。仓库现有 `docs/preview.png`（920×500）与 `docs/pill.png`（464×92）**都不达标**，需要重拍整屏截图。
4. **隐私政策 URL**：必填（读通知/媒体信息属个人信息范畴）。
5. **年龄分级问卷**（IARC）、分类、系统要求、支持联系方式。
6. **文案避免商标词**：listing 里不要出现 Apple「Dynamic Island」或小米「超级岛」等表述。
7. **自包含包**：建议用 `-SelfContained` 出包，用户无需另装 .NET 10 桌面运行时（本地构建需要能访问 nuget.org，CI 可以）。

## 已知限制

- 包名默认是 `MoeOrigin.Islora.StoreTest`，与 `identity` 注册的稀疏包**可以共存**，但两者同时运行会出现两个岛；
  日常使用建议只保留一种形态。
- MSIX 卸载不会清理 `%LOCALAPPDATA%\Islora`（用户设置会保留，这是刻意的）。
