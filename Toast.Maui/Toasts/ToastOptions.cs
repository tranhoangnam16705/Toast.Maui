namespace Toast.Maui.Toasts;

public sealed class ToastOptions
{
    public string Message { get; set; } = string.Empty;

    public ToastType Type { get; set; } = ToastType.Info;

    public double Duration { get; set; } = 3;

    public ToastPosition Position { get; set; } = ToastPosition.Top;

    public string? ActionText { get; set; }

    public Action? OnAction { get; set; }

    public bool SwipeToDismiss { get; set; } = true;

    public bool PauseOnTouch { get; set; } = true;

    public bool DismissOnTap { get; set; } = true;
}
