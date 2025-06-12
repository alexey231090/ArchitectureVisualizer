using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace ArchitectureVisualizer
{
    public class DependencyNode : Node
    {
        public string type;
        public Port input;
        public Port output;

        public DependencyNode(string title, string type)
        {
            this.title = title;
            this.type = type;

            // Создаем порты
            input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            input.portName = "Input";
            inputContainer.Add(input);

            output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            output.portName = "Output";
            outputContainer.Add(output);

            // Добавляем стили
            AddToClassList("dependency-node");

            // Обновляем расширение
            RefreshExpandedState();
            RefreshPorts();
        }

        public void SetTitle(string newTitle)
        {
            title = newTitle;
            var titleLabel = titleContainer.Q<Label>("title");
            if (titleLabel != null)
            {
                titleLabel.text = newTitle;
            }
        }
    }
} 