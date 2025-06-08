using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

public class DependencyNode : Node
{
    public Port input { get; private set; }
    public Port output { get; private set; }

    public DependencyNode()
    {
        Debug.Log("Создание нового узла зависимости");
        
        // Создаем порты
        input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        input.portName = "Input";
        inputContainer.Add(input);
        Debug.Log("Добавлен входной порт");

        output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
        output.portName = "Output";
        outputContainer.Add(output);
        Debug.Log("Добавлен выходной порт");

        // Добавляем стили
        AddToClassList("dependency-node");
        
        // Устанавливаем размер
        style.minWidth = 200;
        style.minHeight = 100;
        
        // Добавляем заголовок
        var titleLabel = new Label(title) { name = "title" };
        titleContainer.Add(titleLabel);
        Debug.Log("Узел создан успешно");
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