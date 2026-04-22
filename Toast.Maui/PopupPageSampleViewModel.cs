using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Toast.Maui.Toasts;

namespace Toast.Maui
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
                _ = _toast.ShowInfoAsync("Toast #1 — top stack", 4);
                _ = _toast.ShowSuccessAsync("Toast #2 — top stack", 4);
                _ = _toast.ShowWarningAsync("Toast #3 — bottom stack", 4, ToastPosition.Bottom);
                _ = _toast.ShowErrorAsync("Toast #4 — bottom stack", 4, ToastPosition.Bottom);
            });
        }
    }
}