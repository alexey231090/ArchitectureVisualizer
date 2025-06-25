using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace ArchitectureVisualizer
{
    // Весь файл закомментирован для устранения дублирования класса и методов
    /*
    public class ArchitectureVisualizerWindow : EditorWindow
    {
        // ... здесь должны быть остальные поля класса ...

        private void UpdateEventTracking()
        {
            if (eventTrackingContainer == null) return;
            eventTrackingContainer.Clear();

            // Контейнер для кнопки "Add Path"
            var controlsContainer = new VisualElement();
            controlsContainer.AddToClassList("et-controls-container");
            var addPathButton = new Button(ShowAddPathDialog) { text = "Add New Path" };
            addPathButton.AddToClassList("et-button");
            addPathButton.AddToClassList("et-button--add");
            controlsContainer.Add(addPathButton);
            eventTrackingContainer.Add(controlsContainer);

            // Отрисовка каждого пути как сворачиваемого блока
            foreach (var path in EventTrackingManager.TrackingPaths)
            {
                var pathContainer = new Foldout();
                pathContainer.text = path.pathName;
                pathContainer.value = path.isExpanded;
                pathContainer.RegisterValueChangedCallback(evt => path.isExpanded = evt.newValue);
                pathContainer.AddToClassList("et-path-container");
                eventTrackingContainer.Add(pathContainer);

                // --- Содержимое Foldout для каждого пути ---
                var pathControls = new VisualElement();
                pathControls.AddToClassList("et-path-controls");
                
                var trackButton = new Button(() => ToggleTracking(path)) { text = path.isTracking ? "Stop" : "Track" };
                trackButton.AddToClassList("et-button");
                trackButton.AddToClassList(path.isTracking ? "et-button--stop" : "et-button--track");
                pathControls.Add(trackButton);

                var editButton = new Button(() => ShowEditPathDialog(path)) { text = "Edit" };
                editButton.AddToClassList("et-button");
                pathControls.Add(editButton);

                var deleteButton = new Button(() => DeletePath(path)) { text = "Delete" };
                deleteButton.AddToClassList("et-button");
                deleteButton.AddToClassList("et-button--danger");
                pathControls.Add(deleteButton);
                
                pathContainer.Add(pathControls);

                if (!string.IsNullOrEmpty(path.description))
                {
                    var descriptionLabel = new Label(path.description);
                    descriptionLabel.AddToClassList("et-step-comment");
                    pathContainer.Add(descriptionLabel);
                }
                
                var addStepButton = new Button(() => ShowAddStepDialog(path)) { text = "Add Step" };
                addStepButton.AddToClassList("et-button");
                pathContainer.Add(addStepButton);

                // Горизонтальный ScrollView для шагов
                var stepsScrollView = new ScrollView(ScrollViewMode.Horizontal);
                stepsScrollView.AddToClassList("et-steps-scrollview");
                pathContainer.Add(stepsScrollView);

                // Контейнер для строки шагов
                var stepsRow = new VisualElement();
                stepsRow.AddToClassList("et-steps-row");
                stepsScrollView.Add(stepsRow);

                // Отрисовка каждого шага как колонки в горизонтальном ряду
                foreach (var step in path.steps)
                {
                    var stepColumn = new VisualElement();
                    stepColumn.AddToClassList("et-step-column");

                    // Получаем переменные для шага без дублирования
                    List<string> variables = (step.variableNames != null && step.variableNames.Count > 0)
                        ? step.variableNames
                        : (!string.IsNullOrEmpty(step.variableName) ? new List<string> { step.variableName } : new List<string>());
                    var stepLabelText = $"{step.scriptName} ({string.Join(", ", variables)})";
                    var stepLabel = new Label(stepLabelText);
                    stepLabel.AddToClassList("et-step-label");
                    stepColumn.Add(stepLabel);
                    
                    if (!string.IsNullOrEmpty(step.comment))
                    {
                        var commentLabel = new Label(step.comment);
                        commentLabel.AddToClassList("et-step-comment");
                        stepColumn.Add(commentLabel);
                    }
                    
                    var valuesContainer = new VisualElement { name = "values-container" };
                    valuesContainer.AddToClassList("et-values-container");
                    stepColumn.Add(valuesContainer);

                    if (path.isTracking)
                    {
                        foreach (var instance in step.trackedInstances)
                        {
                            if (instance is MultiVarTrackedInstance multiInst)
                            {
                                string gameObjectName = multiInst.component != null ? multiInst.component.gameObject.name : "[Instance]";
                                var instanceContainer = new VisualElement();
                                instanceContainer.AddToClassList("et-instance-container");
                                valuesContainer.Add(instanceContainer);

                                var instanceNameLabel = new Label(gameObjectName);
                                instanceNameLabel.AddToClassList("et-instance-name");
                                instanceContainer.Add(instanceNameLabel);

                                foreach (var varName in variables)
                                {
                                    string val = multiInst.lastValues.TryGetValue(varName, out var value) ? value : "[No value]";
                                    Label valueLabel;
                                    if (variables.Count == 1)
                                    {
                                        valueLabel = new Label(val); // Только значение
                                    }
                                    else
                                    {
                                        valueLabel = new Label($"{varName}: {val}"); // Имя: значение
                                    }
                                    valueLabel.AddToClassList("et-value-label");
                                    if (multiInst.valueLabels == null)
                                    {
                                        multiInst.valueLabels = new Dictionary<string, Label>();
                                    }
                                    multiInst.valueLabels[varName] = valueLabel;
                                    instanceContainer.Add(valueLabel);
                                }
                            }
                        }
                    }

                    var deleteStepButton = new Button(() => DeleteStep(path, step)) { text = "Delete Step" };
                    deleteStepButton.AddToClassList("et-button");
                    deleteStepButton.AddToClassList("et-button--danger");
                    stepColumn.Add(deleteStepButton);
                    
                    stepsRow.Add(stepColumn);
                }
            }
        }
    }
    */
} 