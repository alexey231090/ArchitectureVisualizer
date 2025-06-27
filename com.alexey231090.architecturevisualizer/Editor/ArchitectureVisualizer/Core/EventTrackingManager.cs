using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ArchitectureVisualizer
{
    [Serializable]
    public class TrackedInstance
    {
        public int instanceId;
        public string lastValue;
        public double highlightStartTime;
        public double lastCheckTime;

        [NonSerialized]
        public UnityEngine.UIElements.Label valueLabel;
        
        [NonSerialized]
        public Component component;
    }

    [Serializable]
    public class TrackingStep
    {
        public string scriptName;
        public string variableName;
        public List<string> variableNames = new List<string>();
        public string comment;
        public List<TrackedInstance> trackedInstances = new List<TrackedInstance>();

        public bool isMonoBehaviourTracked = true;
        public string hostScriptName;
        public string instanceFieldName;
    }

    [Serializable]
    public class TrackingPath
    {
        public string pathName;
        public string description;
        public List<TrackingStep> steps = new List<TrackingStep>();
        public bool isTracking;
        public string lastUpdateTime;
        public bool isExpanded = true;
    }
    
    // Helper for serializing lists
    [Serializable]
    public class Serialization<T>
    {
        [SerializeField]
        private List<T> items;
        public List<T> ToList() { return items; }
        public Serialization(List<T> items) { this.items = items; }
    }

    public static class EventTrackingManager
    {
        public static List<TrackingPath> TrackingPaths = new List<TrackingPath>();
        private const string TrackingPathsKey = "EventTrackingManager_TrackingPaths";
        private const string AiPathsFileName = "ArchitectureVisualizer.ai.json";
        private static List<TrackingPath> AiTrackingPaths = new List<TrackingPath>();

        public static void EnsureJsonFilesExist()
        {
            string dir = System.IO.Path.Combine(Application.dataPath, "../ProjectSettings");
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            string filePath = System.IO.Path.Combine(dir, "ArchitectureVisualizer.paths.json");
            if (!System.IO.File.Exists(filePath))
            {
                var empty = new Serialization<TrackingPath>(new List<TrackingPath>());
                string json = JsonUtility.ToJson(empty, true);
                System.IO.File.WriteAllText(filePath, json);
            }
            string aiFilePath = System.IO.Path.Combine(dir, AiPathsFileName);
            if (!System.IO.File.Exists(aiFilePath))
            {
                string aiJson = "{\n  \"items\": []\n}\n";
                System.IO.File.WriteAllText(aiFilePath, aiJson);
            }
            string aiInstructionsPath = System.IO.Path.Combine(dir, "ArchitectureVisualizer.ai.instructions.txt");
            if (!System.IO.File.Exists(aiInstructionsPath))
            {
                string instructions = "Формат файла ArchitectureVisualizer.ai.json для AI:\n\n{\n  \"items\": [\n    {\n      \"pathName\": \"Название цепочки\",\n      \"description\": \"Описание цепочки\",\n      \"steps\": [\n        {\n          \"scriptName\": \"ПолноеИмяСкрипта\",\n          \"variableNames\": [\"имяПеременной1\", \"имяПеременной2\"],\n          \"comment\": \"Комментарий к шагу\"\n        }\n      ]\n    }\n  ]\n}\n\nПравила:\n- Все новые пути добавлять в массив items.\n- Не использовать комментарии // или /* ... */.\n- Не добавлять лишние поля в корневой объект.\n- Пример выше можно копировать и изменять.\n- Файл должен быть в кодировке UTF-8.\n\nПример рабочего пути:\n\n{\n  \"items\": [\n    {\n      \"pathName\": \"Отслеживание жизни зомби\",\n      \"description\": \"Путь для дебага переменной жизни зомби.\",\n      \"steps\": [\n        {\n          \"scriptName\": \"FpsZomby.Zombi\",\n          \"variableNames\": [\"lifeZombi\"],\n          \"comment\": \"Текущее значение жизни зомби.\"\n        }\n      ]\n    }\n  ]\n}\n";
                System.IO.File.WriteAllText(aiInstructionsPath, instructions);
            }
        }

        static EventTrackingManager()
        {
            EnsureJsonFilesExist();
            LoadPaths();
        }

        public static void AddPath(TrackingPath path)
        {
            if (path != null)
            {
                TrackingPaths.Add(path);
                SavePaths();
            }
        }

        public static void DeletePath(TrackingPath path)
        {
            if (path != null && TrackingPaths.Contains(path))
            {
                TrackingPaths.Remove(path);
                SavePaths();
            }
        }

        public static void UpdatePath()
        {
            SavePaths();
        }

        public static void AddStep(TrackingPath path, TrackingStep step)
        {
            if (path != null && step != null)
            {
                if (path.steps == null)
                {
                    path.steps = new List<TrackingStep>();
                }
                path.steps.Add(step);
                SavePaths();
            }
        }

        public static void DeleteStep(TrackingPath path, TrackingStep step)
        {
            if (path != null && step != null && path.steps != null && path.steps.Contains(step))
            {
                path.steps.Remove(step);
                SavePaths();
            }
        }

        public static void SavePaths()
        {
            var serializedData = new Serialization<TrackingPath>(TrackingPaths);
            string json = JsonUtility.ToJson(serializedData, true);
            string dir = System.IO.Path.Combine(Application.dataPath, "../ProjectSettings");
            string filePath = System.IO.Path.Combine(dir, "ArchitectureVisualizer.paths.json");
            System.IO.File.WriteAllText(filePath, json);
        }

        public static void LoadPaths()
        {
            string dir = System.IO.Path.Combine(Application.dataPath, "../ProjectSettings");
            string filePath = System.IO.Path.Combine(dir, "ArchitectureVisualizer.paths.json");
            if (System.IO.File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);
                Debug.Log($"[ArchitectureVisualizer] Загружаю основной json: {filePath}");
                if (!string.IsNullOrEmpty(json))
                {
                    var loadedData = JsonUtility.FromJson<Serialization<TrackingPath>>(json);
                    if (loadedData != null)
                    {
                        TrackingPaths = loadedData.ToList();
                        Debug.Log($"[ArchitectureVisualizer] Основной json загружен, путей: {TrackingPaths.Count}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ArchitectureVisualizer] Основной json не десериализован!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ArchitectureVisualizer] Основной json пустой!");
                }
            }
            else
            {
                Debug.LogWarning($"[ArchitectureVisualizer] Основной json не найден: {filePath}");
                TrackingPaths = new List<TrackingPath>();
            }
            // Загрузка AI-файла
            string aiFilePath = System.IO.Path.Combine(dir, AiPathsFileName);
            if (System.IO.File.Exists(aiFilePath))
            {
                string aiJson = System.IO.File.ReadAllText(aiFilePath);
                Debug.Log($"[ArchitectureVisualizer] Загружаю AI json: {aiFilePath}");
                if (!string.IsNullOrEmpty(aiJson))
                {
                    var loadedAiData = JsonUtility.FromJson<Serialization<TrackingPath>>(aiJson);
                    if (loadedAiData != null)
                    {
                        AiTrackingPaths = loadedAiData.ToList();
                        Debug.Log($"[ArchitectureVisualizer] AI json загружен, путей: {AiTrackingPaths.Count}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ArchitectureVisualizer] AI json не десериализован!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ArchitectureVisualizer] AI json пустой!");
                }
            }
            else
            {
                Debug.LogWarning($"[ArchitectureVisualizer] AI json не найден: {aiFilePath}");
                AiTrackingPaths = new List<TrackingPath>();
            }
        }

        public static List<TrackingPath> AllTrackingPaths
        {
            get
            {
                var all = new List<TrackingPath>();
                if (TrackingPaths != null) all.AddRange(TrackingPaths);
                if (AiTrackingPaths != null) all.AddRange(AiTrackingPaths);
                return all;
            }
        }

        public static void AcceptAiPath(TrackingPath aiPath)
        {
            if (aiPath != null && AiTrackingPaths.Contains(aiPath))
            {
                AiTrackingPaths.Remove(aiPath);
                TrackingPaths.Add(aiPath);
                SavePaths();
                // Перезаписать AI-файл без этого пути
                SaveAiPaths();
            }
        }

        private static void SaveAiPaths()
        {
            var serializedData = new Serialization<TrackingPath>(AiTrackingPaths);
            string dir = System.IO.Path.Combine(Application.dataPath, "../ProjectSettings");
            string filePath = System.IO.Path.Combine(dir, AiPathsFileName);
            string json = JsonUtility.ToJson(serializedData, true);
            System.IO.File.WriteAllText(filePath, json);
        }
    }
} 