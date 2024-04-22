using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using System.Text.RegularExpressions;
using Lazy.Utility;
using System.Diagnostics.CodeAnalysis;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>Specifies whatever the ASM callbacks should be run in parallel for any callbacks defined in this script.</summary>
    public class ParallelASMCallbacks : Attribute
    { }

    public class CombineNull
    {
        public static readonly CombineNull value = new CombineNull();

#if UNITY_EDITOR
        public static bool IsCombineNull(CoroutineDiagHelper.SubroutineDetails coroutine) =>
            coroutine.type == typeof(CombineNull).FullName || (coroutine.isDefaultYieldInstructionComponent);
#endif

    }

    #region Diag

#if UNITY_EDITOR

    [Serializable]
    public abstract class Serializable<T, TValue> : ISerializationCallbackReceiver
    {

        [SerializeField] private TValue m_value;

        public T value { get; set; }
        public abstract TValue Convert(T value);
        public abstract T ConvertBack(TValue value);

        public void OnAfterDeserialize()
        {
            value = ConvertBack(m_value);
        }

        public void OnBeforeSerialize()
        {
            m_value = Convert(value);
        }

    }

    [Serializable]
    public class SerializableDateTime : Serializable<DateTime, long>
    {
        public override long Convert(DateTime value) => value.Ticks;
        public override DateTime ConvertBack(long value) => new DateTime(value);
        public static implicit operator SerializableDateTime(DateTime time) => new SerializableDateTime() { value = time };
        public static implicit operator DateTime(SerializableDateTime serializable) => serializable?.value ?? default;
        public string ToString(string format) => value.ToString(format);
    }

    [Serializable]
    public class SerializableTimeSpan : Serializable<TimeSpan, long>
    {
        public override long Convert(TimeSpan value) => value.Ticks;
        public override TimeSpan ConvertBack(long value) => new TimeSpan(value);
        public static implicit operator SerializableTimeSpan(TimeSpan time) => new SerializableTimeSpan() { value = time };
        public static implicit operator TimeSpan(SerializableTimeSpan serializable) => serializable?.value ?? default;
        public string ToString(string format) => value.ToString(format);
    }

    [Serializable]
    public class CoroutineDiagHelper
    {

        public SerializableDateTime startTime;
        public int startFrame;
        public CallerDetails caller = new CallerDetails();
        public bool isParallel;

        /// <summary>Sets this coroutine as parallel, does nothing beyond provide info for later use, through property <see cref="isParallel"/>.</summary>
        public void SetParallel(bool isParallel) =>
            this.isParallel = isParallel;

        public string description;
        public List<SubroutineDetails> details = new List<SubroutineDetails>();

        public SerializableTimeSpan duration;
        public int durationFrames;
        public bool isPaused;
        public bool isComplete;
        public bool wasCancelled;

        [NonSerialized]
        Stopwatch watch = null;

        public CoroutineDiagHelper()
        { }

        public CoroutineDiagHelper((MethodBase method, string file, int line) caller, string description)
        {

            this.caller.method = caller.method?.Name;
            this.caller.className = caller.method?.DeclaringType?.Name;
            this.caller.methodParameters = caller.method?.GetParameters()?.Select(p => p.ParameterType.Name)?.ToArray() ?? Array.Empty<string>();
            this.caller.file = caller.file;
            this.caller.line = caller.line;
            this.description = description;

        }

        internal void OnStart()
        {
            startTime = DateTime.Now;
            startFrame = Time.frameCount;
            isComplete = false;
            watch = new Stopwatch();
            watch.Start();
        }

        public TimeSpan diagOffset => details.FirstOrDefault()?.startTime ?? default;

        /// <summary>Logs diag data for this couroutine.</summary>
        internal SubroutineDetails Log(object subroutine, int level, SubroutineDetails parent)
        {
            var detail = new SubroutineDetails(subroutine, level, this, parent);
            details.Add(detail);
            return detail;
        }

        internal void Pause(bool isPaused)
        {
            if (isPaused)
                watch.Stop();
            else
                watch.Start();
            this.isPaused = isPaused;
        }

        internal void End(bool wasCancelled = false)
        {

            watch?.Stop();
            duration = watch?.Elapsed ?? default;
            durationFrames = (Time.frameCount + 1) - startFrame;

            isComplete = true;
            this.wasCancelled = wasCancelled;

        }

        /// <summary>View caller in code editor.</summary>
        public void ViewCallerInCodeEditor()
        {

            var relativePath =
                caller.file.Contains("/Packages/")
                ? caller.file.Substring(caller.file.IndexOf("/Packages/") + 1)
                : "Assets" + caller.file.Replace(Application.dataPath, "");

            if (UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(relativePath))
                UnityEditor.AssetDatabase.OpenAsset(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(relativePath), caller.line, 0);
            else
                Debug.LogError($"Could not find '{relativePath}'");


        }

        public override string ToString() =>
            !string.IsNullOrEmpty(description)
            ? description
            : caller.ToString();

        [Serializable]
        public class CallerDetails
        {

            public string className;
            public string method;
            public string[] methodParameters;
            public string file;
            public int line;

            public override string ToString() =>
              className + "." + method + "(" + string.Join(", ", methodParameters ?? Array.Empty<string>()) + ")";

        }

        [Serializable]
        public class SubroutineDetails
        {

            public string subroutine;
            public int level;
            public string type;
            public bool isMethod;
            public bool isValueType;
            public bool isDefaultYieldInstruction;
            public bool isDefaultYieldInstructionComponent;

            public SerializableTimeSpan startTime;
            public SerializableTimeSpan endTime;

            public int startFrame;
            public int endFrame;

            public CoroutineDiagHelper helper { get; }

            public bool isNull => string.IsNullOrEmpty(type) && !isValueType;

            public void End()
            {
                endTime = helper.watch.Elapsed;
                endFrame = Time.frameCount;
            }

            public override string ToString() =>
                subroutine;

            public SubroutineDetails(object subroutine, int level, CoroutineDiagHelper helper, SubroutineDetails parent)
            {

                var t = subroutine?.GetType();
                type = subroutine?.GetType()?.FullName;
                isMethod = t?.ReflectedType != null;
                isDefaultYieldInstructionComponent = parent?.isDefaultYieldInstruction ?? false;
                isDefaultYieldInstruction = CallbackUtility.delayInstructions.Contains(t);

                if (Convert.GetTypeCode(subroutine) == TypeCode.Object || subroutine is null)
                    this.subroutine = GetSubroutineString(subroutine);
                else
                {
                    this.subroutine = Convert.ToString(subroutine);
                    isValueType = true;
                }

                this.helper = helper;
                this.level = level;
                startFrame = Time.frameCount;
                startTime = helper.watch.Elapsed;

            }

            string GetSubroutineString(object obj)
            {

                if (int.TryParse(obj?.ToString(), out var i))
                    return "yield return " + i;

                var param = GetDefaultYieldInstructionParam(obj);
                var (methodName, isMethod) = NicifyName(obj?.ToString());

                var newText = obj == null || isMethod || isValueType ? "" : "new ";
                var yieldText = "yield return " + newText;
                var paramText = obj == null ? "" : "(" + (param?.ToString() ?? "") + ")";

                return yieldText + (methodName ?? "null") + paramText;

            }

            object GetDefaultYieldInstructionParam(object yield)
            {

                if (yield is WaitForSecondsRealtime wait)
                    return wait.waitTime;
                else if (yield is WaitForSeconds)
                {
                    //WaitForSeconds does not have a public accessor for seconds value...
                    var field = typeof(WaitForSeconds).GetField("m_Seconds", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
                    return field?.GetValue(yield);
                }

                return null;

            }

            /// <summary>Nicifies names like 'ButtonManagerScene+<OnSceneOpen>d_3', to 'ButtonManagerScene.OnSceneOpen()'.</summary>
            (string name, bool isMethod) NicifyName(string name)
            {

                if (name is null)
                    return default;

                if (!name.Contains("<") || type == "Method")
                    return (name, false);

                name = name.Replace("<", "").Replace("+", ".");

                name = Regex.Replace(name, @"(.+)\>.+", "$1", RegexOptions.None);
                name = Regex.Replace(name, @"(.+)\>.__+(.+)\|.+", "$1().$2", RegexOptions.None);
                return (name, true);

            }

        }

    }

#endif

    #endregion

    /// <summary>Contains utility functions for <see cref="Action"/>.</summary>
    public static class ActionUtility
    {

        /// <summary>Tries to invoke the action, then logs error to the console if an error occurred.</summary>
        public static void LogInvoke(this Action action)
        {
            if (!TryInvoke(action, out var e))
                Debug.LogException(e);
        }

        /// <summary>Tries to invoke the action, eats the exception.</summary>
        public static void TryInvoke(this Action action) =>
            TryInvoke(action, out _);

        /// <summary>Tries to invoke the action.</summary>
        /// <param name="action">The action to invoke.</param>
        /// <param name="exception">The exception that occurred when invoking action. <see langword="null"/> if <see langword="true"/> was returned.</param>
        /// <returns><see langword="true"/> if invoke succeeded with no exception.</returns>
        public static bool TryInvoke(this Action action, [NotNullWhen(false)] out Exception exception)
        {
            try
            {
                action?.Invoke();
                exception = null;
                return true;
            }
            catch (Exception e)
            {
                exception = e;
                return false;
            }
        }

    }

    /// <summary>An utility class that invokes callbacks (defined in interfaces based on <see cref="ISceneCallbacks"/>), and tracks performance and provides tools for optimizing and diagnosing bottlenecks in these callbacks.</summary>
    public class CallbackUtility
#if UNITY_EDITOR
        : EditorWindow
#endif
    {

        public delegate IEnumerator Callback(object obj, object param);

        public static readonly Type[] yieldInstructions =
            typeof(Application).Assembly.GetTypes().
            Where(t => typeof(YieldInstruction).IsAssignableFrom(t)).
            ToArray();

        public static readonly Type[] customYieldInstructions =
            typeof(Application).Assembly.GetTypes().
            Where(t => typeof(CustomYieldInstruction).IsAssignableFrom(t)).
            ToArray();

        public static readonly Type[] delayInstructions =
            new Type[] { null }.
            Concat(yieldInstructions).
            Concat(customYieldInstructions).
            ToArray();

        #region Editor

        static bool isOpen =>
#if UNITY_EDITOR
            GetWindow() && instance.m_isOpen;
#else
            false;
#endif

#if UNITY_EDITOR

        [Serializable]
        class CoroutineDiagHelperList : List<CoroutineDiagHelper>
        { }

        [Serializable]
        class StringCoroutineDictionary : SerializableDictionary<string, CoroutineDiagHelperList>
        { }

        [SerializeField] private SerializableStringBoolDict expanded = new SerializableStringBoolDict() { throwOnDeserializeWhenKeyValueMismatch = false };
        [SerializeField] private StringCoroutineDictionary coroutines = new StringCoroutineDictionary() { throwOnDeserializeWhenKeyValueMismatch = false };
        [SerializeField] private bool autoScroll = true;
        [SerializeField] private bool durationColumn = true;
        [SerializeField] private bool depthColumn = true;
        [SerializeField] private bool subroutineColumn = true;
        [SerializeField] private ShowMode showMode;

        public enum ShowMode
        {
            Time, Frames
        }

        static CallbackUtility GetWindow() =>
            Resources.FindObjectsOfTypeAll<CallbackUtility>().FirstOrDefault();

        static CallbackUtility instance
        {
            get
            {
                if (GetWindow() is CallbackUtility window)
                    return window;
                else
                    return CreateInstance<CallbackUtility>();
            }
        }

        bool m_isOpen;
        public static void Open()
        {
            instance.titleContent = new GUIContent("Callback analyzer");
            instance.Show();
        }

        void OnEnable()
        {
            var json = SceneManager.settings.user.callbackUtilityWindow;
            JsonUtility.FromJsonOverwrite(json, this);
            m_isOpen = true;
        }

        void OnDisable()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            m_isOpen = false;
            Content.ClearDynamic();
            Save();
        }

        internal static void Initialize()
        {

            if (isOpen)
            {
                SceneManager.app.beforeRestart += instance.SceneManager_beforeStart;
                instance.SceneManager_beforeStart();
            }

        }

        void Save()
        {
            var json = JsonUtility.ToJson(this);
            SceneManager.settings.user.callbackUtilityWindow = json;
            SceneManager.settings.user.Save();
        }

        void SceneManager_beforeStart()
        {
            coroutines.Clear();
            Save();
        }

        #region OnGUI

        Vector2 scroll;
        float i = 0;
        float dir = 1;
        Rect rect;

        static class Styles
        {

            public static GUIStyle root { get; private set; }
            public static GUIStyle header { get; private set; }
            public static GUIStyle playButton { get; private set; }
            public static GUIStyle box { get; private set; }
            public static GUIStyle foldoutHeader { get; private set; }
            public static GUIStyle foldoutHeader2 { get; private set; }
            public static GUIStyle coroutine { get; private set; }
            public static GUIStyle subroutineText { get; private set; }
            public static GUIStyle subroutineRightText { get; private set; }

            static bool isInitialized;
            public static void Initialize()
            {

                if (isInitialized)
                    return;
                isInitialized = true;

                root = new GUIStyle() { margin = new RectOffset(22, 22, 22, 22) };
                header = new GUIStyle() { margin = new RectOffset(6, 6, 6, 0) };
                playButton = new GUIStyle(GUI.skin.button) { padding = new RectOffset(6, 6, 4, 6) };
                box = new GUIStyle(GUI.skin.box) { margin = new RectOffset(12, 0, 0, 0) };
                foldoutHeader = new GUIStyle(EditorStyles.foldoutHeader) { margin = new RectOffset(0, 0, 0, 0), padding = new RectOffset(16, 0, 0, 0), fixedHeight = 22, fontSize = 12, fontStyle = FontStyle.Bold };
                foldoutHeader2 = new GUIStyle(EditorStyles.foldoutHeader) { margin = new RectOffset(12, 4, 6, 6), padding = new RectOffset(16, 0, 0, 0), fixedHeight = 22, fontSize = 12, fontStyle = FontStyle.Bold };
                coroutine = new GUIStyle() { margin = new RectOffset(24, 0, 6, 6) };
                subroutineText = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                subroutineRightText = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };

            }

        }

        static class Content
        {

            public static GUIContent play { get; private set; }
            public static GUIContent enterPlayMode { get; private set; }
            public static GUIContent empty { get; private set; }
            public static GUIContent autoScrollWhenCoroutineActive { get; private set; }
            public static GUIContent noDataAvailable { get; private set; }

            public static GUIContent display { get; private set; }
            public static GUIContent showFps { get; private set; }
            public static GUIContent showTime { get; private set; }

            public static GUIContent duration { get; private set; }
            public static GUIContent depth { get; private set; }
            public static GUIContent subroutine { get; private set; }

            static readonly Dictionary<string, GUIContent> dynamicContent = new Dictionary<string, GUIContent>();
            public static GUIContent Dynamic(string key, string text, string tooltip = null)
            {
                if (!dynamicContent.ContainsKey(key))
                    dynamicContent.Add(key, new GUIContent(text, tooltip));
                dynamicContent[key].text = text;
                dynamicContent[key].tooltip = tooltip;
                return dynamicContent[key];
            }

            public static void ClearDynamic() =>
                dynamicContent.Clear();

            static bool isInitialized;
            public static void Initialize()
            {

                if (isInitialized)
                    return;
                isInitialized = true;

                play = new GUIContent("▶");
                enterPlayMode = new GUIContent("Enter play mode");
                empty = new GUIContent("");
                noDataAvailable = new GUIContent("No data available");
                autoScrollWhenCoroutineActive = new GUIContent("Autoscroll when coroutine active");

                display = new GUIContent("Display:/");
                showFps = new GUIContent(display.text + "Frames");
                showTime = new GUIContent(display.text + "Time");
                duration = new GUIContent(display.text + "Duration");
                depth = new GUIContent(display.text + "Depth");
                subroutine = new GUIContent(display.text + "Subroutine");

            }

        }

        private void OnGUI()
        {

            Styles.Initialize();
            Content.Initialize();

            wantsMouseMove = true;
            wantsMouseEnterLeaveWindow = true;
            autoRepaintOnSceneChange = true;

            DrawHeader();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.BeginVertical(Styles.root);

            if (coroutines.Any(c => c.Value.Any()))
                foreach (var coroutine in coroutines)
                    DrawCoroutineList(coroutine);
            else if (EditorApplication.isPlayingOrWillChangePlaymode)
                DrawLoading();
            else
                DrawEmpty();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();

            if (Event.current.rawType == EventType.MouseDown)
            {
                GUI.FocusControl("");
                Repaint();
            }

        }

        void DrawHeader()
        {

            GUILayout.BeginHorizontal(Styles.header);

            if (GUILayout.Button(Content.play, Styles.playButton, GUILayout.Height(22), GUILayout.Width(22)))
            {
                SceneManager.app.Start();
                GUIUtility.ExitGUI();
            }

            GUILayout.Label(Content.enterPlayMode);

            GUILayout.Space(6);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.ToggleLeft(new GUIContent("Enabled", "Enable callback analyzer, this decreases performance and is as such disabled by default. Note that it remains enabled even when window is closed."), isEnabled);
            if (EditorGUI.EndChangeCheck())
                isEnabled = !isEnabled;

            GUILayout.FlexibleSpace();

            if (EditorGUILayout.DropdownButton(Content.empty, FocusType.Passive))
            {

                var menu = new GenericMenu();

                menu.AddItem(Content.autoScrollWhenCoroutineActive, autoScroll, () => autoScroll = !autoScroll);
                menu.AddItem(Content.showTime, showMode == ShowMode.Time, () => showMode = ShowMode.Time);
                menu.AddItem(Content.showFps, showMode == ShowMode.Frames, () => showMode = ShowMode.Frames);
                menu.AddSeparator(Content.display.text);
                menu.AddItem(Content.duration, durationColumn, () => durationColumn = !durationColumn);
                menu.AddItem(Content.depth, depthColumn, () => depthColumn = !depthColumn);
                menu.AddItem(Content.subroutine, subroutineColumn, () => subroutineColumn = !subroutineColumn);

                menu.ShowAsContext();

            }

            GUILayout.EndHorizontal();

        }

        void DrawLoading()
        {

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Box("", GUILayout.Width(position.width / 2));

            if (Event.current.type == EventType.Repaint)
            {

                var r = GUILayoutUtility.GetLastRect();

                i += 1.5f * dir * Time.deltaTime;
                if (i >= 1 - (10f / r.width)) dir = -1;
                if (i < 0) dir = 1;

                rect = new Rect(r.x + Mathf.Lerp(0, r.width, i), r.y, 10, r.height);

            }

            var c = GUI.color;
            GUI.color = Color.black;
            GUI.Box(rect, Content.empty);
            GUI.color = c;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            Repaint();

        }

        void DrawEmpty()
        {

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(Content.noDataAvailable, GUILayout.Width(110));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

        }

        void DrawCoroutineList(KeyValuePair<string, CoroutineDiagHelperList> coroutine)
        {

            EditorGUILayout.BeginVertical(Styles.box);

            if (expanded.Set(coroutine.Key, EditorGUILayout.BeginFoldoutHeaderGroup(expanded.GetValue(coroutine.Key), coroutine.Key.Replace("Assets/", ""), Styles.foldoutHeader)))
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                var i = 0;
                foreach (var c in coroutine.Value)
                {

                    var header = c.ToString() +
                        (c.wasCancelled ? " [Cancelled]" : "") +
                        (c.isPaused ? " [Paused]" : "") +
                        (!c.isComplete && Application.isPlaying ? " [Active]" : "") +
                        (c.isParallel ? " [Parallel]" : "");

                    if (expanded.Set(coroutine.Key + ":" + i, EditorGUILayout.BeginFoldoutHeaderGroup(expanded.GetValue(coroutine.Key + ":" + i), header, Styles.foldoutHeader2)))
                        DrawCoroutine(c);

                    EditorGUILayout.EndFoldoutHeaderGroup();

                    if (autoScroll && Event.current.type == EventType.Repaint &&
                        !c.isComplete && Application.isPlaying &&
                        expanded.GetValue(coroutine.Key + ":" + i))
                        scroll = GUILayoutUtility.GetLastRect().position;

                    i += 1;
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();

        }

        void DrawCoroutine(CoroutineDiagHelper diag)
        {

            EditorGUILayout.BeginVertical(Styles.coroutine);

            //Coroutine info
            EditorGUILayout.BeginVertical();

            if ((diag.startTime - DateTime.Now).TotalDays > 1)
                EditorGUILayout.LabelField("Start time: " + diag.startTime.ToString(@"MM/dd \- HH\:mm\:ss"));
            else
                EditorGUILayout.LabelField("Start time: " + diag.startTime.ToString(@"HH\:mm\:ss"));

            if (showMode == ShowMode.Frames)
                EditorGUILayout.LabelField("Start frame: " + diag.startFrame);

            if (showMode == ShowMode.Time)
                EditorGUILayout.LabelField("Total duration: " + diag.duration?.value.ToDisplayString());
            else if (showMode == ShowMode.Frames)
                EditorGUILayout.LabelField("Total duration: " + diag.durationFrames);

            EditorGUILayout.LabelField("Method: " + diag.details.FirstOrDefault()?.subroutine);

            EditorGUILayout.EndVertical();

            const float startTimeRight = 22;

            //Subroutine info
            DrawSubroutine("Duration:", "Depth:", "Subroutine:", background: Color.black, foreground: Color.white, isHeader: true, startTimeRight: startTimeRight);

            var details = diag.details.Skip(1);

            if (!(details?.Any() ?? false))
                DrawSubroutine("No subroutines", "", "");
            else
            {

                var prevTime = diag.details.First().startTime.value;
                var prevFrame = diag.details.First().startFrame;
                var grouped = details.GroupConsecutive(ShouldCombine).ToArray();
                bool isFirst = true;
                foreach (var group in grouped)
                {

                    var current = group.First();

                    if (showMode == ShowMode.Time)
                    {

                        (string time, string unit) =
                            isFirst
                             ? TimeSpanUtility.FormatUnits_Components((float)Math.Abs((current.startTime - diag.diagOffset - current.endTime).TotalMilliseconds))
                             : TimeSpanUtility.FormatUnits_Components((float)Math.Abs((current.endTime - diag.diagOffset - prevTime).TotalMilliseconds));

                        DrawSubroutine(
                            startTime: time,
                            outsideStartTime: unit,
                            startTimeRight: startTimeRight,
                            level: current.level.ToString(),
                            subroutine: (CombineNull.IsCombineNull(current) ? "yield return null" : current.subroutine) + (group.Count() > 1 ? $" (x{group.Count()})" : ""));

                    }
                    else if (showMode == ShowMode.Frames)
                    {

                        //Every yield takes atleast one frame, but when Time.frameCount is logged,
                        //it may still be the same frame, doing this isn't accurate, but effectively speaking, does it matter?
                        var duration = Math.Max(1, Math.Abs(current.startFrame - prevFrame));

                        DrawSubroutine(
                            startTime: duration.ToString(),
                            outsideStartTime: "",
                            startTimeRight: startTimeRight,
                            level: current.level.ToString(),
                            subroutine: (CombineNull.IsCombineNull(current) ? "yield return null" : current.subroutine) + (group.Count() > 1 ? $" (x{group.Count()})" : ""));

                    }

                    prevTime = group.Last().endTime;
                    prevFrame = group.Last().endFrame;
                    isFirst = false;

                }

            }

            bool ShouldCombine(CoroutineDiagHelper.SubroutineDetails c1, CoroutineDiagHelper.SubroutineDetails c2) =>
                CombineNull.IsCombineNull(c1) && CombineNull.IsCombineNull(c2) && c1.level == c2.level;

            EditorGUILayout.EndVertical();

        }

        void DrawSubroutine(string startTime, string level, string subroutine, Color? background = null, Color? foreground = null, bool isHeader = false, string outsideStartTime = null, float startTimeRight = 0)
        {

            if (!durationColumn && !depthColumn && !subroutineColumn)
                return;

            var c = GUI.color;

            if (!background.HasValue)
                EditorGUILayout.BeginHorizontal();
            else
            {
                GUI.color = background.Value;
                EditorGUILayout.BeginHorizontal(GUI.skin.box);
                GUI.color = foreground.Value;
            }

            if (durationColumn)
            {

                EditorGUILayout.LabelField(Content.Dynamic(nameof(startTime), startTime), Styles.subroutineRightText, GUILayout.Width(64 + (isHeader ? startTimeRight : 0)), GUILayout.ExpandWidth(false));

                if (!isHeader)
                    EditorGUILayout.LabelField(outsideStartTime, GUILayout.Width(startTimeRight));

            }

            int.TryParse(level, out var i2);
            var indent = isHeader || i2 == 0 ? "" : string.Join("", Enumerable.Repeat("--", i2 - 1));

            if (depthColumn)
                EditorGUILayout.LabelField(Content.Dynamic("level" + level, level), Styles.subroutineText, GUILayout.Width(64), GUILayout.ExpandWidth(false));

            if (subroutineColumn)
            {

                var c1 = Content.Dynamic(indent + subroutine, indent + subroutine);
                if (isHeader)
                    EditorGUILayout.LabelField(c1);
                else
                    EditorGUILayout.LabelField(c1, GUILayout.Width(EditorStyles.label.CalcSize(c1).x));

            }

            EditorGUILayout.EndHorizontal();
            GUI.color = c;

        }

        #endregion

#endif

        #endregion

#if UNITY_EDITOR

        static readonly Dictionary<GlobalCoroutine, CoroutineDiagHelper> diag = new Dictionary<GlobalCoroutine, CoroutineDiagHelper>();

        static bool isEnabled
        {
            get => SceneManager.settings.user.isCallbackUtilityEnabled;
            set => SceneManager.settings.user.isCallbackUtilityEnabled = value;
        }

        static CallbackUtility()
        {
            CoroutineUtility.Events.onCoroutineStarted += OnCoroutineStarted;
            CoroutineUtility.Events.onCoroutineEnded += OnCoroutineEnded;
            CoroutineUtility.Events.onSubroutineStart += OnCoroutineFrameStart;
            CoroutineUtility.Events.onSubroutineEnd += OnCoroutineFrameEndEvent;
        }

        static void OnCoroutineStarted(GlobalCoroutine coroutine)
        {

            if (!diag.ContainsKey(coroutine))
                diag.Add(coroutine, new CoroutineDiagHelper(coroutine.caller, coroutine.description));
            diag[coroutine].OnStart();

        }

        static void OnCoroutineEnded(GlobalCoroutine coroutine)
        {
            diag.GetValue(coroutine)?.End();
            diag.Remove(coroutine);
        }

        static object OnCoroutineFrameStart(GlobalCoroutine coroutine, object data, int level, object parentUserData, bool isPause) =>
            diag.GetValue(coroutine)?.Log(isPause ? "[Pause]" : data, level, parentUserData as CoroutineDiagHelper.SubroutineDetails);

        static void OnCoroutineFrameEndEvent(GlobalCoroutine coroutine, object userData) =>
           (userData as CoroutineDiagHelper.SubroutineDetails)?.End();

#endif

        public static FluentInvokeAPI<T> Invoke<T>() =>
            new();

        static IEnumerator Invoke<T>(FluentInvokeAPI<T>.Callback invoke, object param, params object[] obj)
        {

            var callbackObjects = obj.
                SelectMany(o => GetT<T>(o)).
                ToArray();

            if (!callbackObjects.Any())
                yield break;

            var parallelCallbacks = callbackObjects.Where(so => so.GetType().GetCustomAttribute<ParallelASMCallbacks>() != null).ToArray();
            var staggeredCallbacks = callbackObjects.Except(parallelCallbacks).ToArray();

            var coroutines = new List<GlobalCoroutine>();
            yield return parallelCallbacks.Select(so => Add(so, isParallel: true)).ToArray().WaitAll();

            foreach (var callback in staggeredCallbacks)
                yield return Add(callback);

            GlobalCoroutine Add(T callback, bool isParallel = false)
            {

                var isEnabled = (callback is MonoBehaviour mb && mb.isActiveAndEnabled) || callback is ScriptableObject;

                var c = invoke.Invoke(callback, isEnabled).StartCoroutine(description: callback.ToString());
                coroutines.Add(c);
                if (callback is MonoBehaviour m)
                    SetupDiag(c, isParallel, m.ASMScene());
                return c;

            }

        }

        static IEnumerable<T> GetT<T>(object obj)
        {

            if (obj is ScriptableObject so && so is T t)
                yield return t;
            else if (obj is Scene scene && scene.isOpen && !scene.isPreloaded)
                foreach (var item in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<T>()))
                    yield return item;

        }

        static void SetupDiag(GlobalCoroutine coroutine, bool isParallel, Scene scene)
        {
#if UNITY_EDITOR

            if (isEnabled)
            {

                CoroutineUtility.Events.enableEvents = true;

                if (!CallbackUtility.diag.ContainsKey(coroutine))
                    CallbackUtility.diag.Add(coroutine, new CoroutineDiagHelper(coroutine.caller, coroutine.description));

                var diag = CallbackUtility.diag[coroutine];

                diag.SetParallel(isParallel);

                instance.coroutines.Add(scene.id, diag);
                instance.Save();

            }

#endif
        }

        static readonly Dictionary<Type, Callback> defaultCallbacks = new()
        {

            { typeof(ISceneOpen),            (o, p) => Call(() => ((ISceneOpen)o).OnSceneOpen()) },
            { typeof(ISceneClose),           (o, p) => Call(() => ((ISceneClose)o).OnSceneClose()) },
            { typeof(ICollectionOpen),       (o, p) => Call(() => ((ICollectionOpen)o).OnCollectionOpen(p as SceneCollection)) },
            { typeof(ICollectionClose),      (o, p) => Call(() => ((ICollectionClose)o).OnCollectionClose(p as SceneCollection)) },

            { typeof(ISceneOpenAsync),       (o, p) =>  ((ISceneOpenAsync)o).OnSceneOpen() },
            { typeof(ISceneCloseAsync),      (o, p) =>  ((ISceneCloseAsync)o).OnSceneClose() },
            { typeof(ICollectionOpenAsync),  (o, p) =>  ((ICollectionOpenAsync)o).OnCollectionOpen(p as SceneCollection) },
            { typeof(ICollectionCloseAsync), (o, p) =>  ((ICollectionCloseAsync)o).OnCollectionClose(p as SceneCollection) },

        };

        static IEnumerator Call(Action action)
        {
            action.LogInvoke();
            yield break;
        }

        static IEnumerator DefaultCallback(Type t, object obj, object param = null) =>
            typeof(ISceneCallbacks).IsAssignableFrom(t)
                ? defaultCallbacks.GetValue(t)?.Invoke(obj, param)
                : null;

        public static IEnumerator DoSceneOpenCallbacks(Scene scene) =>
            CoroutineUtility.WaitAll(
                Invoke<ISceneOpen>().On(scene),
                Invoke<ISceneOpenAsync>().On(scene));

        public static IEnumerator DoSceneCloseCallbacks(Scene scene) =>
            CoroutineUtility.WaitAll(
                Invoke<ISceneClose>().On(scene),
                Invoke<ISceneCloseAsync>().On(scene));

        public static IEnumerator DoCollectionOpenCallbacks(SceneCollection collection)
        {

            if (collection && collection.userData)
                yield return CoroutineUtility.WaitAll(
                    Invoke<ICollectionOpen>().WithParam(collection).On(collection.userData),
                    Invoke<ICollectionOpenAsync>().WithParam(collection).On(collection.userData));

            if (collection)
                yield return CoroutineUtility.WaitAll(
                    Invoke<ICollectionOpen>().WithParam(collection).On(collection),
                    Invoke<ICollectionOpenAsync>().WithParam(collection).On(collection));

        }

        public static IEnumerator DoCollectionCloseCallbacks(SceneCollection collection)
        {

            if (collection && collection.userData)
                yield return CoroutineUtility.WaitAll(
                    Invoke<ICollectionClose>().WithParam(collection).On(collection.userData),
                    Invoke<ICollectionCloseAsync>().WithParam(collection).On(collection.userData));

            if (collection)
                yield return CoroutineUtility.WaitAll(
                    Invoke<ICollectionClose>().WithParam(collection).On(collection),
                    Invoke<ICollectionCloseAsync>().WithParam(collection).On(collection));

        }

        /// <summary>An helper class to facilitate a fluent api.</summary>
        /// <remarks>Usage: <see cref="Invoke{T}"/></remarks>
        public sealed class FluentInvokeAPI<T>
        {

            public delegate IEnumerator Callback(T obj, bool isEnabled);
            Callback callback;
            object param;

            /// <summary>Gets whatever <typeparamref name="T"/> has a default callback. All callbacks inheriting from <see cref="ISceneCallbacks"/> should have one.</summary>
            public bool hasDefaultCallback =>
                defaultCallbacks.ContainsKey(typeof(T));

            /// <summary>Specify a callback, this should point to the interface method which provides a <see cref="IEnumerator"/>.</summary>
            /// <remarks>This is not needed for callback interfaces inheriting from <see cref="ISceneCallbacks"/>.</remarks>
            public FluentInvokeAPI<T> WithCallback(Callback callback) =>
                Set(() => this.callback = callback);

            /// <summary>Specify a parameter to use when invoking the callback.</summary>
            public FluentInvokeAPI<T> WithParam(object param) =>
                Set(() => this.param = param);

            /// <summary>Specify the collection scenes to run this callback on and start execution.</summary>
            public IEnumerator On(SceneCollection collection, params Scene[] additionalScenes) =>
                On(collection.scenes.Concat(additionalScenes).ToArray());

            /// <summary>Specify the collection scenes to run this callback on and start execution..</summary>
            public IEnumerator OnAllOpenScenes() =>
                On(SceneManager.runtime.openScenes.ToArray());

            /// <summary>Specify the scenes to run this callback on and start execution.</summary>
            public IEnumerator On(params Scene[] scenes)
            {

                scenes = scenes.NonNull().Where(s => s.isOpen).ToArray();
                if (scenes.Length == 0)
                    yield break;

                if (hasDefaultCallback && callback is null)
                    callback = (c, isEnabled) => DefaultCallback(typeof(T), c, param);

                if (callback is null)
                {
                    Debug.LogError($"No callback specified for a callback of type '{typeof(T).Name}'");
                    yield break;
                }

                yield return Invoke(callback, param, scenes);

            }

            /// <summary>Specify the scenes to run this callback on and start execution.</summary>
            public IEnumerator On(params ScriptableObject[] scriptableObjects)
            {

                scriptableObjects = scriptableObjects.Where(s => s).ToArray();
                if (scriptableObjects.Length == 0)
                    yield break;

                if (hasDefaultCallback && callback is null)
                    callback = (c, isEnabled) => DefaultCallback(typeof(T), c, param);

                if (callback is null)
                {
                    Debug.LogError($"No callback specified for a callback of type '{typeof(T).Name}'");
                    yield break;
                }

                yield return Invoke(callback, param, scriptableObjects.Where(s => s).ToArray());

            }

            FluentInvokeAPI<T> Set(Action action)
            {
                action?.Invoke();
                return this;
            }

        }

    }

}
