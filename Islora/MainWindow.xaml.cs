using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Storage.Streams;
using Islora.Core;
using Islora.Interop;
using Islora.Models;
using Islora.Services;
using Islora.Theme;
using Islora.Views;

namespace Islora;

/// <summary>
/// Islora主窗口：透明、置顶、不抢焦点、不进任务栏。
/// 胶囊可拖拽（松手后吸附 左/中/右），点击展开成卡片；
/// 日间浅色云母 / 夜间深色云母自动切换；全屏自动隐藏；多显示器跟随。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>区分点击与拖拽的位移阈值（逻辑像素）。</summary>
    private const double DragThreshold = 4.0;

    private readonly IslandController _controller = new();
    private readonly ThemeScheduler _themeScheduler = new();
    private readonly MediaService _media = new();
    private readonly NotificationService _notifications = new();
    private readonly BatteryService _battery = new();
    private readonly ForegroundWatcher _foreground = new();
    private readonly DispatcherTimer _progressTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer _topmostTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _notificationTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _audioTimer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly AudioService _audio = new();
    private readonly float[] _audioBands = new float[6];
    private bool _audioStarted;
    private readonly SystemMonitorService _systemMonitor = new();
    private readonly DispatcherTimer _widgetTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private TrayIcon? _tray;
    private GlobalMouseHook? _mouseHook;
    private bool _fullscreen;
    private bool _mediaActive;
    private int _mediaSeq;

    // 拖拽状态（系统级拖拽：位移超阈值后交给系统标题栏拖拽）
    private bool _systemDragging;
    private Point _dragStartInWindow;

    public MainWindow()
    {
        InitializeComponent();

        _controller.StateChanged += OnStateChanged;
        _themeScheduler.ThemeChanged += OnThemeChanged;
        _media.SessionChanged += OnMediaChanged;
        _battery.ChargePercentChanged += OnBatteryChanged;
        _battery.PowerStateChanged += OnPowerStateChanged;
        _notifications.NotificationAdded += OnNotificationAdded;
        _foreground.FullscreenChanged += OnFullscreenChanged;
        _foreground.MonitorChanged += OnMonitorChanged;

        Card.Clicked += (_, _) => _controller.Collapse();
        Card.PlayPauseClicked += async (_, _) => await _media.TogglePlayPauseAsync();
        Card.NextClicked += async (_, _) => await _media.SkipNextAsync();
        Card.PreviousClicked += async (_, _) => await _media.SkipPreviousAsync();
        _progressTimer.Tick += (_, _) => UpdateProgress();
        Card.SeekRequested += async (sec) => await _media.SeekAsync(sec);

        // 胶囊：按下=可能拖拽，抬起=未拖动则视为点击展开
        Pill.PreviewMouseLeftButtonDown += OnIslandMouseDown;
        Pill.PreviewMouseMove += OnIslandMouseMove;
        Pill.PreviewMouseLeftButtonUp += OnIslandMouseUp;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // 应用云母 + 圆角 + 不抢焦点（此时窗口句柄已创建）
        MicaController.Apply(this, ThemeManager.Current == AppTheme.Night);

        // 监听 WM_EXITSIZEMOVE：系统拖拽结束后吸附到边缘
        var hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);

        // 初始定位到紧凑胶囊（无动画）
        PositionCompact(animate: false);

        // 应用已保存的主题设置
        ApplyThemeSettings();

        // 保持始终置顶：周期性强制置于 Z 序最顶层（不抢焦点、不改变位置尺寸）。
        // 全屏隐藏时不置顶，避免与 Hide() 冲突。
        _topmostTimer.Tick += (_, _) => { if (!_fullscreen) NativeMethods.KeepTopmost(hwnd); };
        _topmostTimer.Start();

        // 启动各服务
        _themeScheduler.Start();
        _battery.Start();
        _foreground.Start();
        _tray = new TrayIcon();
        _tray.OpenSettings += OpenSettingsWindow;
        _tray.TestNotification += () => OnNotificationAdded(LocalizationService.T("Notif.Test"));
        _mouseHook = new GlobalMouseHook();
        _mouseHook.LeftButtonDown += OnGlobalLeftDown;
        _progressTimer.Start();
        _audioTimer.Tick += (_, _) => UpdateAudio();
        _audioTimer.Start();
        // 系统小组件：每秒采样一次 CPU / 内存（展开卡片显示）
        _systemMonitor.Changed += (cpu, mem) => Card.SetWidgets(cpu, mem, _systemMonitor.MemoryUsedGb, _systemMonitor.MemoryTotalGb);
        _widgetTimer.Tick += (_, _) => _systemMonitor.Poll();
        ApplyWidgetSettings();
        _notificationTimer.Tick += async (_, _) => await _notifications.PollAsync();
        // 注意：轮询定时器不在这里启动——等通知服务初始化后，
        // 仅在「无包身份 → 轮询回退」模式下才启动（有身份时用事件订阅，无需轮询）。

        if (SettingsService.Current.AutoUpdate)
            _ = CheckForUpdatesAsync();   // 启动后检查 GitHub 是否有新版本

        _ = InitializeWinRtServicesAsync();
    }

    /// <summary>把音频频谱推给卡片（约 25fps）：驱动霓虹亮度 + 频谱条。</summary>
    private void UpdateAudio()
    {
        if (!_audio.IsCapturing && !_audioStarted)
        {
            _audio.Start();
            _audioStarted = true;
        }
        int n = _audio.CopyBands(_audioBands);
        Card.SetAudioLevel(n > 0 ? _audio.Level : 0f, _audioBands, _audio.IsActive);
    }

    private async System.Threading.Tasks.Task InitializeWinRtServicesAsync()
    {
        // 各自独立 try/catch：媒体初始化失败不影响通知（反之亦然）
        try { await _media.InitializeAsync(); } catch { }

        // 通知：有包身份时用官方事件订阅；否则回退轮询。
        // 授权失败时提示用户去系统设置开启"通知访问权限"。
        try
        {
            bool ok = await _notifications.InitializeAsync();
            if (!ok && _tray is not null)
                _tray.ShowUpdate(LocalizationService.T("Notif.PermissionHint"), string.Empty);
            else if (ok && !_notifications.UsesEventSubscription)
                _notificationTimer.Start();   // 仅轮询模式需要定时器
        }
        catch { }
    }

    /// <summary>应用已保存的主题设置（模式 + 日夜间小时）。</summary>
    private void ApplyThemeSettings()
    {
        var s = SettingsService.Current;
        _themeScheduler.Mode = s.ThemeMode;
        _themeScheduler.DayStartHour = Math.Clamp(s.DayStartHour, 0, 23);
        _themeScheduler.NightStartHour = Math.Clamp(s.NightStartHour, 0, 23);
        _themeScheduler.ApplyNow();
    }

    /// <summary>应用系统小组件设置：显示时启动每秒采样（先采一次建立 CPU 基准）。</summary>
    private void ApplyWidgetSettings()
    {
        bool show = SettingsService.Current.ShowSystemWidgets;
        Card.SetWidgetsVisible(show);
        if (show)
        {
            if (!_widgetTimer.IsEnabled)
            {
                _systemMonitor.Poll();
                _widgetTimer.Start();
            }
        }
        else
        {
            _widgetTimer.Stop();
        }
    }

    /// <summary>打开设置窗口；保存后重新应用设置。</summary>
    private void OpenSettingsWindow()
    {
        var win = new SettingsWindow { Owner = this };
        if (win.ShowDialog() == true)
        {
            ApplyThemeSettings();
            ApplyWidgetSettings();
            AutoStart.Set(SettingsService.Current.AutoStart);
        }
    }

    /// <summary>启动后检查 GitHub 是否有新版本，有新版本则托盘气泡提示。</summary>
    private async System.Threading.Tasks.Task CheckForUpdatesAsync()
    {
        var upd = await UpdateChecker.CheckAsync();
        if (upd is null || _tray is null) return;
        _tray.ShowUpdate(LocalizationService.T("Tray.UpdateBalloon", upd.Value.Version), upd.Value.Url);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _mouseHook?.Dispose();
        _tray?.Dispose();
        _media.Dispose();
        _notifications.Dispose();
        _battery.Dispose();
        _foreground.Stop();
        _audio.Dispose();
    }

    // ---- 拖拽与点击 ----

    private void OnIslandMouseDown(object sender, MouseButtonEventArgs e)
    {
        _systemDragging = false;
        _dragStartInWindow = e.GetPosition(this);
    }

    private void OnIslandMouseMove(object sender, MouseEventArgs e)
    {
        // 系统已接管拖拽，或左键未按下：直接返回
        if (_systemDragging || e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(this);
        var dx = pos.X - _dragStartInWindow.X;
        var dy = pos.Y - _dragStartInWindow.Y;

        // 位移小于阈值视为点击，不进入拖拽
        if (Math.Abs(dx) < DragThreshold && Math.Abs(dy) < DragThreshold) return;

        // 超过阈值：交给系统标题栏拖拽（透明窗口上 CaptureMouse 不可靠）
        _systemDragging = true;
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(hwnd, NativeMethods.WM_NCLBUTTONDOWN, (IntPtr)NativeMethods.HTCAPTION, IntPtr.Zero);
    }

    private void OnIslandMouseUp(object sender, MouseButtonEventArgs e)
    {
        // 系统拖拽中：松手由 WM_EXITSIZEMOVE 处理吸附
        if (_systemDragging) return;
        _controller.Toggle(); // 未拖动 = 点击展开
    }

    /// <summary>窗口消息钩子：系统拖拽结束（WM_EXITSIZEMOVE）后吸附到 左/中/右。</summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_EXITSIZEMOVE && _systemDragging)
        {
            _systemDragging = false;
            SnapToEdges();
        }
        return IntPtr.Zero;
    }

    /// <summary>拖拽结束吸附：顶部对齐，水平方向吸到 左/中/右 最近一侧。</summary>
    private void SnapToEdges()
    {
        var work = MonitorHelper.GetPrimaryWorkArea();
        double scale = DpiScale;
        double workX = work.X / scale;
        double workW = work.Width / scale;
        double workY = work.Y / scale;

        double[] xs =
        {
            workX + 8,                                   // 左
            workX + (workW - Width) / 2,                 // 中
            workX + workW - Width - 8,                   // 右
        };
        double targetX = xs.OrderBy(x => Math.Abs(x - Left)).First();

        // 平滑缓动（比 QuinticEase 温和，且是标准 EasingFunction 能可靠缩放透明窗口）
        var ease = new SineEase { EasingMode = EasingMode.EaseOut };
        BeginAnimation(LeftProperty, new DoubleAnimation { To = targetX, Duration = TimeSpan.FromMilliseconds(180), EasingFunction = ease, FillBehavior = FillBehavior.HoldEnd });
        BeginAnimation(TopProperty, new DoubleAnimation { To = workY, Duration = TimeSpan.FromMilliseconds(180), EasingFunction = ease, FillBehavior = FillBehavior.HoldEnd });
    }

    // ---- 主题 / 服务事件 ----

    private void OnThemeChanged(AppTheme theme)
        => MicaController.SetTheme(this, theme == AppTheme.Night);

    /// <summary>媒体会话变化（SMTC 事件可能在非 UI 线程触发）：先封送到 UI 线程再处理。</summary>
    private void OnMediaChanged(MediaSessionInfo? info)
        => Dispatcher.InvokeAsync(() => OnMediaChangedOnUi(info));

    private async void OnMediaChangedOnUi(MediaSessionInfo? info)
    {
        int seq = ++_mediaSeq;   // 会话序号：用于丢弃旧会话迟到的封面
        bool hasMedia = info is not null && !string.IsNullOrWhiteSpace(info.Title);
        _mediaActive = hasMedia;

        if (!hasMedia)
        {
            Pill.SetMedia(null);
            Card.SetMedia(null);
            Card.SetNeon(false);
        }
        else
        {
            var title = info!.Title;
            var artist = info.Artist;
            var text = string.IsNullOrWhiteSpace(artist) ? title : $"{title} · {artist}";

            // 先立即显示标题（封面流读取可能卡住，不能阻塞标题显示）
            Pill.SetMedia(text, null);
            Card.SetMedia(title, artist, null, info.IsPlaying);
            Card.SetNeon(info.IsPlaying);

            // 封面异步单独加载，拿到后再补上；若期间已切到别的媒体，丢弃旧封面
            var cover = await LoadCoverAsync(info.Thumbnail);
            if (seq != _mediaSeq) return;
            if (cover is not null)
            {
                Pill.SetMedia(text, cover);
                Card.SetMedia(title, artist, cover, info.IsPlaying);
            }
        }

        // 展开状态下，媒体有无会改变卡片尺寸，需要重新定位
        if (_controller.State == IslandState.Expanded && !_fullscreen)
            PositionExpanded();
    }

    /// <summary>把 WinRT 封面流解码成 WPF BitmapImage（失败返回 null，不影响主流程）。</summary>
    private static async System.Threading.Tasks.Task<BitmapImage?> LoadCoverAsync(IRandomAccessStreamReference? thumb)
    {
        if (thumb is null) return null;
        try
        {
            using var ras = await thumb.OpenReadAsync();
            using var ms = new MemoryStream();
            await ras.AsStreamForRead().CopyToAsync(ms);
            ms.Position = 0;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>电量变化（WinRT 事件可能在非 UI 线程触发）：封送到 UI 线程再更新。</summary>
    private void OnBatteryChanged(double percent)
        => Dispatcher.InvokeAsync(() => Card.SetBattery(percent));

    /// <summary>插拔电状态变化（WinRT 事件可能在非 UI 线程触发）：封送到 UI 线程，卡片与胶囊同步指示。</summary>
    private void OnPowerStateChanged(bool charging)
        => Dispatcher.InvokeAsync(() =>
        {
            Card.SetPowerState(charging);
            Pill.SetPowerState(charging);
        });

    /// <summary>每 500ms 把媒体播放进度推给卡片（系统媒体时间轴）。</summary>
    private void UpdateProgress()
    {
        var p = _media.GetProgress();
        if (p is null)
            Card.SetProgress(null, null);
        else
            Card.SetProgress(p.Value.Position, p.Value.Duration);
    }

    /// <summary>新通知（WinRT 事件可能在非 UI 线程触发）：封送到 UI 线程，胶囊和卡片同步显示。</summary>
    private void OnNotificationAdded(string text)
        => Dispatcher.InvokeAsync(() =>
        {
            Pill.ShowNotification(text);
            Card.ShowNotification(text);
        });

    private void OnFullscreenChanged(bool fullscreen)
    {
        _fullscreen = fullscreen;
        if (fullscreen) Hide();
        else Show();
    }

    /// <summary>全局左键按下：展开态下点在岛窗口外任意处 → 自动缩回胶囊。</summary>
    private void OnGlobalLeftDown(System.Drawing.Point p)
    {
        if (_controller.State != IslandState.Expanded || _fullscreen) return;
        if (IsPointInsideWindow(p.X, p.Y)) return; // 在岛内：交给岛自己处理（控件/点击）
        _controller.Collapse();
    }

    /// <summary>指定屏幕物理坐标是否落在岛窗口内。</summary>
    private bool IsPointInsideWindow(int x, int y)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.GetWindowRect(hwnd, out var r);
        return x >= r.Left && x <= r.Right && y >= r.Top && y <= r.Bottom;
    }

    private void OnMonitorChanged(WorkArea work)
    {
        if (_fullscreen) return;
        Reposition(work);
    }

    private void OnStateChanged(IslandState state)
    {
        bool expanding = state == IslandState.Expanded;
        Card.Visibility = expanding ? Visibility.Visible : Visibility.Collapsed;
        Pill.Visibility = expanding ? Visibility.Collapsed : Visibility.Visible;

        if (expanding) PositionExpanded();
        else PositionCompact();

        // 尺寸瞬间定位（不缩放透明窗口），改做内容快速淡入，既可靠又顺滑
        var target = expanding ? (System.Windows.UIElement)Card : (System.Windows.UIElement)Pill;
        target.Opacity = 0;
        target.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(1, TimeSpan.FromMilliseconds(150))
            { EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut } });
    }

    // ---- 定位（DPI 感知） ----

    /// <summary>当前窗口 DPI 缩放（物理像素 → 逻辑像素的除数，150% 缩放时 = 1.5）。</summary>
    private double DpiScale
        => NativeMethods.GetDpiForWindow(new WindowInteropHelper(this).Handle) / 96.0;

    /// <summary>按当前状态 + 是否有媒体计算窗口尺寸（音乐面板比时钟卡大很多）。</summary>
    private (double W, double H) SizeFor()
        => _controller.State == IslandState.Expanded
            ? (_mediaActive
                ? (IslandMetrics.MediaExpandedWidth, IslandMetrics.MediaExpandedHeight)
                : (IslandMetrics.ExpandedWidth, IslandMetrics.ExpandedHeight))
            : (IslandMetrics.CompactWidth, IslandMetrics.CompactHeight);

    private void Reposition(WorkArea work)
    {
        var (w, h) = SizeFor();
        AnimateTo(work, w, h, animate: true);
    }

    private void PositionCompact(bool animate = true)
    {
        var (w, h) = SizeFor();
        AnimateTo(MonitorHelper.GetPrimaryWorkArea(), w, h, animate);
    }

    private void PositionExpanded(bool animate = true)
    {
        var (w, h) = SizeFor();
        AnimateTo(MonitorHelper.GetPrimaryWorkArea(), w, h, animate);
    }

    private void AnimateTo(WorkArea work, double width, double height, bool animate)
    {
        // 关键：工作区来自 GetMonitorInfo（物理像素），而 WPF 的
        // Left/Top/Width/Height 是逻辑像素（DIP）。必须按 DPI 换算，
        // 否则高缩放屏幕下窗口会偏到右侧（表现为"卡在右上角"）。
        double scale = DpiScale;
        double workX = work.X / scale;
        double workW = work.Width / scale;
        double workY = work.Y / scale;

        double left = workX + (workW - width) / 2;
        double top = workY;

        // 直接设置尺寸/位置：透明窗口的尺寸动画既卡又不稳（缩回易卡住），
        // 故不用 BeginAnimation，改为瞬间定位，由 OnStateChanged 做内容淡入。
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }
}

