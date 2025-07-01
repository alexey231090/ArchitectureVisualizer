using System;
using System.Collections.Generic;

namespace ArchitectureVisualizer.Models
{
    [Serializable]
    public class TrackingPath
    {
        public string pathName;
        public string description;
        public string category = "Default";
        public List<TrackingStep> steps = new List<TrackingStep>();
        public bool isTracking;
        public string lastUpdateTime;
        public bool isExpanded = true;
    }
}
