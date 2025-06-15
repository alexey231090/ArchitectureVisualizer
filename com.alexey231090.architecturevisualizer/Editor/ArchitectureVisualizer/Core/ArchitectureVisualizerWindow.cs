using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace ArchitectureVisualizer
{
    public class ArchitectureVisualizerWindow : EditorWindow
    {
        private TabView tabView;
        private VisualElement structureContainer;
        private ScrollView tablesContainer;
        private ScrollView scriptDetailsContainer;
        private DependencyData dependencyData = new DependencyData();
        private string selectedFolder = "Assets"; // По умолчанию анализируем всю папку Assets
        private Label pathLabel; // Добавляем поле для метки пути

        private void OnEnable()
        {
            Debug.Log("ArchitectureVisualizerWindow: OnEnable");
            CreateGUI();
        }

        [MenuItem("Tools/Architecture Visualizer")]
        public static void ShowWindow()
        {
            Debug.Log("Opening Architecture Visualizer window...");
            var window = GetWindow<ArchitectureVisualizerWindow>("Architecture Visualizer");
            window.minSize = new Vector2(800, 600);
            window.Show();
            window.Focus();
            Debug.Log("Window created successfully");
        }

        private void CreateGUI()
        {
            Debug.Log("Starting CreateGUI...");
            var rootVisualElement = this.rootVisualElement;
            
            // Очищаем существующие элементы
            rootVisualElement.Clear();
            
            // Добавляем стили
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.alexey231090.architecturevisualizer/Editor/ArchitectureVisualizer/Resources/styles.uss"));

            // Создаем контейнер для кнопок
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.alignItems = Align.Center;
            buttonContainer.style.marginBottom = 10;
            buttonContainer.style.paddingTop = 10;
            buttonContainer.style.paddingRight = 10;
            buttonContainer.style.paddingBottom = 10;
            buttonContainer.style.paddingLeft = 10;

            // Кнопка анализа проекта (слева)
            var analyzeButton = new Button(() => AnalyzeProject()) { text = "Analyze Project" };
            analyzeButton.style.width = 150;
            analyzeButton.style.backgroundColor = new Color(0.4f, 0.5f, 0.3f);
            analyzeButton.style.color = Color.white;
            buttonContainer.Add(analyzeButton);

            // Кнопка выбора папки (после кнопки анализа)
            var openFolderButton = new Button(() => OpenProjectFolder()) { text = "Select Folder" };
            openFolderButton.style.width = 150;
            openFolderButton.style.backgroundColor = new Color(0.3f, 0.4f, 0.5f);
            openFolderButton.style.color = Color.white;
            buttonContainer.Add(openFolderButton);

            // Метка с текущим путем (после кнопки выбора папки)
            pathLabel = new Label($"Current path: {selectedFolder}");
            pathLabel.style.marginLeft = 10;
            pathLabel.style.marginRight = 10;
            buttonContainer.Add(pathLabel);

            rootVisualElement.Add(buttonContainer);

            Debug.Log("Creating TabView...");
            // Создаем TabView
            tabView = new TabView();
            tabView.style.flexGrow = 1;
            tabView.style.marginTop = 10;
            tabView.style.marginRight = 10;
            tabView.style.marginBottom = 10;
            tabView.style.marginLeft = 10;
            rootVisualElement.Add(tabView);

            // Добавляем вкладку для таблиц (первая)
            var tablesTab = new Tab("Tables");
            tablesContainer = new ScrollView();
            tablesContainer.style.flexGrow = 1;
            tablesTab.SetContent(tablesContainer);
            tabView.AddTab(tablesTab);

            // Добавляем вкладку для структуры (вторая)
            var structureTab = new Tab("Structure");
            structureContainer = new ScrollView();
            structureContainer.style.flexGrow = 1;
            structureTab.SetContent(structureContainer);
            tabView.AddTab(structureTab);

            // Добавляем вкладку для деталей скриптов (третья)
            var scriptDetailsTab = new Tab("Script Details");
            scriptDetailsContainer = new ScrollView();
            scriptDetailsContainer.style.flexGrow = 1;
            scriptDetailsTab.SetContent(scriptDetailsContainer);
            tabView.AddTab(scriptDetailsTab);

            Debug.Log($"Created {tabView.childCount} tabs");
            foreach (var tab in tabView.Children())
            {
                if (tab is Tab tabElement)
                {
                    Debug.Log($"Tab: {tabElement.text}, Content: {tabElement.content != null}");
                }
            }

            // Выбираем первую вкладку по умолчанию
            if (tabView.childCount > 0)
            {
                var firstTab = tabView.ElementAt(0) as Tab;
                if (firstTab != null)
                {
                    firstTab.AddToClassList("unity-tab--selected");
                    firstTab.MarkDirtyRepaint();
                }
            }

            // Принудительно обновляем окно
            rootVisualElement.MarkDirtyRepaint();
            Debug.Log("GUI creation completed");
        }

        private void SelectFolder()
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder", selectedFolder, "");
            if (!string.IsNullOrEmpty(path))
            {
                // Преобразуем путь в относительный путь Unity
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                selectedFolder = relativePath;
                Debug.Log($"Selected folder: {selectedFolder}");
                // Обновляем метку пути
                pathLabel.text = $"Current path: {selectedFolder}";
            }
        }

        private void AnalyzeProject()
        {
            Debug.Log("Starting project analysis...");
            try
            {
                // Сохраняем текущую вкладку
                var currentTab = tabView.Q<Tab>(className: "unity-tab--selected");
                int currentTabIndex = currentTab != null ? tabView.IndexOf(currentTab) : -1;

                // Если вкладка не выбрана или выбрана неправильно, выбираем Tables
                if (currentTabIndex == -1 || currentTabIndex >= tabView.childCount)
                {
                    currentTabIndex = 0; // Индекс вкладки Tables
                }

                // Анализируем скрипты
                AnalyzeScriptsInFolder(selectedFolder);

                // Обновляем все вкладки
                UpdateTables();
                UpdateStructure();
                UpdateScriptDetails();

                // Восстанавливаем выбранную вкладку
                if (currentTabIndex >= 0 && currentTabIndex < tabView.childCount)
                {
                    // Сначала убираем класс у всех вкладок
                    foreach (var tab in tabView.Children())
                    {
                        tab.RemoveFromClassList("unity-tab--selected");
                    }
                    
                    // Затем добавляем класс к нужной вкладке
                    var tabToSelect = tabView.ElementAt(currentTabIndex) as Tab;
                    if (tabToSelect != null)
                    {
                        tabToSelect.AddToClassList("unity-tab--selected");
                        tabToSelect.MarkDirtyRepaint();
                    }
                }

                Debug.Log("Project analysis completed successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during project analysis: {ex.Message}");
                EditorUtility.DisplayDialog("Analysis Error", 
                    "An error occurred during project analysis. Check the console for details.", "OK");
            }
        }

        private void AnalyzeScriptsInFolder(string folderPath)
        {
            Debug.Log($"Analyzing scripts in folder: {folderPath}");
            try
            {
                // Получаем все скрипты в выбранной папке
                string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { folderPath });
                foreach (string guid in scriptGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path))
                    {
                        Debug.Log($"Found script: {path}");
                        // Здесь можно добавить дополнительный анализ скрипта
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error analyzing scripts: {ex.Message}");
                throw;
            }
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

            // Получаем все скрипты в выбранной папке
            string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { selectedFolder });
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
                        MethodOrProperty = field.Name,
                        Notes = "Поле типа ScriptableObject"
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

            // Анализ интерфейсов для DI
            var interfaces = type.GetInterfaces();
            foreach (var iface in interfaces)
            {
                dependencyData.Dependencies.Add(new DependencyData.DIData
                {
                    Class = type.Name,
                    Interface = iface.Name,
                    InjectionMethod = "Constructor/Field",
                    Notes = "Реализация интерфейса"
                });
            }

            // Анализ сообщений
            var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var method in methods)
            {
                if (method.Name.Contains("Send") || method.Name.Contains("Publish"))
                {
                    dependencyData.Messages.Add(new DependencyData.MessageData
                    {
                        Sender = type.Name,
                        Receiver = "Неизвестно",
                        MessageType = method.Name,
                        Notes = "Отправка сообщения"
                    });
                }
                else if (method.Name.Contains("Receive") || method.Name.Contains("Subscribe"))
                {
                    dependencyData.Messages.Add(new DependencyData.MessageData
                    {
                        Sender = "Неизвестно",
                        Receiver = type.Name,
                        MessageType = method.Name,
                        Notes = "Получение сообщения"
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
            table.style.width = new Length(100, LengthUnit.Percent);
            table.style.minWidth = 800; // Минимальная ширина таблицы

            // Заголовки столбцов
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            headerRow.style.paddingTop = 5;
            headerRow.style.paddingRight = 5;
            headerRow.style.paddingBottom = 5;
            headerRow.style.paddingLeft = 5;
            headerRow.style.width = new Length(100, LengthUnit.Percent);

            string[] headers = { "Генератор", "Подписчик", "Метод подписчика", "Описание" };
            float[] columnWidths = { 20, 20, 20, 40 }; // Процентные значения ширины колонок
            foreach (var i in Enumerable.Range(0, headers.Length))
            {
                var headerCell = new Label(headers[i]);
                headerCell.style.width = new Length(columnWidths[i], LengthUnit.Percent);
                headerCell.style.minWidth = 150; // Минимальная ширина ячейки
                headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerCell.style.paddingTop = 5;
                headerCell.style.paddingRight = 5;
                headerCell.style.paddingBottom = 5;
                headerCell.style.paddingLeft = 5;
                headerCell.style.whiteSpace = WhiteSpace.Normal;
                headerCell.style.overflow = Overflow.Hidden;
                headerCell.style.textOverflow = TextOverflow.Ellipsis;
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
                row.style.width = new Length(100, LengthUnit.Percent);

                // Добавляем ячейки с фиксированной шириной
                AddCell(row, evt.Generator, columnWidths[0]);
                AddCell(row, evt.Subscriber, columnWidths[1]);
                AddCell(row, evt.SubscriberMethod, columnWidths[2]);
                AddCell(row, evt.Description, columnWidths[3]);

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
            var header = new Label("2. Таблица для жёстких зависимостей (GetComponent, FindObjectOfType)");
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
            table.style.width = new Length(100, LengthUnit.Percent);
            table.style.minWidth = 800;

            // Заголовки столбцов
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            headerRow.style.paddingTop = 5;
            headerRow.style.paddingRight = 5;
            headerRow.style.paddingBottom = 5;
            headerRow.style.paddingLeft = 5;
            headerRow.style.width = new Length(100, LengthUnit.Percent);

            string[] headers = { "Потребитель", "Зависимость", "Метод доступа", "Риски" };
            float[] columnWidths = { 25, 25, 25, 25 }; // Равномерное распределение
            foreach (var i in Enumerable.Range(0, headers.Length))
            {
                var headerCell = new Label(headers[i]);
                headerCell.style.width = new Length(columnWidths[i], LengthUnit.Percent);
                headerCell.style.minWidth = 150;
                headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerCell.style.paddingTop = 5;
                headerCell.style.paddingRight = 5;
                headerCell.style.paddingBottom = 5;
                headerCell.style.paddingLeft = 5;
                headerCell.style.whiteSpace = WhiteSpace.Normal;
                headerCell.style.overflow = Overflow.Hidden;
                headerCell.style.textOverflow = TextOverflow.Ellipsis;
                headerRow.Add(headerCell);
            }
            table.Add(headerRow);

            // Добавляем строки с данными
            foreach (var dep in dependencyData.HardDependencies)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.paddingTop = 5;
                row.style.paddingRight = 5;
                row.style.paddingBottom = 5;
                row.style.paddingLeft = 5;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                row.style.width = new Length(100, LengthUnit.Percent);

                AddCell(row, dep.Consumer, columnWidths[0]);
                AddCell(row, dep.Dependency, columnWidths[1]);
                AddCell(row, dep.AccessMethod, columnWidths[2]);
                AddCell(row, dep.Risks, columnWidths[3]);

                table.Add(row);
            }

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

        private void AddCell(VisualElement row, string text, float widthPercent)
        {
            var cell = new Label(text);
            cell.style.width = new Length(widthPercent, LengthUnit.Percent);
            cell.style.minWidth = 150;
            cell.style.paddingTop = 5;
            cell.style.paddingRight = 5;
            cell.style.paddingBottom = 5;
            cell.style.paddingLeft = 5;
            cell.style.whiteSpace = WhiteSpace.Normal;
            cell.style.overflow = Overflow.Hidden;
            cell.style.textOverflow = TextOverflow.Ellipsis;
            row.Add(cell);
        }

        private void CreateDITable()
        {
            var container = new VisualElement();
            container.style.marginBottom = 20;

            // Заголовок
            var header = new Label("3. Таблица для внедрения зависимостей (DI)");
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
            table.style.width = new Length(100, LengthUnit.Percent);
            table.style.minWidth = 800;

            // Заголовки столбцов
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            headerRow.style.paddingTop = 5;
            headerRow.style.paddingRight = 5;
            headerRow.style.paddingBottom = 5;
            headerRow.style.paddingLeft = 5;
            headerRow.style.width = new Length(100, LengthUnit.Percent);

            string[] headers = { "Класс", "Интерфейс", "Метод внедрения", "Примечания" };
            float[] columnWidths = { 25, 25, 25, 25 }; // Равномерное распределение
            foreach (var i in Enumerable.Range(0, headers.Length))
            {
                var headerCell = new Label(headers[i]);
                headerCell.style.width = new Length(columnWidths[i], LengthUnit.Percent);
                headerCell.style.minWidth = 150;
                headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerCell.style.paddingTop = 5;
                headerCell.style.paddingRight = 5;
                headerCell.style.paddingBottom = 5;
                headerCell.style.paddingLeft = 5;
                headerCell.style.whiteSpace = WhiteSpace.Normal;
                headerCell.style.overflow = Overflow.Hidden;
                headerCell.style.textOverflow = TextOverflow.Ellipsis;
                headerRow.Add(headerCell);
            }
            table.Add(headerRow);

            // Добавляем строки с данными
            foreach (var di in dependencyData.Dependencies)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.paddingTop = 5;
                row.style.paddingRight = 5;
                row.style.paddingBottom = 5;
                row.style.paddingLeft = 5;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                row.style.width = new Length(100, LengthUnit.Percent);

                AddCell(row, di.Class, columnWidths[0]);
                AddCell(row, di.Interface, columnWidths[1]);
                AddCell(row, di.InjectionMethod, columnWidths[2]);
                AddCell(row, di.Notes, columnWidths[3]);

                table.Add(row);
            }

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
            table.style.width = new Length(100, LengthUnit.Percent);
            table.style.minWidth = 800;

            // Заголовки столбцов
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            headerRow.style.paddingTop = 5;
            headerRow.style.paddingRight = 5;
            headerRow.style.paddingBottom = 5;
            headerRow.style.paddingLeft = 5;
            headerRow.style.width = new Length(100, LengthUnit.Percent);

            string[] headers = { "ScriptableObject", "Использование", "Метод/Свойство", "Примечания" };
            float[] columnWidths = { 25, 25, 25, 25 }; // Равномерное распределение
            foreach (var i in Enumerable.Range(0, headers.Length))
            {
                var headerCell = new Label(headers[i]);
                headerCell.style.width = new Length(columnWidths[i], LengthUnit.Percent);
                headerCell.style.minWidth = 150;
                headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerCell.style.paddingTop = 5;
                headerCell.style.paddingRight = 5;
                headerCell.style.paddingBottom = 5;
                headerCell.style.paddingLeft = 5;
                headerCell.style.whiteSpace = WhiteSpace.Normal;
                headerCell.style.overflow = Overflow.Hidden;
                headerCell.style.textOverflow = TextOverflow.Ellipsis;
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
                row.style.width = new Length(100, LengthUnit.Percent);

                AddCell(row, so.SOObject, columnWidths[0]);
                AddCell(row, so.User, columnWidths[1]);
                AddCell(row, so.MethodOrProperty, columnWidths[2]);
                AddCell(row, so.Notes, columnWidths[3]);

                table.Add(row);
            }

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
            table.style.width = new Length(100, LengthUnit.Percent);
            table.style.minWidth = 800;

            // Заголовки столбцов
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            headerRow.style.paddingTop = 5;
            headerRow.style.paddingRight = 5;
            headerRow.style.paddingBottom = 5;
            headerRow.style.paddingLeft = 5;
            headerRow.style.width = new Length(100, LengthUnit.Percent);

            string[] headers = { "Singleton", "Использование", "Метод доступа", "Проблемы" };
            float[] columnWidths = { 25, 25, 25, 25 }; // Равномерное распределение
            foreach (var i in Enumerable.Range(0, headers.Length))
            {
                var headerCell = new Label(headers[i]);
                headerCell.style.width = new Length(columnWidths[i], LengthUnit.Percent);
                headerCell.style.minWidth = 150;
                headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerCell.style.paddingTop = 5;
                headerCell.style.paddingRight = 5;
                headerCell.style.paddingBottom = 5;
                headerCell.style.paddingLeft = 5;
                headerCell.style.whiteSpace = WhiteSpace.Normal;
                headerCell.style.overflow = Overflow.Hidden;
                headerCell.style.textOverflow = TextOverflow.Ellipsis;
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
                row.style.width = new Length(100, LengthUnit.Percent);

                AddCell(row, singleton.SingletonClass, columnWidths[0]);
                AddCell(row, singleton.User, columnWidths[1]);
                AddCell(row, singleton.AccessMethod, columnWidths[2]);
                AddCell(row, singleton.Problems, columnWidths[3]);

                table.Add(row);
            }

            container.Add(table);
            tablesContainer.Add(container);
        }

        private void CreateMessageBusTable()
        {
            var container = new VisualElement();
            container.style.marginBottom = 20;

            // Заголовок
            var header = new Label("6. Таблица для MessageBus");
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
            table.style.width = new Length(100, LengthUnit.Percent);
            table.style.minWidth = 800;

            // Заголовки столбцов
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            headerRow.style.paddingTop = 5;
            headerRow.style.paddingRight = 5;
            headerRow.style.paddingBottom = 5;
            headerRow.style.paddingLeft = 5;
            headerRow.style.width = new Length(100, LengthUnit.Percent);

            string[] headers = { "Отправитель", "Получатель", "Тип сообщения", "Примечания" };
            float[] columnWidths = { 25, 25, 25, 25 }; // Равномерное распределение
            foreach (var i in Enumerable.Range(0, headers.Length))
            {
                var headerCell = new Label(headers[i]);
                headerCell.style.width = new Length(columnWidths[i], LengthUnit.Percent);
                headerCell.style.minWidth = 150;
                headerCell.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerCell.style.paddingTop = 5;
                headerCell.style.paddingRight = 5;
                headerCell.style.paddingBottom = 5;
                headerCell.style.paddingLeft = 5;
                headerCell.style.whiteSpace = WhiteSpace.Normal;
                headerCell.style.overflow = Overflow.Hidden;
                headerCell.style.textOverflow = TextOverflow.Ellipsis;
                headerRow.Add(headerCell);
            }
            table.Add(headerRow);

            // Добавляем строки с данными
            foreach (var message in dependencyData.Messages)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.paddingTop = 5;
                row.style.paddingRight = 5;
                row.style.paddingBottom = 5;
                row.style.paddingLeft = 5;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                row.style.width = new Length(100, LengthUnit.Percent);

                AddCell(row, message.Sender, columnWidths[0]);
                AddCell(row, message.Receiver, columnWidths[1]);
                AddCell(row, message.MessageType, columnWidths[2]);
                AddCell(row, message.Notes, columnWidths[3]);

                table.Add(row);
            }

            container.Add(table);
            tablesContainer.Add(container);
        }

        private void UpdateStructure()
        {
            structureContainer.Clear();

            // Получаем все префабы в выбранной папке
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { selectedFolder });
            foreach (var guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Packages/") || path.Contains("Library/")) continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var prefabElement = CreatePrefabElement(prefab);
                structureContainer.Add(prefabElement);
            }

            // Получаем все сцены в выбранной папке
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { selectedFolder });
            foreach (var guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Packages/") || path.Contains("Library/")) continue;

                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (scene == null) continue;

                var sceneElement = new VisualElement();
                sceneElement.style.marginBottom = 10;
                sceneElement.style.paddingTop = 10;
                sceneElement.style.paddingRight = 10;
                sceneElement.style.paddingBottom = 10;
                sceneElement.style.paddingLeft = 10;
                sceneElement.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

                var sceneHeader = new Label($"Scene: {scene.name}");
                sceneHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                sceneHeader.style.fontSize = 14;
                sceneElement.Add(sceneHeader);

                structureContainer.Add(sceneElement);
            }
        }

        private VisualElement CreatePrefabElement(GameObject prefab)
        {
            var container = new VisualElement();
            container.style.marginBottom = 10;
            container.style.paddingTop = 10;
            container.style.paddingRight = 10;
            container.style.paddingBottom = 10;
            container.style.paddingLeft = 10;
            container.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

            var header = new Label($"Prefab: {prefab.name}");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 14;
            container.Add(header);

            AddChildObjects(container, prefab.transform, 0);

            return container;
        }

        private void AddChildObjects(VisualElement parent, Transform transform, int depth)
        {
            foreach (Transform child in transform)
            {
                var childElement = CreateObjectElement(child.gameObject, depth);
                parent.Add(childElement);
                AddChildObjects(childElement, child, depth + 1);
            }
        }

        private VisualElement CreateObjectElement(GameObject obj, int depth)
        {
            var container = new VisualElement();
            container.style.marginLeft = depth * 20;
            container.style.paddingTop = 5;
            container.style.paddingRight = 5;
            container.style.paddingBottom = 5;
            container.style.paddingLeft = 5;
            container.style.borderLeftWidth = 1;
            container.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);

            var label = new Label(obj.name);
            label.style.unityFontStyleAndWeight = FontStyle.Normal;
            container.Add(label);
            return container;
        }

        private void UpdateScriptDetails()
        {
            Debug.Log("Updating Script Details...");
            
            if (scriptDetailsContainer == null)
            {
                Debug.LogError("Script Details container is null!");
                return;
            }

            scriptDetailsContainer.Clear();

            // Получаем все скрипты в выбранной папке
            string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { selectedFolder });
            Debug.Log($"Found {scriptGuids.Length} scripts in {selectedFolder}");

            foreach (var guid in scriptGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Packages/") || path.Contains("Library/")) continue;

                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null) continue;

                var type = script.GetClass();
                if (type == null) continue;

                Debug.Log($"Processing script: {script.name}");

                var scriptContainer = new VisualElement();
                scriptContainer.style.marginBottom = 10;
                scriptContainer.style.paddingTop = 10;
                scriptContainer.style.paddingRight = 10;
                scriptContainer.style.paddingBottom = 10;
                scriptContainer.style.paddingLeft = 10;
                scriptContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                scriptContainer.style.flexDirection = FlexDirection.Column;

                // Заголовок скрипта
                var scriptHeader = new Label($"Script: {script.name}");
                scriptHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                scriptHeader.style.fontSize = 14;
                scriptContainer.Add(scriptHeader);

                // Поля скрипта
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var field in fields)
                {
                    var fieldContainer = new VisualElement();
                    fieldContainer.style.marginTop = 5;
                    fieldContainer.style.marginLeft = 20;

                    var fieldLabel = new Label($"{field.Name} ({field.FieldType.Name})");
                    fieldLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                    fieldContainer.Add(fieldLabel);

                    var valueLabel = new Label($"Value: {GetFieldValue(field, type)}");
                    valueLabel.style.marginLeft = 20;
                    fieldContainer.Add(valueLabel);

                    scriptContainer.Add(fieldContainer);
                }

                scriptDetailsContainer.Add(scriptContainer);
            }

            scriptDetailsContainer.MarkDirtyRepaint();
            Debug.Log("Script Details updated successfully");
        }

        private string GetFieldValue(FieldInfo field, Type type)
        {
            try
            {
                if (type == null || field == null)
                    return "N/A";

                // Проверяем, является ли поле частью вложенного типа
                if (field.DeclaringType != type)
                {
                    return $"Type: {field.FieldType.Name} (from {field.DeclaringType.Name})";
                }

                // Для MonoBehaviour ищем объекты на сцене
                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    var objects = UnityEngine.Object.FindObjectsOfType(type);
                    if (objects != null && objects.Length > 0)
                    {
                        var obj = objects[0];
                        var value = field.GetValue(obj);
                        if (value != null)
                        {
                            if (value is UnityEngine.Object unityObj)
                            {
                                return unityObj.name;
                            }
                            else if (value is Array array)
                            {
                                var elements = new List<string>();
                                for (int i = 0; i < array.Length; i++)
                                {
                                    var element = array.GetValue(i);
                                    elements.Add(element?.ToString() ?? "null");
                                }
                                return $"Array[{array.Length}]: [{string.Join(", ", elements)}]";
                            }
                            else if (value is System.Collections.IList list)
                            {
                                var elements = new List<string>();
                                foreach (var item in list)
                                {
                                    elements.Add(item?.ToString() ?? "null");
                                }
                                return $"List[{list.Count}]: [{string.Join(", ", elements)}]";
                            }
                            else if (value is System.Collections.IDictionary dict)
                            {
                                var elements = new List<string>();
                                foreach (var entry in dict)
                                {
                                    var keyValue = (KeyValuePair<object, object>)entry;
                                    elements.Add($"{keyValue.Key}: {keyValue.Value}");
                                }
                                return $"Dictionary[{dict.Count}]: [{string.Join(", ", elements)}]";
                            }
                            else
                            {
                                return value.ToString();
                            }
                        }
                        return "null";
                    }
                    return $"Type: {type.Name} (MonoBehaviour)";
                }

                // Для остальных типов создаем экземпляр
                try
                {
                    object instance;
                    
                    // Если тип вложенный, создаем экземпляр внешнего типа
                    if (type.IsNested)
                    {
                        var outerType = type.DeclaringType;
                        if (typeof(MonoBehaviour).IsAssignableFrom(outerType))
                        {
                            return $"Type: {type.Name} (Nested in MonoBehaviour)";
                        }
                        var outerInstance = Activator.CreateInstance(outerType);
                        instance = Activator.CreateInstance(type, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { outerInstance }, null);
                    }
                    else
                    {
                        instance = Activator.CreateInstance(type);
                    }

                    var value = field.GetValue(instance);
                    
                    if (value == null)
                        return "null";

                    // Обработка специальных типов
                    if (value is UnityEngine.Object unityObj)
                    {
                        return unityObj.name;
                    }
                    else if (value is Array array)
                    {
                        var elements = new List<string>();
                        for (int i = 0; i < array.Length; i++)
                        {
                            var element = array.GetValue(i);
                            elements.Add(element?.ToString() ?? "null");
                        }
                        return $"Array[{array.Length}]: [{string.Join(", ", elements)}]";
                    }
                    else if (value is System.Collections.IList list)
                    {
                        var elements = new List<string>();
                        foreach (var item in list)
                        {
                            elements.Add(item?.ToString() ?? "null");
                        }
                        return $"List[{list.Count}]: [{string.Join(", ", elements)}]";
                    }
                    else if (value is System.Collections.IDictionary dict)
                    {
                        var elements = new List<string>();
                        foreach (var entry in dict)
                        {
                            var keyValue = (KeyValuePair<object, object>)entry;
                            elements.Add($"{keyValue.Key}: {keyValue.Value}");
                        }
                        return $"Dictionary[{dict.Count}]: [{string.Join(", ", elements)}]";
                    }
                    else if (value is System.Enum enumValue)
                    {
                        return enumValue.ToString();
                    }
                    else if (value is System.ValueType)
                    {
                        return value.ToString();
                    }
                    else if (type.IsPrimitive || type == typeof(string))
                    {
                        return value.ToString();
                    }
                    else
                    {
                        return value.ToString();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to create instance of {type.Name}: {ex.Message}");
                    
                    // Если не удалось создать экземпляр, возвращаем информацию о типе
                    if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
                    {
                        return $"Type: {type.Name}";
                    }
                    else if (type.IsArray)
                    {
                        return $"Array of {type.GetElementType().Name}";
                    }
                    else if (type.IsGenericType)
                    {
                        var genericArgs = type.GetGenericArguments();
                        var typeNames = string.Join(", ", genericArgs.Select(t => t.Name));
                        return $"Generic<{typeNames}>";
                    }
                    else if (type.IsNested)
                    {
                        return $"Nested Type: {type.Name} in {type.DeclaringType.Name}";
                    }
                    else
                    {
                        return $"Type: {type.Name}";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Warning getting field value for {field?.Name}: {ex.Message}");
                return $"Type: {type?.Name ?? "Unknown"}";
            }
        }

        private void OpenProjectFolder()
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder to Analyze", selectedFolder, "");
            if (!string.IsNullOrEmpty(path))
            {
                // Преобразуем путь в относительный путь Unity
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                selectedFolder = relativePath;
                Debug.Log($"Selected folder: {selectedFolder}");
                // Обновляем метку пути
                pathLabel.text = $"Current path: {selectedFolder}";
            }
        }
    }

    public class DependencyData
    {
        public List<EventData> Events = new List<EventData>();
        public List<HardDependencyData> HardDependencies = new List<HardDependencyData>();
        public List<DIData> Dependencies = new List<DIData>();
        public List<ScriptableObjectData> ScriptableObjects = new List<ScriptableObjectData>();
        public List<SingletonData> Singletons = new List<SingletonData>();
        public List<MessageData> Messages = new List<MessageData>();

        public class EventData
        {
            public string Generator;
            public string Subscriber;
            public string SubscriberMethod;
            public string Description;
        }

        public class HardDependencyData
        {
            public string Consumer;
            public string Dependency;
            public string AccessMethod;
            public string Risks;
        }

        public class DIData
        {
            public string Class;
            public string Interface;
            public string InjectionMethod;
            public string Notes;
        }

        public class ScriptableObjectData
        {
            public string SOObject;
            public string User;
            public string MethodOrProperty;
            public string Notes;
        }

        public class SingletonData
        {
            public string SingletonClass;
            public string User;
            public string AccessMethod;
            public string Problems;
        }

        public class MessageData
        {
            public string Sender;
            public string Receiver;
            public string MessageType;
            public string Notes;
        }
    }
}












