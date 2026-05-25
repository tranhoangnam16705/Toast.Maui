using Mopups.Hosting;
using Toart.Maui;

namespace Toast.Maui.Sample
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                }).UseToast().ConfigureMopups();

            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<PopupPageSampleViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<PopupPageSample>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}