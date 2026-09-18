using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using HyperMoeland.Services;

namespace HyperMoeland.Views;

/// <summary>
/// 展开态卡片（小米超级岛样式）：无媒体 → 时钟卡（时间/日期/电量）；
/// 播放中 → 大媒体面板（大封面 + 标题/歌手 + 控制按钮 + 进度条），点击收回。
/// </summary>
public partial class ExpandedCard : UserControl
{
    public event EventHandler? Clicked;
    public event EventHandler? PreviousClicked;
    public event EventHandler? PlayPauseClicked;
    public event EventHandler? NextClicked;
    public event Action<double>? SeekRequested;

    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _notificationTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private readonly DispatcherTimer _neonTimer = new() { Interval = TimeSpan.FromMilliseconds(900) }; // 约66BPM舒缓呼吸
    private readonly UIElement[] _neonLayers; // 每个元素为一种固定配色（已缓存为静态位图）
    private readonly Rectangle[] _specBars;   // 频谱条（6 频段）
    private int _neonIdx;
    private double? _progressRatio;
    private double _currentDuration;
    private bool _liked;
    private bool _audioDriven;                // 霓虹亮度是否正被音频驱动

    public ExpandedCard()
    {
        InitializeComponent();
        _neonLayers = new UIElement[] { NeonLayer0, NeonLayer1, NeonLayer2 };
        _specBars = new[] { SpecBar0, SpecBar1, SpecBar2, SpecBar3, SpecBar4, SpecBar5 };
        AssignNeonBitmaps();
        _clock.Tick += (_, _) => UpdateClock();
        _clock.Start();
        _notificationTimer.Tick += (_, _) => { NotificationText.Visibility = Visibility.Collapsed; _notificationTimer.Stop(); };
        _neonTimer.Tick += (_, _) => PulseNeon();
        ProgressTrack.SizeChanged += (_, _) => ApplyProgressRatio();
        UpdateClock();
        ApplyLanguage();
        LocalizationService.LanguageChanged += ApplyLanguage;
    }

    /// <summary>刷新卡片上所有本地化文字（ToolTip、电量、日期）。</summary>
    private void ApplyLanguage()
    {
        LikeButton.ToolTip = LocalizationService.T("Card.Like");
        PreviousButton.ToolTip = LocalizationService.T("Card.Previous");
        PlayPauseButton.ToolTip = LocalizationService.T("Card.PlayPause");
        NextButton.ToolTip = LocalizationService.T("Card.Next");
        UpdateClock();
    }

    /// <summary>为每个配色层生成一幅"带抖动的霓虹渐变位图"（消除 8bit 渐变带/摩尔纹），并铺满整层。</summary>
    private void AssignNeonBitmaps()
    {
        // 每组配色：中段色 + 底部色（顶部透明，向下增强；半透明渐变位图天然消除 banding）
        var sets = new[]
        {
            (Mid: Color.FromRgb(0x5E, 0xE8, 0xF8), Low: Color.FromRgb(0x9B, 0x74, 0xE8)), // 浅青→淡紫
            (Mid: Color.FromRgb(0xF8, 0x9B, 0xCB), Low: Color.FromRgb(0x9B, 0x74, 0xE8)), // 浅粉→淡紫
            (Mid: Color.FromRgb(0x8B, 0xCB, 0xF8), Low: Color.FromRgb(0x7E, 0xE8, 0xE0)), // 淡蓝→淡青
        };
        // 固定分辨率用 2×（920×500），避免位图被 ImageBrush 拉伸放大时产生重采样带纹
        const int w = 920, h = 500;
        var brushes = new Brush[sets.Length];
        for (int i = 0; i < sets.Length; i++)
            brushes[i] = CreateDitheredNeonBrush(sets[i].Mid, sets[i].Low, w, h);

        NeonLayer0.Background = brushes[0];
        NeonLayer1.Background = brushes[1];
        NeonLayer2.Background = brushes[2];
        foreach (var b in brushes) b.Freeze();
    }

