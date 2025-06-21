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
        public string instanceName;
        public string currentValue;
        public string previousValue;
        public bool hasChanged;
        public double lastChangeTime;
    }

    [Serializable]
    public class TrackingStep
    {
        public string scriptName;
        public string variableName;
        public string comment;
        public List<TrackedInstance> trackedInstances = new List<TrackedInstance>();
    }

    [Serializable]
    public class TrackingPath
    {
        public string pathName;
        public string description;
        public List<TrackingStep> steps = new List<TrackingStep>();
        public bool isTracking;
        public string lastUpdateTime;
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

        static EventTrackingManager()
        {
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
            string json = JsonUtility.ToJson(serializedData);
            EditorPrefs.SetString(TrackingPathsKey, json);
        }

        public static void LoadPaths()
        {
            if (EditorPrefs.HasKey(TrackingPathsKey))
            {
                string json = EditorPrefs.GetString(TrackingPathsKey);
                if (!string.IsNullOrEmpty(json))
                {
                    var loadedData = JsonUtility.FromJson<Serialization<TrackingPath>>(json);
                    if(loadedData != null)
                    {
                        TrackingPaths = loadedData.ToList();
                        return;
                    }
                }
            }
            TrackingPaths = new List<TrackingPath>();
        }
    }
} 