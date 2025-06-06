using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

public class ScriptAnalyzer
{
    private const int MAX_NODES = 100; // Максимальное количество узлов для отображения
    private const int GRID_SPACING = 250; // Расстояние между узлами

    public class ScriptInfo
    {
        public string Name { get; set; }
        public List<DependencyInfo> Dependencies { get; set; } = new List<DependencyInfo>();
    }

    public class DependencyInfo
    {
        public string Requester { get; set; }
        public string DataSource { get; set; }
        public string DataType { get; set; }
        public string AccessMethod { get; set; }
    }

    private static List<ScriptInfo> analyzedScripts = new List<ScriptInfo>();

    public static List<ScriptInfo> GetAnalyzedScripts()
    {
        return analyzedScripts;
    }

    public static void AnalyzeScripts(DependencyGraphView graph)
    {
        Debug.Log("Начинаем анализ скриптов...");
        analyzedScripts.Clear();
        
        // Получаем все скрипты в проекте, включая Assets
        string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { "Assets" });
        
        // Фильтруем скрипты, исключая системные и скрипты в папке Editor
        var filteredScripts = scriptGuids
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => 
                !path.Contains("Packages/") && 
                !path.Contains("Library/") &&
                !path.Contains("/Editor/") && // Исключаем скрипты в папке Editor
                path.EndsWith(".cs")) // Убеждаемся, что это C# скрипт
            .Take(MAX_NODES)
            .ToList();
        
        Dictionary<string, DependencyNode> nodes = new Dictionary<string, DependencyNode>();

        // Создаем узлы для каждого скрипта
        foreach (string path in filteredScripts)
        {
            string scriptName = Path.GetFileNameWithoutExtension(path);
            var node = new DependencyNode { title = scriptName };
            graph.AddElementWithLogging(node);
            nodes[scriptName] = node;

            // Создаем информацию о скрипте для таблицы
            var scriptInfo = new ScriptInfo { Name = scriptName };
            analyzedScripts.Add(scriptInfo);
        }

        // Анализируем зависимости только между отфильтрованными скриптами
        foreach (string path in filteredScripts)
        {
            string scriptName = Path.GetFileNameWithoutExtension(path);
            string scriptContent = File.ReadAllText(path);
            var scriptInfo = analyzedScripts.First(s => s.Name == scriptName);
            
            // Анализ зависимостей
            var dependencies = AnalyzeDependencies(scriptContent, scriptName);
            scriptInfo.Dependencies.AddRange(dependencies);
            
            // Создаем связи для найденных зависимостей
            foreach (var dependency in dependencies)
            {
                string dependencyName = dependency.DataSource;
                if (nodes.ContainsKey(dependencyName))
                {
                    var edge = new Edge 
                    { 
                        output = nodes[scriptName].output,
                        input = nodes[dependencyName].input
                    };
                    graph.AddElementWithLogging(edge);
                }
            }
        }

        // Располагаем узлы в сетке
        LayoutNodes(nodes.Values.ToList());

        // Центрируем граф
        graph.CenterGraph();

        Debug.Log("Анализ завершен!");
    }

    private static List<DependencyInfo> AnalyzeDependencies(string scriptContent, string scriptName)
    {
        var dependencies = new List<DependencyInfo>();
        
        // Поиск [SerializeField] полей
        var serializedFields = Regex.Matches(scriptContent, @"\[SerializeField\].*?private\s+(\w+)\s+(\w+);");
        foreach (Match match in serializedFields)
        {
            var dataType = match.Groups[1].Value;
            var fieldName = match.Groups[2].Value;
            
            dependencies.Add(new DependencyInfo
            {
                Requester = scriptName,
                DataSource = dataType, // Используем тип как источник данных
                DataType = fieldName,  // Используем имя поля как тип данных
                AccessMethod = "[SerializeField]"
            });
        }

        // Поиск GetComponent
        var getComponents = Regex.Matches(scriptContent, @"GetComponent<(\w+)>\(\)");
        foreach (Match match in getComponents)
        {
            var componentType = match.Groups[1].Value;
            dependencies.Add(new DependencyInfo
            {
                Requester = scriptName,
                DataSource = "GameObject",
                DataType = componentType,
                AccessMethod = "GetComponent"
            });
        }

        // Поиск FindObjectOfType
        var findObjects = Regex.Matches(scriptContent, @"FindObjectOfType<(\w+)>\(\)");
        foreach (Match match in findObjects)
        {
            var objectType = match.Groups[1].Value;
            dependencies.Add(new DependencyInfo
            {
                Requester = scriptName,
                DataSource = "Scene",
                DataType = objectType,
                AccessMethod = "FindObjectOfType"
            });
        }

        // Поиск прямых ссылок на другие компоненты
        var componentReferences = Regex.Matches(scriptContent, @"private\s+(\w+)\s+(\w+);");
        foreach (Match match in componentReferences)
        {
            var dataType = match.Groups[1].Value;
            var fieldName = match.Groups[2].Value;
            
            // Проверяем, что это не базовый тип Unity
            if (!dataType.StartsWith("Unity") && !dataType.StartsWith("System"))
            {
                dependencies.Add(new DependencyInfo
                {
                    Requester = scriptName,
                    DataSource = dataType,
                    DataType = fieldName,
                    AccessMethod = "Direct Reference"
                });
            }
        }

        Debug.Log($"Найдено {dependencies.Count} зависимостей для {scriptName}");
        foreach (var dep in dependencies)
        {
            Debug.Log($"Зависимость: {dep.Requester} -> {dep.DataSource} ({dep.DataType}) через {dep.AccessMethod}");
        }

        return dependencies;
    }

    private static void LayoutNodes(List<DependencyNode> nodes)
    {
        if (nodes.Count == 0)
        {
            Debug.LogWarning("Нет узлов для размещения!");
            return;
        }

        Debug.Log($"Размещаем {nodes.Count} узлов...");

        // Простой алгоритм размещения узлов в сетке
        int nodesPerRow = Mathf.CeilToInt(Mathf.Sqrt(nodes.Count));
        int currentX = 0;
        int currentY = 0;

        foreach (var node in nodes)
        {
            var rect = new Rect(
                currentX * GRID_SPACING,
                currentY * GRID_SPACING,
                200, // Ширина узла
                100  // Высота узла
            );
            
            node.SetPosition(rect);
            Debug.Log($"Узел {node.title} размещен в позиции: x={rect.x}, y={rect.y}");

            currentX++;
            if (currentX >= nodesPerRow)
            {
                currentX = 0;
                currentY++;
            }
        }
    }
} 