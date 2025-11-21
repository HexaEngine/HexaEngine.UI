namespace HexaEngine.UI.XamlGen
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Registry for value converters that convert XAML string values to C# code.
    /// Uses Type-based registration for proper lookup.
    /// </summary>
    public static class ValueConverter
    {
        private static readonly Dictionary<Type, IValueConverter> typeConverters = new();

        // Singleton converter instances - reuse, don't recreate!
        private static readonly BrushConverter brushConverter = new();

        private static readonly ThicknessConverter thicknessConverter = new();
        private static readonly GridLengthConverter gridLengthConverter = new();

        /// <summary>
        /// Registers a converter for a specific Type.
        /// </summary>
        public static void RegisterTypeConverter(Type type, IValueConverter converter)
        {
            typeConverters[type] = converter;
        }

        /// <summary>
        /// Tries to convert a value using a registered converter for the given property type.
        /// </summary>
        public static bool TryConvert(Type propertyType, string value, out string code)
        {
            if (propertyType == null)
            {
                code = null;
                return false;
            }

            // Direct type match
            if (typeConverters.TryGetValue(propertyType, out var converter))
            {
                return converter.TryConvert(value, out code);
            }

            // Check by type name as last resort (for when assemblies aren't loaded yet)
            // Use singleton instances - NEVER create new ones!
            string typeName = propertyType.Name;
            IValueConverter nameBasedConverter = typeName switch
            {
                "Brush" or "SolidColorBrush" => brushConverter,
                "Thickness" => thicknessConverter,
                "GridLength" => gridLengthConverter,
                _ => null
            };

            if (nameBasedConverter != null)
            {
                // Cache it for future use
                typeConverters[propertyType] = nameBasedConverter;
                return nameBasedConverter.TryConvert(value, out code);
            }

            code = null;
            return false;
        }

        /// <summary>
        /// Ensures common converters are registered for a loaded assembly.
        /// Call this after loading an assembly via reflection.
        /// </summary>
        public static void EnsureCommonConvertersRegistered(Type[] types)
        {
            foreach (var type in types)
            {
                if (typeConverters.ContainsKey(type))
                    continue;

                // Register based on actual Type identity, not name
                // Use singleton instances - NEVER create new ones!
                if (type.Name == "Brush" || type.Name == "SolidColorBrush")
                {
                    typeConverters[type] = brushConverter;
                }
                else if (type.Name == "Thickness")
                {
                    typeConverters[type] = thicknessConverter;
                }
                else if (type.Name == "GridLength")
                {
                    typeConverters[type] = gridLengthConverter;
                }
            }
        }

        public static string Convert(string value, ReadOnlySpan<char> propertyName, ReadOnlySpan<char> targetTypeName, ReadOnlySpan<char> xmlPrefix)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "null";
            }

            Type? propertyType = AssemblyCache.GetPropertyType(targetTypeName, propertyName, xmlPrefix);

            if (propertyType == null)
            {
                // No type info available - this is an ERROR, not a silent fallback
                throw new InvalidOperationException($"Could not determine type for property '{propertyName}' on type '{targetTypeName}'");
            }

            return Convert(value, propertyType, propertyName);
        }


        public static string Convert(string value, Type propertyType, ReadOnlySpan<char> propertyName)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "null";
            }

            // Try registered converter first
            if (TryConvert(propertyType, value, out string code))
            {
                return code;
            }

            // Handle enum types
            if (propertyType.IsEnum)
            {
                return $"{propertyType.Name}.{value}";
            }

            // Handle boolean
            if (propertyType == typeof(bool))
            {
                if (bool.TryParse(value, out bool boolValue))
                {
                    return boolValue ? "true" : "false";
                }
                throw new FormatException($"Invalid boolean value: '{value}' for property '{propertyName}'");
            }

            // Handle numeric types - VALIDATE the input
            if (IsNumericType(propertyType))
            {
                if (!IsValidNumber(value, propertyType))
                {
                    throw new FormatException($"Invalid numeric value: '{value}' for property '{propertyName}' of type '{propertyType.Name}'");
                }
                return value;
            }

            // Handle string
            if (propertyType == typeof(string))
            {
                return $"\"{value}\"";
            }

            // Unknown type - this is an ERROR
            throw new NotSupportedException($"No converter registered for property '{propertyName}' of type '{propertyType.FullName}'");
        }

        private static bool IsValidNumber(string value, Type numericType)
        {
            System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.InvariantCulture;

            if (numericType == typeof(int))
                return int.TryParse(value, System.Globalization.NumberStyles.Integer, culture, out _);
            if (numericType == typeof(long))
                return long.TryParse(value, System.Globalization.NumberStyles.Integer, culture, out _);
            if (numericType == typeof(short))
                return short.TryParse(value, System.Globalization.NumberStyles.Integer, culture, out _);
            if (numericType == typeof(byte))
                return byte.TryParse(value, System.Globalization.NumberStyles.Integer, culture, out _);
            if (numericType == typeof(uint))
                return uint.TryParse(value, System.Globalization.NumberStyles.Integer, culture, out _);
            if (numericType == typeof(ulong))
                return ulong.TryParse(value, System.Globalization.NumberStyles.Integer, culture, out _);
            if (numericType == typeof(ushort))
                return ushort.TryParse(value, System.Globalization.NumberStyles.Integer, culture, out _);
            if (numericType == typeof(sbyte))
                return sbyte.TryParse(value, System.Globalization.NumberStyles.Integer, culture, out _);
            if (numericType == typeof(float))
                return float.TryParse(value, System.Globalization.NumberStyles.Float, culture, out _);
            if (numericType == typeof(double))
                return double.TryParse(value, System.Globalization.NumberStyles.Float, culture, out _);
            if (numericType == typeof(decimal))
                return decimal.TryParse(value, System.Globalization.NumberStyles.Float, culture, out _);

            return false;
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) ||
                   type == typeof(long) ||
                   type == typeof(short) ||
                   type == typeof(byte) ||
                   type == typeof(uint) ||
                   type == typeof(ulong) ||
                   type == typeof(ushort) ||
                   type == typeof(sbyte) ||
                   type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(decimal);
        }
    }
}