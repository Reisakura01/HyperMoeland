using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Islora.Services;

/// <summary>系统托盘图标（后台常驻）：自定义应用图标 + 干净现代右键菜单。</summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _autoStartItem;
    private readonly ToolStripMenuItem _testNotifItem;
    private readonly ToolStripMenuItem _exitItem;
    private bool _updateConnected;

    private static readonly Color TextColor = Color.FromArgb(0x1B, 0x1B, 0x1B);
    private static readonly Color MutedColor = Color.FromArgb(0x8A, 0x8F, 0x99);

    /// <summary>点击"设置"菜单触发。</summary>
    public event Action? OpenSettings;

    /// <summary>点击"测试通知"菜单触发（用于验证通知展示）。</summary>
    public event Action? TestNotification;

    public TrayIcon()
    {
        var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Font = new Font("Microsoft YaHei UI", 9.5f),
            Renderer = new TrayRenderer(),
            Padding = new Padding(2, 6, 2, 6),
            ShowCheckMargin = false,
        };

        // 打开设置
        _settingsItem = new ToolStripMenuItem { Padding = new Padding(10, 7, 10, 7) };
        _settingsItem.Click += (_, _) => OpenSettings?.Invoke();
        menu.Items.Add(_settingsItem);

        // 开机自启
        _autoStartItem = new ToolStripMenuItem
        {
            Padding = new Padding(10, 7, 10, 7),
            Checked = AutoStart.IsEnabled(),
        };
        _autoStartItem.Click += (_, _) =>
        {
            // 以「当前实际状态」取反，而不是以菜单的勾选态为准——
            // 打包版的自启状态是异步读回来的，菜单可能在缓存就绪前就建好了。
            bool target = !AutoStart.IsEnabled();
            AutoStart.Set(target);
            SettingsService.Current.AutoStart = target;
            SettingsService.Save();
            _autoStartItem.Checked = AutoStart.IsEnabled();
        };
        menu.Items.Add(_autoStartItem);

        // 测试通知（验证通知展示链路）
        _testNotifItem = new ToolStripMenuItem { Padding = new Padding(10, 7, 10, 7) };
        _testNotifItem.Click += (_, _) => TestNotification?.Invoke();
        menu.Items.Add(_testNotifItem);

        // 退出
        _exitItem = new ToolStripMenuItem { Padding = new Padding(10, 7, 10, 7) };
        _exitItem.Click += (_, _) => System.Windows.Application.Current.Shutdown();
        menu.Items.Add(_exitItem);

        // 每次打开菜单时刷新自启勾选态：
        // 打包（Store）版的自启状态来自异步的 StartupTask 查询，
        // 构造菜单时缓存可能还没就绪，会导致「明明开着却显示没开」。
        menu.Opening += (_, _) => _autoStartItem.Checked = AutoStart.IsEnabled();

        var icon = LoadAppIcon() ?? SystemIcons.Application;

        _icon = new NotifyIcon
        {
            Icon = icon,
            Text = "Islora",
            ContextMenuStrip = menu,
            Visible = true,
        };

        // 语言切换时刷新菜单文字
        LocalizationService.LanguageChanged += ApplyLanguage;
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        _settingsItem.Text = LocalizationService.T("Tray.OpenSettings");
        _autoStartItem.Text = LocalizationService.T("Tray.AutoStart");
        _testNotifItem.Text = LocalizationService.T("Tray.TestNotif");
        _exitItem.Text = LocalizationService.T("Tray.Exit");
        _icon.Text = LocalizationService.T("Tray.Tooltip");
    }

    /// <summary>从可执行文件提取应用图标（App.ico 由 ApplicationIcon 编译进 exe）。
    /// 单文件发布下 Assembly.Location 为空，用 Environment.ProcessPath 定位实际 exe。</summary>
    private static Icon? LoadAppIcon()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) return null;
            return Icon.ExtractAssociatedIcon(exe);
        }
        catch { return null; }
    }

    /// <summary>托盘气泡显示"发现新版本"，点击打开下载页。</summary>
    public void ShowUpdate(string message, string url)
    {
        // URL 存字段、点击时读最新值。
        // 原来是把 url 直接捕获进「只挂一次」的点击处理器里，
        // 于是先弹的「通知权限提示」（url 为空）会把处理器钉死在空串上，
        // 之后的「发现新版本」气泡点了毫无反应。
        _balloonUrl = url;
        _icon.ShowBalloonTip(8000, LocalizationService.T("Tray.Tooltip"), message, ToolTipIcon.Info);
        if (!_updateConnected)
        {
            _updateConnected = true;
            _icon.BalloonTipClicked += (_, _) =>
            {
                if (!string.IsNullOrEmpty(_balloonUrl)) OpenUrl(_balloonUrl);
            };
        }
    }

    private string _balloonUrl = string.Empty;

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    /// <summary>现代菜单渲染器：圆角、浅色背景、柔和选中高亮、细圆分隔线。</summary>
    private sealed class TrayRenderer : ToolStripProfessionalRenderer
    {
        public TrayRenderer() : base(new TrayColorTable()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // 常规项深色，禁用/次要项浅灰
            e.TextColor = e.Item.Enabled ? TextColor : MutedColor;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var g = e.Graphics;
            bool selected = e.Item.Selected && e.Item.Enabled;
            var rect = new Rectangle(1, 1, e.Item.Width - 3, e.Item.Height - 3);
            using var brush = selected
                ? new SolidBrush(Color.FromArgb(0xED, 0xF2, 0xFF))  // 选中浅蓝
                : new SolidBrush(Color.White);
            g.FillRectangle(brush, rect);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var g = e.Graphics;
            using var pen = new Pen(Color.FromArgb(0xEC, 0xEE, 0xF3));
            g.DrawLine(pen, 10, e.Item.Height / 2, e.Item.Width - 10, e.Item.Height / 2);
        }
    }

    private sealed class TrayColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.White;
        public override Color MenuBorder => Color.FromArgb(0xE7, 0xE9, 0xEF);
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Color.FromArgb(0xED, 0xF2, 0xFF);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(0xED, 0xF2, 0xFF);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(0xED, 0xF2, 0xFF);
    }

    public void Dispose()
    {
        // 静态事件必须退订：LocalizationService.LanguageChanged 是 static，
        // 不退订会把本对象（及其持有的 NotifyIcon）永久 root 住。
        LocalizationService.LanguageChanged -= ApplyLanguage;
        _icon.Visible = false;
        _icon.Dispose();
    }
}
