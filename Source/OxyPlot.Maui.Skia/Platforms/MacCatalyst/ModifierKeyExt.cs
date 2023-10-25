using UIKit;

namespace OxyPlot.Maui.Skia.MacCatalyst;

public static class ModifierKeyExt
{
    public static OxyModifierKeys ToOxyModifierKeys(this UIKeyModifierFlags vkm)
    {
        var modifiers = OxyModifierKeys.None;
        
        if (vkm.HasFlag(UIKeyModifierFlags.Shift))
        {
            modifiers |= OxyModifierKeys.Shift;
        }

        if (vkm.HasFlag(UIKeyModifierFlags.Command))
        {
            modifiers |= OxyModifierKeys.Control;
        }

        if (vkm.HasFlag(UIKeyModifierFlags.Control))
        {
            modifiers |= OxyModifierKeys.Alt;
        }

        return modifiers;
    }
}