using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class ArchitectureVisualizerWindow : EditorWindow
{
    private DependencyGraphView graph;
    private VisualElement tableContainer;
    private TabView tabView;
    private VisualElement structureContainer;
    private ScrollView tablesContainer;
    private DependencyData dependencyData = new DependencyData();

    [MenuItem("Tools/Architecture Visualizer")]
    public static void ShowWindow()
    {
        var window = GetWindow<ArchitectureVisualizerWindow>();
        window.titleContent = new GUIContent("Architecture Visualizer");
    }

    private void CreateGUI()
    {
        // Создаем контейнер для кнопок
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.marginBottom = 10;
        rootVisualElement.Add(buttonContainer);

        // Кнопка для анализа
        var analyzeButton = new Button(() => AnalyzeProject())
        {
            text = "Analyze Project",
            style = { marginRight = 10 }
        };
        analyzeButton.style.backgroundColor = new Color(0.4f, 0.5f, 0.3f); // Цвет хаки
        analyzeButton.style.color = Color.white; // Белый текст для лучшей читаемости
        buttonContainer.Add(analyzeButton);

        // Кнопка для центрирования
        var centerButton = new Button(() => CenterGraph())
        {
            text = "Center Graph"
        };
        buttonContainer.Add(centerButton);

        // Создаем TabView
        tabView = new TabView();
        tabView.style.flexGrow = 1;
        rootVisualElement.Add(tabView);

        // Добавляем вкладку для таблиц (первая)
        var tablesTab = new Tab("Tables");
        tablesContainer = new ScrollView();
        tablesContainer.style.flexGrow = 1;
        tablesTab.SetContent(tablesContainer);
        tabView.AddTab(tablesTab);

        // Добавляем вкладку для графа (вторая)
        var graphTab = new Tab("Graph");
        graph = new DependencyGraphView();
        graph.style.flexGrow = 1;
        graphTab.SetContent(graph);
        tabView.AddTab(graphTab);

        // Добавляем вкладку для структуры (третья)
        var structureTab = new Tab("Structure");
        structureContainer = new ScrollView();
        structureContainer.style.flexGrow = 1;
        structureTab.SetContent(structureContainer);
        tabView.AddTab(structureTab);
    }

    private void UpdateTables()
    {
        tablesContainer.Clear();
        CollectDependencyData();

        // Создаем таблицы для разных типов зависимостей
        CreateEventsTable();
        CreateHardDependenciesTable();
        CreateDITable();
        CreateScriptableObjectTable();
        CreateSingletonTable();
        CreateMessageBusTable();
    }

    private void CollectDependencyData()
    {
        // Очищаем предыдущие данные
        dependencyData = new DependencyData();

        // Получаем все скрипты
        string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { "Assets" });
        foreach (var guid in scriptGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Packages/") || path.Contains("Library/")) continue;

            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script == null) continue;

            var type = script.GetClass();
            if (type == null) continue;

            // Анализируем зависимости
            AnalyzeDependencies(type);
        }
    }

    private void AnalyzeDependencies(System.Type type)
    {
        // Анализ событий
        var events = type.GetEvents();
        foreach (var evt in events)
        {
            dependencyData.Events.Add(new DependencyData.EventData
            {
                Generator = type.Name,
                Subscriber = "Неизвестно", // Нужно анализировать подписчиков
                SubscriberMethod = "Неизвестно",
                Description = $"Событие {evt.Name}"
            });
        }

        // Анализ полей и свойств
        var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var field in fields)
        {
            // Проверяем на GetComponent
            if (field.FieldType.IsSubclassOf(typeof(UnityEngine.Component)))
            {
                dependencyData.HardDependencies.Add(new DependencyData.HardDependencyData
                {
                    Consumer = type.Name,
                    Dependency = field.FieldType.Name,
                    AccessMethod = "GetComponent",
                    Risks = "Зависимость от компонента"
                });
            }
            // Проверяем на ScriptableObject
            else if (field.FieldType.IsSubclassOf(typeof(UnityEngine.ScriptableObject)))
            {
                dependencyData.ScriptableObjects.Add(new DependencyData.ScriptableObjectData
                {
                    SOObject = field.FieldType.Name,
                    User = type.Name,
                    MethodOrProperty = field.Name
                });
            }
        }

        // Анализ атрибутов
        var attributes = type.GetCustomAttributes(true);
        foreach (var attr in attributes)
        {
            // Проверяем на Singleton
            if (attr.GetType().Name.Contains("Singleton"))
            {
                dependencyData.Singletons.Add(new DependencyData.SingletonData
                {
                    SingletonClass = type.Name,
                    User = "Неизвестно",
                    AccessMethod = "Instance",
                    Problems = "Глобальное состояние"
                });
            }
        }
    }

    private void CreateEventsTable()
    {
        var container = new VisualElement();
        container.style.marginBottom = 20;

        // Заголовок
        var header = new Label("1. Таблица для событий (Events / UnityEvent)");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 16;
        header.style.marginBottom = 10;
        container.Add(header);

        // Создаем таблицу
        var table = new VisualElement();
        table.style.flexDirection = FlexDirection.Column;
        table.style.borderLeftWidth = 1;
        table.style.borderRightWidth = 1;
        table.style.borderTopWidth = 1;
        table.style.borderBottomWidth = 1;
        table.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

        // Заголовки столбцов
        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        headerRow.style.paddingTop = 5;
        headerRow.style.paddingRight = 5;
        headerRow.style.paddingBottom = 5;
        headerRow.style.paddingLeft = 5;

        string[] headers = { "Генератор", "Подписчик", "Метод подписчика", "Описание" };
        foreach (var headerText in headers)
        {
            var headerCell = new Label(headerText);
            headerCell.style.flexGrow = 1;
            headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerCell.style.paddingTop = 5;
            headerCell.style.paddingRight = 5;
            headerCell.style.paddingBottom = 5;
            headerCell.style.paddingLeft = 5;
            headerRow.Add(headerCell);
        }
        table.Add(headerRow);

        // Добавляем строки с данными
        foreach (var evt in dependencyData.Events)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.paddingTop = 5;
            row.style.paddingRight = 5;
            row.style.paddingBottom = 5;
            row.style.paddingLeft = 5;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            AddCell(row, evt.Generator);
            AddCell(row, evt.Subscriber);
            AddCell(row, evt.SubscriberMethod);
            AddCell(row, evt.Description);

            table.Add(row);
        }

        // Добавляем таблицу в контейнер
        container.Add(table);
        tablesContainer.Add(container);
    }

    private void CreateHardDependenciesTable()
    {
        var container = new VisualElement();
        container.style.marginBottom = 20;

        // Заголовок
        var header = new Label("2. Таблица для жёстких зависимостей (GetComponent / public поля)");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 16;
        header.style.marginBottom = 10;
        container.Add(header);

        // Создаем таблицу
        var table = new VisualElement();
        table.style.flexDirection = FlexDirection.Column;
        table.style.borderLeftWidth = 1;
        table.style.borderRightWidth = 1;
        table.style.borderTopWidth = 1;
        table.style.borderBottomWidth = 1;
        table.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

        // Заголовки столбцов
        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        headerRow.style.paddingTop = 5;
        headerRow.style.paddingRight = 5;
        headerRow.style.paddingBottom = 5;
        headerRow.style.paddingLeft = 5;

        string[] headers = { "Класс-потребитель", "Зависимость", "Как получает" };
        foreach (var headerText in headers)
        {
            var headerCell = new Button(() => SortTable(table, headerText))
            {
                text = headerText
            };
            headerCell.style.flexGrow = 1;
            headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerCell.style.paddingTop = 5;
            headerCell.style.paddingRight = 5;
            headerCell.style.paddingBottom = 5;
            headerCell.style.paddingLeft = 5;
            headerCell.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            headerCell.style.borderBottomWidth = 0;
            headerCell.style.borderTopWidth = 0;
            headerCell.style.borderLeftWidth = 0;
            headerCell.style.borderRightWidth = 0;
            headerRow.Add(headerCell);
        }
        table.Add(headerRow);

        // Группируем зависимости по классам-потребителям
        var groupedDependencies = dependencyData.HardDependencies
            .GroupBy(d => d.Consumer)
            .OrderBy(g => g.Key);

        foreach (var group in groupedDependencies)
        {
            // Создаем заголовок группы
            var groupHeader = new VisualElement();
            groupHeader.style.flexDirection = FlexDirection.Row;
            groupHeader.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            groupHeader.style.paddingTop = 5;
            groupHeader.style.paddingRight = 5;
            groupHeader.style.paddingBottom = 5;
            groupHeader.style.paddingLeft = 5;
            groupHeader.style.borderBottomWidth = 1;
            groupHeader.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            // Кнопка сворачивания/разворачивания
            var toggleButton = new Button(() => ToggleGroup(groupHeader))
            {
                text = "▼"
            };
            toggleButton.style.width = 20;
            toggleButton.style.marginRight = 5;
            toggleButton.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            toggleButton.style.borderBottomWidth = 0;
            toggleButton.style.borderTopWidth = 0;
            toggleButton.style.borderLeftWidth = 0;
            toggleButton.style.borderRightWidth = 0;
            groupHeader.Add(toggleButton);

            // Название группы
            var groupLabel = new Label($"{group.Key} ({group.Count()} зависимостей)");
            groupLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            groupLabel.style.flexGrow = 1;
            groupHeader.Add(groupLabel);

            table.Add(groupHeader);

            // Контейнер для строк группы
            var groupContainer = new VisualElement();
            groupContainer.style.flexDirection = FlexDirection.Column;
            groupContainer.name = "GroupContainer";

            // Добавляем строки группы
            foreach (var dep in group.OrderBy(d => d.Dependency))
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.paddingTop = 5;
                row.style.paddingRight = 5;
                row.style.paddingBottom = 5;
                row.style.paddingLeft = 5;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

                AddCell(row, dep.Consumer);
                AddCell(row, dep.Dependency);
                AddCell(row, dep.AccessMethod);

                groupContainer.Add(row);
            }

            table.Add(groupContainer);
        }

        // Добавляем таблицу в контейнер
        container.Add(table);
        tablesContainer.Add(container);
    }

    private void ToggleGroup(VisualElement groupHeader)
    {
        var groupContainer = groupHeader.parent.Children().ElementAt(groupHeader.parent.IndexOf(groupHeader) + 1);
        var toggleButton = (Button)groupHeader.Children().First();
        
        if (groupContainer.style.display == DisplayStyle.None)
        {
            groupContainer.style.display = DisplayStyle.Flex;
            toggleButton.text = "▼";
        }
        else
        {
            groupContainer.style.display = DisplayStyle.None;
            toggleButton.text = "▶";
        }
    }

    private void SortTable(VisualElement table, string columnName)
    {
        // Получаем все группы (пропускаем заголовок таблицы)
        var groups = table.Children().Skip(1).Where((_, index) => index % 2 == 0).ToList();
        var groupContainers = table.Children().Skip(1).Where((_, index) => index % 2 == 1).ToList();

        for (int i = 0; i < groups.Count; i++)
        {
            var groupContainer = groupContainers[i];
            var rows = groupContainer.Children().ToList();
            
            var sortedRows = rows.OrderBy(row => 
            {
                var cells = row.Children().ToList();
                switch (columnName)
                {
                    case "Класс-потребитель":
                        return ((Label)cells[0]).text;
                    case "Зависимость":
                        return ((Label)cells[1]).text;
                    case "Как получает":
                        return ((Label)cells[2]).text;
                    default:
                        return "";
                }
            }).ToList();

            // Очищаем контейнер группы
            foreach (var row in rows)
            {
                groupContainer.Remove(row);
            }

            // Добавляем отсортированные строки
            foreach (var row in sortedRows)
            {
                groupContainer.Add(row);
            }
        }
    }

    private void AddCell(VisualElement row, string text)
    {
        var cell = new Label(text);
        cell.style.flexGrow = 1;
        cell.style.paddingTop = 5;
        cell.style.paddingRight = 5;
        cell.style.paddingBottom = 5;
        cell.style.paddingLeft = 5;
        cell.style.borderRightWidth = 1;
        cell.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
        row.Add(cell);
    }

    private void CreateDITable()
    {
        var container = new VisualElement();
        container.style.marginBottom = 20;

        // Заголовок
        var header = new Label("3. Таблица для DI (внедрение зависимостей)");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 16;
        header.style.marginBottom = 10;
        container.Add(header);

        // Создаем таблицу
        var table = new VisualElement();
        table.style.flexDirection = FlexDirection.Column;
        table.style.borderLeftWidth = 1;
        table.style.borderRightWidth = 1;
        table.style.borderTopWidth = 1;
        table.style.borderBottomWidth = 1;
        table.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

        // Заголовки столбцов
        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        headerRow.style.paddingTop = 5;
        headerRow.style.paddingRight = 5;
        headerRow.style.paddingBottom = 5;
        headerRow.style.paddingLeft = 5;

        string[] headers = { "Класс", "Зависимость", "Способ внедрения" };
        foreach (var headerText in headers)
        {
            var headerCell = new Label(headerText);
            headerCell.style.flexGrow = 1;
            headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerCell.style.paddingTop = 5;
            headerCell.style.paddingRight = 5;
            headerCell.style.paddingBottom = 5;
            headerCell.style.paddingLeft = 5;
            headerRow.Add(headerCell);
        }
        table.Add(headerRow);

        // Добавляем строки с данными
        foreach (var dep in dependencyData.Dependencies)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.paddingTop = 5;
            row.style.paddingRight = 5;
            row.style.paddingBottom = 5;
            row.style.paddingLeft = 5;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            AddCell(row, dep.Class);
            AddCell(row, dep.Dependency);
            AddCell(row, dep.InjectionMethod);

            table.Add(row);
        }

        // Добавляем таблицу в контейнер
        container.Add(table);
        tablesContainer.Add(container);
    }

    private void CreateScriptableObjectTable()
    {
        var container = new VisualElement();
        container.style.marginBottom = 20;

        // Заголовок
        var header = new Label("4. Таблица для ScriptableObject");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 16;
        header.style.marginBottom = 10;
        container.Add(header);

        // Создаем таблицу
        var table = new VisualElement();
        table.style.flexDirection = FlexDirection.Column;
        table.style.borderLeftWidth = 1;
        table.style.borderRightWidth = 1;
        table.style.borderTopWidth = 1;
        table.style.borderBottomWidth = 1;
        table.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

        // Заголовки столбцов
        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        headerRow.style.paddingTop = 5;
        headerRow.style.paddingRight = 5;
        headerRow.style.paddingBottom = 5;
        headerRow.style.paddingLeft = 5;

        string[] headers = { "SO-Объект", "Кто использует", "Метод/свойство" };
        foreach (var headerText in headers)
        {
            var headerCell = new Label(headerText);
            headerCell.style.flexGrow = 1;
            headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerCell.style.paddingTop = 5;
            headerCell.style.paddingRight = 5;
            headerCell.style.paddingBottom = 5;
            headerCell.style.paddingLeft = 5;
            headerRow.Add(headerCell);
        }
        table.Add(headerRow);

        // Добавляем строки с данными
        foreach (var so in dependencyData.ScriptableObjects)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.paddingTop = 5;
            row.style.paddingRight = 5;
            row.style.paddingBottom = 5;
            row.style.paddingLeft = 5;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            AddCell(row, so.SOObject);
            AddCell(row, so.User);
            AddCell(row, so.MethodOrProperty);

            table.Add(row);
        }

        // Добавляем таблицу в контейнер
        container.Add(table);
        tablesContainer.Add(container);
    }

    private void CreateSingletonTable()
    {
        var container = new VisualElement();
        container.style.marginBottom = 20;

        // Заголовок
        var header = new Label("5. Таблица для Singleton");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 16;
        header.style.marginBottom = 10;
        container.Add(header);

        // Создаем таблицу
        var table = new VisualElement();
        table.style.flexDirection = FlexDirection.Column;
        table.style.borderLeftWidth = 1;
        table.style.borderRightWidth = 1;
        table.style.borderTopWidth = 1;
        table.style.borderBottomWidth = 1;
        table.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

        // Заголовки столбцов
        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        headerRow.style.paddingTop = 5;
        headerRow.style.paddingRight = 5;
        headerRow.style.paddingBottom = 5;
        headerRow.style.paddingLeft = 5;

        string[] headers = { "Singleton-класс", "Кто использует", "Как обращается", "Проблемы" };
        foreach (var headerText in headers)
        {
            var headerCell = new Label(headerText);
            headerCell.style.flexGrow = 1;
            headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerCell.style.paddingTop = 5;
            headerCell.style.paddingRight = 5;
            headerCell.style.paddingBottom = 5;
            headerCell.style.paddingLeft = 5;
            headerRow.Add(headerCell);
        }
        table.Add(headerRow);

        // Добавляем строки с данными
        foreach (var singleton in dependencyData.Singletons)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.paddingTop = 5;
            row.style.paddingRight = 5;
            row.style.paddingBottom = 5;
            row.style.paddingLeft = 5;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            AddCell(row, singleton.SingletonClass);
            AddCell(row, singleton.User);
            AddCell(row, singleton.AccessMethod);
            AddCell(row, singleton.Problems);

            table.Add(row);
        }

        // Добавляем таблицу в контейнер
        container.Add(table);
        tablesContainer.Add(container);
    }

    private void CreateMessageBusTable()
    {
        var container = new VisualElement();
        container.style.marginBottom = 20;

        // Заголовок
        var header = new Label("6. Таблица для Message Bus / Signal Bus");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 16;
        header.style.marginBottom = 10;
        container.Add(header);

        // Создаем таблицу
        var table = new VisualElement();
        table.style.flexDirection = FlexDirection.Column;
        table.style.borderLeftWidth = 1;
        table.style.borderRightWidth = 1;
        table.style.borderTopWidth = 1;
        table.style.borderBottomWidth = 1;
        table.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
        table.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

        // Заголовки столбцов
        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        headerRow.style.paddingTop = 5;
        headerRow.style.paddingRight = 5;
        headerRow.style.paddingBottom = 5;
        headerRow.style.paddingLeft = 5;

        string[] headers = { "Сигнал", "Кто отправляет", "Кто принимает", "Метод обработки" };
        foreach (var headerText in headers)
        {
            var headerCell = new Label(headerText);
            headerCell.style.flexGrow = 1;
            headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerCell.style.paddingTop = 5;
            headerCell.style.paddingRight = 5;
            headerCell.style.paddingBottom = 5;
            headerCell.style.paddingLeft = 5;
            headerRow.Add(headerCell);
        }
        table.Add(headerRow);

        // Добавляем строки с данными
        foreach (var msg in dependencyData.MessageBus)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.paddingTop = 5;
            row.style.paddingRight = 5;
            row.style.paddingBottom = 5;
            row.style.paddingLeft = 5;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            AddCell(row, msg.Signal);
            AddCell(row, msg.Sender);
            AddCell(row, msg.Receiver);
            AddCell(row, msg.HandlerMethod);

            table.Add(row);
        }

        // Добавляем таблицу в контейнер
        container.Add(table);
        tablesContainer.Add(container);
    }

    private void AnalyzeProject()
    {
        if (graph == null)
        {
            Debug.LogError("Graph is null!");
            return;
        }

        graph.ClearGraph(); // Очищаем предыдущий граф
        ScriptAnalyzer.AnalyzeScripts(graph);
        UpdateStructure(); // Обновляем структуру
        UpdateTables(); // Обновляем таблицы
    }

    private void CenterGraph()
    {
        if (graph == null)
        {
            Debug.LogError("Graph is null!");
            return;
        }

        graph.CenterGraph();
    }

    private void UpdateStructure()
    {
        structureContainer.Clear();

        // Создаем дерево
        var tree = new VisualElement();
        tree.style.flexGrow = 1;
        tree.style.flexDirection = FlexDirection.Column;
        tree.style.paddingTop = 10;
        tree.style.paddingRight = 10;
        tree.style.paddingBottom = 10;
        tree.style.paddingLeft = 10;
        structureContainer.Add(tree);

        // Заголовок
        var header = new Label("Иерархия объектов");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 16;
        header.style.marginBottom = 10;
        tree.Add(header);

        // Получаем все префабы
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        Debug.Log($"Найдено {prefabGuids.Length} префабов в проекте");

        var prefabs = prefabGuids
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => 
                !path.Contains("Packages/") && 
                !path.Contains("Library/"))
            .ToList();

        Debug.Log($"Отфильтровано {prefabs.Count} префабов после исключения Packages и Library");

        if (prefabs.Count == 0)
        {
            var noPrefabsLabel = new Label("Префабы не найдены. Создайте префабы в папке Assets для отображения иерархии.");
            noPrefabsLabel.style.marginTop = 20;
            noPrefabsLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            tree.Add(noPrefabsLabel);
            return;
        }

        foreach (var prefabPath in prefabs)
        {
            Debug.Log($"Загрузка префаба: {prefabPath}");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                Debug.Log($"Успешно загружен префаб: {prefab.name}");
                var prefabElement = CreatePrefabElement(prefab);
                tree.Add(prefabElement);

                // Добавляем все дочерние объекты
                AddChildObjects(prefabElement, prefab.transform, 1);
            }
            else
            {
                Debug.LogWarning($"Не удалось загрузить префаб: {prefabPath}");
            }
        }
    }

    private VisualElement CreatePrefabElement(GameObject prefab)
    {
        var element = new VisualElement();
        element.style.flexDirection = FlexDirection.Column;
        element.style.marginTop = 10;

        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;
        element.Add(header);

        var icon = new Label("📦");
        icon.style.marginRight = 5;
        header.Add(icon);

        var name = new Label($"{prefab.name} (Prefab)");
        name.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.Add(name);

        return element;
    }

    private void AddChildObjects(VisualElement parent, Transform transform, int depth)
    {
        foreach (Transform child in transform)
        {
            var childElement = CreateObjectElement(child.gameObject, depth);
            parent.Add(childElement);

            // Рекурсивно добавляем дочерние объекты
            if (child.childCount > 0)
            {
                AddChildObjects(childElement, child, depth + 1);
            }
        }
    }

    private VisualElement CreateObjectElement(GameObject obj, int depth)
    {
        var element = new VisualElement();
        element.style.flexDirection = FlexDirection.Column;
        element.style.marginLeft = depth * 20;
        element.style.marginTop = 2;

        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;
        element.Add(header);

        // Добавляем отступы для иерархии
        var indent = new Label(new string(' ', depth * 2) + "├─ ");
        indent.style.whiteSpace = WhiteSpace.NoWrap;
        header.Add(indent);

        // Иконка в зависимости от типа объекта
        var icon = new Label(obj.transform.childCount > 0 ? "📁" : "📄");
        icon.style.marginRight = 5;
        header.Add(icon);

        // Имя объекта
        var name = new Label(obj.name);
        header.Add(name);

        // Добавляем информацию о компонентах
        var components = obj.GetComponents<Component>();
        if (components.Length > 1) // Больше 1, потому что Transform всегда есть
        {
            var componentsList = new VisualElement();
            componentsList.style.marginLeft = 20;
            componentsList.style.marginTop = 2;

            foreach (var component in components)
            {
                if (component is Transform) continue; // Пропускаем Transform

                var componentElement = new VisualElement();
                componentElement.style.flexDirection = FlexDirection.Row;
                componentElement.style.marginTop = 2;

                var componentIndent = new Label(new string(' ', (depth + 1) * 2) + "└─ ");
                componentIndent.style.whiteSpace = WhiteSpace.NoWrap;
                componentElement.Add(componentIndent);

                var componentName = new Label(component.GetType().Name);
                componentElement.Add(componentName);

                componentsList.Add(componentElement);
            }

            element.Add(componentsList);
        }

        return element;
    }
}












