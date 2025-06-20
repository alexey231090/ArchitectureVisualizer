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
using ArchitectureVisualizer;

namespace ArchitectureVisualizer
{
    public class ArchitectureVisualizerWindow : EditorWindow
    {
        private TabView tabView;
        private VisualElement structureContainer;
        private ScrollView tablesContainer;
        private ScrollView scriptDetailsContainer;
        private ScrollView eventTrackingContainer;
        private DependencyData dependencyData = new DependencyData();
        private string selectedFolder = "Assets";
        private Label pathLabel;
        
        // Добавляем словарь для отслеживания состояния скриптов
        private Dictionary<string, bool> scriptExpandedStates = new Dictionary<string, bool>();
        private List<string> scriptOrder = new List<string>();

        // Добавляем данные для Event Tracking
        private List<TrackingPath> trackingPaths = new List<TrackingPath>();
        private Dictionary<string, bool> pathTrackingStates = new Dictionary<string, bool>();

        private bool isGlobalTrackingActive = false;
        private double lastUpdateTime = 0;
        private const double updateInterval = 0.5; // Обновлять значения раз в 0.5 сек

        private string selectedScriptName;

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
            Debug.Log("Creating GUI...");
            var rootVisualElement = this.rootVisualElement;
            
            // Очищаем корневой элемент перед созданием нового UI
            rootVisualElement.Clear();
            
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            // Создаем контейнер для кнопок
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.marginTop = 10;
            buttonContainer.style.marginRight = 10;
            buttonContainer.style.marginBottom = 10;
            buttonContainer.style.marginLeft = 10;

            var analyzeButton = new Button(() => AnalyzeProject());
            analyzeButton.text = "Analyze Project";
            analyzeButton.style.marginRight = 10;
            analyzeButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f);
            buttonContainer.Add(analyzeButton);

            var folderButton = new Button(() => SelectFolder());
            folderButton.text = "Select Folder";
            folderButton.style.marginRight = 10;
            buttonContainer.Add(folderButton);

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

            // Добавляем новую вкладку для Event Tracking (четвертая)
            var eventTrackingTab = new Tab("Event Tracking");
            eventTrackingContainer = new ScrollView();
            eventTrackingContainer.style.flexGrow = 1;
            eventTrackingTab.SetContent(eventTrackingContainer);
            tabView.AddTab(eventTrackingTab);

