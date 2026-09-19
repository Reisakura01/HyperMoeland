# Islora 安装包（Inno Setup）

用 [Inno Setup 6](https://jrsoftware.org/isdl.php) 把 `dotnet publish` 的产物打成
一个标准的 Windows 安装程序（`Islora-<版本>-setup.exe`）。

## 一键构建

```powershell
# 在仓库根目录执行
pwsh -File packaging/installer/Build-Installer.ps1

# 常用参数
pwsh -File packaging/installer/Build-Installer.ps1 -Version 1.3.1   # 指定版本号
pwsh -File packaging/installer/Build-Installer.ps1 -SkipPublish     # 复用已有暂存目录
pwsh -File packaging/installer/Build-Installer.ps1 -OpenOutput      # 完成后打开产物目录
```

脚本会依次：

1. `dotnet publish -c Release -o artifacts\app`（框架依赖发布，顺带剔除 `.pdb` 与包身份开发产物）
2. 从 `Islora/Islora.csproj` 的 `<Version>` 读取版本号
3. 调用 `ISCC.exe` 生成安装包到 `artifacts\`

> 未安装 Inno Setup 时脚本会给出提示。安装：`winget install --id JRSoftware.InnoSetup`，
> 或到 <https://jrsoftware.org/isdl.php> 下载。

## 直接调用编译器（不用包装脚本）

```powershell
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" `
    /DAppVersion=1.3.0 `
    /DNumericVersion=1.3.0.0 `
    /DStagingDir=artifacts\app `
    /DOutputDir=artifacts `
    packaging\installer\Islora.iss
```

## 安装包行为

| 项目 | 说明 |
|---|---|
| 安装位置 | `%LOCALAPPDATA%\Programs\Islora`（**每位用户安装，不需要管理员权限，不弹 UAC**） |
| 安装大小 | 约 26 MB（其中 `Microsoft.Windows.SDK.NET.dll` 占绝大部分），安装包约 5.8 MB |
| 快捷方式 | 开始菜单（默认）+ 桌面（可选任务，默认不勾选） |
| 运行时检查 | 安装前检查 .NET 10 桌面运行时；缺失时给出下载地址，由用户决定是否继续 |
| 系统要求 | Windows 11（build 22000+），x64 |
| 覆盖安装 | `AppId` 固定，可直接覆盖升级；升级时自动保留 `%LOCALAPPDATA%\Islora\settings.json` |
| 卸载 | 控制面板 / 设置 → 应用；卸载会结束正在运行的岛并清理开始菜单与安装目录 |
| 随包附带 | `identity\`（稀疏包身份脚本，装完可按需开启）、`README.md`、`LICENSE` |

### 可选：安装后开启「包身份」

安装包**不含**证书与私钥（避免分发开发证书）。想启用包身份（让通知走官方事件订阅）的
用户，在自己机器上执行一次即可：

```powershell
# 需要管理员权限，脚本会自动请求提权
powershell -ExecutionPolicy Bypass -File "$env:LOCALAPPDATA\Programs\Islora\identity\Install-Identity.ps1" -Version 1.3.0.0 -Channel stable
```

脚本会自动识别「安装目录布局」：外部位置 = 安装目录，工作目录 = `identity\build\`。
详见 [`../identity/README.md`](../identity/README.md)。

## 本地化

安装界面提供 **简体中文** 与 **English** 两种语言（安装时自动跟随系统语言，也可在向导首页切换）。

- `ChineseSimplified.isl`：由 Inno Setup 自带的 `Default.isl` 衍生，共翻译 163 条安装/卸载流程词条；
  未列出的冷门词条沿用英文。
- 想补充词条：直接用文本编辑器（**UTF-8 带 BOM**）修改该文件，按 `键=值` 追加即可，键名需与
  `Default.isl` 一致。

## 文件说明

| 文件 | 作用 |
|---|---|
| `Islora.iss` | 安装脚本：安装位置、快捷方式、语言、运行时检查、卸载逻辑 |
| `ChineseSimplified.isl` | 简体中文安装界面词条 |
| `Build-Installer.ps1` | 一键构建脚本（发布 + 定位 ISCC + 编译） |
