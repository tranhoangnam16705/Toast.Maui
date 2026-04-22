using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using Toast.Maui.Toasts;

namespace Toast.Maui
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
                }).ConfigureMopups();

            builder.Services.AddSingleton<IToastService, ToastService>();
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