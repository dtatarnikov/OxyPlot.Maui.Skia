using Android.Views;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using OxyPlot.Maui.Skia.Effects;
using View = Android.Views.View;

namespace OxyPlot.Maui.Skia.Droid.Effects
{
    public class PlatformTouchEffect : PlatformEffect
    {
        MyTouchEffect touchEffect;
        View view;

        protected override void OnAttached()
        {
            // Get the Android View corresponding to the Element that the effect is attached to
            view = Control == null ? Container : Control;

            // Get access to the TouchEffect class in the .NET Standard library
            touchEffect = Element.Effects.OfType<MyTouchEffect>().FirstOrDefault();

            if (touchEffect != null && view != null)
            {
                // Set event handler on View
                view.SetOnTouchListener(new TouchListener(Element, this));
            }
        }

        protected override void OnDetached()
        {
            if (touchEffect != null && view != null)
            {
                view.SetOnTouchListener(null);
            }
        }

        class TouchListener : GestureDetector.SimpleOnGestureListener, View.IOnTouchListener
        {
            /// <summary>
            /// the gesture detector used for detecting taps and long-presses, ...
            /// </summary>
            private readonly GestureDetector _gestureDetector;
            
            Microsoft.Maui.Controls.Element _element;        // Forms element for firing events
            private readonly PlatformTouchEffect _effect;

            private Point[] _screenPointerCoords;
            private int _pointerId;

            /// <summary>
            /// Constructor
            /// </summary>
            public TouchListener(Microsoft.Maui.Controls.Element element, PlatformTouchEffect eff)
            {
                _element = element;
                _effect = eff;

                _gestureDetector = new GestureDetector(eff.view.Context, this);
            }

            /// <inheritdoc />
            public bool OnTouch(View v, MotionEvent e)
            {
                // Two object common to all the events
                int[] twoIntArray = new int[2];
                v.GetLocationOnScreen(twoIntArray);

                var frame = new Rect(twoIntArray[0], twoIntArray[1], v.Width, v.Height);

                var list = new List<Point>();
                for (var pointerIndex = 0; pointerIndex < e.PointerCount; pointerIndex++)
                {
                    var p = new Point(twoIntArray[0] + e.GetX(pointerIndex), twoIntArray[1] + e.GetY(pointerIndex));
                    if (CheckForBoundary(frame, p))
                        continue;

                    list.Add(p);
                }
                
                _screenPointerCoords = list.ToArray();
                _pointerId = e.GetPointerId(e.ActionIndex);
            
                // Use ActionMasked here rather than Action to reduce the number of possibilities
                switch (e.ActionMasked)
                {
                    case MotionEventActions.Down:
                    case MotionEventActions.PointerDown:
                        FireEvent(TouchActionType.Pressed, true);
                        break;
                    case MotionEventActions.Move:
                        FireEvent(TouchActionType.Moved, true);
                        break;
                }

                _gestureDetector.OnTouchEvent(e);

                switch (e.ActionMasked)
                {
                    case MotionEventActions.Up:
                    case MotionEventActions.Pointer1Up:
                        FireEvent(TouchActionType.Released, false);
                        break;
                }

                return true; // indicate event was handled
            }
            
            /// <inheritdoc />
            public override void OnLongPress(MotionEvent e)
            {
                FireEvent(TouchActionType.LongPress, e.ActionMasked == MotionEventActions.Down);
            }
            
            /// <inheritdoc />
            public override bool OnDoubleTap(MotionEvent e)
            {
                FireEvent(TouchActionType.DoubleTapped, false);

                return true;
            }
            
            bool CheckForBoundary(Rect frame, Point point)
            {
                return _effect.touchEffect.HitFrameEnabled && !frame.Contains(point);
            }

            private void FireEvent(TouchActionType actionType, bool isInContact)
            {
                if (_screenPointerCoords.Length <= 0)
                    return;

                // Get the location of the pointer within the view
                int[] twoIntArray = new int[2];
                _effect.view.GetLocationOnScreen(twoIntArray);
                Func<double, double> pointerFunc = _effect.view.Context.FromPixels;
                List<Point> locations = new List<Point>();
                foreach (var loc in _screenPointerCoords)
                {
                    var x = loc.X - twoIntArray[0];
                    var y = loc.Y - twoIntArray[1];
                    var point = new Point(pointerFunc(x), pointerFunc(y));
                    locations.Add(point);
                }

                // Call the method
                _effect.touchEffect.OnTouchAction(_element,
                    new TouchActionEventArgs(_pointerId, actionType, locations.ToArray(), isInContact));
            }
        }
    }
}