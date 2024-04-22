#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using scene = UnityEngine.SceneManagement.Scene;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>An utility for adding extra icons to scene fields in the heirarchy window. Only available in editor.</summary>
    public static class HierarchyGUIUtility
    {

        #region Callbacks

        /// <summary>Called after reserving a rect in hierarchy scene field. Return true to indicate that something was drawn, false means that the rect will be re-used for next OnGUI callback.</summary>
        public delegate bool HierarchySceneGUI(scene scene);

        /// <summary>Called after reserving a rect in hierarchy game object field. Return true to indicate that something was drawn, false means that the rect will be re-used for next OnGUI callback.</summary>
        public delegate bool HierarchyGameObjectGUI(GameObject gameObject);

        static readonly List<Callback> callbacks = new List<Callback>();

        class Callback
        {

            public object onGUI;
            public int index;

            public Callback(object onGUI, int index)
            {
                this.onGUI = onGUI;
                this.index = index;
            }

            public bool OnGUI(object obj)
            {
                if (obj is scene scene && this.onGUI is HierarchySceneGUI onGUI)
                    return onGUI.Invoke(scene);
                else if (obj is GameObject o && this.onGUI is HierarchyGameObjectGUI onGUI2)
                    return onGUI2.Invoke(o);
                return false;
            }

        }

        /// <summary>Adds a onGUI call for <see cref="AdvancedSceneManager.Models.Scene"/> fields.</summary>
        public static void AddSceneGUI(HierarchySceneGUI onGUI, int index = 0) =>
            Add(onGUI, index);

        /// <summary>Adds a onGUI call for <see cref="GameObject"/> fields.</summary>
        public static void AddGameObjectGUI(HierarchyGameObjectGUI onGUI, int index = 0) =>
            Add(onGUI, index);

        /// <summary>Remove a onGUI call for a <see cref="AdvancedSceneManager.Models.Scene"/>.</summary>
        public static void RemoveSceneGUI(HierarchySceneGUI onGUI) =>
            Remove(onGUI);

        /// <summary>Remove a onGUI call for a <see cref="GameObject"/>.</summary>
        public static void RemoveGameObjectGUI(HierarchyGameObjectGUI onGUI) =>
            Remove(onGUI);

        static void Add(object onGUI, int index)
        {
            if (!callbacks.Any(i => i.onGUI == onGUI))
                callbacks.Add(new Callback(onGUI, index));
            Repaint();
        }

        static void Remove(object onGUI)
        {
            _ = callbacks.RemoveAll(i => i.onGUI == onGUI);
            Repaint();
        }

        #endregion

        /// <summary>The default style for text in hierarchy.</summary>
        public static GUIStyle defaultStyle { get; private set; }

        internal static void Initialize() =>
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;

        /// <inheritdoc cref="EditorApplication.RepaintHierarchyWindow"/>
        public static void Repaint() =>
            EditorApplication.RepaintHierarchyWindow();

        static bool isResizing;
        static EditorWindow hierarchyWindow;
        static bool IsResizing()
        {

            if (!hierarchyWindow)
            {

                if (!FindHierarchyWindow(out hierarchyWindow))
                    return false;

                hierarchyWindow.rootVisualElement.RegisterCallback<GeometryChangedEvent>(async e =>
                {

                    lastResizeEvent = EditorApplication.timeSinceStartup;
                    isResizing = true;

                    await Task.Delay(300);

                    if (EditorApplication.timeSinceStartup - lastResizeEvent < 0.25)
                    {
                        isResizing = false;
                        Repaint();
                    }

                });

            }

            return isResizing;

        }

        static bool FindHierarchyWindow(out EditorWindow window)
        {
            var type = typeof(EditorApplication).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            window = (EditorWindow)Resources.FindObjectsOfTypeAll(type).FirstOrDefault();
            return window;
        }

        static double lastResizeEvent;
        static void OnHierarchyGUI(int instanceID, Rect position)
        {

            if (IsResizing())
                return;

            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                return;

            if (callbacks.Count == 0)
                return;

            if (defaultStyle == null)
                defaultStyle = new GUIStyle() { padding = new RectOffset(), margin = new RectOffset(4, 4, 0, 0), alignment = TextAnchor.MiddleRight, normal = new GUIStyleState() { textColor = new Color(1, 1, 1, 0.6f) } };

            var obj = GetObj(instanceID);
            var r = GetRect(position, obj.name);

            if (obj.obj is scene)
                r.height -= 1;

            bool didArea = false;
            bool didHorizontal = false;
            try
            {

                GUILayout.BeginArea(r);
                didArea = true;
                GUILayout.BeginHorizontal();
                didHorizontal = true;

                GUILayout.FlexibleSpace();

                foreach (var callback in callbacks.OrderBy(onGUI => onGUI.index).ToArray())
                    _ = callback.OnGUI(obj.obj);

            }
            catch (Exception)
            { }
            finally
            {
                if (didHorizontal) GUILayout.EndHorizontal();
                if (didArea) GUILayout.EndArea();
            }

        }

        static Rect GetRect(Rect originalRect, string name, bool isActive = false)
        {
            var content = new GUIContent(name);
            var size = (isActive ? EditorStyles.boldLabel : EditorStyles.label).CalcSize(content);
            var offset = 20 + size.x;
            return new Rect(originalRect.x + offset, originalRect.y, originalRect.width - offset, originalRect.height);
        }

        static (object obj, string name) GetObj(int instanceID)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) is GameObject obj)
                return (obj, obj.name);
            else if (Application.isPlaying && SceneManager.utility.dontDestroyOnLoad.unityScene.Value.handle == instanceID)
                return (SceneManager.utility.dontDestroyOnLoad.unityScene.Value, "DontDestroyOnLoad");
            else
            {
                var scene = SceneUtility.GetAllOpenUnityScenes().FirstOrDefault(s => s.handle == instanceID);
                return (scene, scene.name);
            }
        }

    }

}
#endif
