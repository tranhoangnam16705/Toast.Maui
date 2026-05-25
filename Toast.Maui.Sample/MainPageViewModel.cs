using Mopups.Services;
using System.Windows.Input;
using Toast.Maui.Toasts;

namespace Toast.Maui.Sample;

public sealed class MainPageViewModel
{
    private readonly IToastService _toast;
    public INavigation _naviagtion;

    public MainPageViewModel(IToastService toast)
    {
        _toast = toast;

        ShowSuccessCommand = new Command(async () =>
             _toast.ShowMessageSuccess("Lưu thành công ✓"));

        ShowErrorCommand = new Command(async () =>
             _toast.ShowMessageError("Không thể kết nối tới máy chủ", 4, ToastPosition.Bottom));

        ShowWarningCommand = new Command(async () =>
             _toast.ShowMessageWarning("Pin thiết bị yếu", 3, ToastPosition.Center));

        ShowInfoCommand = new Command(async () =>
             _toast.ShowMessageInfo("Bạn có 3 tin nhắn mới"));

        ShowActionCommand = new Command(async () =>
            await _toast.ShowAsync(new ToastOptions
            {
                Message = "Đã xoá 1 mục",
                Type = ToastType.Success,
                Duration = 5,
                Position = ToastPosition.Top,
                ActionText = "Undo",
                OnAction = async () =>
                     _toast.ShowMessageInfo("Đã hoàn tác", 2, ToastPosition.Bottom),
            }));

        ShowQueueCommand = new Command(() =>
        {
            _toast.ShowMessageInfo("Toast #1 — top stack", 4);
            _toast.ShowMessageSuccess("Toast #2 — top stack", 4);
            _toast.ShowMessageWarning("Toast #3 — bottom stack", 4, ToastPosition.Bottom);
            _toast.ShowMessageError("Toast #4 — bottom stack", 4, ToastPosition.Bottom);
        });

        ShowPopupCommand = new Command(async () =>
        {
            await MopupService.Instance.PushAsync(new PopupPageSample(_toast));
        });

        ShowModelPageCommand = new Command(async () =>
        {
            await _naviagtion.PushModalAsync(new NavigationPage(new ModelPageSample(_toast)), true);
        });
    }

    public ICommand ShowSuccessCommand { get; }

    public ICommand ShowErrorCommand { get; }

    public ICommand ShowWarningCommand { get; }

    public ICommand ShowInfoCommand { get; }

    public ICommand ShowActionCommand { get; }

    public ICommand ShowQueueCommand { get; }

    public ICommand ShowPopupCommand { get; }
    public ICommand ShowModelPageCommand { get; }
}