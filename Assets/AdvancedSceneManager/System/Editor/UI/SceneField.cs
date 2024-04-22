using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Editor
{

    /// <summary>A <see cref="ObjectField"/> that only accepts <see cref="Scene"/>, with support for <see cref="SceneAsset"/> drag drop.</summary>
    public class SceneField : ObjectField, INotifyValueChanged<Scene>
    {

        public void SetObjectPickerEnabled(bool value) =>
            this.Q(className: "unity-base-field__input").SetEnabled(value);

        readonly Action buttonRefresh;

        public SceneField()
        {

            SceneOpenButtonsHelper.AddButtons(this, () => value, out buttonRefresh);

            allowSceneObjects = false;
            objectType = typeof(Scene);

            SetupDragDropTarget();
            SetupMouseEvents();

        }

        #region Value

        public new Scene value
        {
            get => base.value as Scene;
            set => base.value = value;
        }

        public override void SetValueWithoutNotify(Object newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshButtons();
        }

        public void SetValueWithoutNotify(Scene newValue)
        {

            if (value) value.PropertyChanged -= Value_PropertyChanged;
            if (newValue) newValue.PropertyChanged += Value_PropertyChanged;

            base.SetValueWithoutNotify(newValue);

            RefreshButtons();

        }

        void Value_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
            RefreshButtons();

        public void RegisterValueChangedCallback(EventCallback<ChangeEvent<Scene>> callback) =>
            INotifyValueChangedExtensions.RegisterValueChangedCallback<Object>(this,
                e => callback.Invoke(ChangeEvent<Scene>.GetPooled(e.previousValue as Scene, e.newValue as Scene)));

        #endregion
        #region Drag drop target

        void SetupDragDropTarget()
        {

            //This fixes a bug where dropping a scene one pixel above this element would result in null being assigned to this field
            var element = this.Q(className: "unity-object-field-display");
            if (element == null)
                return;

            element.RegisterCallback<DragUpdatedEvent>(DragUpdated, TrickleDown.TrickleDown);
            element.RegisterCallback<DragPerformEvent>(DragPerform, TrickleDown.TrickleDown);

            void DragUpdated(DragUpdatedEvent e)
            {

                if (!HasSceneAsset(out var scene))
                    return;

                DragAndDrop.AcceptDrag();
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;

            }

            void DragPerform(DragPerformEvent e)
            {

                if (!HasSceneAsset(out var scene))
                    return;

                value = scene;

            }

            bool HasSceneAsset(out Scene asset)
            {
                var l = GetDragDropScenes().ToArray();
                asset = GetDragDropScenes().FirstOrDefault();
                return asset;
            }

        }

        public static IEnumerable<Scene> GetDragDropScenes() =>
            DragAndDrop.objectReferences.OfType<Scene>().Concat(
                DragAndDrop.objectReferences.
                OfType<SceneAsset>().
                Select(o => o.ASMScene()).
                NonNull()).
                Distinct();

        #endregion
        #region Mouse events

        public delegate void OnClick(PointerDownEvent e);

        OnClick onClick;
        public void OnClickCallback(OnClick onClick) =>
            this.onClick = onClick;

        void SetupMouseEvents()
        {

            var element = this.Q(className: "unity-object-field__object");
            var clickCount = 0;

            element.RegisterCallback<PointerDownEvent>(MouseDown);
            element.RegisterCallback<PointerLeaveEvent>(MouseLeave);
            element.RegisterCallback<PointerUpEvent>(MouseUp);

            void MouseDown(PointerDownEvent e)
            {

                onClick?.Invoke(e);

                if (e.button != 0)
                    return;

                if (!e.isPropagationStopped)
                {
                    e.StopPropagation();
                    clickCount = e.clickCount;
                    element.CaptureMouse();
                }

            }

            void MouseLeave(PointerLeaveEvent e)
            {

                if (e.isPropagationStopped)
                    return;

                if (clickCount == 1 && e.pressedButtons == 1)
                    StartDrag();

                clickCount = 0;
                element.ReleaseMouse();

            }

            void MouseUp(PointerUpEvent e)
            {

                if (e.isPropagationStopped)
                    return;

                if (clickCount == 0)
                    return;

                e.StopPropagation();

                if (!value)
                    return;

                PingAsset();
                element.ReleaseMouse();

                clickCount = 0;

            }

        }

        void StartDrag()
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new[] { value };
            DragAndDrop.StartDrag("Scene drag:" + value.name);
        }

        /// <summary>Pings the associated SceneAsset in project window.</summary>
        public void PingAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(((Scene)value).path);
            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>Opens the associated SceneAsset.</summary>
        public void OpenAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(((Scene)value).path);
            _ = AssetDatabase.OpenAsset(asset);
            Selection.activeObject = asset;
        }

        #endregion
        #region Open buttons

        void RefreshButtons() =>
            buttonRefresh?.Invoke();

        #endregion
        #region Factory

        public new class UxmlFactory : UxmlFactory<SceneField, UxmlTraits>
        {

            public override string uxmlNamespace => "AdvancedSceneManager";

        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {

            private readonly UxmlStringAttributeDescription m_propertyPath = new() { name = "Binding-path" };
            private readonly UxmlStringAttributeDescription m_label = new() { name = "Label" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {

                base.Init(ve, bag, cc);

                if (ve is SceneField field)
                {
                    field.label = m_label.GetValueFromBag(bag, cc);
                    field.bindingPath = m_propertyPath.GetValueFromBag(bag, cc);
                }

            }

        }

        #endregion

    }

}
