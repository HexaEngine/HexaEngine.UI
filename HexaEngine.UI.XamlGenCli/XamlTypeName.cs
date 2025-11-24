namespace HexaEngine.UI.XamlGenCli
{
    public struct XamlTypeName
    {
        public string Raw;
        public int NamespaceIndex;

        public XamlTypeName(string raw)
        {
            Raw = raw;
            NamespaceIndex = raw.IndexOf(':');
        }

        public readonly ReadOnlySpan<char> Name => NamespaceIndex >= 0 ? Raw.AsSpan(NamespaceIndex + 1) : Raw.AsSpan();

        public readonly ReadOnlySpan<char> Namespace => NamespaceIndex >= 0 ? Raw.AsSpan(0, NamespaceIndex) : [];

        public readonly bool HasNamespace => NamespaceIndex >= 0;

        public static implicit operator string(in XamlTypeName type) => type.Raw;

        public static explicit operator XamlTypeName(string raw) => new(raw);

        public override readonly string ToString() => Raw;
    }
}
