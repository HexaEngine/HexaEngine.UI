namespace HexaEngine.UI.XamlGen
{
    /// <summary>
    /// Converts thickness values to C# Thickness code.
    /// Matches the behavior of ThicknessParser in HexaEngine.UI.Markup.Parser.
    /// </summary>
    public class ThicknessConverter : IValueConverter
    {
        public bool TryConvert(string value, out string code)
        {
            if (string.IsNullOrEmpty(value))
            {
                code = null;
                return false;
            }

            string clean = value.Replace(" ", "");
            string[] parts = clean.Split(',');

            code = parts.Length switch
            {
                1 => $"new Thickness({parts[0]})",
                2 => $"new Thickness({parts[0]}, {parts[1]}, {parts[0]}, {parts[1]})",
                4 => $"new Thickness({parts[0]}, {parts[1]}, {parts[2]}, {parts[3]})",
                _ => null
            };

            return code != null;
        }
    }
}
