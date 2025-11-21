#nullable enable

namespace HexaEngine.UI.XamlGen
{
    using System;
    using System.Collections.Generic;

    public struct ClrTypeName : IEquatable<ClrTypeName>
    {
        public string Name;
        public string Namespace;

        public ClrTypeName(string name, string @namespace)
        {
            Name = name;
            Namespace = @namespace;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is ClrTypeName name && Equals(name);
        }

        public readonly bool Equals(ClrTypeName other)
        {
            return Name == other.Name &&
                   Namespace == other.Namespace;
        }

        public override readonly int GetHashCode()
        {
            int hashCode = -179327946;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Namespace);
            return hashCode;
        }

        public static bool operator ==(ClrTypeName left, ClrTypeName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ClrTypeName left, ClrTypeName right)
        {
            return !(left == right);
        }

        public override readonly string ToString()
        {
            return $"{Namespace}.{Name}";
        }
    }

    public static class ClrTypeNameExtensions
    {
        public static ClrTypeName GetClrTypeName(this Type type)
        {
            return new ClrTypeName(type.Name, type.Namespace ?? string.Empty);
        }
    }
}