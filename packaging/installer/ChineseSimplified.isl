; *** HyperMoeland 简体中文词条（由 Inno Setup 6 自带 Default.isl 衍生）***
;
; 只翻译了安装/卸载流程中会看到的词条，未列出的条目沿用英文原文。
; 如需补充词条：直接在本文件中添加/修改 键=值 即可（键名必须与 Default.isl 一致）。
; 注意：不要在原本没有句点的消息末尾添加句点。

; *** Inno Setup version 6.5.0+ English messages ***
;
; To download user-contributed translations of this file, go to:
;   https://jrsoftware.org/files/istrans/
;
; Note: When translating this text, do not add periods (.) to the end of
; messages that didn't have them already, because on those messages Inno
; Setup adds the periods automatically (appending a period would result in
; two periods being displayed).

[LangOptions]
; The following three entries are very important. Be sure to read and
; understand the '[LangOptions] section' topic in the help file.
LanguageName=简体中文
LanguageID=$0804
; LanguageCodePage should always be set if possible, even if this file is Unicode
; For English it's set to zero anyway because English only uses ASCII characters
LanguageCodePage=936
; If the language you are translating to requires special font faces or
; sizes, uncomment any of the following entries and change them accordingly.
;DialogFontName=
;DialogFontSize=9
;DialogFontBaseScaleWidth=7
;DialogFontBaseScaleHeight=15
;WelcomeFontName=Segoe UI
;WelcomeFontSize=14

[Messages]

; *** Application titles
SetupAppTitle=安装程序
SetupWindowTitle=安装程序 - %1
UninstallAppTitle=卸载
UninstallAppFullTitle=%1 卸载

; *** Misc. common
InformationTitle=信息
ConfirmTitle=确认
ErrorTitle=错误

; *** SetupLdr messages
SetupLdrStartupMessage=This will install %1. Do you wish to continue?
LdrCannotCreateTemp=Unable to create a temporary file. Setup aborted
LdrCannotExecTemp=Unable to execute file in the temporary directory. Setup aborted
HelpTextNote=

; *** Startup error messages
LastErrorMessage=%1.%n%nError %2: %3
SetupFileMissing=The file %1 is missing from the installation directory. Please correct the problem or obtain a new copy of the program.
SetupFileCorrupt=The setup files are corrupted. Please obtain a new copy of the program.
SetupFileCorruptOrWrongVer=The setup files are corrupted, or are incompatible with this version of Setup. Please correct the problem or obtain a new copy of the program.
InvalidParameter=An invalid parameter was passed on the command line:%n%n%1
SetupAlreadyRunning=Setup is already running.
WindowsVersionNotSupported=This program does not support the version of Windows your computer is running.
WindowsServicePackRequired=This program requires %1 Service Pack %2 or later.
NotOnThisPlatform=This program will not run on %1.
OnlyOnThisPlatform=This program must be run on %1.
OnlyOnTheseArchitectures=This program can only be installed on versions of Windows designed for the following processor architectures:%n%n%1
WinVersionTooLowError=This program requires %1 version %2 or later.
WinVersionTooHighError=This program cannot be installed on %1 version %2 or later.
AdminPrivilegesRequired=You must be logged in as an administrator when installing this program.
PowerUserPrivilegesRequired=You must be logged in as an administrator or as a member of the Power Users group when installing this program.
SetupAppRunningError=安装程序检测到 %1 正在运行。%n%n请关闭它，然后点击「确定」继续；或点击「取消」退出。
UninstallAppRunningError=卸载程序检测到 %1 正在运行。%n%n请关闭它，然后点击「确定」继续；或点击「取消」退出。

; *** Startup questions
PrivilegesRequiredOverrideTitle=Select Setup Install Mode
PrivilegesRequiredOverrideInstruction=Select install mode
PrivilegesRequiredOverrideText1=%1 can be installed for all users (requires administrative privileges), or for you only.
PrivilegesRequiredOverrideText2=%1 can be installed for you only, or for all users (requires administrative privileges).
PrivilegesRequiredOverrideAllUsers=Install for &all users
PrivilegesRequiredOverrideAllUsersRecommended=Install for &all users (recommended)
PrivilegesRequiredOverrideCurrentUser=Install for &me only
PrivilegesRequiredOverrideCurrentUserRecommended=Install for &me only (recommended)

; *** Misc. errors
ErrorCreatingDir=安装程序无法创建目录「%1」
ErrorTooManyFilesInDir=无法在目录「%1」中创建文件，因为该目录包含的文件太多

