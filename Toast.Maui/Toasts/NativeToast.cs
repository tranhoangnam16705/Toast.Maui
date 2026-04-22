namespace Toast.Maui.Toasts;

// Native toast host. Each platform partial owns its own state (overlay
// window, per-position stacks) so the orchestration logic lives close to
// the UIKit/Android APIs that drive rendering. This avoids hosting MAUI
// Grid on a disembodied top-level window, which doesn't propagate layout
// invalidation without a Window ancestor.
internal static partial class NativeToast
{
    public static partial Task ShowAsync(ToastOptions options);

    public static partial void DismissCurrent();

    public static partial void DismissAll();
}
