using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArchitectureVisualizer
{
    [Serializable]
    public class TrackingStep
    {
        public string scriptName;
        public string variableName;
        public string comment;
        public string currentValue;
        public string previousValue;
        public bool hasChanged;
        // Для затухающей подсветки
        public Dictionary<string, double> lastChangeTime = new Dictionary<string, double>();
    }

    [Serializable]
    public class TrackingPath
    {
        public string pathName;
        public string description;
        public List<TrackingStep> steps;
        public bool isTracking;
        public string lastUpdateTime;

        public TrackingPath()
        {
            steps = new List<TrackingStep>();
            isTracking = false;
            lastUpdateTime = "";
        }
    }

    public static class EventTrackingManager
    {
        // Здесь будет логика управления трекингом событий, хранение путей и шагов
        // Пример:
        public static List<TrackingPath> TrackingPaths = new List<TrackingPath>();
        // Методы для добавления, удаления, обновления путей и шагов
    }
} 