using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArchitectureVisualizer.Models
{
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
}
