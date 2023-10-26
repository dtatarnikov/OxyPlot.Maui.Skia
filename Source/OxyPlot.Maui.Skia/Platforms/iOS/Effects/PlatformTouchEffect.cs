using CoreGraphics;
using Foundation;
using Microsoft.Maui.Controls.Platform;
using OxyPlot.Maui.Skia.Effects;
using UIKit;

namespace OxyPlot.Maui.Skia.iOS.Effects
{
    public class PlatformTouchEffect : PlatformEffect
    {
        Action<Microsoft.Maui.Controls.Element, TouchActionEventArgs> onTouchAction;
        TouchRecognizer touchRecognizer;

        protected override void OnAttached()
        {
            // Get the iOS UIView corresponding to the Element that the effect is attached to
            var view = Control == null ? Container : Control;

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
                touchRecognizer = new TouchRecognizer(Element, this);
                touchRecognizer.Attach(view);
            }
        }

        protected override void OnDetached()
        {
            if (touchRecognizer != null)
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

            static Dictionary<UIView, TouchRecognizer> viewDictionary = new();

            static Dictionary<long, TouchRecognizer> idToTouchDictionary = new();

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

                viewDictionary.Add(view, this);
            }

            public void Detach()
            {
                view.RemoveGestureRecognizer(this);

                view.RemoveGestureRecognizer(doubleTapRecognizer);
                view.RemoveGestureRecognizer(longPressRecognizer);

                viewDictionary.Remove(view);
            }
            
            private void OnDoubleTap(UITapGestureRecognizer recognizer)
            {
                if (recognizer.State != UIGestureRecognizerState.Recognized || !view.UserInteractionEnabled)
                    return;
                
                long id = ((IntPtr)recognizer.Handle).ToInt64();

                var cgPoint = recognizer.LocationInView(view);
                var point = new Point(cgPoint.X, cgPoint.Y);

                effect.onTouchAction(element,
                    new TouchActionEventArgs(id, TouchActionType.DoubleTapped, new[] { point }, false));
            }
            
            private void OnLongPress(UILongPressGestureRecognizer recognizer)
            {
                if (recognizer.State != UIGestureRecognizerState.Recognized || !view.UserInteractionEnabled)
                    return;
                
                long id = ((IntPtr)recognizer.Handle).ToInt64();
                
                var cgPoint = recognizer.LocationInView(view);
                var point = new Point(cgPoint.X, cgPoint.Y);

                effect.onTouchAction(element,
                    new TouchActionEventArgs(id, TouchActionType.LongPress, new[] { point }, false));
            }

            // touches = touches of interest; evt = all touches of type UITouch
            public override void TouchesBegan(NSSet touches, UIEvent evt)
            {
                base.TouchesBegan(touches, evt);

                var list = new List<Point>();
                foreach (var touch in touches.Cast<UITouch>())
                {
                    var cgPoint = touch.LocationInView(view);
                    list.Add(new Point(cgPoint.X, cgPoint.Y));

                    long id = ((IntPtr)touch.Handle).ToInt64();
                    if (!idToTouchDictionary.ContainsKey(id))
                    {
                        idToTouchDictionary.Add(id, this);
                    }
                }

                effect.onTouchAction(element,
                    new TouchActionEventArgs(((IntPtr)evt.Handle).ToInt64(), TouchActionType.Pressed, list.ToArray(), true));
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
                        var cgPoint = touch.LocationInView(view);
                        list.Add(new Point(cgPoint.X, cgPoint.Y));
                    }
                }

                if (list.Count > 0)
                {
                    effect.onTouchAction(element,
                        new TouchActionEventArgs(((IntPtr)evt.Handle).ToInt64(), TouchActionType.Moved, list.ToArray(), true));
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
                        var cgPoint = touch.LocationInView(view);
                        list.Add(new Point(cgPoint.X, cgPoint.Y));
                    }

                    idToTouchDictionary.Remove(id);
                }

                if (list.Count > 0)
                {
                    effect.onTouchAction(element,
                        new TouchActionEventArgs(((IntPtr)evt.Handle).ToInt64(), TouchActionType.Released, list.ToArray(), false));
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

                // TODO: Might require converting to a List for multiple hits
                TouchRecognizer recognizerHit = null;
                CGPoint position = CGPoint.Empty;

                foreach (UIView view in viewDictionary.Keys)
                {
                    position = touch.LocationInView(view);

                    if (new CGRect(new CGPoint(), view.Frame.Size).Contains(position))
                    {
                        recognizerHit = viewDictionary[view];
                        break;
                    }
                }
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