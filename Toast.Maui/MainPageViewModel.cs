using Mopups.Pages;
using Mopups.Services;
using System.Windows.Input;
using Toast.Maui.Toasts;

namespace Toast.Maui;

public sealed class MainPageViewModel
{
    private readonly IToastService _toast;

    public MainPageViewModel(IToastService toast)
    {
        _toast = toast;

        ShowSuccessCommand = new Command(async () =>
            await _toast.ShowSuccessAsync("Lưu thành công ✓"));

        ShowErrorCommand = new Command(async () =>
            await _toast.ShowErrorAsync("Không thể kết nối tới máy chủ", 4, ToastPosition.Bottom));

        ShowWarningCommand = new Command(async () =>
            await _toast.ShowWarningAsync("Pin thiết bị yếu", 3, ToastPosition.Center));

        ShowInfoCommand = new Command(async () =>
            await _toast.ShowInfoAsync("Bạn có 3 tin nhắn mới"));

        ShowActionCommand = new Command(async () =>
            await _toast.ShowAsync(new ToastOptions
            {
                Message = "Đã xoá 1 mục",
                Type = ToastType.Success,
                Duration = 5,
                Position = ToastPosition.Top,
                ActionText = "Undo",
                OnAction = async () =>
                    await _toast.ShowInfoAsync("Đã hoàn tác", 2, ToastPosition.Bottom),
            }));

        ShowQueueCommand = new Command(() =>
        {
            _ = _toast.ShowInfoAsync("Toast #1 — top stack", 4);
            _ = _toast.ShowSuccessAsync("Toast #2 — top stack", 4);
            _ = _toast.ShowWarningAsync("Toast #3 — bottom stack", 4, ToastPosition.Bottom);
            _ = _toast.ShowErrorAsync("Toast #4 — bottom stack", 4, ToastPosition.Bottom);
        });

        ShowPopupCommand = new Command(async () =>
        {
            await MopupService.Instance.PushAsync(new PopupPageSample(_toast));
        });
    }

    public ICommand ShowSuccessCommand { get; }

    public ICommand ShowErrorCommand { get; }

    public ICommand ShowWarningCommand { get; }

    public ICommand ShowInfoCommand { get; }

    public ICommand ShowActionCommand { get; }

    public ICommand ShowQueueCommand { get; }

    public ICommand ShowPopupCommand { get; }
    
}
