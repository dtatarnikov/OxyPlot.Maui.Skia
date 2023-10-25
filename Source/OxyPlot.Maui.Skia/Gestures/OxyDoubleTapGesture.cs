namespace OxyPlot.Maui.Skia.Gestures
{
    /// <summary>
    /// Represents a double tap gesture.
    /// </summary>
    /// <remarks>The input gesture can be bound to a command in a <see cref="PlotController" />.</remarks>
    public class OxyDoubleTapGesture : OxyInputGesture
    {
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><c>true</c> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.</returns>
        public override bool Equals(OxyInputGesture other)
        {
            var dtg = other as OxyDoubleTapGesture;
            return dtg != null;
        }
    }
}