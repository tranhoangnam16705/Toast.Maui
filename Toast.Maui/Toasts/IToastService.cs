namespace Toast.Maui.Toasts;

public interface IToastService
{
    Task ShowAsync(ToastOptions options);

    void ShowMessageSuccess(string message, double duration = 3, ToastPosition position = ToastPosition.Bottom);

    void ShowMessageError(string message, double duration = 3, ToastPosition position = ToastPosition.Bottom);

    void ShowMessageWarning(string message, double duration = 3, ToastPosition position = ToastPosition.Bottom);

    void ShowMessageInfo(string message, double duration = 3, ToastPosition position = ToastPosition.Bottom);

    void DismissCurrent();

    void DismissAll();
}