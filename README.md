# HexaEngine.UI

<p align="center">
  <img width="300" height="300" src="https://raw.githubusercontent.com/HexaEngine/HexaEngine.UI/main/icon.png">
</p>

[![NuGet](https://img.shields.io/nuget/v/HexaEngine.UI.svg)](https://www.nuget.org/packages/HexaEngine.UI/)

A high-performance retained-mode UI framework for **HexaEngine** — a cross-platform 3D game engine. HexaEngine.UI provides a complete UI solution for building in-engine editors, game interfaces, and interactive applications with a familiar XAML-inspired workflow.

## Community
- Discord: [https://discord.gg/VawN5d8HMh](https://discord.gg/VawN5d8HMh)

## Features

### 🎨 **Retained-Mode Architecture**
- Declarative UI tree with automatic layout and rendering
- XAML-based markup for defining UI hierarchies
- Resource dictionaries for reusable styles and templates

### 🧩 **Rich Widget Library**
Built-in controls for common UI patterns:
- **Layout Containers**: `Grid`, `StackPanel`, `Border`, `Panel`
- **Content Controls**: `Button`, `Label`, `ContentControl`
- **Text Input**: `TextBox`, `TextBoxBase`
- **Image Display**: `Image` with various stretch modes
- **Scrolling**: `ScrollViewer`, `ScrollBar` with customizable behavior
- **Primitives**: `Thumb`, `ButtonBase`, `RangeBase` for building custom controls

### 📐 **Flexible Layout System**
- Automatic layout calculation with measure/arrange passes
- Support for margins, padding, and alignment
- Grid-based layouts with row/column definitions
- Stack-based layouts (horizontal/vertical)
- Thickness and corner radius for borders

### 🖌️ **Advanced Rendering**
- Hardware-accelerated rendering with command list batching
- Custom brush system for fills and strokes
- Clipping and transformation support
- Vector and sprite font rendering
- Text layout with word wrapping, alignment, and flow control

### 📝 **Typography & Text**
- FreeType-based font rendering (sprite and vector fonts)
- Advanced text layout with metrics
- Support for text alignment, wrapping, and direction
- Paragraph alignment and incremental tab stops
- Glyph-level control and custom text formatting

### 🎬 **Animation System**
- Timeline-based animations with storyboards
- Animatable properties via `IAnimatable` interface
- Animation scheduling and composition
- Duration and easing support

### 🔗 **Data Binding**
- Dependency property system for reactive updates
- Binding support with multiple modes (OneWay, TwoWay, OneTime, OneWayToSource)
- Value converters for data transformation
- Update source triggers for fine-grained control

### 🖱️ **Input Handling**
- Mouse and keyboard event routing
- Focus management and text composition
- Drag operations with delta tracking
- Click modes (Press, Release, Hover)

### 🎯 **Modern .NET Features**
- Built for **.NET 10**
- Nullable reference types enabled
- Trim and AOT analysis enabled
- Unsafe code support for performance-critical paths

## Installation

### NuGet Package
```bash
dotnet add package HexaEngine.UI
```

### Package Manager Console
```powershell
Install-Package HexaEngine.UI
```

## Quick Start

### Creating a Simple UI Window

```csharp
using HexaEngine.UI;
using HexaEngine.UI.Controls;

// Create a UI window
var window = new UIWindow();

// Create a root layout
var grid = new Grid();
grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

// Add a label
var label = new Label
{
    Content = "Hello, HexaEngine.UI!",
    FontSize = 24,
    HorizontalAlignment = HorizontalAlignment.Center,
    VerticalAlignment = VerticalAlignment.Center
};
Grid.SetRow(label, 0);
grid.Children.Add(label);

// Add a button
var button = new Button
{
    Content = "Click Me",
    Width = 120,
    Height = 40,
    HorizontalAlignment = HorizontalAlignment.Center
};
button.Click += (s, e) => Console.WriteLine("Button clicked!");
Grid.SetRow(button, 1);
grid.Children.Add(button);

// Set the root element
window.Content = grid;
```

### Using XAML Markup

```csharp
using HexaEngine.UI.Markup;

string xaml = @"
<Grid xmlns='http://hexaengine.com/ui'>
    <Grid.RowDefinitions>
        <RowDefinition Height='*'/>
        <RowDefinition Height='Auto'/>
    </Grid.RowDefinitions>
    
    <Label Grid.Row='0' Content='Hello, World!' 
           FontSize='24' 
           HorizontalAlignment='Center' 
           VerticalAlignment='Center'/>
    
    <Button Grid.Row='1' Content='Click Me' 
            Width='120' Height='40' 
            HorizontalAlignment='Center'/>
</Grid>";

var element = XamlReader.Parse(xaml);
```

### Data Binding Example

```csharp
public class ViewModel : DependencyObject
{
    public static readonly DependencyProperty MessageProperty = 
        DependencyProperty.Register(
            nameof(Message), 
            typeof(string), 
            typeof(ViewModel), 
            new PropertyMetadata("Hello"));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }
}

// Bind a label to the view model
var viewModel = new ViewModel();
var label = new Label();
label.SetBinding(Label.ContentProperty, new Binding(nameof(ViewModel.Message)) 
{ 
    Source = viewModel,
    Mode = BindingMode.OneWay
});
```

## Architecture

### Core Components

- **`UIElement`**: Base class for all visual elements with layout and input handling
- **`FrameworkElement`**: Extends UIElement with data binding and styling
- **`DependencyObject`**: Provides dependency property system for reactive properties
- **`Visual`**: Low-level rendering primitive with transformation and clipping
- **`UIRenderer`**: Manages rendering of UI trees to graphics device
- **`UICommandList`**: Command buffer for batched rendering operations

### Layout Pipeline

1. **Measure**: Elements calculate their desired size
2. **Arrange**: Elements are positioned and sized within available space
3. **Render**: Visual tree is traversed and rendered to command list

### Rendering Pipeline

HexaEngine.UI uses a custom rendering pipeline optimized for UI:
- **Primitive Rendering**: Basic shapes (rectangles, lines)
- **Texture Rendering**: Images and sprite fonts
- **Vector Rendering**: Vector graphics and vector fonts
- **Shader Support**: Customizable HLSL shaders for advanced effects

Shaders included:
- `assets/HexaEngine.UI/shaders/prim/` - Primitive rendering shaders
- `assets/HexaEngine.UI/shaders/tex/` - Texture rendering shaders
- `assets/HexaEngine.UI/shaders/vec/` - Vector rendering shaders

## Requirements

- **.NET 10** or higher
- Graphics device supporting DirectX/Vulkan/OpenGL (via HexaEngine.Core)

## Performance Considerations

- Command list batching reduces draw calls
- Layout invalidation is propagated efficiently through the visual tree
- Text layout caching minimizes re-computation
- Resource pooling for graphics objects
- Unsafe code paths for critical rendering operations

## Documentation

For detailed API documentation, visit [https://docs.hexa-studios.net/](https://docs.hexa-studios.net/)

### Key Namespaces

- `HexaEngine.UI` - Core UI classes and framework
- `HexaEngine.UI.Controls` - Built-in UI controls
- `HexaEngine.UI.Controls.Primitives` - Base classes for custom controls
- `HexaEngine.UI.Graphics` - Rendering and graphics resources
- `HexaEngine.UI.Graphics.Text` - Text rendering and typography
- `HexaEngine.UI.Markup` - XAML parsing and markup extensions
- `HexaEngine.UI.Animation` - Animation system
- `HexaEngine.UI.Xaml` - XAML schema and type system

## Contributing

Contributions are welcome! Please visit the [HexaEngine GitHub repository](https://github.com/HexaEngine/HexaEngine) for contribution guidelines.

## License

This project is licensed under the terms specified in the [LICENSE.txt](LICENSE.txt) file.

## Acknowledgments

- **FreeType** for font rendering support

## Related Projects

- [HexaEngine](https://github.com/HexaEngine/HexaEngine) - Main game engine

---

For more information, visit [https://hexa-studios.net/](https://hexa-studios.net/)