; *** Setup common messages
ExitSetupTitle=退出安装程序
ExitSetupMessage=安装尚未完成。如果现在退出，程序将不会被安装。%n%n你可以稍后再次运行安装程序来完成安装。%n%n要退出安装程序吗？
AboutSetupMenuItem=&About Setup...
AboutSetupTitle=About Setup
AboutSetupMessage=%1 version %2%n%3%n%n%1 home page:%n%4
AboutSetupNote=
TranslatorNote=

; *** Buttons
ButtonBack=< 上一步(&B)
ButtonNext=下一步(&N) >
ButtonInstall=安装(&I)
ButtonOK=确定
ButtonCancel=取消
ButtonYes=是(&Y)
ButtonYesToAll=Yes to &All
ButtonNo=否(&N)
ButtonNoToAll=N&o to All
ButtonFinish=完成(&F)
ButtonBrowse=浏览(&B)...
ButtonWizardBrowse=浏览(&R)...
ButtonNewFolder=新建文件夹(&M)

; *** "Select Language" dialog messages
SelectLanguageTitle=选择安装语言
SelectLanguageLabel=请选择安装过程中使用的语言。

; *** Common wizard text
ClickNext=点击「下一步」继续，或点击「取消」退出安装程序。
BeveledLabel=
BrowseDialogTitle=Browse For Folder
BrowseDialogLabel=Select a folder in the list below, then click OK.
NewFolderName=New Folder

; *** "Welcome" wizard page
WelcomeLabel1=欢迎使用 [name] 安装向导
WelcomeLabel2=即将在你的计算机上安装 [name/ver]。%n%n建议在继续之前关闭其它应用程序。

; *** "Password" wizard page
WizardPassword=Password
PasswordLabel1=This installation is password protected.
PasswordLabel3=Please provide the password, then click Next to continue. Passwords are case-sensitive.
PasswordEditLabel=&Password:
IncorrectPassword=The password you entered is not correct. Please try again.

; *** "License Agreement" wizard page
WizardLicense=License Agreement
LicenseLabel=Please read the following important information before continuing.
LicenseLabel3=Please read the following License Agreement. You must accept the terms of this agreement before continuing with the installation.
LicenseAccepted=I &accept the agreement
LicenseNotAccepted=I &do not accept the agreement

; *** "Information" wizard pages
WizardInfoBefore=Information
InfoBeforeLabel=Please read the following important information before continuing.
InfoBeforeClickLabel=When you are ready to continue with Setup, click Next.
WizardInfoAfter=Information
InfoAfterLabel=Please read the following important information before continuing.
InfoAfterClickLabel=When you are ready to continue with Setup, click Next.

; *** "User Information" wizard page
WizardUserInfo=User Information
UserInfoDesc=Please enter your information.
UserInfoName=&User Name:
UserInfoOrg=&Organization:
UserInfoSerial=&Serial Number:
UserInfoNameRequired=You must enter a name.

; *** "Select Destination Location" wizard page
WizardSelectDir=选择目标位置
SelectDirDesc=[name] 应该安装到哪里？
SelectDirLabel3=安装程序将把 [name] 安装到以下文件夹。
SelectDirBrowseLabel=点击「下一步」继续。如果想更换文件夹，请点击「浏览」。
DiskSpaceGBLabel=至少需要 [gb] GB 可用磁盘空间。
DiskSpaceMBLabel=至少需要 [mb] MB 可用磁盘空间。
CannotInstallToNetworkDrive=安装程序无法安装到网络驱动器。
CannotInstallToUNCPath=安装程序无法安装到 UNC 路径。
InvalidPath=请输入带盘符的完整路径，例如：%n%nC:\APP%n%n或使用 UNC 路径：%n%n\\server\share
InvalidDrive=所选磁盘（或 UNC 共享）不存在或不可访问，请另选一个。
DiskSpaceWarningTitle=磁盘空间不足
DiskSpaceWarning=安装程序至少需要 %1 KB 可用磁盘空间，但所选磁盘只有 %2 KB 可用。%n%n仍要继续吗？
DirNameTooLong=文件夹名称或路径太长。
InvalidDirName=文件夹名称无效。
BadDirName32=文件夹名称不能包含下列字符：%n%n%1
DirExistsTitle=文件夹已存在
DirExists=文件夹：%n%n%1%n%n已经存在。仍要安装到该文件夹吗？
DirDoesntExistTitle=文件夹不存在
DirDoesntExist=文件夹：%n%n%1%n%n不存在。要创建该文件夹吗？

