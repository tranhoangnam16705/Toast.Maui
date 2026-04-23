# Toast.Maui

Native toast notifications for .NET MAUI on iOS and Android. Each platform is implemented with the underlying UI framework (UIKit on iOS, `FrameLayout` + `LinearLayout` on Android) rather than hosting MAUI views on a disembodied top-level window — layout invalidation, touch dispatch, and animation all behave the way the OS expects.

Targets: `net10.0-android` (API 21+), `net10.0-ios` (15.0+).

## Features

- Four built-in types: `Success`, `Error`, `Warning`, `Info` (each with its own color + glyph).
- Three positions: `Top`, `Center`, `Bottom`.
- Multiple toasts stack vertically per position, with fade + slide animation.
- Optional action button (e.g. "Undo") with callback.
- Interactions: swipe to dismiss, tap to dismiss, pause-on-touch (duration freezes while the finger is down).
- Sits above MAUI content via a dedicated overlay (`UIWindow` on iOS, decor-view overlay on Android) so popups and modals don't cover toasts.

## Setup

Register the service in `MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<IToastService, ToastService>();
```

Inject `IToastService` anywhere (page, view model, service).

## Usage

Quick helpers:

```csharp
await _toast.ShowSuccessAsync("Saved ✓");
await _toast.ShowErrorAsync("Can't reach server", duration: 4, ToastPosition.Bottom);
await _toast.ShowWarningAsync("Battery low", 3, ToastPosition.Center);
await _toast.ShowInfoAsync("3 new messages");
```

Full options (action button, custom behavior):

```csharp
await _toast.ShowAsync(new ToastOptions
{
    Message = "Item deleted",
    Type = ToastType.Success,
    Duration = 5,
    Position = ToastPosition.Top,
    ActionText = "Undo",
    OnAction = async () => await _toast.ShowInfoAsync("Restored"),
    SwipeToDismiss = true,
    PauseOnTouch = true,
    DismissOnTap = true,
});
```

Dismiss programmatically:

```csharp
_toast.DismissCurrent(); // topmost toast only
_toast.DismissAll();
```

### `ToastOptions`

| Property | Default | Notes |
| --- | --- | --- |
| `Message` | `""` | Required. Wraps up to 4 lines with tail truncation. |
| `Type` | `Info` | Drives background color + icon. |
| `Duration` | `3` (seconds) | Wall time the toast stays at full opacity. Excludes fade-in/out. |
| `Position` | `Top` | `Top`, `Center`, `Bottom`. |
| `ActionText` | `null` | If set, renders a trailing button. |
| `OnAction` | `null` | Fires on action tap; the toast dismisses afterward. |
| `SwipeToDismiss` | `true` | Horizontal pan past ~35% width dismisses. |
| `PauseOnTouch` | `true` | Holding the toast freezes the duration countdown. |
| `DismissOnTap` | `true` | Taps on the toast dismiss it. |

## Architecture

```
Toasts/
  IToastService.cs        — public API
  ToastService.cs         — thin wrapper that calls into the platform partial
  NativeToast.cs          — partial class declaration
  ToastOptions.cs / ToastType.cs / ToastPosition.cs
Platforms/
  iOS/NativeToast.iOS.cs          — UIWindow overlay + UIStackView per position
  Android/NativeToast.Android.cs  — decor-view FrameLayout overlay + LinearLayout per position
```

Each platform owns:
- Its own overlay host (created lazily on first toast, torn down when the last toast dismisses).
- A vertical stack per position.
- Per-toast lifecycle: build → add-to-stack → animate-in → duration loop (pausable, dismissable) → animate-out → remove.

## iOS gotcha: passthrough `UIWindow`

The overlay sits on its own `UIWindow` at `UIWindowLevel.Alert + 1` so it stays above alerts and modal popups. iOS dispatches touches by walking every visible window from the highest `WindowLevel` down and picking the first whose `HitTest` returns non-nil — which means a naive overlay window will absorb every tap, including the ones aimed at empty space below it.

`UIView.HitTest`'s default has a fallback clause: if no subview claims the point, return `self`. So even when every subview of the overlay (the pass-through root view, the three stack views) correctly returns `nil` on empty-space taps, `UIWindow.HitTest` still falls back to returning the window itself. iOS sees a non-nil hit at the highest level and stops there — taps on the MAUI window underneath never fire.

The fix is a `UIWindow` subclass that applies the same "return `nil` if the hit is me" rule at the window level:

```csharp
private sealed class PassThroughWindow : UIWindow
{
    public PassThroughWindow(UIWindowScene scene) : base(scene) { }

    public override UIView? HitTest(CGPoint point, UIEvent? uievent)
    {
        var hit = base.HitTest(point, uievent);
        return ReferenceEquals(hit, this) ? null : hit;
    }
}
```

With this in place the overlay window is visually on top but transparent to input on empty areas; taps on an actual toast view still resolve normally.

## Playground app

The project in this repo is also the sample app. `MainPage` has buttons for each type, one with an action button, and a "Stack 4 Toasts (Top + Bottom)" button to verify multi-toast stacking. There's also a Mopups popup page to confirm toasts render above in-app popups.
