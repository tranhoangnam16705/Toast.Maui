using Microsoft.Maui.LifecycleEvents;
using System;
using System.Collections.Generic;
using System.Text;
using Toast.Maui.Toasts;

namespace Toart.Maui
{
    public static class Extensions
    {
        public static MauiAppBuilder UseToast(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<IToastService, ToastService>();
            return builder;
        }
    }
}