; *** "Select Components" wizard page
WizardSelectComponents=Select Components
SelectComponentsDesc=Which components should be installed?
SelectComponentsLabel2=Select the components you want to install; clear the components you do not want to install. Click Next when you are ready to continue.
FullInstallation=Full installation
; if possible don't translate 'Compact' as 'Minimal' (I mean 'Minimal' in your language)
CompactInstallation=Compact installation
CustomInstallation=Custom installation
NoUninstallWarningTitle=Components Exist
NoUninstallWarning=Setup has detected that the following components are already installed on your computer:%n%n%1%n%nDeselecting these components will not uninstall them.%n%nWould you like to continue anyway?
ComponentSize1=%1 KB
ComponentSize2=%1 MB
ComponentsDiskSpaceGBLabel=Current selection requires at least [gb] GB of disk space.
ComponentsDiskSpaceMBLabel=Current selection requires at least [mb] MB of disk space.

; *** "Select Additional Tasks" wizard page
WizardSelectTasks=选择附加任务
SelectTasksDesc=需要执行哪些附加任务？
SelectTasksLabel2=请选择安装 [name] 时要执行的附加任务，然后点击「下一步」。

; *** "Select Start Menu Folder" wizard page
WizardSelectProgramGroup=选择开始菜单文件夹
SelectStartMenuFolderDesc=安装程序应该把程序快捷方式放在哪里？
SelectStartMenuFolderLabel3=安装程序将在以下开始菜单文件夹中创建程序快捷方式。
SelectStartMenuFolderBrowseLabel=点击「下一步」继续。如果想更换文件夹，请点击「浏览」。
MustEnterGroupName=必须输入文件夹名称。
GroupNameTooLong=文件夹名称或路径太长。
InvalidGroupName=文件夹名称无效。
BadGroupName=文件夹名称不能包含前导空格或小数点。
NoProgramGroupCheck2=不创建开始菜单文件夹(&D)

; *** "Ready to Install" wizard page
WizardReady=准备安装
ReadyLabel1=安装程序已准备好在你的计算机上安装 [name]。
ReadyLabel2a=点击「安装」开始安装；如果想检查或更改设置，请点击「上一步」。
ReadyLabel2b=点击「安装」开始安装。
ReadyMemoUserInfo=用户信息：
ReadyMemoDir=目标位置：
ReadyMemoType=安装类型：
ReadyMemoComponents=已选组件：
ReadyMemoGroup=开始菜单文件夹：
ReadyMemoTasks=附加任务：

; *** TDownloadWizardPage wizard page and DownloadTemporaryFile
DownloadingLabel2=Downloading files...
ButtonStopDownload=&Stop download
StopDownload=Are you sure you want to stop the download?
ErrorDownloadAborted=Download aborted
ErrorDownloadFailed=Download failed: %1 %2
ErrorDownloadSizeFailed=Getting size failed: %1 %2
ErrorProgress=Invalid progress: %1 of %2
ErrorFileSize=Invalid file size: expected %1, found %2

; *** TExtractionWizardPage wizard page and ExtractArchive
ExtractingLabel=Extracting files...
ButtonStopExtraction=&Stop extraction
StopExtraction=Are you sure you want to stop the extraction?
ErrorExtractionAborted=Extraction aborted
ErrorExtractionFailed=Extraction failed: %1

; *** Archive extraction failure details
ArchiveIncorrectPassword=The password is incorrect
ArchiveIsCorrupted=The archive is corrupted
ArchiveUnsupportedFormat=The archive format is unsupported

; *** "Preparing to Install" wizard page
WizardPreparing=正在准备安装
PreparingDesc=安装程序正在准备在你的计算机上安装 [name]。
PreviousInstallNotCompleted=上一个程序的安装/卸载没有完成。你需要重启计算机才能完成该操作。%n%n重启后请再次运行安装程序以完成 [name] 的安装。
CannotContinue=安装程序无法继续，请点击「取消」退出。
ApplicationsFound=以下应用程序正在使用需要由安装程序更新的文件。建议允许安装程序自动关闭这些应用程序。
ApplicationsFound2=以下应用程序正在使用需要由安装程序更新的文件。建议允许安装程序自动关闭这些应用程序。安装完成后，安装程序会尝试重新启动这些应用程序。
CloseApplications=自动关闭这些应用程序(&A)
DontCloseApplications=不要关闭这些应用程序(&D)
ErrorCloseApplications=安装程序无法自动关闭所有应用程序。请在继续之前手动关闭这些程序。
PrepareToInstallNeedsRestart=Setup must restart your computer. After restarting your computer, run Setup again to complete the installation of [name].%n%nWould you like to restart now?

; *** "Installing" wizard page
WizardInstalling=正在安装
InstallingLabel=安装程序正在把 [name] 安装到你的计算机上，请稍候。

