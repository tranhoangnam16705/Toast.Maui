using Mopups.Pages;
using Toast.Maui.Toasts;

namespace Toast.Maui.Sample;

public partial class PopupPageSample : PopupPage
{
	public PopupPageSample(IToastService toastService)
	{
		InitializeComponent();
        BindingContext = new PopupPageSampleViewModel(toastService);
    }
}