using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.ApplicationModel;
using UIKit;

namespace Toast.Maui.Toasts;

internal static partial class NativeToast
{
    private const float TopInset = 12f;
    private const float BottomInset = 12f;
    private const float SideInset = 16f;
    private const float StackGap = 8f;
    private const float SlideDistance = 60f;

    private static UIWindow? _overlayWindow;
    private static UIStackView? _topStack;
    private static UIStackView? _centerStack;
    private static UIStackView? _bottomStack;

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
        var window = EnsureOverlayWindow();
        if (window is null) return;

        var stack = options.Position switch
        {
            ToastPosition.Top => _topStack!,
            ToastPosition.Bottom => _bottomStack!,
            _ => _centerStack!,
        };

        var view = BuildToastView(options);
        var active = new Active(view, options, options.Position);

        var slide = options.Position switch
        {
            ToastPosition.Top => -SlideDistance,
            ToastPosition.Bottom => SlideDistance,
            _ => 0f,
        };

        // Set initial state BEFORE entering the view hierarchy, wrapped in a
        // disabled-actions CATransaction so these assignments never get folded
        // into another toast's in-flight UIView.Animate block.
        CATransaction.Begin();
        CATransaction.DisableActions = true;
        view.Alpha = 0f;
        view.Transform = CGAffineTransform.MakeTranslation(0, slide);
        CATransaction.Commit();

        ActiveToasts.Add(active);

        if (options.Position == ToastPosition.Bottom)
            stack.InsertArrangedSubview(view, 0);
        else
            stack.AddArrangedSubview(view);

        stack.LayoutIfNeeded();

        AttachGestures(view, active);

