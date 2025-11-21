namespace HexaEngine.UI.XamlGen
{
    /// <summary>
    /// Interface for converting XAML string values to C# code strings.
    /// </summary>
    public interface IValueConverter
    {
        /// <summary>
        /// Tries to convert a XAML value string to C# code.
        /// </summary>
        /// <param name="value">The XAML value string (e.g., "#FF0000", "10,20,30,40")</param>
        /// <param name="code">The generated C# code (e.g., "new Color(0xFF0000)", "new Thickness(10, 20, 30, 40)")</param>
        /// <returns>True if conversion succeeded, false otherwise</returns>
        bool TryConvert(string value, out string code);
    }
}
