using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Islora.Interop;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace Islora.Services;

/// <summary>
/// 通知实时活动：监听系统 Toast 通知，读取来源 App + 通知正文（发送人/消息），回调展示文本。
///
/// 双模式（自动选择）：
///   • **事件订阅**（首选）：进程拥有 Windows 包身份时（通过 packaging/identity 注册稀疏包），
///     可正常订阅 UserNotificationListener.NotificationChanged —— 实时、省资源。
///   • **轮询回退**：无包身份时该事件订阅会抛 0x80070490 (ERROR_NOT_FOUND)，
///     此时退化为定时轮询 GetNotificationsAsync 对比快照（兼容性兜底）。
/// 外部通过 <see cref="UsesEventSubscription"/> 判断是否需要启动轮询定时器。
/// </summary>
internal sealed class NotificationService : IDisposable
{
    private UserNotificationListener? _listener;
    private HashSet<uint> _seenIds = new();
    private bool _subscribed;

    /// <summary>新通知（参数为展示文本，如 "📩 微信 · 张三：在吗"）。</summary>
    public event Action<string>? NotificationAdded;

    /// <summary>true = 已用官方事件订阅；false = 需要外部定时调用 <see cref="PollAsync"/>。</summary>
    public bool UsesEventSubscription { get; private set; }

    /// <summary>当前进程的包家族名；null 表示无包身份（仅用于诊断显示）。</summary>
    public string? PackageFamilyName { get; private set; }

    public async Task<bool> InitializeAsync()
    {
        PackageFamilyName = NativeMethods.TryGetPackageFamilyName();
        try
        {
            _listener = UserNotificationListener.Current;
            var status = await _listener.RequestAccessAsync();
            if (status != UserNotificationListenerAccessStatus.Allowed)
            {
                _listener = null;
                return false;
            }

            // 有包身份 → 尝试官方事件订阅（实时推送）
            if (PackageFamilyName is not null)
            {
                try
                {
                    _listener.NotificationChanged += OnNotificationChanged;
                    _subscribed = true;
                    UsesEventSubscription = true;
                    return true;
                }
                catch
                {
                    // 订阅失败（系统差异等）→ 继续走轮询兜底
                    _subscribed = false;
                    UsesEventSubscription = false;
                }
            }

            // 无身份 / 订阅失败 → 轮询模式：先记录快照，避免启动时把历史通知全弹出来
            await SnapshotAsync();
            return true;
        }
        catch
        {
            _listener = null;
            return false;
        }
    }

    /// <summary>事件订阅回调（可能在非 UI 线程触发，订阅方需自行封送）。</summary>
    private void OnNotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
    {
        if (args.ChangeKind != UserNotificationChangedKind.Added) return;
        try
        {
            var n = sender.GetNotification(args.UserNotificationId);
            if (n is not null) NotificationAdded?.Invoke(BuildDisplay(n));
        }
        catch
        {
            // 单条通知读取失败，忽略
        }
    }

    /// <summary>轮询一次（仅在非事件订阅模式下使用）：把新增的 Toast 通知回调出去。</summary>
    public async Task PollAsync()
    {
        if (_listener is null || UsesEventSubscription) return;
        try
        {
            var notifs = await _listener.GetNotificationsAsync(NotificationKinds.Toast);

            // 用「本轮实际存在的 ID 集合」替换旧集合：
            // 原来只 Add 不 Remove，集合只增不减；一旦系统回收并复用了某个已消失通知的 ID，
            // 新通知会被误判成「见过」而静默吞掉。
            var currentIds = new HashSet<uint>();
            foreach (var n in notifs)
            {
                // 每条独立 try/catch：某些通知的 AppInfo 访问会抛 NotImplementedException，
                // 不能让它中断整轮遍历。
                try
                {
                    currentIds.Add(n.Id);
                    if (_seenIds.Add(n.Id))
                        NotificationAdded?.Invoke(BuildDisplay(n));
                }
                catch
                {
                    // 跳过无法读取的通知
                }
            }
            _seenIds = currentIds;
        }
        catch
        {
            // 本轮轮询失败，下一轮重试
        }
    }

    /// <summary>仅更新快照，不触发回调（轮询模式初始化时用）。</summary>
    private async Task SnapshotAsync()
    {
        if (_listener is null) return;
        try
        {
            var notifs = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
            foreach (var n in notifs) _seenIds.Add(n.Id);
        }
        catch { }
    }

    private static string BuildDisplay(UserNotification n)
    {
        string appName = "通知";
        try { appName = n.AppInfo?.DisplayInfo?.DisplayName ?? "通知"; }
        catch { /* 某些通知的 AppInfo 抛 NotImplementedException，用默认名 */ }

        var message = ExtractText(n);
        return string.IsNullOrEmpty(message)
            ? $"📩 {appName}"
            : $"📩 {appName} · {message}";
    }

    /// <summary>从 Toast 通用模板读取正文文本（通常 [发送人, 消息]，用 "：" 连接）。失败返回空串。</summary>
    private static string ExtractText(UserNotification n)
    {
        try
        {
            var visual = n.Notification?.Visual;
            if (visual is null) return string.Empty;
            var binding = visual.GetBinding(KnownNotificationBindings.ToastGeneric);
            var texts = binding?.GetTextElements();
            if (texts is null || texts.Count == 0) return string.Empty;
            return string.Join("：", texts);
        }
        catch
        {
            return string.Empty;
        }
    }

    public void Dispose()
    {
        if (_listener is not null && _subscribed)
        {
            try { _listener.NotificationChanged -= OnNotificationChanged; } catch { }
        }
        _subscribed = false;
        _listener = null;
    }
}
