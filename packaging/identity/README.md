# HyperMoeland 包身份（Sparse Package Identity）

为「包外」的 `HyperMoeland.exe` 授予 Windows **包身份（Package Identity）**，
解锁需要身份的系统能力：

- ✅ `UserNotificationListener` **事件订阅**（无身份时订阅会抛 `0x80070490 ERROR_NOT_FOUND`，只能退化为轮询）
- ✅ Toast 通知拥有正规 AUMID，通知中心里可正确归属
- ✅ 其它依赖包身份的 WinRT API

原理：注册一个**只含清单**的稀疏包（sparse package），并通过
`Add-AppxPackage -ExternalLocation <目录>` 把包指向 exe 所在目录。
注册后，**从该目录启动的 `HyperMoeland.exe` 会自动获得包身份**（无需改代码）。

> 参考实现：WinIsland 的 `packaging/identity`（Rust 项目，同机制）。

---

## 前置条件

- **Windows SDK**（提供 `MakeAppx.exe` 与 `SignTool.exe`）
  本机已装：`C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\`
- **管理员权限**（脚本会自动弹出 UAC 提权）
- 无需开启「开发者模式」（包已用受信任证书签名）

---

## 使用方式

### 1. 构建程序

```powershell
dotnet publish HyperMoeland\HyperMoeland.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

### 2. 注册包身份（首次，需管理员）

```powershell
# 默认指向仓库下的 dist 目录，版本 1.2.0.0
.\packaging\identity\Install-Identity.ps1

# 或指定目录 / 版本 / 渠道（dev 与 stable 可共存）
.\packaging\identity\Install-Identity.ps1 -ExternalLocation "D:\apps\HyperMoeland" -Version 1.3.0.0 -Channel dev
```

脚本会依次：
1. 生成/复用自签名证书（`CN=MoeOrigin Team`）
2. 用 `MakeAppx` 打包 + `SignTool` 签名出 `.msix`
3. 把公钥证书导入 `本地计算机 → 受信任人`
4. `Add-AppxPackage -ExternalLocation` 完成注册

### 3. 启动（重要：影响是否带身份）

注册后，**包身份取决于启动方式**：

| 启动方式 | 是否带身份 | 通知实现 |
|---|---|---|
| 运行 `Start-WithIdentity.ps1` | ✅ 带身份 | **官方事件订阅**（实时、零轮询） |
| 从「带身份」快捷方式启动（`New-Shortcut.ps1` 创建） | ✅ 带身份 | 官方事件订阅 |
| **直接双击 `HyperMoeland.exe`** | ❌ 不带 | 自动退化为**轮询**（1 秒一次） |
| 开机自启（Run 键，直接拉起 exe） | ❌ 不带 | 自动退化为轮询 |

> 程序会**自动检测**并选择模式，两种模式功能都正常，区别只是实时性与资源占用。
> 想要事件订阅，就用下面的脚本启动。

```powershell
# 直接以包身份启动
.\packaging\identity\Start-WithIdentity.ps1

# 创建带身份的快捷方式（桌面 / 开始菜单）
.\packaging\identity\New-Shortcut.ps1 -Destination Desktop
.\packaging\identity\New-Shortcut.ps1 -Destination StartMenu
```

### 4. 卸载

```powershell
.\packaging\identity\Remove-Identity.ps1              # 仅移除包注册
.\packaging\identity\Remove-Identity.ps1 -RemoveCertificate   # 同时移除受信任证书
```

---

## 文件说明

| 文件 | 作用 |
|---|---|
| `AppxManifest.xml.template` | 清单模板（占位符由构建脚本替换） |
| `New-Certificate.ps1` | 生成/复用自签名证书并导出 `.pfx` |
| `Build-IdentityPackage.ps1` | 生成图标 → 替换占位符 → `MakeAppx` 打包 → `SignTool` 签名 → 导出 `.cer` |
| `Install-Identity.ps1` | 一键：证书 + 打包签名 + 导入信任 + 注册包（自动提权） |
| `Start-WithIdentity.ps1` | 以包身份（AUMID 激活）启动程序 |
| `New-Shortcut.ps1` | 创建带身份的桌面/开始菜单快捷方式 |
| `Remove-Identity.ps1` | 移除包注册（可选移除证书） |

---

## 清单关键点

| 项 | 值 | 说明 |
|---|---|---|
| `Publisher` | `CN=MoeOrigin Team` | **必须与签名证书 Subject 完全一致** |
| `rescap:runFullTrust` | ✔ | 桌面应用全信任 |
| `rescap:unvirtualizedResources` | ✔ | 不虚拟化文件/注册表，保持 `%LOCALAPPDATA%` 真实路径 |
| `uap10:AllowExternalContent` | `true` | 允许 exe 位于包外 |
| `uap10:TrustLevel` | `mediumIL` | 中等完整性级别（普通桌面权限） |
| `uap10:RuntimeBehavior` | `win32App` | 以桌面应用方式运行 |
| `AppListEntry` | `none` | 不在开始菜单/应用列表出现（托盘常驻应用） |

---

## 注意事项

- **Publisher 与证书必须匹配**，否则 `Add-AppxPackage` 会报签名/发布者不匹配。
- 注册后更新程序**无需重新注册**：只要 exe 路径不变，替换 exe 即可。
- 若更换了证书（例如重新生成了证书），需要先 `Remove-Identity.ps1` 再重新安装。
- 包的清单版本（`-Version`）必须是**四段式**（如 `1.2.0.0`）。
