using CoreGraphics;
using Foundation;
using Microsoft.Maui.Controls.Platform;
using OxyPlot.Maui.Skia.Effects;
using UIKit;

namespace OxyPlot.Maui.Skia.MacCatalyst.Effects
{
    public class PlatformTouchEffect : PlatformEffect
    {
        UIView view;
        MyTouchEffect touchEffect;

        TouchRecognizer touchRecognizer;
        UITapGestureRecognizer doubleTapRecognizer;
        UILongPressGestureRecognizer longPressRecognizer;
        
        protected override void OnAttached()
        {
            // Get the iOS UIView corresponding to the Element that the effect is attached to
            view = Control == null ? Container : Control;

            // Uncomment this line if the UIView does not have touch enabled by default
            view.UserInteractionEnabled = true;
            view.MultipleTouchEnabled = true;

            // Get access to the TouchEffect class in the .NET Standard library
            touchEffect = Element.Effects.OfType<MyTouchEffect>().FirstOrDefault();

            if (touchEffect != null && view != null)
            {
                // Create a TouchRecognizer for this UIView
                doubleTapRecognizer = new UITapGestureRecognizer(OnDoubleTap) { NumberOfTapsRequired = 2 }; //, ButtonMaskRequired = UIEventButtonMask.Primary
                longPressRecognizer = new UILongPressGestureRecognizer(OnLongPress);
                touchRecognizer = new TouchRecognizer(Element, this);
                
                view.AddGestureRecognizer(touchRecognizer);
                view.AddGestureRecognizer(doubleTapRecognizer);
                view.AddGestureRecognizer(longPressRecognizer);
                
                //touchRecognizer.RequireGestureRecognizerToFail(doubleTapRecognizer);
                //touchRecognizer.RequireGestureRecognizerToFail(longPressRecognizer);
            }
        }

        protected override void OnDetached()
        {
            if (touchEffect != null && view != null)
            {
                view.RemoveGestureRecognizer(touchRecognizer);

                view.RemoveGestureRecognizer(doubleTapRecognizer);
                view.RemoveGestureRecognizer(longPressRecognizer);
            }
        }
        
        private void OnDoubleTap(UITapGestureRecognizer recognizer)
        {
            if (recognizer.State != UIGestureRecognizerState.Recognized || !view.UserInteractionEnabled)
                return;
                
            long id = ((IntPtr)recognizer.Handle).ToInt64();

            var cgPoint = recognizer.LocationInView(view);
            var point = new Point(cgPoint.X, cgPoint.Y);

            touchEffect.OnTouchAction(Element,
                new TouchActionEventArgs(id, TouchActionType.DoubleTapped, new[] { point }, false));
        }
            
        private void OnLongPress(UILongPressGestureRecognizer recognizer)
        {
            if (recognizer.State != UIGestureRecognizerState.Recognized || !view.UserInteractionEnabled)
                return;
                
            long id = ((IntPtr)recognizer.Handle).ToInt64();
                
            var cgPoint = recognizer.LocationInView(view);
            var point = new Point(cgPoint.X, cgPoint.Y);

            touchEffect.OnTouchAction(Element,
                new TouchActionEventArgs(id, TouchActionType.LongPress, new[] { point }, false));
        }

        class TouchRecognizer : UIGestureRecognizer
        {
            Microsoft.Maui.Controls.Element element;        // Forms element for firing events
            PlatformTouchEffect effect;
            
            public TouchRecognizer(Microsoft.Maui.Controls.Element element, PlatformTouchEffect effect)
            {
                this.element = element;
                this.effect = effect;
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
                var point = touch.LocationInView(View);
                var frame = new CGRect(new CGPoint(), View.Frame.Size);

                return effect.touchEffect.HitFrameEnabled && !frame.Contains(point);
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
                        new TouchActionEventArgs(((IntPtr)evt.Handle).ToInt64(), actionType, list.ToArray(), isInContact)
                        {
                            ModifierKeys = evt.ModifierFlags.ToOxyModifierKeys()
                        });
                }
            }
        }
    }
}