; *** "Setup Completed" wizard page
FinishedHeadingLabel=正在完成 [name] 安装向导
FinishedLabelNoIcons=安装程序已完成 [name] 的安装。
FinishedLabel=安装程序已完成 [name] 的安装。可以通过已安装的快捷方式启动该程序。
ClickFinish=点击「完成」退出安装程序。
FinishedRestartLabel=要完成 [name] 的安装，必须重启计算机。现在重启吗？
FinishedRestartMessage=要完成 [name] 的安装，必须重启计算机。%n%n现在重启吗？
ShowReadmeCheck=是，我要查看自述文件
YesRadio=是，立即重启计算机(&Y)
NoRadio=否，稍后由我重启计算机(&N)
; used for example as 'Run MyProg.exe'
RunEntryExec=Run %1
; used for example as 'View Readme.txt'
RunEntryShellExec=View %1

; *** "Setup Needs the Next Disk" stuff
ChangeDiskTitle=Setup Needs the Next Disk
SelectDiskLabel2=Please insert Disk %1 and click OK.%n%nIf the files on this disk can be found in a folder other than the one displayed below, enter the correct path or click Browse.
PathLabel=&Path:
FileNotInDir2=The file "%1" could not be located in "%2". Please insert the correct disk or select another folder.
SelectDirectoryLabel=Please specify the location of the next disk.

; *** Installation phase messages
SetupAborted=Setup was not completed.%n%nPlease correct the problem and run Setup again.
AbortRetryIgnoreSelectAction=Select action
AbortRetryIgnoreRetry=&Try again
AbortRetryIgnoreIgnore=&Ignore the error and continue
AbortRetryIgnoreCancel=Cancel installation
RetryCancelSelectAction=Select action
RetryCancelRetry=&Try again
RetryCancelCancel=Cancel

; *** Installation status messages
StatusClosingApplications=正在关闭应用程序...
StatusCreateDirs=正在创建目录...
StatusExtractFiles=正在解压文件...
StatusDownloadFiles=正在下载文件...
StatusCreateIcons=正在创建快捷方式...
StatusCreateIniEntries=正在写入 INI 项...
StatusCreateRegistryEntries=正在写入注册表项...
StatusRegisterFiles=正在注册文件...
StatusSavingUninstall=正在保存卸载信息...
StatusRunProgram=正在完成安装...
StatusRestartingApplications=正在重启应用程序...
StatusRollback=正在回滚更改...

; *** Misc. errors
ErrorInternal2=Internal error: %1
ErrorFunctionFailedNoCode=%1 failed
ErrorFunctionFailed=%1 failed; code %2
ErrorFunctionFailedWithMessage=%1 failed; code %2.%n%3
ErrorExecutingProgram=无法执行文件：%n%1

; *** Registry errors
ErrorRegOpenKey=打开注册表项时出错：%n%1\%2
ErrorRegCreateKey=创建注册表项时出错：%n%1\%2
ErrorRegWriteKey=写入注册表项时出错：%n%1\%2

; *** INI errors
ErrorIniEntry=在文件「%1」中创建 INI 项时出错。

; *** File copying errors
FileAbortRetryIgnoreSkipNotRecommended=&Skip this file (not recommended)
FileAbortRetryIgnoreIgnoreNotRecommended=&Ignore the error and continue (not recommended)
SourceIsCorrupted=源文件已损坏
SourceDoesntExist=源文件「%1」不存在
SourceVerificationFailed=Verification of the source file failed: %1
VerificationSignatureDoesntExist=The signature file "%1" does not exist
VerificationSignatureInvalid=The signature file "%1" is invalid
VerificationKeyNotFound=The signature file "%1" uses an unknown key
VerificationFileNameIncorrect=The name of the file is incorrect
VerificationFileTagIncorrect=The tag of the file is incorrect
VerificationFileSizeIncorrect=The size of the file is incorrect
VerificationFileHashIncorrect=The hash of the file is incorrect
ExistingFileReadOnly2=无法替换现有文件，因为它被标记为只读。
ExistingFileReadOnlyRetry=清除只读属性并重试(&R)
ExistingFileReadOnlyKeepExisting=保留现有文件(&K)
ErrorReadingExistingDest=读取现有文件时出错：
FileExistsSelectAction=要如何处理该文件？
FileExists2=该文件已经存在。%n%n%1
FileExistsOverwriteExisting=覆盖现有文件(&O)
FileExistsKeepExisting=保留现有文件(&K)
FileExistsOverwriteOrKeepAll=对之后所有冲突都这样处理(&A)
ExistingFileNewerSelectAction=要如何处理该文件？
ExistingFileNewer2=计算机上已存在一个比安装程序要安装的版本更新的文件：%n%n%1
ExistingFileNewerOverwriteExisting=覆盖现有文件(&O)
ExistingFileNewerKeepExisting=保留现有文件（推荐）(&K)
ExistingFileNewerOverwriteOrKeepAll=对之后所有冲突都这样处理(&A)
ErrorChangingAttr=更改以下文件的属性时出错：%n%1
ErrorCreatingTemp=在目标目录中创建文件时出错：%n%1
ErrorReadingSource=读取源文件时出错：%n%1
ErrorCopying=复制文件时出错：%n%1%n%n点击「重试」重试，或点击「取消」放弃。
ErrorDownloading=An error occurred while trying to download a file:
ErrorExtracting=An error occurred while trying to extract an archive:
ErrorReplacingExistingFile=替换现有文件时出错：%n%1
ErrorRestartReplace=重启替换失败：
ErrorRenamingTemp=重命名目标目录中的文件时出错：%n%1
ErrorRegisterServer=Unable to register the DLL/OCX: %1
ErrorRegSvr32Failed=RegSvr32 failed with exit code %1
ErrorRegisterTypeLib=Unable to register the type library: %1

