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

        private string selectedScriptName;

        private void OnEnable()
        {
            Debug.Log("ArchitectureVisualizerWindow: OnEnable");
            CreateGUI();
            EditorApplication.update += UpdateTrackedValues;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateTrackedValues;
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

        private void UpdateTrackedValues()
        {
            bool anyPathIsTracking = EventTrackingManager.TrackingPaths.Any(p => p.isTracking);
            if (!anyPathIsTracking)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup - lastUpdateTime < updateInterval)
            {
                // Больше не перерисовываем на каждом кадре для плавности, чтобы избежать зависаний.
                // UI будет обновляться только по основному таймеру.
                return;
            }
            lastUpdateTime = EditorApplication.timeSinceStartup;

            foreach (var path in EventTrackingManager.TrackingPaths.Where(p => p.isTracking))
            {
                foreach (var step in path.steps)
                {
                    Type scriptType = FindType(step.scriptName);
                    if (scriptType == null) continue;

                    var activeObjects = FindObjectsOfType(scriptType);

                    step.trackedInstances.RemoveAll(inst => !activeObjects.Any(obj => obj.GetInstanceID() == inst.instanceId));

                    foreach (var obj in activeObjects)
                    {
                        var component = obj as Component;
                        if (component == null) continue;

                        int instanceId = component.gameObject.GetInstanceID();
                        var trackedInstance = step.trackedInstances.FirstOrDefault(i => i.instanceId == instanceId);

                        if (trackedInstance == null)
                        {
                            trackedInstance = new TrackedInstance { instanceId = instanceId, instanceName = component.gameObject.name };
                            step.trackedInstances.Add(trackedInstance);
                        }
                        
                        object value = GetValue(component, step.variableName);
                        string stringValue = GetValueAsString(value);

                        if (trackedInstance.currentValue != stringValue)
                        {
                            trackedInstance.previousValue = trackedInstance.currentValue;
                            trackedInstance.currentValue = stringValue;
                            trackedInstance.hasChanged = true;
                            trackedInstance.lastChangeTime = EditorApplication.timeSinceStartup;
                        }
                    }
                }
                path.lastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
            }

            UpdateEventTracking();
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

                var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
                var title = new Label($"Path: {path.pathName} ({(path.isTracking ? "Active" : "Inactive")})");
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.fontSize = 14;
                headerRow.Add(title);
                
                var buttonsRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };

                var startStopBtn = new Button(() => ToggleTracking(path)) { text = path.isTracking ? "Stop" : "Start" };
                startStopBtn.style.marginLeft = 10;
                buttonsRow.Add(startStopBtn);
                
                var editBtn = new Button(() => ShowEditPathDialog(path)) { text = "Edit" };
                editBtn.style.marginLeft = 5;
                buttonsRow.Add(editBtn);

                var deleteBtn = new Button(() => DeletePath(path)) { text = "Delete" };
                deleteBtn.style.marginLeft = 5;
                buttonsRow.Add(deleteBtn);

                headerRow.Add(buttonsRow);
                pathBox.Add(headerRow);
                
                var descriptionLabel = new Label(path.description);
                descriptionLabel.style.marginTop = 5;
                descriptionLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                pathBox.Add(descriptionLabel);

                if (path.steps != null && path.steps.Count > 0)
                {
                    var stepsContainer = new VisualElement();
                    stepsContainer.style.marginTop = 10;
                    stepsContainer.style.marginLeft = 15;
                    
                    foreach (var step in path.steps)
                    {
                        var stepBox = new VisualElement();
                        var stepLabel = new Label($"Step: {step.scriptName} -> {step.variableName}");
                        stepLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                        stepBox.Add(stepLabel);

                        if (step.trackedInstances != null)
                        {
                            foreach (var instance in step.trackedInstances)
                            {
                                var instanceLabel = new Label($"  - {instance.instanceName} (ID:{instance.instanceId}): {instance.currentValue}");
                                if (instance.hasChanged)
                                {
                                    double timeSinceChange = EditorApplication.timeSinceStartup - instance.lastChangeTime;
                                    float fade = Mathf.Clamp01(1.0f - ((float)timeSinceChange / 2.0f)); // Затухание в течение 2 секунд
                                    if (fade > 0)
                                    {
                                        instanceLabel.style.color = Color.Lerp(Color.white, new Color(0.5f, 1f, 0.5f), fade);
                                    }
                                    else
                                    {
                                        instance.hasChanged = false; // Сбрасываем флаг после затухания
                                    }
                                }
                                stepBox.Add(instanceLabel);
                            }
                        }
                        
                        var deleteStepBtn = new Button(() => DeleteStep(path, step)) { text = "Remove Step" };
                        deleteStepBtn.style.alignSelf = Align.FlexStart;
                        deleteStepBtn.style.height = 20;
                        deleteStepBtn.style.marginTop = 5;
                        stepBox.Add(deleteStepBtn);
                        
                        stepsContainer.Add(stepBox);
                    }
                    pathBox.Add(stepsContainer);
                }

                var addStepBtn = new Button(() => ShowAddStepDialog(path)) { text = "Add Step" };
                addStepBtn.style.marginTop = 10;
                pathBox.Add(addStepBtn);

                var timeLabel = new Label($"Last update: {path.lastUpdateTime}");
                timeLabel.style.fontSize = 9;
                timeLabel.style.color = Color.gray;
                timeLabel.style.marginTop = 5;
                pathBox.Add(timeLabel);

                eventTrackingContainer.Add(pathBox);
            }
        }

        private void ToggleTracking(TrackingPath path)
        {
            path.isTracking = !path.isTracking;
            EventTrackingManager.UpdatePath(); // Сохраняем новое состояние

            if (path.isTracking)
            {
                // Принудительно запускаем немедленное обновление для заполнения данных
                lastUpdateTime = 0;
                UpdateTrackedValues();
            }
            else
            {
                // При остановке очищаем инстансы и перерисовываем UI
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