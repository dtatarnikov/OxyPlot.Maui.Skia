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
        Action<Microsoft.Maui.Controls.Element, TouchActionEventArgs> onTouchAction;

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
            var touchEffect = Element.Effects.OfType<MyTouchEffect>().FirstOrDefault();

            if (touchEffect != null && view != null)
            {
                // Save the method to call on touch events
                onTouchAction = touchEffect.OnTouchAction;

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
            view.RemoveGestureRecognizer(touchRecognizer);

            view.RemoveGestureRecognizer(doubleTapRecognizer);
            view.RemoveGestureRecognizer(longPressRecognizer);
        }
        
        private void OnDoubleTap(UITapGestureRecognizer recognizer)
        {
            if (recognizer.State != UIGestureRecognizerState.Recognized || !view.UserInteractionEnabled)
                return;
                
            long id = ((IntPtr)recognizer.Handle).ToInt64();

            var cgPoint = recognizer.LocationInView(view);
            var point = new Point(cgPoint.X, cgPoint.Y);

            onTouchAction(Element,
                new TouchActionEventArgs(id, TouchActionType.DoubleTapped, new[] { point }, false));
        }
            
        private void OnLongPress(UILongPressGestureRecognizer recognizer)
        {
            if (recognizer.State != UIGestureRecognizerState.Recognized || !view.UserInteractionEnabled)
                return;
                
            long id = ((IntPtr)recognizer.Handle).ToInt64();
                
            var cgPoint = recognizer.LocationInView(view);
            var point = new Point(cgPoint.X, cgPoint.Y);

            onTouchAction(Element,
                new TouchActionEventArgs(id, TouchActionType.LongPress, new[] { point }, false));
        }

        class TouchRecognizer : UIGestureRecognizer
        {
            Microsoft.Maui.Controls.Element element;        // Forms element for firing events
            PlatformTouchEffect effect;
            
            Dictionary<long, UITouch> idToTouchDictionary = new();

            public TouchRecognizer(Microsoft.Maui.Controls.Element element, PlatformTouchEffect effect)
            {
                this.element = element;
                this.effect = effect;
            }

            // touches = touches of interest; evt = all touches of type UITouch
            public override void TouchesBegan(NSSet touches, UIEvent evt)
            {
                base.TouchesBegan(touches, evt);

                var list = new List<Point>();
                foreach (var touch in touches.Cast<UITouch>())
                {
                    var cgPoint = touch.LocationInView(View);
                    list.Add(new Point(cgPoint.X, cgPoint.Y));

                    long id = ((IntPtr)touch.Handle).ToInt64();
                    if (!idToTouchDictionary.ContainsKey(id))
                    {
                        idToTouchDictionary.Add(id, touch);
                    }
                }

                effect.onTouchAction(element,
                    new TouchActionEventArgs(((IntPtr)evt.Handle).ToInt64(), TouchActionType.Pressed, list.ToArray(), true)
                    {
                        ModifierKeys = evt.ModifierFlags.ToOxyModifierKeys()
                    });
            }

            public override void TouchesMoved(NSSet touches, UIEvent evt)
            {
                base.TouchesMoved(touches, evt);
                
                var list = new List<Point>();
                foreach (var touch in touches.Cast<UITouch>())
                {
                    CheckForBoundaryHop(touch);
                    
                    long id = ((IntPtr)touch.Handle).ToInt64();
                    if (idToTouchDictionary[id] != null)
                    {
                        var cgPoint = touch.LocationInView(View);
                        list.Add(new Point(cgPoint.X, cgPoint.Y));
                    }
                }

                if (list.Count > 0)
                {
                    effect.onTouchAction(element,
                        new TouchActionEventArgs(((IntPtr)evt.Handle).ToInt64(), TouchActionType.Moved, list.ToArray(), true)
                        {
                            ModifierKeys = evt.ModifierFlags.ToOxyModifierKeys()
                        });
                }
            }

            public override void TouchesEnded(NSSet touches, UIEvent evt)
            {
                base.TouchesEnded(touches, evt);
                
                var list = new List<Point>();
                foreach (var touch in touches.Cast<UITouch>())
                {
                    CheckForBoundaryHop(touch);

                    long id = ((IntPtr)touch.Handle).ToInt64();
                    if (idToTouchDictionary[id] != null)
                    {
                        var cgPoint = touch.LocationInView(View);
                        list.Add(new Point(cgPoint.X, cgPoint.Y));
                    }

                    idToTouchDictionary.Remove(id);
                }

                if (list.Count > 0)
                {
                    effect.onTouchAction(element,
                        new TouchActionEventArgs(((IntPtr)evt.Handle).ToInt64(), TouchActionType.Released, list.ToArray(), false)
                        {
                            ModifierKeys = evt.ModifierFlags.ToOxyModifierKeys()
                        });
                }
            }

            public override void TouchesCancelled(NSSet touches, UIEvent evt)
            {
                base.TouchesCancelled(touches, evt);

                foreach (var touch in touches.Cast<UITouch>())
                {
                    long id = ((IntPtr)touch.Handle).ToInt64();
                    idToTouchDictionary.Remove(id);
                }
            }

            void CheckForBoundaryHop(UITouch touch)
            {
                long id = ((IntPtr)touch.Handle).ToInt64();
                
                var position = touch.LocationInView(View);
                var recognizerHit = new CGRect(new CGPoint(), View.Frame.Size).Contains(position) ? touch : null;
                
                if (recognizerHit != idToTouchDictionary[id])
                {
                    if (idToTouchDictionary[id] != null)
                    {
                        effect.onTouchAction(element,
                            new TouchActionEventArgs(id, TouchActionType.Pressed, new []{new Point(position.X, position.Y)}, true));
                    }
                    if (recognizerHit != null)
                    {
                        effect.onTouchAction(element,
                            new TouchActionEventArgs(id, TouchActionType.Released, new []{new Point(position.X, position.Y)}, true));
                    }
                    idToTouchDictionary[id] = recognizerHit;
                }
            }
        }
    }
}