using CoreGraphics;
using Foundation;
using Microsoft.Maui.Controls.Platform;
using OxyPlot.Maui.Skia.Effects;
using UIKit;

namespace OxyPlot.Maui.Skia.iOS.Effects
{
    public class PlatformTouchEffect : PlatformEffect
    {
        UIView view;
        MyTouchEffect touchEffect;

        TouchRecognizer touchRecognizer;

        protected override void OnAttached()
        {
            // Get the iOS UIView corresponding to the Element that the effect is attached to
            view = Control == null ? Container : Control;

            // Enable touch by default
            view.UserInteractionEnabled = true;
            view.MultipleTouchEnabled = true;

            // Get access to the TouchEffect class in the .NET Standard library
            touchEffect = Element.Effects.OfType<MyTouchEffect>().FirstOrDefault();

            if (touchEffect != null && view != null)
            {
                // Create a TouchRecognizer for this UIView
                touchRecognizer = new TouchRecognizer(Element, this);
                touchRecognizer.Attach(view);
            }
        }

        protected override void OnDetached()
        {
            if (touchEffect != null && view != null)
            {
                // Clean up the TouchRecognizer object
                touchRecognizer.Detach();
            }
        }
        
        class TouchRecognizer : UIGestureRecognizer
        {
            Microsoft.Maui.Controls.Element element;        // Forms element for firing events
            UIView view;            // iOS UIView 
            PlatformTouchEffect effect;

            UITapGestureRecognizer doubleTapRecognizer;
            UILongPressGestureRecognizer longPressRecognizer;

            public TouchRecognizer(Microsoft.Maui.Controls.Element element, PlatformTouchEffect effect)
            {
                this.element = element;
                this.effect = effect;

                doubleTapRecognizer = new UITapGestureRecognizer(OnDoubleTap) { NumberOfTapsRequired = 2 };
                longPressRecognizer = new UILongPressGestureRecognizer(OnLongPress);
            }

            public void Attach(UIView view)
            {
                this.view = view;
                
                view.AddGestureRecognizer(this);

                view.AddGestureRecognizer(doubleTapRecognizer);
                view.AddGestureRecognizer(longPressRecognizer);
                
                //RequireGestureRecognizerToFail(doubleTapRecognizer);
                //RequireGestureRecognizerToFail(longPressRecognizer);
            }

            public void Detach()
            {
                view.RemoveGestureRecognizer(this);

                view.RemoveGestureRecognizer(doubleTapRecognizer);
                view.RemoveGestureRecognizer(longPressRecognizer);
            }
            
            private void OnDoubleTap(UITapGestureRecognizer recognizer)
            {
                if (recognizer.State != UIGestureRecognizerState.Recognized)
                    return;
                
                long id = ((IntPtr)recognizer.Handle).ToInt64();

                var cgPoint = recognizer.LocationInView(view);
                var point = new Point(cgPoint.X, cgPoint.Y);

                effect.touchEffect.OnTouchAction(element,
                    new TouchActionEventArgs(id, TouchActionType.DoubleTapped, new[] { point }, false));
            }
            
            private void OnLongPress(UILongPressGestureRecognizer recognizer)
            {
                if (recognizer.State != UIGestureRecognizerState.Recognized)
                    return;
                
                long id = ((IntPtr)recognizer.Handle).ToInt64();
                
                var cgPoint = recognizer.LocationInView(view);
                var point = new Point(cgPoint.X, cgPoint.Y);

                effect.touchEffect.OnTouchAction(element,
                    new TouchActionEventArgs(id, TouchActionType.LongPress, new[] { point }, false));
            }

            // touches = touches of interest; evt = all touches of type UITouch
            public override void TouchesBegan(NSSet touches, UIEvent evt)
            {
                base.TouchesBegan(touches, evt);
                
                FireEvent(touches.Cast<UITouch>(), evt, TouchActionType.Pressed, true);
            }

            public override void TouchesMoved(NSSet touches, UIEvent evt)
            {
                base.TouchesMoved(touches, evt);
                
                FireEvent(touches.Cast<UITouch>(), evt, TouchActionType.Moved, true);
            }

            public override void TouchesEnded(NSSet touches, UIEvent evt)
            {
                base.TouchesEnded(touches, evt);
                
                FireEvent(touches.Cast<UITouch>(), evt, TouchActionType.Released, false);
            }

            public override void TouchesCancelled(NSSet touches, UIEvent evt)
            {
                base.TouchesCancelled(touches, evt);

                //skip
            }

            bool CheckForBoundary(UITouch touch)
            {
                if (!effect.touchEffect.HitFrameEnabled)
                    return false;

                var point = touch.LocationInView(View);
                var frame = new CGRect(new CGPoint(), View.Frame.Size);

                return !frame.Contains(point);
            }

            private void FireEvent(IEnumerable<UITouch> touches, UIEvent evt, TouchActionType actionType, bool isInContact)
            {
                var list = new List<Point>();
                foreach (var touch in touches)
                {
                    if (CheckForBoundary(touch))
                        continue;

                    var cgPoint = touch.LocationInView(View);
                    list.Add(new Point(cgPoint.X, cgPoint.Y));
                }

                if (list.Count > 0)
                {
                    effect.touchEffect.OnTouchAction(element,
                        new TouchActionEventArgs(((IntPtr)evt.Handle).ToInt64(), actionType, list.ToArray(), isInContact));
                }
            }
        }
    }
}