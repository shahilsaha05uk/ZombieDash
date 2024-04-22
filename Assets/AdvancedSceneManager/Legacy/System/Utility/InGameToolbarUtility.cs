#pragma warning disable IDE0011 // Add braces

using UnityEngine.UIElements;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using AdvancedSceneManager.Core;
using System.Linq;
using System;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>Provides an in-game toolbar that makes debugging scene management in build easier.</summary>
    /// <remarks>Only activates in editor and developer builds, and is disabled in non dev build.</remarks>
    public class InGameToolbarUtility : MonoBehaviour
    {

        internal static void Initialize()
        {
#if UNITY_EDITOR
            InitializeSettings();
#endif
            Show();
        }

        /// <inheritdoc cref="ASMSettings.inGameToolbarEnabled"/>
        public static bool isEnabled
        {
            get => SceneManager.settings.project.inGameToolbarEnabled;
            set
            {
                SceneManager.settings.project.inGameToolbarEnabled = value;
                SceneManager.settings.project.Save();
            }
        }

        /// <inheritdoc cref="ASMSettings.inGameToolbarExpandedByDefault"/>
        public static bool expandedByDefault
        {
            get => SceneManager.settings.project.inGameToolbarExpandedByDefault;
            set
            {
                SceneManager.settings.project.inGameToolbarExpandedByDefault = value;
                SceneManager.settings.project.Save();
            }
        }

#if UNITY_EDITOR

        /// <summary>Enables or disables <see cref="InGameToolbarUtility"/> in editor.</summary>
        public static bool isEnabledInEditor
        {
            get => SceneManager.settings.local.inGameToolbarInEditor;
            set
            {
                SceneManager.settings.local.inGameToolbarInEditor = value;
                SceneManager.settings.local.Save();
            }
        }

        #region ASMSettings

        static bool isInitialized;
        static void InitializeSettings()
        {

            if (isInitialized)
                return;
            isInitialized = true;

            SettingsTab.instance.Add(CreateElement(), header: SettingsTab.instance.DefaultHeaders.Options_InGameToolbar);
            SettingsTab.instance.Add(CreateElement2(), header: SettingsTab.instance.DefaultHeaders.Options_InGameToolbar);
            SettingsTab.instance.Add(CreateElement3(), header: SettingsTab.instance.DefaultHeaders.Options_InGameToolbar);

        }

        static VisualElement CreateElement()
        {
            var element = new UnityEngine.UIElements.Toggle("Display in-game toolbar:") { value = isEnabled };
            _ = element.RegisterValueChangedCallback(e => { isEnabled = e.newValue; ToggleIfInPlayMode(); });
            element.tooltip = "Displays the in-game toolbar, it is collapsed by default (it looks like an arrow then), and can be expanded to more easily debug scene management issues in build.";
            return element;
        }

        static VisualElement CreateElement2()
        {
            var element = new UnityEngine.UIElements.Toggle("Display in editor:") { value = isEnabledInEditor };
            _ = element.RegisterValueChangedCallback(e => { isEnabledInEditor = e.newValue; ToggleIfInPlayMode(); });
            element.tooltip = "Displays the in-game toolbar, it is collapsed by default (it looks like an arrow then), and can be expanded to more easily debug scene management issues in build.";
            return element;
        }

        static VisualElement CreateElement3()
        {
            var element = new UnityEngine.UIElements.Toggle("Expanded by default:") { value = expandedByDefault };
            _ = element.RegisterValueChangedCallback(e => expandedByDefault = e.newValue);
            return element;
        }

        static void ToggleIfInPlayMode()
        {
            if (Application.isPlaying)
                if (isOpen)
                    Hide();
                else
                    Show();
        }

        #endregion

#endif
        #region Show

        [HideInInspector] private bool isExpanded;
        [HideInInspector] private bool displayGameObjects;
        [HideInInspector] private bool displayComponents;
        [HideInInspector] private Vector2 scroll;
        [HideInInspector] private float width = 200;

        static InGameToolbarUtility script;
        static bool isOpen => script;
        static void Show()
        {

            if (!Application.isPlaying)
                return;

            if (!isEnabled)
                return;

#if UNITY_EDITOR
            if (!isEnabledInEditor)
                return;
#else
            if (!Debug.isDebugBuild)
                return;
#endif

            script = Object.FindObjectOfType<InGameToolbarUtility>();
            if (script && script.gameObject)
                return;

            script = SceneManager.utility.AddToDontDestroyOnLoad<InGameToolbarUtility>();
            script.isExpanded = SceneManager.settings.project.inGameToolbarExpandedByDefault;

        }

        static void Hide()
        {
            Destroy(script);
            script = null;
        }

        #endregion
        #region OnGUI

        readonly SerializableDictionary<int, bool> expanded = new SerializableDictionary<int, bool>();

        static Texture2D texture;
        bool isMouseDownThisFrame;
        bool isMouseDown;
        void OnGUI()
        {

            Styles.Initialize();
            Content.Initialize();

            if (!texture)
            {
                texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, Color.white);
            }

            isMouseDownThisFrame = false;
            if (Event.current.rawType == EventType.MouseDown)
                isMouseDown = isMouseDownThisFrame = true;
            else if (Event.current.rawType == EventType.MouseUp)
                isMouseDown = false;

            if (isExpanded)
                Toolbar();
            else
            {
                expanded.Clear();
                objects.Clear();
                properties.Clear();
            }

            Expander();

        }

        #region Expander button

        readonly Vector2 expanderSize = new Vector2(22, 52);

        readonly Toggle expander = new Toggle() { onContent = "→", offContent = "←", onClick = ToggleExpanded, normalColor = new Color(0, 0, 0, 0.5f), hoverColor = new Color(0, 0, 0, 0.65f), clickColor = new Color(0, 0, 0, 0.8f) };

        void Expander()
        {
            expander.style = Styles.expanderStyle;
            var r = new Rect(Screen.width - expanderSize.x, (Screen.height / 2) - (expanderSize.y / 2), expanderSize.x, expanderSize.y);
            expander.OnGUI(r);
        }

        #endregion
        #region Toolbar

        static Color borderColor = new Color32(100, 100, 100, 150);
        static Color backgroundColor = new Color32(0, 0, 0, 200);
        static Color foregroundColor = new Color32(200, 200, 200, 255);

        readonly Panel panel = new Panel() { normalColor = backgroundColor };

        bool isDragging;
        Rect position;
        void Toolbar()
        {

            var c = GUI.color;
            GUI.color = foregroundColor;

            position = new Rect(Screen.width - width - 22, 22, width, Screen.height - 44);
            panel.OnGUI(position);

            GUILayout.BeginArea(position);

            Header();
            Separator();
            SceneOperations();
            Separator();
            Scenes();

            GUILayout.EndArea();
            GUI.color = c;

            Resize();
            width = Mathf.Clamp(width, 186, Screen.width - 44);

        }

        #region Header

        readonly Button restartGame = new Button() { content = "↻", onClick = Restart, options = new[] { GUILayout.Width(26), GUILayout.Height(26) } };
        readonly Button reopenCollection = new Button() { content = "↻ collection", onClick = ReopenCollection, options = new[] { GUILayout.Height(26) } };
        readonly Button quit = new Button() { content = "×", onClick = Quit, options = new[] { GUILayout.Width(26), GUILayout.Height(26) } };
        readonly Toggle displayGameObjectsButton = new Toggle() { content = "Display gameobjects:", onToggled = b => script.displayGameObjects = b, options = new[] { GUILayout.Width(22), GUILayout.Height(22) } };
        readonly Toggle displayComponentsButton = new Toggle() { content = "Display components:", middleSpacing = 9, onToggled = b => script.displayComponents = b, options = new[] { GUILayout.Width(22), GUILayout.Height(22) } };

        void Header()
        {

            //restart / reopen / collapse buttons
            restartGame.style = Styles.button;
            reopenCollection.style = Styles.reopenCollection;
            quit.style = Styles.quit;
            displayGameObjectsButton.style = Styles.button;
            displayGameObjectsButton.isOn = displayGameObjects;
            displayComponentsButton.style = Styles.button;
            displayComponentsButton.isOn = displayComponents;

            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            restartGame.OnGUI();
            reopenCollection.OnGUI();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            quit.OnGUI(new Rect(position.width - 26 - 12, 12, 26, 26));
            GUILayout.Space(12);
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            //Properties
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Space(16);
            displayGameObjectsButton.OnGUI();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(16);
            displayComponentsButton.OnGUI();
            GUILayout.EndHorizontal();

        }

        static void Restart() =>
               SceneManager.runtime.Restart();

        static void ReopenCollection() =>
             SceneManager.collection.Reopen();

        static void Quit() =>
             SceneManager.runtime.Quit();

        static void ToggleExpanded() =>
            script.isExpanded = !script.isExpanded;

        #endregion
        #region SceneOperations

        void SceneOperations()
        {

            GUILayout.Label("Scene Operations:", Styles.h1);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Queued:", Styles.h2);
            GUILayout.Label("Running:", Styles.h2);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(QueueUtility<SceneOperation>.queue.Count().ToString(), Styles.h2);
            GUILayout.Label(QueueUtility<SceneOperation>.running.Count().ToString(), Styles.h2);
            GUILayout.EndHorizontal();

        }

        #endregion
        #region Scenes

        struct Obj
        {
            public GameObject obj;
            public Component[] components;
            public Obj[] children;
        }

        readonly Dictionary<OpenSceneInfo, Obj[]> objects = new Dictionary<OpenSceneInfo, Obj[]>();
        readonly Dictionary<Component, string[]> properties = new Dictionary<Component, string[]>();

        string collectionTitle =>
            SceneManager.collection.current
            ? SceneManager.collection.current.title
            : "";

        bool anyScenes;
        void Scenes()
        {

            //Updates objects, if enabled
            UpdateObjects();

            anyScenes = false;

            var r = GUILayoutUtility.GetLastRect();
            GUILayout.BeginVertical(Styles.GetMargin(0, 0, 12, 0));

            scroll = GUILayout.BeginScrollView(scroll, alwaysShowHorizontal: false, alwaysShowVertical: false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, Styles.scroll);

            var special = SceneManager.utility.openScenes.Where(s => s.isSpecial).Concat(new[] { SceneManager.utility.dontDestroyOnLoad }).ToArray();
            var persistent = SceneManager.utility.openScenes.Where(s => s.isPersistent && !s.isSpecial).Except(special).ToArray();
            var collection = SceneManager.collection.openScenes.Except(persistent).Except(special).ToArray();
            var standalone = SceneManager.standalone.openScenes.Except(persistent).Except(special).ToArray();
            var untracked = SceneManager.utility.openScenes.Where(s => s.isUntracked && s.unityScene.HasValue).Select(s => s.unityScene.Value).
                Concat(SceneUtility.GetAllOpenUnityScenes().Where(s => SceneManager.utility.FindOpenScene(s) == null && !DefaultSceneUtility.IsDefaultScene(s))).ToArray();

            DrawScenes("Persistent", persistent);
            DrawScenes(collectionTitle, collection);
            DrawScenes("Standalone", standalone);
            DrawScenes("Special", special);
            DrawScenes("Untracked", untracked);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (!anyScenes)
                GUILayout.Label("No open scenes.", Styles.alignCenter);

        }

        void DrawScenes(string header, IEnumerable<OpenSceneInfo> scenes, bool drawHeaderWhenEmpty = false)
        {

            DrawSceneHeader(hasItems: scenes.Any(), header, drawHeaderWhenEmpty, () =>
            {
                foreach (var scene in scenes.GroupBy(s => s.scene).Select(s => s.First()).ToArray())
                    if (CollapsibleHeader(
                        scene.ToString() + (scene.isPreloaded ? " (preloaded)" : ""),
                        hasChildren: objects.GetValue(scene)?.Any() ?? false,
                        key: scene.scene,
                        defaultValue: SceneManager.utility.activeScene == scene,
                        isBold: scene.isActive))
                        DrawObjects(scene);
            });

        }

        void DrawScenes(string header, IEnumerable<UnityEngine.SceneManagement.Scene> scenes, bool drawHeaderWhenEmpty = false)
        {

            DrawSceneHeader(hasItems: scenes.Any(), header, drawHeaderWhenEmpty, () =>
            {
                foreach (var scene in scenes.ToArray())
                    GUILayout.Label(scene.name, UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle == scene.handle ? Styles.noWordWrapBold : Styles.noWordWrap);
            });

        }

        void DrawSceneHeader(bool hasItems, string header, bool drawHeaderWhenEmpty = false, Action onListCallback = null)
        {

            if (hasItems)
            {

                anyScenes = true;

                GUILayout.Label(header + ":", Styles.h1);
                GUILayout.BeginHorizontal(Styles.GetMargin(12, 0, 0, 0));
                GUILayout.BeginVertical(Styles.margin_12_12_0_0);

                onListCallback.Invoke();

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

            }
            else if (drawHeaderWhenEmpty)
                GUILayout.Label(header, Styles.h1);

        }

        void DrawObjects(OpenSceneInfo scene)
        {
            if (expanded[scene.scene.GetInstanceID()] && objects.ContainsKey(scene))
                foreach (var obj in objects[scene])
                    DrawObjects(scene, obj);
        }

        const float objMargin = 16;
        void DrawObjects(OpenSceneInfo scene, Obj obj, int depth = 1)
        {

            if (!obj.obj)
            {
                UpdateObjects();
                GUIUtility.ExitGUI();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(objMargin * depth);

            var isExpanded = CollapsibleHeader(header: obj.obj.name, hasChildren: obj.children.Any() || (obj.components?.Any() ?? false), key: obj.obj, defaultValue: false);

            GUILayout.EndHorizontal();

            if (isExpanded)
            {

                GUILayout.BeginVertical(Styles.GetMargin(Mathf.RoundToInt(objMargin * depth), 0, 0, 0));
                if (displayComponents && (obj.components?.Any() ?? false))
                {

                    GUILayout.Label("Components:", Styles.inListHeader);
                    GUILayout.BeginVertical(Styles.margin_12_0_0_0);

                    foreach (var c in obj.components)
                        if (CollapsibleHeader(c.GetType().Name, hasChildren: true, key: c, false))
                        {

                            try
                            {
                                if (!properties.ContainsKey(c))
                                    properties.Add(c, c.GetType().GetMembers().
                                        Where(m => m is PropertyInfo || m is FieldInfo).
                                        Where(m => m.GetCustomAttribute<ObsoleteAttribute>() == null).
                                        Select(m => m.Name + ": " + ((m as PropertyInfo)?.GetValue(c) ?? (m as FieldInfo)?.GetValue(c))).
                                        ToArray());
                            }
                            catch (Exception e)
                            {
                                if (!properties.ContainsKey(c))
                                    if (e.InnerException != null)
                                        properties.Add(c, new[] { "Error: " + e.InnerException.Message });
                                    else
                                        properties.Add(c, new[] { "Error: " + e.Message });
                            }

                            foreach (var value in properties[c])
                            {
                                var c1 = GUI.color;
                                if (value.StartsWith("Error:"))
                                    GUI.color = Color.red;
                                GUILayout.Label(value, Styles.propertyItem);
                                GUI.color = c1;
                            }

                        }
                        else
                            _ = properties.Remove(c);

                    GUILayout.EndVertical();

                }
                GUILayout.EndVertical();

                if (obj.children.Any())
                {

                    if (displayComponents)
                    {
                        GUILayout.BeginVertical(Styles.GetMargin(Mathf.RoundToInt(objMargin * (depth)), 0, 0, 0));
                        GUILayout.Label("Children:", Styles.inListHeader);
                        GUILayout.EndVertical();
                    }

                    foreach (var o in obj.children)
                        DrawObjects(scene, o, depth + 1);

                }

            }

        }

        float lastObjectUpdate;
        void UpdateObjects()
        {

            if (!displayGameObjects)
            {
                objects.Clear();
                expanded.Clear();
                return;
            }

            if (Time.time - lastObjectUpdate < 1)
                return;
            lastObjectUpdate = Time.time;

            objects.Clear();

            foreach (var scene in SceneManager.utility.openScenes)
                AddSceneObjects(scene);
            AddSceneObjects(SceneManager.utility.dontDestroyOnLoad);

            foreach (var id in expanded.Keys.ToArray())
                if (!FindObjectFromInstanceID(id))
                    _ = expanded.Remove(id);

            Obj GetObj(GameObject o) =>
                new Obj() { obj = o, children = Children(o).Select(GetObj).ToArray(), components = displayComponents ? Components(o).ToArray() : null };

            void AddSceneObjects(OpenSceneInfo scene) =>
                objects.Add(scene, scene.unityScene.Value.GetRootGameObjects().Select(GetObj).GroupBy(o1 => o1.obj.GetInstanceID()).Select(g => g.First()).ToArray());

            IEnumerable<GameObject> Children(GameObject obj)
            {
                for (var i = 0; i < obj.transform.childCount; i++)
                    yield return obj.transform.GetChild(i).gameObject;
            }

            IEnumerable<Component> Components(GameObject obj) =>
                obj.GetComponents<Component>();

        }

        Func<int, Object> m_FindObjectFromInstanceID = null;
        Object FindObjectFromInstanceID(int instanceID)
        {

            if (m_FindObjectFromInstanceID == null)
                if (typeof(Object).GetMethod("FindObjectFromInstanceID", BindingFlags.NonPublic | BindingFlags.Static) is MethodInfo method)
                    m_FindObjectFromInstanceID = (Func<int, Object>)Delegate.CreateDelegate(typeof(Func<int, Object>), method);
                else
                    Debug.LogError("FindObjectFromInstanceID() was not found in UnityEngine.Object");

            return m_FindObjectFromInstanceID?.Invoke(instanceID);

        }

        #endregion

        #endregion
        #region GUI helpers

        void Separator()
        {
            GUILayout.Space(12);
            var r = GUILayoutUtility.GetRect(position.width, 1);
            GUI.DrawTexture(Rect.MinMaxRect(r.xMin + 12, r.y, r.xMax - 12, r.y + 1), texture, default, default, default, borderColor, 0, 0);
            GUILayout.Space(12);
        }

        bool CollapsibleHeader(string header, bool hasChildren, Object key, bool defaultValue, bool isBold = false)
        {

            if (!key)
                return false;

            if (!expanded.ContainsKey(key.GetInstanceID()))
                expanded.Add(key.GetInstanceID(), defaultValue);

            if (!hasChildren)
                GUILayout.Label(header, isBold ? Styles.noWordWrapBold : Styles.noWordWrap);
            else
            {

                if (GUILayout.Button(header, isBold ? Styles.collapsibleHeaderBold : Styles.collapsibleHeader))
                    expanded[key.GetInstanceID()] = !expanded[key.GetInstanceID()];

                var c = expanded[key.GetInstanceID()] ? Content.expanded : Content.collapsed;
                var size = Styles.collapsibleHeader.CalcSize(c);

                var r = GUILayoutUtility.GetLastRect();
                r = new Rect(r.xMin - size.x - 4, r.y - 0, size.x, size.y);
                GUI.Label(r, c, Styles.collapsibleHeader);

            }

            return expanded[key.GetInstanceID()];

        }

        static Color resizeDragColor = new Color32(150, 150, 150, 255);
        void Resize()
        {
#if UNITY_IOS || UNITY_ANDROID
            var r = new Rect(position.xMin - 22, position.yMin, 22, position.height);
#else
            var r = new Rect(position.xMin - 6, position.yMin, 6, position.height);
#endif

            if (r.Contains(Event.current.mousePosition))
            {

                GUI.DrawTexture(r, texture, default, default, default, Color.white, 0, 0);

                if (isMouseDownThisFrame)
                    isDragging = true;

            }

            if (!isMouseDown)
                isDragging = false;

            if (isDragging)
            {
                GUI.DrawTexture(r, texture, default, default, default, resizeDragColor, 0, 0);
                width = Mathf.Clamp(Screen.width - Event.current.mousePosition.x - 22, 186, Screen.width - 44);
            }

        }

        class Panel : Button
        {
            public override bool isEnabled
            {
                get => false;
                set { }
            }
        }

        class Toggle : Button
        {

            public string offContent = "";
            public string onContent = "✓";
            public Action<bool> onToggled;
            public float middleSpacing = 6;

            public new string content { get; set; }

            public bool isOn = false;

            public override void OnGUI(Rect? rect = null)
            {
                base.content = isOn ? onContent : offContent;
                GUILayout.BeginHorizontal();
                GUILayout.Label(content, GUILayout.ExpandWidth(false));
                GUILayout.Space(middleSpacing);
                base.OnGUI(rect);
                GUILayout.EndHorizontal();
            }

            protected override void OnClick()
            {
                isOn = !isOn;
                base.OnClick();
                onToggled?.Invoke(isOn);
            }

        }

        class Button
        {

            Color? color;
            public Color normalColor { get; set; } = new Color32(93, 93, 93, 255);
            public Color hoverColor { get; set; } = new Color32(70, 70, 70, 255);
            public Color clickColor { get; set; } = new Color32(50, 50, 50, 255);

            public virtual bool isEnabled { get; set; } = true;

            public virtual string content { get; set; }
            public GUIStyle style { get; set; }

            public GUILayoutOption[] options { get; set; }

            public Action onClick;

            GUIStyle label;

            Rect r;
            public virtual void OnGUI(Rect? rect = null)
            {

                if (label == null)
                {
                    label = new GUIStyle(GUI.skin.label);
                    label.normal.background = label.hover.background = label.active.background = texture;
                }

                if (style != null)
                    style.normal.background = style.hover.background = style.active.background = texture;

                var size =
                    style != null
                    ? style.CalcSize(new GUIContent(content))
                    : Vector2.zero;

                r = rect ?? GUILayoutUtility.GetRect(size.x + (style ?? label).padding.horizontal, size.y + (style ?? label).padding.vertical, (style ?? label), options);

                color =
                    isEnabled && r.Contains(Event.current.mousePosition)
                    ? script.isMouseDown
                        ? clickColor
                        : hoverColor
                    : new Color?();

                var c = GUI.backgroundColor;
                GUI.backgroundColor = color ?? normalColor;

                if (isEnabled && (style != null ? GUI.Button(r, content, style) : GUI.Button(r, content)))
                    OnClick();
                else if (!isEnabled)
                    GUI.Label(r, content, label);

                GUI.DrawTexture(r, texture, default, default, default, borderColor, 1, 0);

                GUI.backgroundColor = c;

            }

            protected virtual void OnClick() =>
                onClick?.Invoke();

        }

        static class Styles
        {

            public static GUIStyle expanderStyle { get; private set; }
            public static GUIStyle collapsibleHeader { get; private set; }
            public static GUIStyle collapsibleHeaderBold { get; private set; }
            public static GUIStyle noWordWrap { get; private set; }
            public static GUIStyle noWordWrapBold { get; private set; }

            public static GUIStyle button { get; private set; }
            public static GUIStyle reopenCollection { get; private set; }
            public static GUIStyle quit { get; private set; }

            public static GUIStyle h1 { get; private set; }
            public static GUIStyle h2 { get; private set; }
            public static GUIStyle margin_12_12_0_0 { get; private set; }
            public static GUIStyle margin_12_0_0_0;
            public static GUIStyle scroll { get; private set; }
            public static GUIStyle alignCenter { get; private set; }
            public static GUIStyle propertyItem { get; private set; }
            public static GUIStyle inListHeader { get; private set; }

            static readonly List<GUIStyle> marginStyles = new List<GUIStyle>();

            public static GUIStyle GetMargin(int left, int right, int top, int bottom)
            {

                if (marginStyles.FirstOrDefault(s => s.margin.left == left && s.margin.right == right && s.margin.top == top && s.margin.bottom == bottom) is GUIStyle style)
                    return style;

                style = new GUIStyle() { margin = new RectOffset(left, right, top, bottom) };
                marginStyles.Add(style);
                return style;

            }

            public static void Initialize()
            {

                if (expanderStyle == null) expanderStyle = new GUIStyle(GUI.skin.button) { padding = new RectOffset(4, 0, 0, 0) };
                if (collapsibleHeader == null) collapsibleHeader = new GUIStyle(GUI.skin.label) { wordWrap = false, hover = new GUIStyleState() { textColor = foregroundColor }, active = new GUIStyleState() { textColor = foregroundColor } };
                if (noWordWrap == null) noWordWrap = new GUIStyle(GUI.skin.label) { wordWrap = false };

                if (button == null)
                {
                    button = new GUIStyle(GUI.skin.button) { padding = new RectOffset(0, 0, 0, 0), fontSize = 16 };
                    button.normal.background = button.hover.background = button.active.background = texture;
                }

                if (reopenCollection == null) reopenCollection = new GUIStyle(button) { padding = new RectOffset(6, 6, 0, 0), fontSize = 16 };
                if (quit == null) quit = new GUIStyle(button) { padding = new RectOffset(2, 0, 0, 0), fontSize = 16 };

                if (h1 == null) h1 = new GUIStyle(GUI.skin.label) { margin = new RectOffset(0, 0, 6, 6), padding = new RectOffset(16, 0, 0, 0), fontSize = 15 };
                if (h2 == null) h2 = new GUIStyle(GUI.skin.label) { margin = new RectOffset(0, 0, 6, 6), alignment = TextAnchor.MiddleCenter, };
                if (scroll == null) scroll = new GUIStyle(GUI.skin.scrollView) { margin = new RectOffset(0, 0, 6, 12) };
                if (alignCenter == null) alignCenter = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

                if (margin_12_12_0_0 == null) margin_12_12_0_0 = new GUIStyle() { margin = new RectOffset(12, 12, 0, 0) };
                if (margin_12_0_0_0 == null) margin_12_0_0_0 = new GUIStyle() { margin = new RectOffset(12, 0, 0, 0) };
                if (propertyItem == null) propertyItem = new GUIStyle(GUI.skin.label) { wordWrap = false, normal = new GUIStyleState() { textColor = Color.gray } };
                if (inListHeader == null) inListHeader = new GUIStyle(GUI.skin.label) { wordWrap = false, normal = new GUIStyleState() { textColor = Color.gray } };

                if (collapsibleHeaderBold == null) collapsibleHeaderBold = new GUIStyle(collapsibleHeader) { fontStyle = FontStyle.Bold };
                if (noWordWrapBold == null) noWordWrapBold = new GUIStyle(noWordWrap) { fontStyle = FontStyle.Bold };

            }

        }

        static class Content
        {

            public static GUIContent expanded;
            public static GUIContent collapsed;

            public static void Initialize()
            {
                if (expanded == null) expanded = new GUIContent("▼");
                if (collapsed == null) collapsed = new GUIContent("▶");
            }

        }

        #endregion

        #endregion

    }

}
