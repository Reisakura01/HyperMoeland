using System;

namespace Islora.Core;

/// <summary>
/// 岛控制器：维护紧凑 / 展开状态，广播状态变化。
/// 实际的窗口动画由 MainWindow 负责（WPF DoubleAnimation）。
/// </summary>
internal sealed class IslandController
{
    public IslandState State { get; private set; } = IslandState.Compact;

    /// <summary>状态变化通知。</summary>
    public event Action<IslandState>? StateChanged;

    public void Toggle()
    {
        State = State == IslandState.Compact ? IslandState.Expanded : IslandState.Compact;
        StateChanged?.Invoke(State);
    }

    public void Collapse()
    {
        if (State == IslandState.Compact) return;
        State = IslandState.Compact;
        StateChanged?.Invoke(State);
    }
}