            Debug.Log($"Created {tabView.childCount} tabs");
            foreach (var tab in tabView.Children())
            {
                Debug.Log($"Tab: {tab}");
            }
        }

        private void AnalyzeProject()
        {
            Debug.Log("Starting project analysis...");
            try
            {
                var currentTab = tabView.Q<Tab>(className: "unity-tab--selected");
                int currentTabIndex = currentTab != null ? tabView.IndexOf(currentTab) : -1;
                if (currentTabIndex == -1 || currentTabIndex >= tabView.childCount)
                {
                    currentTabIndex = 0;
                }
                // Анализируем скрипты и зависимости
                DependencyAnalyzer.AnalyzeScriptsInFolder(selectedFolder);
                dependencyData = new DependencyData();
                DependencyAnalyzer.CollectDependencyData(selectedFolder, dependencyData);
                // Обновляем UI вкладок
                UpdateTables();
                UpdateStructure();
                UpdateScriptDetails();
                UpdateEventTracking();
                if (currentTabIndex >= 0 && currentTabIndex < tabView.childCount)
                {
                    foreach (var tab in tabView.Children())
                    {
                        tab.RemoveFromClassList("unity-tab--selected");
                    }
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
                EditorUtility.DisplayDialog("Analysis Error", "An error occurred during project analysis. Check the console for details.", "OK");
            }
        }

        private void SelectFolder()
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

        private void UpdateTables()
        {
            tablesContainer.Clear();
            TableBuilders.CreateEventsTable(tablesContainer, dependencyData);
            TableBuilders.CreateHardDependenciesTable(tablesContainer, dependencyData);
            TableBuilders.CreateDITable(tablesContainer, dependencyData);
            TableBuilders.CreateScriptableObjectTable(tablesContainer, dependencyData);
            TableBuilders.CreateSingletonTable(tablesContainer, dependencyData);
            TableBuilders.CreateMessageBusTable(tablesContainer, dependencyData);
        }

        private void UpdateStructure()
        {
            StructureViewBuilder.UpdateStructure((ScrollView)structureContainer, selectedFolder);
        }

        private void UpdateScriptDetails()
        {
            ScriptDetailsViewBuilder.UpdateScriptDetails(scriptDetailsContainer, selectedFolder);
        }

        private void UpdateEventTracking()
        {
            eventTrackingContainer.Clear();
            var paths = EventTrackingManager.TrackingPaths;
            var addPathButton = new Button(() => ShowAddPathDialog()) { text = "Add Path" };
            addPathButton.style.marginBottom = 8;
            eventTrackingContainer.Add(addPathButton);
            if (paths == null || paths.Count == 0)
            {
                var label = new Label("No tracking paths. Add a path to track variables.");
                label.style.unityFontStyleAndWeight = FontStyle.Italic;
                eventTrackingContainer.Add(label);
                return;
            }
            foreach (var path in paths)
            {
                var pathBox = new VisualElement();
                pathBox.style.marginBottom = 10;
                pathBox.style.paddingTop = 5;
                pathBox.style.paddingBottom = 5;
                pathBox.style.paddingLeft = 10;
                pathBox.style.paddingRight = 10;
                pathBox.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
                pathBox.style.borderLeftWidth = 3;
                pathBox.style.borderLeftColor = path.isTracking ? new Color(0.2f, 0.7f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);

                var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var title = new Label($"Path: {path.pathName} ({(path.isTracking ? "Active" : "Inactive")})");
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.fontSize = 14;
                headerRow.Add(title);
                var startStopBtn = new Button(() => ToggleTracking(path)) { text = path.isTracking ? "Stop" : "Start" };
                startStopBtn.style.marginLeft = 10;
                headerRow.Add(startStopBtn);
                var editBtn = new Button(() => ShowEditPathDialog(path)) { text = "Edit" };
                editBtn.style.marginLeft = 10;
                headerRow.Add(editBtn);
                var delBtn = new Button(() => DeletePath(path)) { text = "Delete" };
                delBtn.style.marginLeft = 10;
                headerRow.Add(delBtn);
                pathBox.Add(headerRow);
                if (!string.IsNullOrEmpty(path.description))
                {
                    var desc = new Label($"Description: {path.description}");
                    desc.style.fontSize = 12;
                    desc.style.marginBottom = 4;
                    pathBox.Add(desc);
                }
                var addStepBtn = new Button(() => ShowAddStepDialog(path)) { text = "Add Step" };
                addStepBtn.style.marginBottom = 4;
                pathBox.Add(addStepBtn);
                if (path.steps != null && path.steps.Count > 0)
                {
                    foreach (var step in path.steps)
                    {
                        var stepBox = new VisualElement();
                        stepBox.style.marginTop = 4;
                        stepBox.style.marginBottom = 4;
                        stepBox.style.paddingLeft = 10;
                        stepBox.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
                        var stepLabel = new Label($"{step.scriptName}.{step.variableName}  |  Current: {step.currentValue}  |  Previous: {step.previousValue}");
                        stepLabel.style.fontSize = 12;
                        stepBox.Add(stepLabel);
                        if (!string.IsNullOrEmpty(step.comment))
                        {
                            var comment = new Label($"Comment: {step.comment}");
                            comment.style.fontSize = 11;
                            comment.style.unityFontStyleAndWeight = FontStyle.Italic;
                            stepBox.Add(comment);
                        }
                        var delStepBtn = new Button(() => DeleteStep(path, step)) { text = "Delete Step" };
                        delStepBtn.style.marginLeft = 10;
                        stepBox.Add(delStepBtn);
                        stepBox.style.marginBottom = 2;
                        pathBox.Add(stepBox);
                    }
                }
                else
                {
                    var empty = new Label("No steps in this path.");
                    empty.style.fontSize = 11;
                    pathBox.Add(empty);
                }
                eventTrackingContainer.Add(pathBox);
            }
        }

        private void ToggleTracking(TrackingPath path)
        {
            path.isTracking = !path.isTracking;
            UpdateEventTracking();
        }

        private void ShowAddPathDialog()
        {
            var window = ScriptableObject.CreateInstance<AddPathWindow>();
            window.titleContent = new GUIContent("Add Path");
            window.OnPathCreated = (newPath) => {
                EventTrackingManager.TrackingPaths.Add(newPath);
                UpdateEventTracking();
            };
            window.selectedFolder = selectedFolder;
            window.ShowUtility();
        }

        private void ShowEditPathDialog(TrackingPath path)
        {
            var window = ScriptableObject.CreateInstance<EditPathWindow>();
            window.titleContent = new GUIContent("Edit Path");
            window.PathToEdit = path;
            window.OnPathEdited = () => {
                UpdateEventTracking();
            };
            window.selectedFolder = selectedFolder;
            window.ShowUtility();
        }

        private void ShowAddStepDialog(TrackingPath path)
        {
            var window = ScriptableObject.CreateInstance<AddStepWindow>();
            window.titleContent = new GUIContent("Добавить шаг");
            window.OnStepCreated = (newStep) => {
                path.steps.Add(newStep);
                UpdateEventTracking();
            };
            window.selectedFolder = selectedFolder;
            window.ShowUtility();
        }

        private void DeletePath(TrackingPath path)
        {
            EventTrackingManager.TrackingPaths.Remove(path);
            UpdateEventTracking();
        }

        private void DeleteStep(TrackingPath path, TrackingStep step)
        {
            path.steps.Remove(step);
            UpdateEventTracking();
        }
    }

    // Вспомогательный класс AddPathWindow
    public class AddPathWindow : EditorWindow
    {
        public Action<TrackingPath> OnPathCreated;
        public string selectedFolder = "Assets";
        private string pathName = "";
        private string comment = "";
        private string selectedScript = "";
        private string selectedVariable = "";
        private List<string> availableScripts = new List<string>();
        private Dictionary<string, List<string>> scriptVariables = new Dictionary<string, List<string>>();
        private Vector2 scrollPos;

        private void OnEnable()
        {
            LoadAvailableScripts();
        }

        private void LoadAvailableScripts()
        {
            availableScripts.Clear();
            scriptVariables.Clear();
            string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { selectedFolder });
            foreach (var guid in scriptGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Packages/") || path.Contains("Library/")) continue;
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null) continue;
                var type = script.GetClass();
                if (type == null) continue;
                string scriptName = script.name;
                availableScripts.Add(scriptName);
                var variables = new List<string>();
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var field in fields)
                {
                    variables.Add(field.Name);
                }
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var property in properties)
                {
                    if (property.CanRead)
                    {
                        variables.Add(property.Name + " [Property]");
                    }
                }
                scriptVariables[scriptName] = variables;
            }
            if (availableScripts.Count > 0)
            {
                selectedScript = availableScripts[0];
                UpdateVariableDropdown();
            }
        }

        private void UpdateVariableDropdown()
        {
            if (!string.IsNullOrEmpty(selectedScript) && scriptVariables.ContainsKey(selectedScript))
            {
                var vars = scriptVariables[selectedScript];
                if (vars.Count > 0)
                    selectedVariable = vars[0];
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Add new tracking path", EditorStyles.boldLabel);
            if (availableScripts.Count == 0)
            {
                GUILayout.Label("No scripts found in selected folder.", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Refresh"))
                {
                    LoadAvailableScripts();
                }
                return;
            }
            int scriptIdx = availableScripts.IndexOf(selectedScript);
            int newScriptIdx = EditorGUILayout.Popup("Script", scriptIdx, availableScripts.ToArray());
            if (newScriptIdx != scriptIdx)
            {
                selectedScript = availableScripts[newScriptIdx];
                UpdateVariableDropdown();
            }
            var vars = scriptVariables.ContainsKey(selectedScript) ? scriptVariables[selectedScript] : new List<string>();
            int varIdx = vars.IndexOf(selectedVariable);
            int newVarIdx = EditorGUILayout.Popup("Variable", varIdx >= 0 ? varIdx : 0, vars.ToArray());
            if (newVarIdx != varIdx && newVarIdx >= 0 && newVarIdx < vars.Count)
            {
                selectedVariable = vars[newVarIdx];
            }
            pathName = EditorGUILayout.TextField("Path name", pathName);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Comment", GUILayout.Width(70));
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(60));
            comment = EditorGUILayout.TextArea(comment, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            if (GUILayout.Button("Add"))
            {
                if (!string.IsNullOrEmpty(pathName) && !string.IsNullOrEmpty(selectedScript) && !string.IsNullOrEmpty(selectedVariable))
                {
                    var newPath = new TrackingPath {
                        pathName = pathName,
                        description = "",
                        steps = new List<TrackingStep> {
                            new TrackingStep {
                                scriptName = selectedScript,
                                variableName = selectedVariable,
                                comment = comment,
                                currentValue = "",
                                previousValue = "",
                                hasChanged = false
                            }
                        },
                        isTracking = false
                    };
                    OnPathCreated?.Invoke(newPath);
                    Close();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Path name, script and variable must be selected!", "OK");
                }
            }
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
        }
    }

    public class AddStepWindow : EditorWindow
    {
        public Action<TrackingStep> OnStepCreated;
        public string selectedFolder = "Assets";
        private string selectedScript = "";
        private string selectedVariable = "";
        private string comment = "";
        private Vector2 scrollPos;
        private List<string> availableScripts = new List<string>();
        private Dictionary<string, List<string>> scriptVariables = new Dictionary<string, List<string>>();

        private void OnEnable()
        {
            LoadAvailableScripts();
        }

        private void LoadAvailableScripts()
        {
            availableScripts.Clear();
            scriptVariables.Clear();
            string[] scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { selectedFolder });
            foreach (var guid in scriptGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Packages/") || path.Contains("Library/")) continue;
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null) continue;
                var type = script.GetClass();
                if (type == null) continue;
                string scriptName = script.name;
                availableScripts.Add(scriptName);
                var variables = new List<string>();
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var field in fields)
                {
                    variables.Add(field.Name);
                }
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var property in properties)
                {
                    if (property.CanRead)
                    {
                        variables.Add(property.Name + " [Property]");
                    }
                }
                scriptVariables[scriptName] = variables;
            }
            if (availableScripts.Count > 0)
            {
                selectedScript = availableScripts[0];
                UpdateVariableDropdown();
            }
        }

        private void UpdateVariableDropdown()
        {
            if (!string.IsNullOrEmpty(selectedScript) && scriptVariables.ContainsKey(selectedScript))
            {
                var vars = scriptVariables[selectedScript];
                if (vars.Count > 0)
                    selectedVariable = vars[0];
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Добавление шага", EditorStyles.boldLabel);
            if (availableScripts.Count == 0)
            {
                GUILayout.Label("Нет доступных скриптов в выбранной папке.", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Обновить"))
                {
                    LoadAvailableScripts();
                }
                return;
            }
            int scriptIdx = availableScripts.IndexOf(selectedScript);
            int newScriptIdx = EditorGUILayout.Popup("Скрипт", scriptIdx, availableScripts.ToArray());
            if (newScriptIdx != scriptIdx)
            {
                selectedScript = availableScripts[newScriptIdx];
                UpdateVariableDropdown();
            }
            var vars = scriptVariables.ContainsKey(selectedScript) ? scriptVariables[selectedScript] : new List<string>();
            int varIdx = vars.IndexOf(selectedVariable);
            int newVarIdx = EditorGUILayout.Popup("Переменная", varIdx >= 0 ? varIdx : 0, vars.ToArray());
            if (newVarIdx != varIdx && newVarIdx >= 0 && newVarIdx < vars.Count)
            {
                selectedVariable = vars[newVarIdx];
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Комментарий", GUILayout.Width(80));
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(60));
            comment = EditorGUILayout.TextArea(comment, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            if (GUILayout.Button("Добавить"))
            {
                if (!string.IsNullOrEmpty(selectedScript) && !string.IsNullOrEmpty(selectedVariable))
                {
                    var step = new TrackingStep
                    {
                        scriptName = selectedScript,
                        variableName = selectedVariable,
                        comment = comment,
                        currentValue = "",
                        previousValue = "",
                        hasChanged = false
                    };
                    OnStepCreated?.Invoke(step);
                    Close();
                }
                else
                {
                    EditorUtility.DisplayDialog("Ошибка", "Выберите скрипт и переменную!", "OK");
                }
            }
            if (GUILayout.Button("Отмена"))
            {
                Close();
            }
        }
    }

    public class EditPathWindow : EditorWindow
    {
        public TrackingPath PathToEdit;
        public Action OnPathEdited;
        public string selectedFolder = "Assets";
        private string pathName = "";
        private string description = "";
        private Vector2 scrollPos;

        private void OnEnable()
        {
            if (PathToEdit != null)
            {
                pathName = PathToEdit.pathName;
                description = PathToEdit.description;
            }
        }

        private void OnGUI()
        {
            if (PathToEdit == null)
            {
                GUILayout.Label("No path selected.", EditorStyles.boldLabel);
                return;
            }
            GUILayout.Label("Edit tracking path", EditorStyles.boldLabel);
            pathName = EditorGUILayout.TextField("Path name", pathName);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Description", GUILayout.Width(80));
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(60));
            description = EditorGUILayout.TextArea(description, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Steps:", EditorStyles.boldLabel);
            if (PathToEdit.steps != null && PathToEdit.steps.Count > 0)
            {
                for (int i = 0; i < PathToEdit.steps.Count; i++)
                {
                    var step = PathToEdit.steps[i];
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField($"Script: {step.scriptName}");
                    EditorGUILayout.LabelField($"Variable: {step.variableName}");
                    EditorGUILayout.LabelField("Comment:");
                    step.comment = EditorGUILayout.TextArea(step.comment, GUILayout.Height(40));
                    if (GUILayout.Button("Delete Step"))
                    {
                        PathToEdit.steps.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            else
            {
                GUILayout.Label("No steps in this path.");
            }
            if (GUILayout.Button("Add Step"))
            {
                var addStepWindow = ScriptableObject.CreateInstance<AddStepWindow>();
                addStepWindow.titleContent = new GUIContent("Add Step");
                addStepWindow.selectedFolder = selectedFolder;
                addStepWindow.OnStepCreated = (newStep) => {
                    PathToEdit.steps.Add(newStep);
                    Repaint();
                };
                addStepWindow.ShowUtility();
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Save"))
            {
                PathToEdit.pathName = pathName;
                PathToEdit.description = description;
                OnPathEdited?.Invoke();
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
        }
    }
} 