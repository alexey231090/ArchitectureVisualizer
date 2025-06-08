using System.Collections.Generic;

public class DependencyData
{
    public class EventData
    {
        public string Generator { get; set; }
        public string Subscriber { get; set; }
        public string SubscriberMethod { get; set; }
        public string Description { get; set; }
    }

    public class HardDependencyData
    {
        public string Consumer { get; set; }
        public string Dependency { get; set; }
        public string AccessMethod { get; set; }
        public string Risks { get; set; }
    }

    public class DIData
    {
        public string Class { get; set; }
        public string Dependency { get; set; }
        public string InjectionMethod { get; set; }
    }

    public class ScriptableObjectData
    {
        public string SOObject { get; set; }
        public string User { get; set; }
        public string MethodOrProperty { get; set; }
    }

    public class SingletonData
    {
        public string SingletonClass { get; set; }
        public string User { get; set; }
        public string AccessMethod { get; set; }
        public string Problems { get; set; }
    }

    public class MessageBusData
    {
        public string Signal { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string HandlerMethod { get; set; }
    }

    public List<EventData> Events { get; } = new List<EventData>();
    public List<HardDependencyData> HardDependencies { get; } = new List<HardDependencyData>();
    public List<DIData> Dependencies { get; } = new List<DIData>();
    public List<ScriptableObjectData> ScriptableObjects { get; } = new List<ScriptableObjectData>();
    public List<SingletonData> Singletons { get; } = new List<SingletonData>();
    public List<MessageBusData> MessageBus { get; } = new List<MessageBusData>();
} 