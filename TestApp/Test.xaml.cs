using HexaEngine.Core.Windows.Events;
using HexaEngine.UI.Controls;

namespace TestApp
{
    public partial class Test
    {
        private void OnButtonClick(object? sender, RoutedEventArgs e)
        {
            Button button = (Button)sender!;
            button.Content = "Clicked!";
        }
    }
}