        try
        {
            await AnimateAsync(0.26, UIViewAnimationOptions.CurveEaseOut, () =>
            {
                view.Alpha = 1f;
                view.Transform = CGAffineTransform.MakeIdentity();
            }).ConfigureAwait(true);

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
                await AnimateAsync(0.2, UIViewAnimationOptions.CurveEaseIn, () =>
                {
                    view.Alpha = 0f;
                    view.Transform = CGAffineTransform.MakeTranslation(0, slide);
                }).ConfigureAwait(true);
            }
            else
            {
                await Task.Delay(180).ConfigureAwait(true);
            }
        }
        finally
        {
            stack.RemoveArrangedSubview(view);
            view.RemoveFromSuperview();
            ActiveToasts.Remove(active);
            TeardownWindowIfEmpty();
        }
    }

    private static NativeToastView BuildToastView(ToastOptions options)
    {
        var (bg, glyph) = ResolvePalette(options.Type);

        var container = new NativeToastView(options)
        {
            BackgroundColor = bg,
        };
        container.Layer.CornerRadius = 14f;
        container.Layer.MasksToBounds = false;
        container.Layer.ShadowColor = UIColor.Black.CGColor;
        container.Layer.ShadowOpacity = 0.2f;
        container.Layer.ShadowOffset = new CGSize(0, 2);
        container.Layer.ShadowRadius = 6;

        var icon = new UILabel
        {
            Text = glyph,
            TextColor = UIColor.White,
            Font = UIFont.SystemFontOfSize(20),
            TextAlignment = UITextAlignment.Center,
            TranslatesAutoresizingMaskIntoConstraints = false,
        };
        container.AddSubview(icon);

        var message = new UILabel
        {
            Text = options.Message,
            TextColor = UIColor.White,
            Font = UIFont.SystemFontOfSize(14),
            Lines = 4,
            LineBreakMode = UILineBreakMode.TailTruncation,
            TranslatesAutoresizingMaskIntoConstraints = false,
        };
        container.AddSubview(message);

        UIButton? action = null;
        if (!string.IsNullOrWhiteSpace(options.ActionText))
        {
            action = UIButton.FromType(UIButtonType.System);
            action.TranslatesAutoresizingMaskIntoConstraints = false;
            action.SetTitle(options.ActionText, UIControlState.Normal);
            action.SetTitleColor(UIColor.White, UIControlState.Normal);
            if (action.TitleLabel is not null)
                action.TitleLabel.Font = UIFont.BoldSystemFontOfSize(13);

            var config = UIButtonConfiguration.PlainButtonConfiguration;
            config.ContentInsets = new NSDirectionalEdgeInsets(6, 12, 6, 12);
            config.BaseForegroundColor = UIColor.White;
            action.Configuration = config;

            action.Layer.CornerRadius = 10f;
            action.Layer.BorderWidth = 1f;
            action.Layer.BorderColor = UIColor.White.ColorWithAlpha(0.4f).CGColor;
            action.Layer.MasksToBounds = true;

            var capturedOptions = options;
            var capturedContainer = container;
            action.TouchUpInside += (_, _) =>
            {
                try { capturedOptions.OnAction?.Invoke(); } catch { }
                foreach (var a in ActiveToasts)
                    if (ReferenceEquals(a.View, capturedContainer)) { a.RequestDismiss(); return; }
            };
            container.AddSubview(action);
            container.ActionButton = action;
        }

        const float padH = 14f;
        const float padV = 12f;

        var constraints = new List<NSLayoutConstraint>
        {
            icon.LeadingAnchor.ConstraintEqualTo(container.LeadingAnchor, padH),
            icon.CenterYAnchor.ConstraintEqualTo(container.CenterYAnchor),
            icon.WidthAnchor.ConstraintEqualTo(28),

            message.LeadingAnchor.ConstraintEqualTo(icon.TrailingAnchor, 12),
            message.TopAnchor.ConstraintEqualTo(container.TopAnchor, padV),
            message.BottomAnchor.ConstraintEqualTo(container.BottomAnchor, -padV),
        };

        if (action is not null)
        {
            constraints.Add(message.TrailingAnchor.ConstraintEqualTo(action.LeadingAnchor, -12));
            constraints.Add(action.TrailingAnchor.ConstraintEqualTo(container.TrailingAnchor, -padH));
            constraints.Add(action.CenterYAnchor.ConstraintEqualTo(container.CenterYAnchor));
        }
        else
        {
            constraints.Add(message.TrailingAnchor.ConstraintEqualTo(container.TrailingAnchor, -padH));
        }

        NSLayoutConstraint.ActivateConstraints(constraints.ToArray());
        return container;
    }

    private static void AttachGestures(NativeToastView view, Active active)
    {
        var pan = new UIPanGestureRecognizer(g =>
        {
            if (g.View is not NativeToastView v) return;
            var translation = g.TranslationInView(v);
            var width = v.Bounds.Width > 0 ? v.Bounds.Width : 320;

            switch (g.State)
            {
                case UIGestureRecognizerState.Began:
                    if (active.Options.PauseOnTouch) active.Pause();
                    break;

                case UIGestureRecognizerState.Changed:
                    if (active.Options.SwipeToDismiss)
                    {
                        v.Transform = CGAffineTransform.MakeTranslation((nfloat)translation.X, 0);
                        v.Alpha = (float)Math.Clamp(1.0 - Math.Abs(translation.X) / width, 0.2, 1.0);
                    }
                    break;

                case UIGestureRecognizerState.Ended:
                case UIGestureRecognizerState.Cancelled:
                case UIGestureRecognizerState.Failed:
                    if (active.Options.PauseOnTouch) active.Resume();

                    if (g.State == UIGestureRecognizerState.Ended
                        && active.Options.SwipeToDismiss
                        && Math.Abs(translation.X) >= width * 0.35)
                    {
                        active.MarkDismissedBySwipe();
                        var target = translation.X < 0 ? -width * 1.2 : width * 1.2;
                        UIView.Animate(0.18, () =>
                        {
                            v.Transform = CGAffineTransform.MakeTranslation((nfloat)target, 0);
                            v.Alpha = 0f;
                        });
                        active.RequestDismiss();
                    }
                    else
                    {
                        UIView.Animate(0.18, () =>
                        {
                            v.Transform = CGAffineTransform.MakeIdentity();
                            v.Alpha = 1f;
                        });
                    }
                    break;
            }
        })
        {
            MaximumNumberOfTouches = 1,
        };
        view.AddGestureRecognizer(pan);

        if (active.Options.DismissOnTap)
        {
            var tap = new UITapGestureRecognizer(() => active.RequestDismiss())
            {
                Delegate = new ActionButtonAvoidingDelegate(view),
            };
            view.AddGestureRecognizer(tap);
        }
    }

    private sealed class ActionButtonAvoidingDelegate : UIGestureRecognizerDelegate
    {
        private readonly NativeToastView _host;
        public ActionButtonAvoidingDelegate(NativeToastView host) => _host = host;

        public override bool ShouldReceiveTouch(UIGestureRecognizer recognizer, UITouch touch)
        {
            if (_host.ActionButton is null) return true;
            var v = touch.View;
            while (v is not null)
            {
                if (ReferenceEquals(v, _host.ActionButton)) return false;
                v = v.Superview;
            }
            return true;
        }
    }

    private static UIWindow? EnsureOverlayWindow()
    {
        if (_overlayWindow is not null) return _overlayWindow;

        UIWindowScene? scene = null;
        foreach (var s in UIApplication.SharedApplication.ConnectedScenes)
        {
            if (s is UIWindowScene ws && ws.ActivationState == UISceneActivationState.ForegroundActive)
            {
                scene = ws;
                break;
            }
        }
        if (scene is null)
        {
            foreach (var s in UIApplication.SharedApplication.ConnectedScenes)
            {
                if (s is UIWindowScene ws) { scene = ws; break; }
            }
        }
        if (scene is null) return null;

        var root = new PassThroughViewController();

        var window = new UIWindow(scene)
        {
            WindowLevel = UIWindowLevel.Alert + 1,
            BackgroundColor = UIColor.Clear,
            UserInteractionEnabled = true,
            RootViewController = root,
        };

        var rootView = root.View!;

        _topStack = BuildStack();
        _centerStack = BuildStack();
        _bottomStack = BuildStack();

        rootView.AddSubview(_topStack);
        rootView.AddSubview(_centerStack);
        rootView.AddSubview(_bottomStack);

        var safe = rootView.SafeAreaLayoutGuide;
        NSLayoutConstraint.ActivateConstraints(new[]
        {
            _topStack.TopAnchor.ConstraintEqualTo(safe.TopAnchor, TopInset),
            _topStack.LeadingAnchor.ConstraintEqualTo(safe.LeadingAnchor, SideInset),
            _topStack.TrailingAnchor.ConstraintEqualTo(safe.TrailingAnchor, -SideInset),

            _bottomStack.BottomAnchor.ConstraintEqualTo(safe.BottomAnchor, -BottomInset),
            _bottomStack.LeadingAnchor.ConstraintEqualTo(safe.LeadingAnchor, SideInset),
            _bottomStack.TrailingAnchor.ConstraintEqualTo(safe.TrailingAnchor, -SideInset),

            _centerStack.CenterYAnchor.ConstraintEqualTo(safe.CenterYAnchor),
            _centerStack.LeadingAnchor.ConstraintEqualTo(safe.LeadingAnchor, SideInset),
            _centerStack.TrailingAnchor.ConstraintEqualTo(safe.TrailingAnchor, -SideInset),
        });

        window.Hidden = false;
        _overlayWindow = window;
        return window;
    }

    private static PassThroughStackView BuildStack()
    {
        return new PassThroughStackView
        {
            Axis = UILayoutConstraintAxis.Vertical,
            Alignment = UIStackViewAlignment.Fill,
            Distribution = UIStackViewDistribution.Fill,
            Spacing = StackGap,
            TranslatesAutoresizingMaskIntoConstraints = false,
        };
    }

    private static void TeardownWindowIfEmpty()
    {
        if (ActiveToasts.Count > 0) return;

        if (_overlayWindow is not null)
        {
            _overlayWindow.Hidden = true;
            _overlayWindow = null;
        }
        _topStack = null;
        _centerStack = null;
        _bottomStack = null;
    }

    private static Task AnimateAsync(double duration, UIViewAnimationOptions options, Action animations)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        UIView.Animate(duration, 0, options, animations, () => tcs.TrySetResult());
        return tcs.Task;
    }

    private static (UIColor Background, string Glyph) ResolvePalette(ToastType type) => type switch
    {
        ToastType.Success => (Hex("#2E7D4F"), "✔"),
        ToastType.Error => (Hex("#C0392B"), "✖"),
        ToastType.Warning => (Hex("#D98E00"), "⚠"),
        ToastType.Info => (Hex("#2F5EA8"), "ⓘ"),
        _ => (Hex("#333333"), "ⓘ"),
    };

    private static UIColor Hex(string hex)
    {
        var h = hex.TrimStart('#');
        var r = Convert.ToInt32(h.Substring(0, 2), 16) / 255f;
        var g = Convert.ToInt32(h.Substring(2, 2), 16) / 255f;
        var b = Convert.ToInt32(h.Substring(4, 2), 16) / 255f;
        return UIColor.FromRGB(r, g, b);
    }

    private sealed class Active
    {
        private int _pauseCount;
        private bool _dismissRequested;
        private bool _dismissedBySwipe;

        public Active(NativeToastView view, ToastOptions options, ToastPosition position)
        {
            View = view;
            Options = options;
            Position = position;
            CreatedAt = DateTime.UtcNow;
        }

        public NativeToastView View { get; }
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

    private sealed class NativeToastView : UIView
    {
        public NativeToastView(ToastOptions options) => Options = options;

        public ToastOptions Options { get; }
        public UIButton? ActionButton { get; set; }
    }

    private sealed class PassThroughViewController : UIViewController
    {
        public override void LoadView() => View = new PassThroughView { BackgroundColor = UIColor.Clear };
    }

    private sealed class PassThroughView : UIView
    {
        public override UIView? HitTest(CGPoint point, UIEvent? uievent)
        {
            var hit = base.HitTest(point, uievent);
            return ReferenceEquals(hit, this) ? null : hit;
        }
    }

    private sealed class PassThroughStackView : UIStackView
    {
        public override UIView? HitTest(CGPoint point, UIEvent? uievent)
        {
            var hit = base.HitTest(point, uievent);
            return ReferenceEquals(hit, this) ? null : hit;
        }
    }
}
