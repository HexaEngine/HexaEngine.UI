namespace HexaEngine.UI
{
    using Hexa.NET.Mathematics;
    using HexaEngine.UI.Graphics;
    using System.Diagnostics.CodeAnalysis;
    using System.Numerics;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class FrameworkElement : UIElement
    {
        private ResourceDictionary? resources;

        public static readonly DependencyProperty<float> ActualWidthProperty = DependencyProperty.Register<FrameworkElement, float>(nameof(ActualWidth), false, new FrameworkMetadata(0f));

        public static readonly DependencyProperty<float> ActualHeightProperty = DependencyProperty.Register<FrameworkElement, float>(nameof(ActualHeight), false, new FrameworkMetadata(0f));

        public float ActualWidth
        {
            get => GetValue(ActualWidthProperty);
            private set => SetValue(ActualWidthProperty, value);
        }

        public float ActualHeight
        {
            get => GetValue(ActualHeightProperty);
            private set => SetValue(ActualHeightProperty, value);
        }

        public static readonly DependencyProperty<object> DataContextProperty = DependencyProperty.Register<FrameworkElement, object>(nameof(DataContext), false, new FrameworkMetadata(null));

        public object? DataContext { get => GetValue(DataContextProperty); set => SetValue(DataContextProperty, value); }

        public static readonly DependencyProperty<Style> StyleProperty = DependencyProperty.Register<FrameworkElement, Style>(nameof(Style), false, new FrameworkMetadata(null, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public Style? Style
        {
            get => GetValue(StyleProperty);
            set
            {
                SetValue(StyleProperty, value);
                if (IsInitialized)
                {
                    ApplyStyle(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the resources owned by this element.
        /// </summary>
        public ResourceDictionary Resources
        {
            get => resources ??= new();
            set => resources = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets whether this element owns any local or merged resources without creating a dictionary.
        /// </summary>
        public bool HasResources => resources != null && !resources.IsEmpty;

        /// <summary>
        /// Searches this element and its ancestors for <paramref name="key"/>.
        /// </summary>
        public bool TryFindResource(object key, out object? value)
        {
            ArgumentNullException.ThrowIfNull(key);

            DependencyObject? current = this;
            while (current != null)
            {
                if (current is FrameworkElement element &&
                    element.resources != null &&
                    element.resources.TryGetValue(key, out value))
                {
                    return true;
                }

                current = current.Parent;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Searches this element and its ancestors for <paramref name="key"/>.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The resource key was not found.</exception>
        public object? FindResource(object key)
        {
            if (TryFindResource(key, out object? value))
            {
                return value;
            }

            throw new KeyNotFoundException($"Resource '{key}' was not found.");
        }

        protected override void OnInitialized()
        {
            Style? style = Style;
            if (style == null && TryFindResource(DependencyObjectType, out object? resource))
            {
                style = resource as Style;
                if (style != null)
                {
                    SetValue(StyleProperty, style);
                }
            }

            ApplyStyle(style);
            base.OnInitialized();
        }

        private void ApplyStyle(Style? style)
        {
            ClearStyleValues();
            if (style == null)
            {
                return;
            }

            Style? slow = style;
            Style? fast = style;
            while (fast?.BasedOn != null)
            {
                slow = slow?.BasedOn;
                fast = fast.BasedOn.BasedOn;
                if (ReferenceEquals(slow, fast))
                {
                    throw new InvalidOperationException("A Style cannot contain a BasedOn cycle.");
                }
            }

            ApplyStyleCore(style);
        }

        private void ApplyStyleCore(Style style)
        {
            if (!style.TargetType.IsAssignableFrom(DependencyObjectType))
            {
                throw new InvalidOperationException($"Style targeting '{style.TargetType}' cannot be applied to '{DependencyObjectType}'.");
            }

            if (style.BasedOn != null)
            {
                ApplyStyleCore(style.BasedOn);
            }

            for (int i = 0; i < style.Setters.Count; i++)
            {
                Setter setter = style.Setters[i];
                if (setter.TargetProperty == null)
                {
                    throw new InvalidOperationException($"Style setter '{setter.Property}' has no target dependency property.");
                }

                SetStyleValue(setter.TargetProperty, setter.Value);
            }
        }

        public override void Render(UICommandList commandList)
        {
            if (Visibility != Visibility.Visible)
            {
                return;
            }

            OnRender(commandList);
        }

        protected override sealed Vector2 MeasureCore(Vector2 availableSize)
        {
            // retrieve all props at once at the start to reduce overhead.
            var width = Width;
            var height = Height;
            var minWidth = MinWidth;
            var minHeight = MinHeight;
            var maxWidth = MaxWidth;
            var maxHeight = MaxHeight;

            var horizontalAlign = HorizontalAlignment;
            var verticalAlign = VerticalAlignment;
            var marginSize = Margin.Size;

            if (!float.IsNaN(width))
            {
                availableSize.X = width;
            }

            if (!float.IsNaN(height))
            {
                availableSize.Y = height;
            }

            if (minWidth == 0)
            {
                minWidth = float.MinValue;
            }

            if (minHeight == 0)
            {
                minHeight = float.MinValue;
            }

            if (maxWidth == 0)
            {
                maxWidth = float.MaxValue;
            }

            if (maxHeight == 0)
            {
                maxHeight = float.MaxValue;
            }

            availableSize = Vector2.Clamp(availableSize, new(minWidth, minHeight), new(maxWidth, maxHeight));

            Vector2 actualAvailableSize = availableSize;

            actualAvailableSize -= marginSize;

            Vector2 desiredSize = MeasureOverride(actualAvailableSize);

            if (horizontalAlign == HorizontalAlignment.Stretch)
            {
                desiredSize.X = availableSize.X;
            }

            if (verticalAlign == VerticalAlignment.Stretch)
            {
                desiredSize.Y = availableSize.Y;
            }

            if (!float.IsNaN(width))
            {
                desiredSize.X = width;
            }

            if (!float.IsNaN(height))
            {
                desiredSize.Y = height;
            }

            desiredSize = Vector2.Clamp(desiredSize, new(minWidth, minHeight), new(maxWidth, maxHeight));

            return desiredSize;
        }

        protected virtual Vector2 MeasureOverride(Vector2 availableSize)
        {
            return default;
        }

        protected override sealed void ArrangeCore(RectangleF finalRect)
        {
            Vector2 origin = new(finalRect.Left, finalRect.Top);
            Vector2 rectSize = finalRect.Size;

            // retrieve all props at once at the start to reduce overhead.
            var desiredSize = DesiredSize;
            var horizontalAlign = HorizontalAlignment;
            var verticalAlign = VerticalAlignment;
            var margin = Margin;

            switch (horizontalAlign)
            {
                case HorizontalAlignment.Left:
                    origin.X = finalRect.Left;
                    break;

                case HorizontalAlignment.Right:
                    origin.X = finalRect.Right - desiredSize.X;
                    break;

                case HorizontalAlignment.Center:
                    origin.X = finalRect.Left + (rectSize.X - desiredSize.X) / 2;
                    break;
            }

            switch (verticalAlign)
            {
                case VerticalAlignment.Top:
                    origin.Y = finalRect.Top;
                    break;

                case VerticalAlignment.Bottom:
                    origin.Y = finalRect.Bottom - desiredSize.Y;
                    break;

                case VerticalAlignment.Center:
                    origin.Y += (rectSize.Y - desiredSize.Y) / 2;
                    break;
            }

            Vector2 position = Vector2.Zero;
            Vector2 positionExtend = Vector2.Zero;

            position.X += margin.Left;
            position.Y += margin.Top;

            // do not compensate size loss in stretch mode, this would cause overlap.
            if (horizontalAlign != HorizontalAlignment.Stretch)
            {
                position.X -= margin.Right;
                positionExtend.X += margin.Left;
            }

            positionExtend.X -= margin.Right;
            positionExtend.Y -= margin.Bottom;

            // do not compensate size loss in stretch mode, this would cause overlap.
            if (verticalAlign != VerticalAlignment.Stretch)
            {
                positionExtend.Y += margin.Top;
                position.Y -= margin.Bottom;
            }

            position += origin;
            positionExtend += origin + desiredSize;

            Vector2 size = positionExtend - position;

            Vector2 contentPosition = position;

            BaseOffset = Matrix3x2.CreateTranslation(position);
            ContentOffset = Matrix3x2.CreateTranslation(contentPosition);

            size = ArrangeOverwrite(size);

            positionExtend = position + size;

            Vector2 contentPositionExtend = positionExtend;
            Vector2 contentSize = contentPositionExtend - contentPosition;

            ActualWidth = size.X;
            ActualHeight = size.Y;
            RenderSize = size;
            BoundingBox = VisualClip = new RectangleF(position, size);
            InnerContentBounds = new RectangleF(contentPosition, contentSize);
        }

        protected virtual Vector2 ArrangeOverwrite(Vector2 size)
        {
            return size;
        }
    }
}
