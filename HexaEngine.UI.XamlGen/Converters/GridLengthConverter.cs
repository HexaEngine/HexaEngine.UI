namespace HexaEngine.UI.XamlGen
{
    /// <summary>
    /// Converts GridLength values to C# GridLength code.
    /// </summary>
    public class GridLengthConverter : IValueConverter
    {
        public bool TryConvert(string value, out string code)
        {
            if (string.IsNullOrEmpty(value))
            {
                code = null;
                return false;
            }

            if (value == "*")
            {
                code = "new GridLength(1, GridUnitType.Star)";
                return true;
            }

            if (value.EndsWith("*"))
            {
                string num = value.Substring(0, value.Length - 1);
                num = string.IsNullOrEmpty(num) ? "1" : num;
                code = $"new GridLength({num}, GridUnitType.Star)";
                return true;
            }

            if (value.Equals("Auto", System.StringComparison.OrdinalIgnoreCase))
            {
                code = "new GridLength(1, GridUnitType.Auto)";
                return true;
            }

            // Pixel value
            code = $"new GridLength({value}, GridUnitType.Pixel)";
            return true;
        }
    }
}
