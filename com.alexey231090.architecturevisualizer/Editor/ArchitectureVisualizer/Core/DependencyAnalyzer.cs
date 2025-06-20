using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace ArchitectureVisualizer
{
    public static class DependencyAnalyzer
    {
        public static void AnalyzeScriptsInFolder(string folderPath)
        {
            Debug.Log($"Analyzing scripts in folder: {folderPath}");
            try
            {
                string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { folderPath });
                foreach (string guid in scriptGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path))
                    {
                        Debug.Log($"Found script: {path}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error analyzing scripts: {ex.Message}");
                throw;
            }
        }

        public static void CollectDependencyData(string selectedFolder, DependencyData dependencyData)
        {
            dependencyData = new DependencyData();
            string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { selectedFolder });
            foreach (var guid in scriptGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Packages/") || path.Contains("Library/")) continue;
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null) continue;
                var type = script.GetClass();
                if (type == null) continue;
                AnalyzeDependencies(type, dependencyData);
            }
        }

        public static void AnalyzeDependencies(System.Type type, DependencyData dependencyData)
        {
            var events = type.GetEvents();
            foreach (var evt in events)
            {
                dependencyData.Events.Add(new DependencyData.EventData
                {
                    Generator = type.Name,
                    Subscriber = "Не определено",
                    SubscriberMethod = "Не определено",
                    Description = $"Событие {evt.Name}"
                });
            }
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType.IsSubclassOf(typeof(UnityEngine.Component)))
                {
                    dependencyData.HardDependencies.Add(new DependencyData.HardDependencyData
                    {
                        Consumer = type.Name,
                        Dependency = field.FieldType.Name,
                        AccessMethod = "GetComponent",
                        Risks = "Жёсткая связь через компонент"
                    });
                }
                else if (field.FieldType.IsSubclassOf(typeof(UnityEngine.ScriptableObject)))
                {
                    dependencyData.ScriptableObjects.Add(new DependencyData.ScriptableObjectData
                    {
                        SOObject = field.FieldType.Name,
                        User = type.Name,
                        MethodOrProperty = field.Name,
                        Notes = "Использует ScriptableObject"
                    });
                }
            }
            var attributes = type.GetCustomAttributes(true);
            foreach (var attr in attributes)
            {
                if (attr.GetType().Name.Contains("Singleton"))
                {
                    dependencyData.Singletons.Add(new DependencyData.SingletonData
                    {
                        SingletonClass = type.Name,
                        User = "Не определено",
                        AccessMethod = "Instance",
                        Problems = "Потенциальные проблемы Singleton"
                    });
                }
            }
            var interfaces = type.GetInterfaces();
            foreach (var iface in interfaces)
            {
                dependencyData.Dependencies.Add(new DependencyData.DIData
                {
                    Class = type.Name,
                    Interface = iface.Name,
                    InjectionMethod = "Constructor/Field",
                    Notes = "Внедрение зависимости через интерфейс"
                });
            }
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in methods)
            {
                if (method.Name.Contains("Send") || method.Name.Contains("Publish"))
                {
                    dependencyData.Messages.Add(new DependencyData.MessageData
                    {
                        Sender = type.Name,
                        Receiver = "Не определено",
                        MessageType = method.Name,
                        Notes = "Отправка сообщения"
                    });
                }
                else if (method.Name.Contains("Receive") || method.Name.Contains("Subscribe"))
                {
                    dependencyData.Messages.Add(new DependencyData.MessageData
                    {
                        Sender = "Не определено",
                        Receiver = type.Name,
                        MessageType = method.Name,
                        Notes = "Получение сообщения"
                    });
                }
            }
        }
    }
} 