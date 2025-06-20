using System.Collections.Generic;

namespace ArchitectureVisualizer
{
    public class DependencyData
    {
        public List<EventData> Events = new List<EventData>();
        public List<HardDependencyData> HardDependencies = new List<HardDependencyData>();
        public List<DIData> Dependencies = new List<DIData>();
        public List<ScriptableObjectData> ScriptableObjects = new List<ScriptableObjectData>();
        public List<SingletonData> Singletons = new List<SingletonData>();
        public List<MessageData> Messages = new List<MessageData>();

        public class EventData
        {
            public string Generator;
            public string Subscriber;
            public string SubscriberMethod;
            public string Description;
        }

        public class HardDependencyData
        {
            public string Consumer;
            public string Dependency;
            public string AccessMethod;
            public string Risks;
        }

        public class DIData
        {
            public string Class;
            public string Interface;
            public string InjectionMethod;
            public string Notes;
        }

        public class ScriptableObjectData
        {
            public string SOObject;
            public string User;
            public string MethodOrProperty;
            public string Notes;
        }

        public class SingletonData
        {
            public string SingletonClass;
            public string User;
            public string AccessMethod;
            public string Problems;
        }

        public class MessageData
        {
            public string Sender;
            public string Receiver;
            public string MessageType;
            public string Notes;
        }
    }
} 