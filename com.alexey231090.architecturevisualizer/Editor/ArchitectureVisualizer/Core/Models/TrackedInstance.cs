using System;
using UnityEngine;
using UnityEngine.UIElements;
using ArchitectureVisualizer.Models;

namespace ArchitectureVisualizer.Models
{
    [Serializable]
    public class TrackedInstance
    {
        public int instanceId;
        public string lastValue;
        public double highlightStartTime;
        public double lastCheckTime;
        [NonSerialized]
        public Label valueLabel;
        [NonSerialized]
        public Component component;
    }
}
