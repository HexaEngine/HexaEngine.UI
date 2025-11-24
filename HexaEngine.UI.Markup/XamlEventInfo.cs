using HexaEngine.UI.XamlGen;
using System.Reflection;

namespace HexaEngine.UI.XamlGenCli
{
    public struct XamlEventInfo
    {
        public EventInfo? Event;
        public FieldInfo? Field;
        public RoutedEvent? RoutedEvent;

        public XamlEventInfo(EventInfo? @event, FieldInfo? field, RoutedEvent? routedEvent)
        {
            Event = @event;
            RoutedEvent = routedEvent;
            Field = field;
        }
    }

    public struct RoutedEvent
    {
        public object Instance;

        public RoutedEvent(object instance)
        {
            Instance = instance;
        }

        public static Type Type => AssemblyCache.RoutedEventType;

        public readonly string Name => (string)Type.GetProperty("Name")!.GetValue(Instance)!;

        public readonly Type HandlerType => (Type)Type.GetProperty("HandlerType")!.GetValue(Instance)!;

        public readonly Type OwnerType => (Type)Type.GetProperty("OwnerType")!.GetValue(Instance)!;
    }
}