; *** Uninstall display name markings
; used for example as 'My Program (32-bit)'
UninstallDisplayNameMark=%1 (%2)
; used for example as 'My Program (32-bit, All users)'
UninstallDisplayNameMarks=%1 (%2, %3)
UninstallDisplayNameMark32Bit=32-bit
UninstallDisplayNameMark64Bit=64-bit
UninstallDisplayNameMarkAllUsers=All users
UninstallDisplayNameMarkCurrentUser=Current user

; *** Post-installation errors
ErrorOpeningReadme=An error occurred while trying to open the README file.
ErrorRestartingComputer=Setup was unable to restart the computer. Please do this manually.

; *** Uninstaller messages
UninstallNotFound=文件「%1」不存在，无法卸载。
UninstallOpenError=无法打开文件「%1」，无法卸载。
UninstallUnsupportedVer=无法识别此版本安装程序生成的卸载日志文件「%1」，无法卸载。
UninstallUnknownEntry=卸载日志中出现未知项（%1）。
ConfirmUninstall=确定要完全删除 %1 及其所有组件吗？
UninstallOnlyOnWin64=此安装只能在 64 位 Windows 上卸载。
OnlyAdminCanUninstall=此安装只能由拥有管理员权限的用户卸载。
UninstallStatusLabel=正在从你的计算机上删除 %1，请稍候。
UninstalledAll=%1 已成功从你的计算机上删除。
UninstalledMost=%1 卸载完成。%n%n部分项目无法删除，需要手动删除。
UninstalledAndNeedsRestart=要完成 %1 的卸载，必须重启计算机。%n%n现在重启吗？
UninstallDataCorrupted=「%1」文件已损坏，无法卸载

; *** Uninstallation phase messages
ConfirmDeleteSharedFileTitle=要删除共享文件吗？
ConfirmDeleteSharedFile2=系统指示以下共享文件已不再被任何程序使用。如果删除这些文件，而其它程序仍在使用它们，这些程序可能无法正常工作。如果不确定，请选择「否」。保留这些文件无害，只是会占用一些磁盘空间。%n%n要删除这些共享文件吗？
SharedFileNameLabel=文件名：
SharedFileLocationLabel=位置：
WizardUninstalling=卸载状态
StatusUninstalling=正在卸载 %1...

; *** Shutdown block reasons
ShutdownBlockReasonInstallingApp=Installing %1.
ShutdownBlockReasonUninstallingApp=Uninstalling %1.

; The custom messages below aren't used by Setup itself, but if you make
; use of them in your scripts, you'll want to translate them.

[CustomMessages]

NameAndVersion=%1 版本 %2
AdditionalIcons=附加快捷方式：
CreateDesktopIcon=创建桌面快捷方式(&D)
CreateQuickLaunchIcon=Create a &Quick Launch shortcut
ProgramOnTheWeb=%1 的官方网站
UninstallProgram=卸载 %1
LaunchProgram=运行 %1
AssocFileExtension=将 %1 关联到 %2 文件扩展名(&A)
AssocingFileExtension=Associating %1 with the %2 file extension...
AutoStartProgramGroupDescription=启动：
AutoStartProgram=自动启动 %1
AddonHostProgramNotFound=%1 could not be located in the folder you selected.%n%nDo you want to continue anyway?
