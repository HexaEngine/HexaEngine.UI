namespace HexaEngine.UI.XamlGen
{
    using System;

    /// <summary>
    /// Converts brush values (color hex codes) to C# SolidColorBrush code.
    /// Matches the behavior of BrushParser in HexaEngine.UI.Markup.Parser.
    /// </summary>
    public class BrushConverter : IValueConverter
    {
        public bool TryConvert(string value, out string code)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("#"))
            {
                code = null;
                return false;
            }

            string hex = value.Substring(1);

            if (hex.Length == 8) // AARRGGBB
            {
                code = $"new SolidColorBrush(new Color(0x{hex}))";
                return true;
            }
            else if (hex.Length == 6) // RRGGBB
            {
                code = $"new SolidColorBrush(new Color(0xFF{hex}))";
                return true;
            }
            else if (hex.Length == 3) // RGB shorthand -> RRGGBB
            {
                char r = hex[0], g = hex[1], b = hex[2];
                code = $"new SolidColorBrush(new Color(0xFF{r}{r}{g}{g}{b}{b}))";
                return true;
            }

            code = null;
            return false;
        }
    }
}
