using System.Windows.Input;
using Toast.Maui.Toasts;

namespace Toast.Maui.Sample
{
    public sealed class PopupPageSampleViewModel
    {
        private readonly IToastService _toast;
        public ICommand ShowWarningCommand { get; }

        public PopupPageSampleViewModel(IToastService toast)
        {
            _toast = toast;
            ShowWarningCommand = new Command(async () =>
            {
                _toast.ShowMessageInfo("Toast #1 — top stack", 4);
                _toast.ShowMessageSuccess("Toast #2 — top stack", 4);
                _toast.ShowMessageWarning("Toast #3 — bottom stack", 4, ToastPosition.Bottom);
                _toast.ShowMessageError("Toast #4 — bottom stack", 4, ToastPosition.Bottom);
            });
        }
    }
}