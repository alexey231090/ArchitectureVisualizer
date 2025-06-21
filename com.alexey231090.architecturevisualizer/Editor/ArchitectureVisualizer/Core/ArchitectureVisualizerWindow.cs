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

        // Удаляем данные для Event Tracking, будем использовать EventTrackingManager
        // private List<TrackingPath> trackingPaths = new List<TrackingPath>();
        // private Dictionary<string, bool> pathTrackingStates = new Dictionary<string, bool>();

        private bool isGlobalTrackingActive = false;
        private double lastUpdateTime = 0;
        private const double updateInterval = 0.5; // Обновлять значения раз в 0.5 сек

        private Dictionary<int, Label> _instanceLabels = new Dictionary<int, Label>();
        private string selectedScriptName;

        private void OnEnable()
        {
            Debug.Log("ArchitectureVisualizerWindow: OnEnable");
            CreateGUI();
            EditorApplication.update += UpdateTrackedValuesAndColors;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateTrackedValuesAndColors;
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

        private void UpdateTrackedValuesAndColors()
        {
            UpdateHighlightColors();
            
            if (EditorApplication.timeSinceStartup - lastUpdateTime < updateInterval)
            {
                return;
            }
            lastUpdateTime = EditorApplication.timeSinceStartup;

            bool structureChanged = UpdateTrackedValuesData();
            if (structureChanged)
            {
                UpdateEventTracking();
            }
        }

        private bool UpdateTrackedValuesData()
        {
            bool structureChanged = false;
            bool anyPathIsTracking = EventTrackingManager.TrackingPaths.Any(p => p.isTracking);
            if (!anyPathIsTracking)
            {
                return false;
            }

            foreach (var path in EventTrackingManager.TrackingPaths.Where(p => p.isTracking))
            {
                foreach (var step in path.steps)
                {
                    Type scriptType = FindType(step.scriptName);
                    if (scriptType == null) continue;

                    var activeObjects = FindObjectsOfType(scriptType);
                    var activeInstanceIds = activeObjects.Select(o => o.GetInstanceID()).ToList();

                    int removedCount = step.trackedInstances.RemoveAll(inst => !activeInstanceIds.Contains(inst.instanceId));
                    if (removedCount > 0)
                    {
                        structureChanged = true;
                    }
                    
                    foreach (var obj in activeObjects)
                    {
                        var component = obj as Component;
                        if (component == null) continue;

                        int instanceId = obj.GetInstanceID();
                        var trackedInstance = step.trackedInstances.FirstOrDefault(inst => inst.instanceId == instanceId);

                        if (trackedInstance == null)
                        {
                            trackedInstance = new TrackedInstance
                            {
                                instanceId = instanceId,
                                component = component,
                                lastValue = null,
                                lastCheckTime = EditorApplication.timeSinceStartup,
                                highlightStartTime = 0
                            };
                            step.trackedInstances.Add(trackedInstance);
                            structureChanged = true;
                        }
                        else
                        {
                            // Re-link the live component, as it's NonSerialized
                            trackedInstance.component = component;
                        }

                        object currentValue = GetValue(component, step.variableName);
                        string currentValueStr = GetValueAsString(currentValue);
                        
                        // Update UI label if it exists
                        if (trackedInstance.valueLabel != null)
                        {
                            trackedInstance.valueLabel.text = $"Value: {currentValueStr}";
                        }

                        if (trackedInstance.lastValue != null && trackedInstance.lastValue != currentValueStr)
                        {
                            trackedInstance.highlightStartTime = EditorApplication.timeSinceStartup;
                        }

                        trackedInstance.lastValue = currentValueStr;
                        trackedInstance.lastCheckTime = EditorApplication.timeSinceStartup;
                    }
                }
            }
            return structureChanged;
        }

        private void UpdateHighlightColors()
        {
            foreach (var path in EventTrackingManager.TrackingPaths)
            {
                foreach (var step in path.steps)
                {
                    foreach (var instance in step.trackedInstances)
                    {
                        if (instance.valueLabel != null && instance.highlightStartTime > 0)
                        {
                            double timeSinceChange = EditorApplication.timeSinceStartup - instance.highlightStartTime;
                            if (timeSinceChange < 1.0) // Highlight for 1 second
                            {
                                float t = (float)timeSinceChange;
                                instance.valueLabel.style.color = Color.Lerp(Color.yellow, Color.white, t);
                            }
                            else
                            {
                                instance.valueLabel.style.color = Color.white;
                                instance.highlightStartTime = 0; // Reset highlight
                            }
                        }
                    }
                }
            }
        }
        
        private object GetValue(object source, string name)
        {
            if (source == null || string.IsNullOrEmpty(name)) return null;

            var type = source.GetType();
            
            while (type != null)
            {
                var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field.GetValue(source);
                }

                var property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                if (property != null)
                {
                    return property.GetValue(source, null);
                }
                
                type = type.BaseType;
            }
            return null;
        }

        private string GetValueAsString(object value)
        {
            if (value == null) return "null";

            if (value is IEnumerable collection && !(value is string))
            {
                var items = new List<string>();
                int count = 0;
                foreach (var item in collection)
                {
                    if (count >= 10) // Ограничиваем количество для отображения
                    {
                        items.Add("...");
                        break;
                    }
                    items.Add(item != null ? item.ToString() : "null");
                    count++;
                }
                return $"[{string.Join(", ", items)}]";
            }

            return value.ToString();
        }

        private Type FindType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
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
            _instanceLabels.Clear();

            // Заголовок
            var headerContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 10 } };
            var titleLabel = new Label("Event Tracking") { style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginRight = 20 } };
            var addButton = new Button(() => ShowAddPathDialog()) { text = "Add Path" };
            headerContainer.Add(titleLabel);
            headerContainer.Add(addButton);
            eventTrackingContainer.Add(headerContainer);

            // Список путей
            foreach (var path in EventTrackingManager.TrackingPaths)
            {
                var pathContainer = new Foldout { text = $"{path.pathName} ({(path.isTracking ? "Tracking" : "Stopped")})", value = true };
                pathContainer.style.marginTop = 5;
                eventTrackingContainer.Add(pathContainer);

                var pathControls = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };
                var toggleButton = new Button(() => ToggleTracking(path)) { text = path.isTracking ? "Stop" : "Start" };
                var editButton = new Button(() => ShowEditPathDialog(path)) { text = "Edit" };
                var deleteButton = new Button(() => DeletePath(path)) { text = "Delete" };
                var addStepButton = new Button(() => ShowAddStepDialog(path)) { text = "Add Step" };

                pathControls.Add(toggleButton);
                pathControls.Add(editButton);
                pathControls.Add(deleteButton);
                pathControls.Add(addStepButton);
                pathContainer.Add(pathControls);

                foreach (var step in path.steps)
                {
                    var stepContainer = new Foldout { text = $"Step: {step.scriptName} -> {step.variableName}", value = true };
                    stepContainer.style.marginLeft = 20;
                    pathContainer.Add(stepContainer);

                    var stepControls = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };
                    var deleteStepButton = new Button(() => DeleteStep(path, step)) { text = "Delete Step" };
                    stepControls.Add(deleteStepButton);
                    stepContainer.Add(stepControls);
                    
                    if (step.comment != null && step.comment != "")
                    {
                        stepContainer.Add(new Label($"Comment: {step.comment}"));
                    }

                    if (path.isTracking)
                    {
                        foreach (var instance in step.trackedInstances)
                        {
                            if (instance.component != null)
                            {
                                var instanceLabel = new Label($"  - {instance.component.gameObject.name}: {instance.lastValue}");
                                instance.valueLabel = instanceLabel; // Link label to instance
                                _instanceLabels[instance.instanceId] = instanceLabel;
                                stepContainer.Add(instanceLabel);
                            }
                        }
                    }
                }
            }
        }

        private void ToggleTracking(TrackingPath path)
        {
            path.isTracking = !path.isTracking;
            EventTrackingManager.UpdatePath();

            if (path.isTracking)
            {
                UpdateTrackedValuesData();
                UpdateEventTracking();
            }
            else
            {
                foreach (var step in path.steps)
                {
                    step.trackedInstances.Clear();
                }
                UpdateEventTracking();
            }
        }

        private void ShowAddPathDialog()
        {
            var window = GetWindow<AddPathWindow>("Add New Path");
            window.OnPathCreated = (newPath) => {
                EventTrackingManager.AddPath(newPath);
                UpdateEventTracking();
            };
        }

        private void ShowEditPathDialog(TrackingPath path)
        {
            var window = GetWindow<EditPathWindow>("Edit Path");
            window.PathToEdit = path;
            window.OnPathEdited = () => {
                EventTrackingManager.UpdatePath();
                UpdateEventTracking();
            };
        }

        private void ShowAddStepDialog(TrackingPath path)
        {
            var window = GetWindow<AddStepWindow>("Add Step");
            window.OnStepCreated = (newStep) => {
                EventTrackingManager.AddStep(path, newStep);
                UpdateEventTracking();
            };
        }

        private void DeletePath(TrackingPath path)
        {
            if (EditorUtility.DisplayDialog("Delete Path", $"Are you sure you want to delete path '{path.pathName}'?", "Yes", "No"))
            {
                EventTrackingManager.DeletePath(path);
                UpdateEventTracking();
            }
        }

        private void DeleteStep(TrackingPath path, TrackingStep step)
        {
            EventTrackingManager.DeleteStep(path, step);
            UpdateEventTracking();
        }

        public static List<string> GetFieldAndPropertyNames(Type type)
        {
            if (type == null) return new List<string>();

            var members = new List<string>();
            var currentType = type;
            
            while (currentType != null && currentType != typeof(MonoBehaviour) && currentType != typeof(object))
            {
                var fields = currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Select(f => f.Name);
                var properties = currentType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Select(p => p.Name);
                
                members.AddRange(fields);
                members.AddRange(properties);
                
                currentType = currentType.BaseType;
            }
            return members.Distinct().OrderBy(s => s).ToList();
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

            var scriptGuids = AssetDatabase.FindAssets("t:script", new[] { selectedFolder });
            foreach (var guid in scriptGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null)
                {
                    var type = script.GetClass();
                    if (type != null && typeof(MonoBehaviour).IsAssignableFrom(type))
                    {
                        string typeName = type.FullName;
                        availableScripts.Add(typeName);
                        scriptVariables[typeName] = ArchitectureVisualizerWindow.GetFieldAndPropertyNames(type);
                    }
                }
            }
            availableScripts = availableScripts.OrderBy(s => s).ToList();
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
            if (GUILayout.Button("Create Path"))
            {
                var newPath = new TrackingPath
                {
                    pathName = pathName,
                    description = comment,
                    steps = new List<TrackingStep>()
                };

                if (!string.IsNullOrEmpty(selectedScript) && !string.IsNullOrEmpty(selectedVariable))
                {
                    var firstStep = new TrackingStep
                    {
                        scriptName = selectedScript,
                        variableName = selectedVariable,
                        comment = "Initial step"
                    };
                    newPath.steps.Add(firstStep);
                }
                
                OnPathCreated?.Invoke(newPath);
                Close();
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

            var scriptGuids = AssetDatabase.FindAssets("t:script", new[] { selectedFolder });
            foreach (var guid in scriptGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null)
                {
                    var type = script.GetClass();
                    if (type != null && typeof(MonoBehaviour).IsAssignableFrom(type))
                    {
                        string typeName = type.FullName;
                        availableScripts.Add(typeName);
                        scriptVariables[typeName] = ArchitectureVisualizerWindow.GetFieldAndPropertyNames(type);
                    }
                }
            }
            availableScripts = availableScripts.OrderBy(s => s).ToList();
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
            GUILayout.Label("Add New Step", EditorStyles.boldLabel);
            
            GUILayout.Label("Select Script:");
            int selectedScriptIndex = availableScripts.IndexOf(selectedScript);
            int newSelectedScriptIndex = EditorGUILayout.Popup(selectedScriptIndex, availableScripts.ToArray());

            if (newSelectedScriptIndex != selectedScriptIndex)
            {
                selectedScript = availableScripts[newSelectedScriptIndex];
                UpdateVariableDropdown();
            }

            if (!string.IsNullOrEmpty(selectedScript) && scriptVariables.ContainsKey(selectedScript))
            {
                GUILayout.Label("Select Variable:");
                int selectedVarIndex = scriptVariables[selectedScript].IndexOf(selectedVariable);
                int newSelectedVarIndex = EditorGUILayout.Popup(selectedVarIndex, scriptVariables[selectedScript].ToArray());
                if (newSelectedVarIndex != selectedVarIndex)
                {
                    selectedVariable = scriptVariables[selectedScript][newSelectedVarIndex];
                }
            }

            GUILayout.Label("Comment:");
            comment = EditorGUILayout.TextArea(comment, GUILayout.Height(60));
            
            GUILayout.Space(10);

            if (GUILayout.Button("Add Step"))
            {
                if (!string.IsNullOrEmpty(selectedScript) && !string.IsNullOrEmpty(selectedVariable))
                {
                    var newStep = new TrackingStep
                    {
                        scriptName = selectedScript,
                        variableName = selectedVariable,
                        comment = comment
                    };
                    OnStepCreated?.Invoke(newStep);
                    Close();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a script and a variable.", "OK");
                }
            }

            if (GUILayout.Button("Cancel"))
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
            if (GUILayout.Button("Сохранить"))
            {
                PathToEdit.pathName = pathName;
                PathToEdit.description = description;

                // Удаляем шаги, которые были отмечены для удаления
                // PathToEdit.steps.RemoveAll(s => stepsToRemove.Contains(s));

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