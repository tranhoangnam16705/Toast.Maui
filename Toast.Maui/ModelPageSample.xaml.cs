using Mopups.Pages;
using Toast.Maui.Toasts;

namespace Toast.Maui;

public partial class ModelPageSample : ContentPage
{
    public ModelPageSample(IToastService toastService)
    {
        InitializeComponent();
        BindingContext = new PopupPageSampleViewModel(toastService);
    }
}