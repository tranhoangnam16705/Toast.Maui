namespace Toast.Maui.Toasts;

public interface IToastService
{
    Task ShowAsync(ToastOptions options);

    void ShowMessageSuccess(string message, double duration = 3, ToastPosition position = ToastPosition.Top);

    void ShowMessageError(string message, double duration = 3, ToastPosition position = ToastPosition.Top);

    void ShowMessageWarning(string message, double duration = 3, ToastPosition position = ToastPosition.Top);

    void ShowMessageInfo(string message, double duration = 3, ToastPosition position = ToastPosition.Top);

    void DismissCurrent();

    void DismissAll();
}