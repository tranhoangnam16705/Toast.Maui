using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Microsoft.Maui.ApplicationModel;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;

namespace Toast.Maui.Toasts;

internal static partial class NativeToast
{
    private const int TopInsetDp = 12;
    private const int BottomInsetDp = 12;
    private const int SideInsetDp = 16;
    private const int StackGapDp = 8;
    private const int SlideDistanceDp = 60;

    private static FrameLayout? _overlay;
    private static LinearLayout? _topStack;
    private static LinearLayout? _centerStack;
    private static LinearLayout? _bottomStack;
    private static ViewGroup? _overlayParent;
    private static Activity? _overlayActivity;

    private static readonly List<Active> ActiveToasts = new();

    public static partial Task ShowAsync(ToastOptions options)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try { await PresentAsync(options).ConfigureAwait(true); }
            catch { }
            finally { tcs.TrySetResult(); }
        });
        return tcs.Task;
    }

    public static partial void DismissCurrent()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (ActiveToasts.Count == 0) return;
            ActiveToasts[^1].RequestDismiss();
        });
    }

    public static partial void DismissAll()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            foreach (var t in ActiveToasts.ToArray())
                t.RequestDismiss();
        });
    }

    private static async Task PresentAsync(ToastOptions options)
    {
        var activity = Platform.CurrentActivity;
        if (activity is null) return;
        if (!EnsureOverlay(activity)) return;

        var stack = options.Position switch
        {
            ToastPosition.Top => _topStack!,
            ToastPosition.Bottom => _bottomStack!,
            _ => _centerStack!,
        };

        var view = BuildToastView(activity, options);
        var active = new Active(view, options, options.Position);
        ActiveToasts.Add(active);

        var gapPx = DpToPx(activity, StackGapDp);
        var slidePx = DpToPx(activity, SlideDistanceDp);

        var lp = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);

        if (options.Position == ToastPosition.Bottom)
        {
            // Insert at top of LL so newer toast appears farther from screen bottom
            if (stack.ChildCount > 0)
            {
                var currentFirst = stack.GetChildAt(0);
                if (currentFirst?.LayoutParameters is LinearLayout.LayoutParams cflp)
                {
                    cflp.TopMargin = gapPx;
                    currentFirst.LayoutParameters = cflp;
                }
            }
            lp.TopMargin = 0;
            view.LayoutParameters = lp;
            stack.AddView(view, 0);
        }
        else
        {
            lp.TopMargin = stack.ChildCount > 0 ? gapPx : 0;
            view.LayoutParameters = lp;
            stack.AddView(view);
        }

        view.Alpha = 0f;
        view.TranslationY = options.Position switch
        {
            ToastPosition.Top => -slidePx,
            ToastPosition.Bottom => slidePx,
            _ => 0f,
        };

        AttachTouchHandler(view, active);

        try
        {
            view.Animate()!
                .TranslationY(0)
                .Alpha(1f)
                .SetDuration(260)
                .SetInterpolator(new DecelerateInterpolator())
                .Start();
            await Task.Delay(260).ConfigureAwait(true);

            var totalMs = Math.Max(0, options.Duration * 1000);
            var elapsed = 0.0;
            const int tickMs = 50;
            while (elapsed < totalMs && !active.DismissRequested)
            {
                await Task.Delay(tickMs).ConfigureAwait(true);
                if (active.DismissRequested) break;
                if (!active.IsPaused) elapsed += tickMs;
            }

            if (!active.DismissedBySwipe)
            {
                view.Animate()!
                    .TranslationY(options.Position switch
                    {
                        ToastPosition.Top => -slidePx,
                        ToastPosition.Bottom => slidePx,
                        _ => 0f,
                    })
                    .Alpha(0f)
                    .SetDuration(200)
                    .SetInterpolator(new AccelerateInterpolator())
                    .Start();
                await Task.Delay(200).ConfigureAwait(true);
            }
            else
            {
                await Task.Delay(180).ConfigureAwait(true);
            }
        }
        finally
        {
            RemoveFromStack(stack, view);
            ActiveToasts.Remove(active);
            TeardownOverlayIfEmpty();
        }
    }

    private static void RemoveFromStack(LinearLayout stack, AView view)
    {
        var index = stack.IndexOfChild(view);
        try { stack.RemoveView(view); } catch { }

        if (index == 0 && stack.ChildCount > 0)
        {
            var newFirst = stack.GetChildAt(0);
            if (newFirst?.LayoutParameters is LinearLayout.LayoutParams nflp)
            {
                nflp.TopMargin = 0;
                newFirst.LayoutParameters = nflp;
            }
        }
    }

    private static AView BuildToastView(Activity activity, ToastOptions options)
    {
        var (bg, glyph) = ResolvePalette(options.Type);

        var container = new LinearLayout(activity)
        {
            Orientation = Orientation.Horizontal,
        };
        container.SetPadding(
            DpToPx(activity, 14), DpToPx(activity, 12),
            DpToPx(activity, 14), DpToPx(activity, 12));

        var background = new GradientDrawable();
        background.SetCornerRadius(DpToPx(activity, 14));
        background.SetColor(bg);
        container.Background = background;
        container.Elevation = DpToPx(activity, 4);

        var icon = new TextView(activity)
        {
            Text = glyph,
            TextSize = 20f,
            Gravity = GravityFlags.Center,
        };
        icon.SetTextColor(AColor.White);
        icon.LayoutParameters = new LinearLayout.LayoutParams(
            DpToPx(activity, 28),
            ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.CenterVertical,
            RightMargin = DpToPx(activity, 12),
        };
        container.AddView(icon);

        var message = new TextView(activity)
        {
            Text = options.Message,
            TextSize = 14f,
        };
        message.SetTextColor(AColor.White);
        message.SetMaxLines(4);
        message.LayoutParameters = new LinearLayout.LayoutParams(
            0,
            ViewGroup.LayoutParams.WrapContent,
            1f)
        {
            Gravity = GravityFlags.CenterVertical,
        };
        container.AddView(message);

        if (!string.IsNullOrWhiteSpace(options.ActionText))
        {
            var btn = new TextView(activity)
            {
                Text = options.ActionText,
                TextSize = 13f,
            };
            btn.SetTextColor(AColor.White);
            btn.SetTypeface(null, TypefaceStyle.Bold);
            btn.SetPadding(
                DpToPx(activity, 12), DpToPx(activity, 6),
                DpToPx(activity, 12), DpToPx(activity, 6));
            var btnBg = new GradientDrawable();
            btnBg.SetCornerRadius(DpToPx(activity, 10));
            btnBg.SetStroke(DpToPx(activity, 1), AColor.Argb(0x66, 0xFF, 0xFF, 0xFF));
            btn.Background = btnBg;
            btn.Clickable = true;
            btn.Focusable = true;
            btn.LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent)
            {
                LeftMargin = DpToPx(activity, 12),
                Gravity = GravityFlags.CenterVertical,
            };
            btn.Click += (_, _) =>
            {
                try { options.OnAction?.Invoke(); } catch { }
                foreach (var a in ActiveToasts)
                    if (ReferenceEquals(a.View, container)) { a.RequestDismiss(); return; }
            };
            container.AddView(btn);
        }

        return container;
    }

    private static void AttachTouchHandler(AView view, Active active)
    {
        view.SetOnTouchListener(new ToastTouchListener(view, active));
    }

    private static bool EnsureOverlay(Activity activity)
    {
        if (_overlay is not null && ReferenceEquals(_overlayActivity, activity))
            return true;

        TeardownOverlay();

        if (activity.Window?.DecorView is not ViewGroup decor) return false;

        var overlay = new FrameLayout(activity);

        var topStack = BuildStack(activity, GravityFlags.Top, padTop: TopInsetDp, padBottom: 0);
        var centerStack = BuildStack(activity, GravityFlags.CenterVertical, padTop: 0, padBottom: 0);
        var bottomStack = BuildStack(activity, GravityFlags.Bottom, padTop: 0, padBottom: BottomInsetDp);

        overlay.AddView(topStack);
        overlay.AddView(centerStack);
        overlay.AddView(bottomStack);

        try
        {
            decor.AddView(overlay, new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent));
        }
        catch
        {
            return false;
        }

        overlay.Elevation = DpToPx(activity, 1000);
        overlay.BringToFront();

        overlay.SetOnApplyWindowInsetsListener(new InsetsListener());
        ApplyCurrentInsets(overlay);
        overlay.RequestApplyInsets();

        _overlay = overlay;
        _topStack = topStack;
        _centerStack = centerStack;
        _bottomStack = bottomStack;
        _overlayParent = decor;
        _overlayActivity = activity;
        return true;
    }

    private static void ApplyCurrentInsets(FrameLayout overlay)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(23)) return;

        var rootInsets = overlay.RootWindowInsets;
        if (rootInsets is null) return;

        ApplyInsetsPadding(overlay, rootInsets);
    }

    private static void ApplyInsetsPadding(FrameLayout overlay, WindowInsets insets)
    {
        int left, top, right, bottom;

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var types = WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout();
            var i = insets.GetInsets(types);
            left = i.Left;
            top = i.Top;
            right = i.Right;
            bottom = i.Bottom;
        }
        else
        {
#pragma warning disable CA1422
            left = insets.SystemWindowInsetLeft;
            top = insets.SystemWindowInsetTop;
            right = insets.SystemWindowInsetRight;
            bottom = insets.SystemWindowInsetBottom;
#pragma warning restore CA1422
        }

        overlay.SetPadding(left, top, right, bottom);
    }

    private sealed class InsetsListener : Java.Lang.Object, AView.IOnApplyWindowInsetsListener
    {
        public WindowInsets OnApplyWindowInsets(AView v, WindowInsets insets)
        {
            if (v is FrameLayout fl) ApplyInsetsPadding(fl, insets);
            return insets;
        }
    }

    private static LinearLayout BuildStack(Activity activity, GravityFlags verticalGravity,
        int padTop, int padBottom)
    {
        var stack = new LinearLayout(activity)
        {
            Orientation = Orientation.Vertical,
        };
        stack.SetPadding(
            DpToPx(activity, SideInsetDp), DpToPx(activity, padTop),
            DpToPx(activity, SideInsetDp), DpToPx(activity, padBottom));
        stack.LayoutParameters = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = verticalGravity | GravityFlags.Start,
        };
        return stack;
    }

    private static void TeardownOverlayIfEmpty()
    {
        if (ActiveToasts.Count > 0) return;
        TeardownOverlay();
    }

    private static void TeardownOverlay()
    {
        try
        {
            if (_overlay is not null && _overlayParent is not null)
                _overlayParent.RemoveView(_overlay);
        }
        catch { }
        _overlay = null;
        _topStack = null;
        _centerStack = null;
        _bottomStack = null;
        _overlayParent = null;
        _overlayActivity = null;
    }

    private static int DpToPx(Activity activity, double dp)
    {
        var density = activity.Resources?.DisplayMetrics?.Density ?? 1f;
        return (int)Math.Round(dp * density);
    }

    private static (AColor Background, string Glyph) ResolvePalette(ToastType type) => type switch
    {
        ToastType.Success => (AColor.ParseColor("#2E7D4F"), "✔"),
        ToastType.Error => (AColor.ParseColor("#C0392B"), "✖"),
        ToastType.Warning => (AColor.ParseColor("#D98E00"), "⚠"),
        ToastType.Info => (AColor.ParseColor("#2F5EA8"), "ⓘ"),
        _ => (AColor.ParseColor("#333333"), "ⓘ"),
    };

    private sealed class Active
    {
        private int _pauseCount;
        private bool _dismissRequested;
        private bool _dismissedBySwipe;

        public Active(AView view, ToastOptions options, ToastPosition position)
        {
            View = view;
            Options = options;
            Position = position;
            CreatedAt = DateTime.UtcNow;
        }

        public AView View { get; }
        public ToastOptions Options { get; }
        public ToastPosition Position { get; }
        public DateTime CreatedAt { get; }

        public bool DismissRequested => Volatile.Read(ref _dismissRequested);
        public bool IsPaused => Volatile.Read(ref _pauseCount) > 0;
        public bool DismissedBySwipe => Volatile.Read(ref _dismissedBySwipe);

        public void RequestDismiss() => Volatile.Write(ref _dismissRequested, true);
        public void Pause() => Interlocked.Increment(ref _pauseCount);
        public void Resume() => Interlocked.Decrement(ref _pauseCount);
        public void MarkDismissedBySwipe() => Volatile.Write(ref _dismissedBySwipe, true);
    }

    private sealed class ToastTouchListener : Java.Lang.Object, AView.IOnTouchListener
    {
        private const float DismissThresholdRatio = 0.35f;
        private const float TapSlopPx = 10f;

        private readonly AView _view;
        private readonly Active _active;
        private float _startRawX;
        private float _totalX;
        private bool _tracking;
        private bool _moved;

        public ToastTouchListener(AView view, Active active)
        {
            _view = view;
            _active = active;
        }

        public bool OnTouch(AView? v, MotionEvent? e)
        {
            if (e is null) return false;

            switch (e.Action)
            {
                case MotionEventActions.Down:
                    _startRawX = e.RawX;
                    _totalX = 0;
                    _tracking = true;
                    _moved = false;
                    if (_active.Options.PauseOnTouch) _active.Pause();
                    return true;

                case MotionEventActions.Move:
                    if (!_tracking) return false;
                    _totalX = e.RawX - _startRawX;
                    if (Math.Abs(_totalX) > TapSlopPx) _moved = true;
                    if (_active.Options.SwipeToDismiss && _moved)
                    {
                        _view.TranslationX = _totalX;
                        var width = _view.Width > 0 ? _view.Width : 320;
                        _view.Alpha = (float)Math.Clamp(1.0 - Math.Abs(_totalX) / width, 0.2, 1.0);
                    }
                    return true;

                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    if (!_tracking) return false;
                    _tracking = false;
                    if (_active.Options.PauseOnTouch) _active.Resume();

                    var w = _view.Width > 0 ? _view.Width : 320;
                    var threshold = w * DismissThresholdRatio;

                    if (e.Action == MotionEventActions.Up
                        && _active.Options.SwipeToDismiss
                        && Math.Abs(_totalX) >= threshold)
                    {
                        _active.MarkDismissedBySwipe();
                        var target = _totalX < 0 ? -w * 1.2f : w * 1.2f;
                        _view.Animate()!
                            .TranslationX(target)
                            .Alpha(0f)
                            .SetDuration(180)
                            .Start();
                        _active.RequestDismiss();
                    }
                    else if (e.Action == MotionEventActions.Up
                        && !_moved
                        && _active.Options.DismissOnTap)
                    {
                        _view.TranslationX = 0;
                        _view.Alpha = 1f;
                        _active.RequestDismiss();
                    }
                    else
                    {
                        _view.Animate()!
                            .TranslationX(0)
                            .Alpha(1f)
                            .SetDuration(180)
                            .Start();
                    }
                    return true;
            }
            return false;
        }
    }
}
