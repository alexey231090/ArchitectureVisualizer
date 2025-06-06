using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class DependencyGraphView : GraphView
{
    private new List<DependencyNode> nodes = new List<DependencyNode>();

    public DependencyGraphView()
    {
        // Добавляем манипуляторы для масштабирования и перетаскивания
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        this.AddManipulator(new ContentZoomer());

        // Добавляем сетку на фон
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        // Загружаем стили
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/Resources/styles.uss");
        if (styleSheet != null)
        {
            styleSheets.Add(styleSheet);
        }
    }

    private void AddTestNode()
    {
        var testNode = new DependencyNode { title = "Test Node" };
        testNode.SetPosition(new Rect(100, 100, 200, 100));
        AddElementWithLogging(testNode);
        Debug.Log("Тестовый узел добавлен");
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach((port) =>
        {
            if (startPort != port && startPort.node != port.node)
            {
                compatiblePorts.Add(port);
            }
        });
        return compatiblePorts;
    }

    public void AddElementWithLogging(GraphElement element)
    {
        if (element is DependencyNode node)
        {
            nodes.Add(node);
        }
        AddElement(element);
    }

    public void ClearGraph()
    {
        nodes.Clear();
        Clear();
        
        // Восстанавливаем сетку на фон
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
    }

    public void CenterGraph()
    {
        if (nodes.Count == 0) return;

        // Используем фиксированные значения для центрирования
        float targetCenterX = 400;
        float targetCenterY = 300;

        // Находим границы графа
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (var node in nodes)
        {
            var pos = node.GetPosition();
            if (!float.IsNaN(pos.x) && !float.IsNaN(pos.y))
            {
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x + pos.width);
                minY = Mathf.Min(minY, pos.y);
                maxY = Mathf.Max(maxY, pos.y + pos.height);
            }
        }

        // Если не нашли валидные координаты, используем значения по умолчанию
        if (minX == float.MaxValue)
        {
            minX = 0;
            maxX = 800;
            minY = 0;
            maxY = 600;
        }

        // Вычисляем центр графа
        float graphCenterX = (minX + maxX) / 2;
        float graphCenterY = (minY + maxY) / 2;

        // Вычисляем смещение для центрирования
        float offsetX = targetCenterX - graphCenterX;
        float offsetY = targetCenterY - graphCenterY;

        // Перемещаем все узлы
        foreach (var node in nodes)
        {
            var pos = node.GetPosition();
            if (float.IsNaN(pos.x) || float.IsNaN(pos.y))
            {
                pos.x = targetCenterX;
                pos.y = targetCenterY;
            }
            else
            {
                pos.x += offsetX;
                pos.y += offsetY;
            }
            node.SetPosition(pos);
        }
    }
}
