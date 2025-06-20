using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchitectureVisualizer
{
    public static class ScriptDetailsViewBuilder
    {
        public static void UpdateScriptDetails(ScrollView scriptDetailsContainer, string selectedFolder)
        {
            Debug.Log("Updating Script Details...");
            if (scriptDetailsContainer == null)
            {
                Debug.LogError("Script Details container is null!");
                return;
            }
            scriptDetailsContainer.Clear();
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
                var scriptContainer = new VisualElement();
                scriptContainer.style.marginBottom = 10;
                scriptContainer.style.paddingTop = 10;
                scriptContainer.style.paddingRight = 10;
                scriptContainer.style.paddingBottom = 10;
                scriptContainer.style.paddingLeft = 10;
                scriptContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                scriptContainer.style.flexDirection = FlexDirection.Column;
                var scriptHeader = new Label($"Script: {script.name}");
                scriptHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                scriptHeader.style.fontSize = 14;
                scriptContainer.Add(scriptHeader);
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
        public static string GetFieldValue(FieldInfo field, Type type)
        {
            try
            {
                if (type == null || field == null)
                    return "N/A";
                if (field.DeclaringType != type)
                {
                    return $"Type: {field.FieldType.Name} (from {field.DeclaringType.Name})";
                }
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
                                    var keyValue = (System.Collections.DictionaryEntry)entry;
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
                if (typeof(ScriptableObject).IsAssignableFrom(type))
                {
                    return $"Type: {type.Name} (ScriptableObject)";
                }
                if (field.IsStatic)
                {
                    var value = field.GetValue(null);
                    if (value == null)
                        return "null";
                    return value.ToString();
                }
                if (!typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    object instance;
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
                    if (value is UnityEngine.Object unityObj2)
                    {
                        return unityObj2.name;
                    }
                    else if (value is Array array2)
                    {
                        var elements = new List<string>();
                        for (int i = 0; i < array2.Length; i++)
                        {
                            var element = array2.GetValue(i);
                            elements.Add(element?.ToString() ?? "null");
                        }
                        return $"Array[{array2.Length}]: [{string.Join(", ", elements)}]";
                    }
                    else if (value is System.Collections.IList list2)
                    {
                        var elements = new List<string>();
                        foreach (var item in list2)
                        {
                            elements.Add(item?.ToString() ?? "null");
                        }
                        return $"List[{list2.Count}]: [{string.Join(", ", elements)}]";
                    }
                    else if (value is System.Collections.IDictionary dict2)
                    {
                        var elements = new List<string>();
                        foreach (var entry in dict2)
                        {
                            var keyValue = (System.Collections.DictionaryEntry)entry;
                            elements.Add($"{keyValue.Key}: {keyValue.Value}");
                        }
                        return $"Dictionary[{dict2.Count}]: [{string.Join(", ", elements)}]";
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
                return $"Type: {type.Name} (UnityEngine.Object)";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Warning getting field value for {field?.Name}: {ex.Message}");
                return $"Type: {type?.Name ?? "Unknown"}";
            }
        }
    }
} 