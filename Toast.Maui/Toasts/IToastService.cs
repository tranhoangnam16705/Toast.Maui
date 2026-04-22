namespace Toast.Maui.Toasts;

public interface IToastService
{
    Task ShowAsync(ToastOptions options);

    Task ShowSuccessAsync(string message, double duration = 3, ToastPosition position = ToastPosition.Top);

    Task ShowErrorAsync(string message, double duration = 3, ToastPosition position = ToastPosition.Top);

    Task ShowWarningAsync(string message, double duration = 3, ToastPosition position = ToastPosition.Top);

    Task ShowInfoAsync(string message, double duration = 3, ToastPosition position = ToastPosition.Top);

    void DismissCurrent();

    void DismissAll();
}
