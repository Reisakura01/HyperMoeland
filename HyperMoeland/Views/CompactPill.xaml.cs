using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace HyperMoeland.Views;

/// <summary>
/// 紧凑态胶囊（小米超级岛风格）：无媒体显示时间；播放音乐显示迷你封面+歌名；
/// 来新通知时暂时显示通知（发送人/消息），5 秒后复原（回到媒体或时钟）；
/// 插电时在左侧显示 ⚡ 并做柔和呼吸，提示正在充电。
/// </summary>
public partial class CompactPill : UserControl
{
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _notificationTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private string? _mediaText;
    private string? _notificationText;
    private ImageSource? _cover;
    private bool _charging;

    public CompactPill()
    {
        InitializeComponent();
        _clock.Tick += (_, _) => Update();
        _clock.Start();
        _notificationTimer.Tick += (_, _) => { _notificationText = null; Update(); };
        Update();
    }

    /// <summary>插拔电指示：插电时闪电显示并常驻柔和呼吸；拔电时隐藏。
    /// 关键：基础状态（Visible + Opacity=1 + Scale=1）直接以固定值设好，动画绝不改变"是否可见"，
    /// 只叠加一个不透明度的缓慢呼吸，避免任何动画把图标缩成 0 或透明到看不见。</summary>
    public void SetPowerState(bool charging)
    {
        _charging = charging;
        if (charging)
        {
            // 归位基础状态：取消旧动画，固定为可见/不透明/scale=1
            ChargingIcon.BeginAnimation(OpacityProperty, null);
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
            ChargingIcon.BeginAnimation(OpacityProperty, null);
            ChargingIcon.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            ChargingIcon.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            ChargingScale.ScaleX = 1;
            ChargingScale.ScaleY = 1;
            ChargingIcon.Opacity = 1;
            ChargingIcon.Visibility = Visibility.Collapsed;
        }
        Update();
    }

    /// <summary>常驻充电状态：闪电做柔和呼吸（0.6↔1.0），表示正在充电。仅调不透明度，不影响可见性。</summary>
    private void StartChargingBreath()
    {
        // 若已拔电则不再启动呼吸
        if (!_charging || ChargingIcon.Visibility != Visibility.Visible) return;
        ChargingIcon.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0.6, 1.0, TimeSpan.FromMilliseconds(900))
            {
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                RepeatBehavior = RepeatBehavior.Forever,
            });
    }

    /// <summary>设置媒体活动（null 表示无媒体，回到时钟；cover 为迷你专辑封面，可空）。</summary>
    public void SetMedia(string? text, ImageSource? cover = null)
    {
        _mediaText = text;
        _cover = cover;
        if (_notificationText is null) Update(); // 正在显示通知时不打断
    }

    /// <summary>来新通知：胶囊暂时改为显示通知内容，5 秒后复原。</summary>
    public void ShowNotification(string text)
    {
        _notificationText = text;
        _notificationTimer.Stop();
        _notificationTimer.Start();
        Update();
    }

    private void Update()
    {
        // 优先级：通知 > 媒体 > 时钟
        if (_notificationText is not null)
        {
            CoverBorder.Visibility = Visibility.Collapsed;
            CoverImage.Source = null;
            ActivityIcon.Visibility = Visibility.Visible;
            ActivityIcon.Text = "📩";
            TitleText.Text = _notificationText;
            return;
        }

        if (_mediaText is not null)
        {
            bool hasCover = _cover is not null;
            CoverBorder.Visibility = hasCover ? Visibility.Visible : Visibility.Collapsed;
            CoverImage.Source = _cover;
            ActivityIcon.Visibility = hasCover ? Visibility.Collapsed : Visibility.Visible;
            ActivityIcon.Text = "🎵";
            TitleText.Text = _mediaText;
        }
        else
        {
            CoverBorder.Visibility = Visibility.Collapsed;
            CoverImage.Source = null;
            ActivityIcon.Visibility = Visibility.Collapsed; // 无媒体时不显示小鲸鱼图标
            TitleText.Text = DateTime.Now.ToString("HH:mm:ss");
        }
    }
}