    /// <summary>垂直霓虹渐变（上半透明，下半发光），对 RGB 与 alpha 通道都加噪声打散色带。
    /// 关键：这是半透明层，合成结果 = 霓虹色×alpha + 卡片色×(1−alpha)，因此 alpha 通道的 8bit
    /// 量化步进才是残余 banding/摩尔纹的主因——必须连同 alpha 一起抖动。</summary>
    private static Brush CreateDitheredNeonBrush(Color mid, Color low, int w, int h)
    {
        const int MaxAlpha = 0xD8;                 // 底部最大不透明度
        const int Dither = 8;                      // 抖动幅度（每通道 ±8，肉眼不可见，能打断色带）
        var rnd = new Random(1234);
        var pixels = new byte[w * h * 4];          // BGRA

        for (int y = 0; y < h; y++)
        {
            double t = y / (double)(h - 1);        // 0..1 向下
            // 顶部 40% 透明，向下线性增强
            double aF = t <= 0.4 ? 0 : (t - 0.4) / 0.6;
            int a = (int)Math.Clamp(aF * MaxAlpha, 0, MaxAlpha);
            // 颜色从中段色过渡到低段色
            var c = Lerp(mid, low, t);
            for (int x = 0; x < w; x++)
            {
                int idx = (y * w + x) * 4;
                int dB = rnd.Next(-Dither, Dither + 1);
                int dG = rnd.Next(-Dither, Dither + 1);
                int dR = rnd.Next(-Dither, Dither + 1);
                pixels[idx + 0] = ClampB(c.B + dB);                 // B
                pixels[idx + 1] = ClampB(c.G + dG);                 // G
                pixels[idx + 2] = ClampB(c.R + dR);                 // R
                pixels[idx + 3] = ClampB(a + rnd.Next(-Dither, Dither + 1)); // A：连同 alpha 一起抖动
            }
        }

        var bmp = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, pixels, w * 4);
        bmp.Freeze();
        var brush = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
        brush.Freeze();
        return brush;
    }

    private static Color Lerp(Color a, Color b, double t)
        => Color.FromRgb(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));

    private static byte ClampB(double v) => (byte)Math.Clamp(v, 0, 255);

    /// <summary>播放音乐时开启斜切霓虹渐变背景脉动；停止/无媒体时关闭并淡出。
    /// 若有真实音频数据，亮度改由音频驱动（见 <see cref="SetAudioLevel"/>）。</summary>
    public void SetNeon(bool on)
    {
        if (on)
        {
            NeonBack.Visibility = Visibility.Visible;
            var dur = TimeSpan.FromMilliseconds(Math.Clamp(SettingsService.Current.NeonSpeedMs, 400, 2000));
            _neonTimer.Interval = dur;
            if (!_neonTimer.IsEnabled) _neonTimer.Start();
            if (!_audioDriven) StartNeonBreath(dur);
        }
        else
        {
            _neonTimer.Stop();
            _audioDriven = false;
            var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(250));
            fade.Completed += (_, _) => NeonBack.Visibility = Visibility.Collapsed;
            NeonBack.BeginAnimation(UIElement.OpacityProperty, fade);
        }
    }

    /// <summary>连续平滑呼吸（正弦缓动；无音频数据时的兜底动效）。</summary>
    private void StartNeonBreath(TimeSpan dur)
        => NeonBack.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0.32, 0.42, dur)
            {
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                RepeatBehavior = RepeatBehavior.Forever,
            });

    /// <summary>
    /// 用真实音频电平驱动视觉（约 25fps，由 MainWindow 推送）：
    ///   • 霓虹背景亮度随总体音量起伏（静音时回落并恢复呼吸动效）
    ///   • 6 个频谱条高度随各频段强度变化
    /// </summary>
    public void SetAudioLevel(float level, float[] bands, bool active)
    {
        // 频谱条（即使无音频也让它们收到最低高度，保持视觉稳定）
        for (int i = 0; i < _specBars.Length; i++)
        {
            float v = i < bands.Length ? bands[i] : 0f;
            if (v < 0f) v = 0f;
            if (v > 1f) v = 1f;
            _specBars[i].Height = 2 + v * 16;   // 2 ~ 18px
        }

        if (NeonBack.Visibility != Visibility.Visible) return;

        if (active)
        {
            // 音频驱动：停止呼吸动画，直接按音量设置亮度
            if (!_audioDriven)
            {
                _audioDriven = true;
                NeonBack.BeginAnimation(UIElement.OpacityProperty, null);
            }
            float lv = Math.Clamp(level, 0f, 1f);
            NeonBack.Opacity = 0.30 + lv * 0.45;   // 0.30 ~ 0.75
        }
        else if (_audioDriven)
        {
            // 声音停止：恢复呼吸动效
            _audioDriven = false;
            StartNeonBreath(TimeSpan.FromMilliseconds(
                Math.Clamp(SettingsService.Current.NeonSpeedMs, 400, 2000)));
        }
    }

    /// <summary>每种节拍：在固定配色层之间交叉淡入淡出。交叉时长固定为 350ms 的短窗口，
    /// 而非整拍间隔，以最小化两层半透明位图同时叠合的时间（避免残余色带/摩尔纹）。</summary>
    private void PulseNeon()
    {
        var next = (_neonIdx + 1) % _neonLayers.Length;
        var cur = _neonIdx;
        _neonIdx = next;
        var dur = TimeSpan.FromMilliseconds(350);
        var smooth = new SineEase { EasingMode = EasingMode.EaseInOut };
        // 旧配色层淡出，新配色层淡入
        _neonLayers[cur].BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, dur) { EasingFunction = smooth });
        _neonLayers[next].BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(1, dur) { EasingFunction = smooth });
    }

    /// <summary>设置媒体信息（null/空白表示无媒体）。</summary>
    public void SetMedia(string? title, string? artist = null, BitmapImage? cover = null, bool isPlaying = false)
    {
        bool hasMedia = !string.IsNullOrWhiteSpace(title);
        MediaArea.Visibility = hasMedia ? Visibility.Visible : Visibility.Collapsed;
        ClockView.Visibility = hasMedia ? Visibility.Collapsed : Visibility.Visible;

        if (hasMedia)
        {
            MediaTitle.Text = title;
            bool hasArtist = !string.IsNullOrWhiteSpace(artist);
            MediaArtist.Visibility = hasArtist ? Visibility.Visible : Visibility.Collapsed;
            MediaArtist.Text = artist ?? string.Empty;
            CoverImage.Source = cover;
            PlayPauseIcon.Data = (Geometry)FindResource(isPlaying ? "PausePath" : "PlayPath");
        }
        else
        {
            MediaTitle.Text = string.Empty;
            MediaArtist.Text = string.Empty;
            MediaArtist.Visibility = Visibility.Collapsed;
            CoverImage.Source = null;
            PlayPauseIcon.Data = (Geometry)FindResource("PausePath");
        }
    }

    /// <summary>设置播放进度（秒）：有总时长才显示整行进度（含时间）；无时间轴数据时整行隐藏。</summary>
    public void SetProgress(double? position, double? duration)
    {
        double dur = duration ?? 0;
        if (dur <= 0)
        {
            ProgressRow.Visibility = Visibility.Collapsed;
            _progressRatio = null;
            MediaProgressFill.Width = 0;
            return;
        }

        double pos = Math.Clamp(position ?? 0, 0, dur);
        _currentDuration = dur;
        ProgressRow.Visibility = Visibility.Visible;
        ProgressTrack.Visibility = Visibility.Visible;   // 关键：显示中间的进度线（之前漏了这条）
        _progressRatio = Math.Clamp(pos / dur, 0, 1);
        ApplyProgressRatio();
        MediaTimePos.Text = FormatTime(pos);
        MediaTimeDur.Text = FormatTime(dur);
    }

    /// <summary>设置歌词显示（当前行 / 翻译 / 下一行）；无歌词时隐藏整块。</summary>
    public void SetLyrics(string? current, string? next, string? translation)
    {
        bool has = !string.IsNullOrWhiteSpace(current) || !string.IsNullOrWhiteSpace(next);
        LyricsArea.Visibility = has ? Visibility.Visible : Visibility.Collapsed;
        if (!has) return;

        LyricCurrent.Text = current ?? string.Empty;
        LyricNext.Text = string.IsNullOrWhiteSpace(next) ? string.Empty : next;

        bool hasTranslation = !string.IsNullOrWhiteSpace(translation);
        LyricTranslation.Visibility = hasTranslation ? Visibility.Visible : Visibility.Collapsed;
        LyricTranslation.Text = translation ?? string.Empty;
    }

    /// <summary>点击进度条跳转：按点击位置换算成秒，触发 SeekRequested。</summary>
    private void OnProgressTrackMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true; // 不冒泡到根，避免触发"点击卡片收回"
        if (_currentDuration <= 0 || ProgressTrack.ActualWidth <= 0) return;
        var x = e.GetPosition(ProgressTrack).X;
        var ratio = Math.Clamp(x / ProgressTrack.ActualWidth, 0, 1);
        SeekRequested?.Invoke(ratio * _currentDuration);
    }

    private static string FormatTime(double sec)
    {
        if (sec < 0) sec = 0;
        var ts = TimeSpan.FromSeconds(sec);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}"
            : $"{ts.Minutes:00}:{ts.Seconds:00}";
    }

    private void ApplyProgressRatio()
    {
        if (_progressRatio is null) return;
        var trackW = ProgressTrack.ActualWidth;
        MediaProgressFill.Width = trackW > 0 ? trackW * _progressRatio.Value : 0;
    }

    /// <summary>短暂显示一条通知（5 秒后自动消失；播放媒体时忽略通知）。</summary>
    public void ShowNotification(string text)
    {
        if (MediaArea.Visibility == Visibility.Visible) return;
        NotificationText.Text = text;
        NotificationText.Visibility = Visibility.Visible;
        _notificationTimer.Stop();
        _notificationTimer.Start();
    }

    /// <summary>设置电量显示。</summary>
    public void SetBattery(double? percent)
        => BatteryText.Text = percent is double p
            ? LocalizationService.T("Card.Battery", $"{p:0}")
            : LocalizationService.T("Card.BatteryUnknown");

    private bool _charging;

    /// <summary>插拔电指示：插电时闪电显示并常驻柔和呼吸；拔电时隐藏。
    /// 基础状态（Visible + Opacity=1 + Scale=1）直接设好，动画只叠加亮度呼吸，绝不改变"是否可见"。</summary>
    public void SetPowerState(bool charging)
    {
        _charging = charging;
        if (charging)
        {
            ChargingIcon.BeginAnimation(UIElement.OpacityProperty, null);
            ChargingIcon.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            ChargingIcon.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            ChargingScale.ScaleX = 1;
            ChargingScale.ScaleY = 1;
            ChargingIcon.Opacity = 1;
            ChargingIcon.Visibility = Visibility.Visible;
            StartChargingBreath();
        }
        else
        {
            ChargingIcon.BeginAnimation(UIElement.OpacityProperty, null);
            ChargingIcon.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            ChargingIcon.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            ChargingScale.ScaleX = 1;
            ChargingScale.ScaleY = 1;
            ChargingIcon.Opacity = 1;
            ChargingIcon.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>常驻充电状态：闪电做柔和呼吸（0.6↔1.0），表示正在充电。</summary>
    private void StartChargingBreath()
    {
        // 若已拔电则不再启动呼吸
        if (!_charging || ChargingIcon.Visibility != Visibility.Visible) return;
        ChargingIcon.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0.6, 1.0, TimeSpan.FromMilliseconds(900))
            {
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                RepeatBehavior = RepeatBehavior.Forever,
            });
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        TimeText.Text = now.ToString("HH:mm:ss");
        // 日期按语言：中文"2026年9月2日 星期三"，英文"Wed, Sep 2, 2026"（缩写，避免溢出）
        DateText.Text = LocalizationService.Current == Models.AppLanguage.Chinese
            ? now.ToString("yyyy年M月d日 dddd")
            : now.ToString("ddd, MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture);
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 点媒体控制按钮（切歌/播放暂停）不算"点击卡片收回"
        if (e.OriginalSource is DependencyObject src && IsInside(src, MediaControls))
            return;
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsInside(DependencyObject node, DependencyObject container)
    {
        for (var n = node; n is not null; n = VisualTreeHelper.GetParent(n))
        {
            if (ReferenceEquals(n, container)) return true;
        }
        return false;
    }

    private void OnPreviousClicked(object sender, RoutedEventArgs e)
        => PreviousClicked?.Invoke(this, EventArgs.Empty);

    private void OnPlayPauseClicked(object sender, RoutedEventArgs e)
        => PlayPauseClicked?.Invoke(this, EventArgs.Empty);

    private void OnNextClicked(object sender, RoutedEventArgs e)
        => NextClicked?.Invoke(this, EventArgs.Empty);

    /// <summary>喜欢/取消喜欢（仅本地视觉切换；Windows SMTC 没有"喜欢"API）。</summary>
    private void OnLikeClicked(object sender, RoutedEventArgs e)
    {
        _liked = !_liked;
        LikeIcon.Fill = _liked
            ? (Brush)FindResource("Theme.Foreground")
            : new SolidColorBrush(Color.FromArgb(0x80, 0x80, 0x80, 0x80));
    }
}
