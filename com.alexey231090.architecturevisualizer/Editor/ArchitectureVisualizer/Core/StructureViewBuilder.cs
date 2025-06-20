using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArchitectureVisualizer
{
    public static class StructureViewBuilder
    {
        public static void UpdateStructure(ScrollView structureContainer, string selectedFolder)
        {
            structureContainer.Clear();
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
        public static VisualElement CreatePrefabElement(GameObject prefab)
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
        public static void AddChildObjects(VisualElement parent, Transform transform, int depth)
        {
            foreach (Transform child in transform)
            {
                var childElement = CreateObjectElement(child.gameObject, depth);
                parent.Add(childElement);
                AddChildObjects(childElement, child, depth + 1);
            }
        }
        public static VisualElement CreateObjectElement(GameObject obj, int depth)
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
    }
} 