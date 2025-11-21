namespace HexaEngine.UI.XamlGen
{
    using System;

    public struct DependencyProperty
    {
        public object Instance;

        public static Type Type => AssemblyCache.DependencyPropertyType;

        public DependencyProperty(object instance)
        {
            Instance = instance;
        }

        public readonly string Name => (string)Type.GetProperty("Name")!.GetValue(Instance)!;

        public readonly Type PropertyType => (Type)Type.GetProperty("PropertyType")!.GetValue(Instance)!;
    }

}