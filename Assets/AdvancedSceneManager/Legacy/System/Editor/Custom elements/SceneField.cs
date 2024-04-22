#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    /// <summary>
    /// <para>An <see cref="ObjectField"/> that only accepts <see cref="Scene"/>, with support for <see cref="SceneAsset"/> drag drop.</para>
    /// <para>Has support for <see cref="labelFilter"/>, which filters scenes based on label (i.e. to only show scenes from 'Collection1', for example, use 'ASM:Collection1').</para>
    /// <para><see cref="showOpenButtons"/> can be used to toggle open buttons.</para>
    /// <para>When <see cref="ObjectField.isReadOnly"/> is true, <see cref="showOpenButtons"/> will still be interactable, but value cannot be changed.</para>
    /// </summary>
    public class SceneField : ObjectField
    {

        bool PassesFilter(Scene scene) =>
            PassesFilter(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path));

        bool PassesFilter(SceneAsset asset) =>
            string.IsNullOrWhiteSpace(labelFilter) || AssetDatabase.GetLabels(asset).Contains(labelFilter);

        public SceneField() : base()
        {

            allowSceneObjects = false;
            objectType = typeof(Scene);

            SetupDragDrop();
            SetupMouseEvents();
            SetupOpenButtons();

            EditorApplication.delayCall += () =>
            {

                if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                    return;

                OnValueChanged(value, value);
                UpdateEnabled();

            };

            if (!string.IsNullOrEmpty(defaultName) && !value)
                this.Q<Label>(className: "unity-object-field-display__label").text = defaultName;

            RegisterValueChangedCallback(e => OnValueChanged(e.previousValue, e.newValue));

        }

        public string labelFilter { get; set; }
        public bool showOpenButtons { get; set; } = true;

        public string defaultName { get; set; }

        #region Mouse events

        void SetupDragDrop()
        {

            var scenes = new List<Scene>();

            //This fixes a bug where dropping a scene one pixel above this element would result in null being assigned to this field
            var element = this.Q(className: "unity-object-field-display");
            if (element == null)
                return;

            element.RegisterCallback<DragEnterEvent>(e =>
            {

                e.PreventDefault();

                if (isReadOnly)
                    return;

                scenes.Clear();
                scenes.AddRange(DragAndDrop.objectReferences.OfType<Scene>());
                scenes.AddRange(DragAndDrop.objectReferences.OfType<SceneAsset>().Select(s => s.FindASMScene()));
                scenes.AddRange(DragAndDrop.paths.Select(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p.Replace('\\', '/').Replace(Application.dataPath, ""))).OfType<SceneAsset>().Select(s => s.FindASMScene()));
                scenes = scenes.Where(s => s).GroupBy(s => s.path).Select(g => g.First()).ToList();

                if (scenes.Any())
                {
                    DragAndDrop.AcceptDrag();
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                }

            });

            element.RegisterCallback<DragUpdatedEvent>(e =>
            {

                e.PreventDefault();

                if (isReadOnly)
                    return;

                DragAndDrop.AcceptDrag();
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;

            });

            element.RegisterCallback<DragLeaveEvent>(e => Cancel());
            element.RegisterCallback<DragPerformEvent>(e =>
            {

                e.PreventDefault();

                if (isReadOnly)
                    return;

                if (scenes.Any())
                    value = scenes.FirstOrDefault();

                Cancel();

            });

            var down = false;
            element.RegisterCallback<MouseDownEvent>(e => down = true);
            element.RegisterCallback<MouseLeaveEvent>(e => down = false);
            element.RegisterCallback<MouseUpEvent>(e => down = false);

            element.RegisterCallback<MouseMoveEvent>(e =>
            {

                if (!down)
                    return;

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { value };
                DragAndDrop.StartDrag("Scene");

            });

            void Cancel() =>
                scenes.Clear();

        }

        void SetupMouseEvents()
        {

            if (isReadOnly)
                style.opacity = 0.5f;

            var clickCount = 0;
            RegisterCallback<MouseDownEvent>(e =>
            {
                if (!IsMouseOverObjectPickerArea(e))
                {
                    clickCount = e.clickCount;
                    e.PreventDefault();
                }

            }, TrickleDown.TrickleDown);

            RegisterCallback<MouseLeaveEvent>(e => clickCount = 0);

            RegisterCallback<MouseUpEvent>(e =>
            {

                if (clickCount == 0)
                    return;

                if (!IsMouseOverObjectPickerArea(e) && e.localMousePosition.x > buttonAdditive.localBound.xMax + 3)
                {

                    e.PreventDefault();

                    if (!value)
                        return;

                    var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(value.assetID));
                    EditorGUIUtilityExt.PingOrOpenAsset(asset, clickCount);

                }

                clickCount = 0;

            }, TrickleDown.TrickleDown);

            bool IsMouseOverObjectPickerArea(IMouseEvent e) =>
                e.localMousePosition.x > worldBound.width - 20;

        }

        #endregion
        #region Open buttons

        /// <summary>This will be passed to Scene.Open().</summary>
        public SceneCollection Collection { get; set; }

        Button buttonSingle;
        Button buttonAdditive;
        public event Action OnSceneOpen;
        public event Action OnSceneOpenAdditive;
        void RefreshSceneOpen()
        {
            if (buttonAdditive == null)
                return;
            buttonAdditive.text = IsSceneOpen() ? "-" : "+";
        }

        void UpdateEnabled() =>
            EditorApplication.delayCall += () =>
            {
                buttonSingle.SetEnabled(value);
                var sd = SceneUtility.GetAllOpenUnityScenes().ToArray();
                buttonAdditive.SetEnabled(value && !(IsSceneOpen() && SceneUtility.sceneCount == 1));
            };

        bool IsSceneOpen() =>
            SceneUtility.GetAllOpenUnityScenes().Any(s => value ? s.path == value.path : false);

        void SetupOpenButtons()
        {

            if (!showOpenButtons)
                return;

            buttonSingle = new Button() { text = "↪", tooltip = "Open scene" };
            buttonSingle.AddToClassList("StandardButton");
            buttonSingle.AddToClassList("OpenScene");

            buttonAdditive = new Button() { text = "+", tooltip = "Open scene additively" };
            buttonAdditive.AddToClassList("StandardButton");
            buttonAdditive.AddToClassList("OpenScene");
            buttonAdditive.AddToClassList("additive");

            buttonSingle.style.unityFont = new StyleFont(Resources.Load<Font>("Fonts/Inter-Regular"));

            buttonSingle.style.marginTop = -0.5f;
            buttonAdditive.style.marginTop = -0.5f;

            buttonSingle.clicked += () => OnOpen(false);
            buttonAdditive.clicked += () => OnOpen(true);

            RefreshSceneOpen();
            UpdateEnabled();

            RegisterValueChangedCallback(e => UpdateEnabled());

            EditorSceneManager.sceneOpened -= EditorSceneManager_sceneOpened;
            EditorSceneManager.sceneClosed -= EditorSceneManager_sceneClosed;

            EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
            EditorSceneManager.sceneClosed += EditorSceneManager_sceneClosed;

            void EditorSceneManager_sceneClosed(UnityEngine.SceneManagement.Scene scene) => UpdateEnabled();
            void EditorSceneManager_sceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode) => UpdateEnabled();

            void OnOpen(bool additive)
            {

                if (!value)
                    return;

                OpenScene(value, additive, Collection);

                if (!additive)
                    OnSceneOpen?.Invoke();
                else
                    OnSceneOpenAdditive?.Invoke();

            }

            Insert(0, buttonAdditive);
            Insert(0, buttonSingle);

        }

        public static void OpenScene(Scene scene, bool additive, SceneCollection collection = null)
        {

            if (!Application.isPlaying)
            {
                OpenEditor(scene, additive);
                scene.OnPropertyChanged();
            }
            else
            {

                _ = Open().StartCoroutine();
                IEnumerator Open() =>
                    SceneField.Open(scene, additive, collection);

            }

        }

        static IEnumerator Open(Scene scene, bool additive, SceneCollection collection)
        {

            if (!Application.isPlaying)
                yield break;

            if (SceneManager.standalone.IsOpen(scene) && additive)
                yield return SceneManager.standalone.Close(scene.GetOpenSceneInfo()).WithCollection(collection);
            else if (additive)
                yield return SceneManager.standalone.Open(scene).WithCollection(collection);
            else
                yield return SceneManager.standalone.OpenSingle(scene).WithCollection(collection);

        }

        static void OpenEditor(Scene scene, bool additive)
        {

            if (Application.isPlaying)
                return;

            if (SceneManager.editor.IsOpen(scene) && additive)
                SceneManager.editor.Close(scene);
            else if (additive)
                SceneManager.editor.Open(scene);
            else
                SceneManager.editor.OpenSingle(scene);

        }

        #endregion
        #region Value changed

        public static EventHandler<(Scene prevScene, Scene newScene)> onValueChanged;

        public SceneField SetValueWithoutNotify(Scene scene)
        {
            base.SetValueWithoutNotify(scene);
            return this;
        }

        public class SceneChangedEvent : ChangeEvent<Scene>
        {

            public SceneChangedEvent(Scene newValue, Scene oldValue)
            {
                this.newValue = newValue;
                this.previousValue = oldValue;
            }

        }

        public void RegisterValueChangedCallback(EventCallback<ChangeEvent<Scene>> callback) =>
            INotifyValueChangedExtensions.RegisterValueChangedCallback(this, new EventCallback<ChangeEvent<UnityEngine.Object>>(e => callback.Invoke(new SceneChangedEvent(e.newValue as Scene, e.previousValue as Scene))));

        public new Scene value
        {
            get => (Scene)base.value;
            set => base.value = value;
        }

        void OnValueChanged(Scene oldValue, Scene newValue)
        {

            if (oldValue)
                oldValue.PropertyChanged -= OnValuePropertyChanged;
            if (newValue)
            {
                newValue.PropertyChanged -= OnValuePropertyChanged;
                newValue.PropertyChanged += OnValuePropertyChanged;
                OnValuePropertyChanged(null, null);
            }

            var label = this.Q<Label>(className: "unity-object-field-display__label");
            var display = this.Q(className: "unity-object-field__input");
            label.TrimLabel((newValue ? newValue.name : "None") + " (Scene)", maxWidth: () => display.resolvedStyle.width - 42, enableAuto: true);

            if (!string.IsNullOrEmpty(defaultName) && !value)
                this.Q<Label>(className: "unity-object-field-display__label").text = defaultName;

            onValueChanged?.Invoke(this, (oldValue, newValue));

        }

        void OnValuePropertyChanged(object sender, EventArgs e)
        {
            RefreshSceneOpen();
        }

        #endregion
        #region Factory

        public new class UxmlFactory : UxmlFactory<SceneField, UxmlTraits>
        { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            readonly UxmlStringAttributeDescription m_label = new UxmlStringAttributeDescription() { name = "label" };
            readonly UxmlStringAttributeDescription m_labelFilter = new UxmlStringAttributeDescription() { name = "labelFilter" };
            readonly UxmlStringAttributeDescription m_type = new UxmlStringAttributeDescription() { name = "type" };
            readonly UxmlBoolAttributeDescription m_showOpenButtons = new UxmlBoolAttributeDescription() { name = "showOpenButtons" };
            readonly UxmlBoolAttributeDescription m_isReadOnly = new UxmlBoolAttributeDescription() { name = "isReadOnly" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {

                base.Init(ve, bag, cc);

                var element = ve as SceneField;
                element.label = m_label.GetValueFromBag(bag, cc);
                if (Type.GetType(m_type.GetValueFromBag(bag, cc)) is Type type)
                    element.objectType = type;

                element.showOpenButtons = m_showOpenButtons.GetValueFromBag(bag, cc);
                element.labelFilter = m_labelFilter.GetValueFromBag(bag, cc);
                element.isReadOnly = m_isReadOnly.GetValueFromBag(bag, cc);

            }

        }

        #endregion

    }

}
