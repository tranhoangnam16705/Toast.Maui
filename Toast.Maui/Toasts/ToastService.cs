namespace Toast.Maui.Toasts;

public sealed class ToastService : IToastService
{
    public Task ShowAsync(ToastOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return NativeToast.ShowAsync(options);
    }

    public void ShowMessageSuccess(string message, double duration = 3, ToastPosition position = ToastPosition.Top)
        => ShowAsync(new ToastOptions { Message = message, Type = ToastType.Success, Duration = duration, Position = position });

    public void ShowMessageError(string message, double duration = 3, ToastPosition position = ToastPosition.Top)
        => ShowAsync(new ToastOptions { Message = message, Type = ToastType.Error, Duration = duration, Position = position });

    public void ShowMessageWarning(string message, double duration = 3, ToastPosition position = ToastPosition.Top)
        => ShowAsync(new ToastOptions { Message = message, Type = ToastType.Warning, Duration = duration, Position = position });

    public void ShowMessageInfo(string message, double duration = 3, ToastPosition position = ToastPosition.Top)
        => ShowAsync(new ToastOptions { Message = message, Type = ToastType.Info, Duration = duration, Position = position });

    public void DismissCurrent() => NativeToast.DismissCurrent();

    public void DismissAll() => NativeToast.DismissAll();
}