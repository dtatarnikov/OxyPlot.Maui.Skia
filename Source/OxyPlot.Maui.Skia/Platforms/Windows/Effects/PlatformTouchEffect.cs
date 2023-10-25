using Microsoft.Maui.Controls.Platform;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using OxyPlot.Maui.Skia.Effects;

namespace OxyPlot.Maui.Skia.Windows.Effects
{
    public class PlatformTouchEffect : PlatformEffect
    {
        FrameworkElement view;
        Action<Microsoft.Maui.Controls.Element, TouchActionEventArgs> onTouchAction;

        protected override void OnAttached()
        {
            // Get the Windows FrameworkElement corresponding to the Element that the effect is attached to
            view = Control == null ? Container : Control;

            // Get access to the TouchEffect class in the .NET Standard library
            var touchEffect = Element.Effects.OfType<MyTouchEffect>().FirstOrDefault();

            if (touchEffect != null && view != null)
            {
                // Save the method to call on touch events
                onTouchAction = touchEffect.OnTouchAction;

                // Set event handlers on FrameworkElement
                view.PointerPressed += OnPointerPressed;
                view.PointerMoved += OnPointerMoved;
                view.PointerReleased += OnPointerReleased;
                view.PointerWheelChanged += FrameworkElement_PointerWheelChanged;
                view.DoubleTapped += OnDoubleTapped;
                view.Holding += OnHolding;
            }
        }

        private void OnHolding(object sender, HoldingRoutedEventArgs args)
        {
            //Touch can produce a Holding action, but mouse devices generally can't.  
            //see https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.uielement.holding?view=winrt-22621

            var windowsPoint = args.GetPosition(sender as UIElement);
            var touchArgs = new TouchActionEventArgs(args.GetHashCode(),
                TouchActionType.LongPress,
                new Point[] { new(windowsPoint.X, windowsPoint.Y) },
                args.HoldingState != HoldingState.Completed);

            onTouchAction(Element, touchArgs);
        }

        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs args)
        {
            var windowsPoint = args.GetPosition(sender as UIElement);
            var touchArgs = new TouchActionEventArgs(args.GetHashCode(),
                TouchActionType.DoubleTapped,
                new Point[] { new(windowsPoint.X, windowsPoint.Y) },
                false);

            onTouchAction(Element, touchArgs);
        }

        private void FrameworkElement_PointerWheelChanged(object sender, PointerRoutedEventArgs args)
        {
            CommonHandler(sender, TouchActionType.MouseWheel, args);
        }

        protected override void OnDetached()
        {
            if (onTouchAction != null)
            {
                view.PointerPressed -= OnPointerPressed;
                view.PointerMoved -= OnPointerMoved;
                view.PointerReleased -= OnPointerReleased;
                view.PointerWheelChanged -= FrameworkElement_PointerWheelChanged;
            }
        }

        private bool _pressed = false;
        void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            _pressed = true;
            CommonHandler(sender, TouchActionType.Pressed, args);
        }

        void OnPointerMoved(object sender, PointerRoutedEventArgs args)
        {
            if (_pressed)
                CommonHandler(sender, TouchActionType.Moved, args);
        }

        void OnPointerReleased(object sender, PointerRoutedEventArgs args)
        {
            _pressed = false;
            CommonHandler(sender, TouchActionType.Released, args);
        }

        void CommonHandler(object sender, TouchActionType touchActionType, PointerRoutedEventArgs args)
        {
            var pointerPoint = args.GetCurrentPoint(sender as UIElement);
            var windowsPoint = pointerPoint.Position;
            var touchArgs = new TouchActionEventArgs(args.Pointer.PointerId,
                touchActionType,
                new Point[] { new(windowsPoint.X, windowsPoint.Y) },
                args.Pointer.IsInContact)
            {
                ModifierKeys = args.KeyModifiers.ToOxyModifierKeys()
            };

            if (touchActionType == TouchActionType.MouseWheel)
            {
                touchArgs.MouseWheelDelta = pointerPoint.Properties.MouseWheelDelta;
            }

            onTouchAction(Element, touchArgs);
        }
    }
}