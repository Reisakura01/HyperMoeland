; ============================================================================
;  Islora — Inno Setup 安装脚本
;
;  编译（推荐用包装脚本，会自动传入版本号与暂存目录）：
;      pwsh -File packaging/installer/Build-Installer.ps1
;  或直接调用编译器：
;      ISCC.exe /DAppVersion=1.3.0 /DStagingDir=..\..\dist packaging\installer\Islora.iss
;
;  设计要点：
;    * 每位用户安装（PrivilegesRequired=lowest），装到 %LOCALAPPDATA%\Programs，
;      不需要管理员权限，也不会弹 UAC——与「应用把设置写在 %LOCALAPPDATA%\Islora」一致。
;    * 应用是框架依赖发布（framework-dependent），因此安装前检查 .NET 10 桌面运行时；
;      缺失时给出下载地址并让用户选择是否继续（不强行阻断）。
;    * 安装包同时附带「稀疏包身份」脚本（identity\），装完可按需开启包身份以启用
;      官方通知事件订阅。
; ============================================================================

#define AppName "Islora"
#define AppPublisher "MoeOrigin Team"
#define AppURL "https://github.com/Reisakura01/Islora"
#define AppExeName "Islora.exe"

; 由命令行覆盖：/DAppVersion=1.3.0 /DStagingDir=<publish 输出目录> /DOutputDir=<产物目录>
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef NumericVersion
  #define NumericVersion "0.0.0.0"
#endif
#ifndef StagingDir
  #define StagingDir "..\..\dist"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\artifacts"
#endif

[Setup]
; AppId 一经发布不可更改，否则会被识别成另一个程序（无法覆盖安装/升级）
AppId={{7C4E9B31-2A6D-4F58-9E14-5B8D3A0F6C21}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
VersionInfoVersion={#NumericVersion}
VersionInfoDescription={#AppName} 安装程序
VersionInfoCompany={#AppPublisher}
VersionInfoCopyright=Copyright (C) MoeOrigin Team

DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
AllowNoIcons=yes
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}

OutputDir={#OutputDir}
OutputBaseFilename={#AppName}-{#AppVersion}-setup
SetupIconFile=..\..\Islora\App.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Islora 使用 WPF + 云母（Mica），最低要求 Windows 11（build 22000）
MinVersion=10.0.22000
; 安装/卸载时用重启管理器自动关闭正在运行的岛
CloseApplications=yes
RestartApplications=no
DisableDirPage=no
DisableReadyPage=no

[Languages]
; 中文词条文件由 Inno Setup 自带的 Default.isl 衍生（见 ChineseSimplified.isl 头部说明）
Name: "chinese"; MessagesFile: "ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
chinese.RuntimeMissing=未检测到 .NET 10 桌面运行时（Microsoft.WindowsDesktop.App 10.x）。%n%nIslora 是框架依赖应用，需要它才能启动。%n请先安装（选「Desktop Runtime」的 x64 版本）：%n%1%n%n是否仍要继续安装？
english.RuntimeMissing=.NET 10 Desktop Runtime (Microsoft.WindowsDesktop.App 10.x) was not found.%n%nIslora is framework-dependent and needs it to run.%nPlease install the x64 "Desktop Runtime" first:%n%1%n%nContinue the installation anyway?

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 应用本体（publish 输出）。排除调试符号与包身份的开发产物。
Source: "{#StagingDir}\*"; DestDir: "{app}"; \
    Excludes: "*.pdb,identity\*,*.log"; \
    Flags: ignoreversion recursesubdirs createallsubdirs
; 稀疏包身份脚本（可选功能，装后可自行开启；不自带证书/私钥）
Source: "..\..\packaging\identity\*.ps1"; DestDir: "{app}\identity"; Flags: ignoreversion
Source: "..\..\packaging\identity\AppxManifest.xml.template"; DestDir: "{app}\identity"; Flags: ignoreversion
Source: "..\..\Islora\App.ico"; DestDir: "{app}\identity"; Flags: ignoreversion
Source: "..\..\README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "..\..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Comment: "{#AppName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Comment: "{#AppName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; 卸载前先结束正在运行的岛，避免文件占用导致残留
Filename: "{cmd}"; Parameters: "/C taskkill /IM {#AppExeName} /F >NUL 2>&1"; Flags: runhidden; RunOnceId: "KillIsland"

[Code]
const
  { FILE_ATTRIBUTE_DIRECTORY 由 Inno Setup 预定义（值 $10），无需再声明 }
  DesktopRuntimeUrl = 'https://dotnet.microsoft.com/download/dotnet/10.0';

{ 在 BaseDir 下查找形如 10.* 的版本目录 }
function HasVersion10Folder(const BaseDir: String): Boolean;
var
  FindRec: TFindRec;
begin
  Result := False;
  if FindFirst(BaseDir + '\10.*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Result := True;
          Break;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

{ .NET 10 桌面运行时是否已安装：先查注册表版本列表，再兜底扫目录 }
function IsDesktopRuntimeInstalled(): Boolean;
var
  Names: TArrayOfString;
  I: Integer;
begin
  Result := False;
  if RegGetSubkeyNames(HKLM,
       'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App',
       Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
    begin
      if Pos('10.', Names[I]) = 1 then
      begin
        Result := True;
        Break;
      end;
    end;
  end;
  if not Result then
    Result := HasVersion10Folder(ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App'));
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if IsDesktopRuntimeInstalled() then
    Exit;
  if MsgBox(FmtMessage(CustomMessage('RuntimeMissing'), [DesktopRuntimeUrl]),
            mbConfirmation, MB_YESNO) = IDNO then
    Result := False;
end;
