using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PipelineControl.UI.Views.Common.Behaviors;

public static class InteractionMotionBehavior
{
    public static readonly DependencyProperty IsMotionEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsMotionEnabled",
            typeof(bool),
            typeof(InteractionMotionBehavior),
            new PropertyMetadata(false, OnIsMotionEnabledChanged));

    public static readonly DependencyProperty HoverLiftProperty =
        DependencyProperty.RegisterAttached(
            "HoverLift",
            typeof(double),
            typeof(InteractionMotionBehavior),
            new PropertyMetadata(3d));

    public static readonly DependencyProperty PressScaleProperty =
        DependencyProperty.RegisterAttached(
            "PressScale",
            typeof(double),
            typeof(InteractionMotionBehavior),
            new PropertyMetadata(0.965d));

    public static readonly DependencyProperty ShadowLevelProperty =
        DependencyProperty.RegisterAttached(
            "ShadowLevel",
            typeof(double),
            typeof(InteractionMotionBehavior),
            new PropertyMetadata(1d, OnShadowLevelChanged));

    public static readonly DependencyProperty IsPageTransitionEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsPageTransitionEnabled",
            typeof(bool),
            typeof(InteractionMotionBehavior),
            new PropertyMetadata(false, OnIsPageTransitionEnabledChanged));

    private static readonly DependencyProperty ScaleTransformProperty =
        DependencyProperty.RegisterAttached("ScaleTransform", typeof(ScaleTransform), typeof(InteractionMotionBehavior));

    private static readonly DependencyProperty TranslateTransformProperty =
        DependencyProperty.RegisterAttached("TranslateTransform", typeof(TranslateTransform), typeof(InteractionMotionBehavior));

    private static readonly CubicEase SoftOut = new() { EasingMode = EasingMode.EaseOut };
    private static readonly QuadraticEase QuickOut = new() { EasingMode = EasingMode.EaseOut };

    public static bool GetIsMotionEnabled(DependencyObject obj) => (bool)obj.GetValue(IsMotionEnabledProperty);

    public static void SetIsMotionEnabled(DependencyObject obj, bool value) => obj.SetValue(IsMotionEnabledProperty, value);

    public static double GetHoverLift(DependencyObject obj) => (double)obj.GetValue(HoverLiftProperty);

    public static void SetHoverLift(DependencyObject obj, double value) => obj.SetValue(HoverLiftProperty, value);

    public static double GetPressScale(DependencyObject obj) => (double)obj.GetValue(PressScaleProperty);

    public static void SetPressScale(DependencyObject obj, double value) => obj.SetValue(PressScaleProperty, value);

    public static double GetShadowLevel(DependencyObject obj) => (double)obj.GetValue(ShadowLevelProperty);

    public static void SetShadowLevel(DependencyObject obj, double value) => obj.SetValue(ShadowLevelProperty, value);

    public static bool GetIsPageTransitionEnabled(DependencyObject obj) => (bool)obj.GetValue(IsPageTransitionEnabledProperty);

    public static void SetIsPageTransitionEnabled(DependencyObject obj, bool value) => obj.SetValue(IsPageTransitionEnabledProperty, value);

    private static void OnIsMotionEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            AttachInteractiveMotion(element);
        }
        else
        {
            DetachInteractiveMotion(element);
        }
    }

    private static void OnShadowLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element && GetIsMotionEnabled(element))
        {
            element.InvalidateVisual();
        }
    }

    private static void AttachInteractiveMotion(FrameworkElement element)
    {
        EnsureTransforms(element);

        element.MouseEnter -= OnMouseEnter;
        element.MouseLeave -= OnMouseLeave;
        element.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        element.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;

        element.MouseEnter += OnMouseEnter;
        element.MouseLeave += OnMouseLeave;
        element.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        element.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
    }

    private static void DetachInteractiveMotion(FrameworkElement element)
    {
        element.MouseEnter -= OnMouseEnter;
        element.MouseLeave -= OnMouseLeave;
        element.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        element.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
    }

    private static void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement element && element.IsEnabled)
        {
            AnimateInteraction(element, -GetHoverLift(element), 1d, TimeSpan.FromMilliseconds(80));
        }
    }

    private static void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            AnimateInteraction(element, 0d, 1d, TimeSpan.FromMilliseconds(100));
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.IsEnabled)
        {
            var lift = -Math.Max(0, GetHoverLift(element) * 0.35);
            AnimateInteraction(element, lift, GetPressScale(element), TimeSpan.FromMilliseconds(35));
        }
    }

    private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var lift = element.IsMouseOver ? -GetHoverLift(element) : 0d;
            AnimateInteraction(element, lift, 1d, TimeSpan.FromMilliseconds(75));
        }
    }

    private static void AnimateInteraction(FrameworkElement element, double translateY, double scale, TimeSpan duration)
    {
        EnsureTransforms(element);
        var translate = (TranslateTransform?)element.GetValue(TranslateTransformProperty);
        var scaleTransform = (ScaleTransform?)element.GetValue(ScaleTransformProperty);
        if (translate is null || scaleTransform is null)
        {
            return;
        }

        IEasingFunction easing = duration.TotalMilliseconds <= 90 ? QuickOut : SoftOut;
        translate.BeginAnimation(TranslateTransform.YProperty, Animation(translateY, duration, easing));
        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, Animation(scale, duration, easing));
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, Animation(scale, duration, easing));
    }

    private static DoubleAnimation Animation(double to, TimeSpan duration, IEasingFunction easing)
    {
        return new DoubleAnimation(to, duration)
        {
            EasingFunction = easing,
            FillBehavior = FillBehavior.HoldEnd
        };
    }

    private static void EnsureTransforms(FrameworkElement element)
    {
        if (element.GetValue(ScaleTransformProperty) is ScaleTransform
            && element.GetValue(TranslateTransformProperty) is TranslateTransform)
        {
            return;
        }

        var scale = new ScaleTransform(1d, 1d);
        var translate = new TranslateTransform();
        var existing = element.RenderTransform;
        TransformGroup group;

        if (existing is TransformGroup existingGroup && !existingGroup.IsFrozen)
        {
            group = existingGroup;
        }
        else
        {
            group = new TransformGroup();
            if (existing is not null && existing != Transform.Identity)
            {
                group.Children.Add(existing);
            }
        }

        group.Children.Add(scale);
        group.Children.Add(translate);
        element.RenderTransform = group;
        element.RenderTransformOrigin = new Point(0.5, 0.5);
        element.SetValue(ScaleTransformProperty, scale);
        element.SetValue(TranslateTransformProperty, translate);
    }

    private static void OnIsPageTransitionEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ContentControl contentControl)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            contentControl.Loaded -= OnPageHostLoaded;
            contentControl.Loaded += OnPageHostLoaded;
            DependencyPropertyDescriptor
                .FromProperty(ContentControl.ContentProperty, typeof(ContentControl))
                .AddValueChanged(contentControl, OnPageHostContentChanged);
        }
    }

    private static void OnPageHostLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ContentControl contentControl)
        {
            RunPageEnter(contentControl);
        }
    }

    private static void OnPageHostContentChanged(object? sender, EventArgs e)
    {
        if (sender is ContentControl contentControl)
        {
            RunPageEnter(contentControl);
        }
    }

    private static void RunPageEnter(ContentControl contentControl)
    {
        EnsureTransforms(contentControl);
        var translate = (TranslateTransform?)contentControl.GetValue(TranslateTransformProperty);
        if (translate is null)
        {
            return;
        }

        contentControl.Opacity = 0.94;
        translate.Y = 4;
        contentControl.BeginAnimation(UIElement.OpacityProperty, Animation(1d, TimeSpan.FromMilliseconds(120), QuickOut));
        translate.BeginAnimation(TranslateTransform.YProperty, Animation(0d, TimeSpan.FromMilliseconds(120), QuickOut));
    }
}
