using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    static class VisualElementUtility
    {

        public static T OnChecked<T>(this T button, Action callback) where T : BaseBoolField
        {
            _ = button.RegisterValueChangedCallback(e => { if (e.newValue) callback.Invoke(); });
            return button;
        }

        public static T OnUnchecked<T>(this T button, Action callback) where T : BaseBoolField
        {
            _ = button.RegisterValueChangedCallback(e => { if (!e.newValue) callback.Invoke(); });
            return button;
        }

        public static T SetChecked<T>(this T button, bool isChecked = true) where T : BaseBoolField
        {
            button.SetValueWithoutNotify(isChecked);
            return button;
        }

        public static void Hide(this VisualElement element) =>
            element.style.display = DisplayStyle.None;

        public static void Show(this VisualElement element) =>
            element.style.display = DisplayStyle.Flex;

        public static void SetVisible(this VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        #region GetAncestor

        public static VisualElement GetAncestor(this VisualElement element, string name = null, string className = null) =>
            GetAncestor<VisualElement>(element, name, className);

        public static T GetAncestor<T>(this VisualElement element, string name = null, string className = null) where T : VisualElement
        {

            if (element == null)
                return null;

            if (element is T && ((string.IsNullOrEmpty(name) || element.name == name) || (string.IsNullOrEmpty(className) || element.ClassListContains(className))))
                return (T)element;

            return element.parent.GetAncestor<T>(className, name);

        }

        #endregion
        #region Rotate animation

        static readonly Dictionary<VisualElement, IVisualElementScheduledItem> rotatingElements = new();
        public static void RotateAnimation(this VisualElement element, long tick = 10, int speed = 15) =>
            _ = rotatingElements.TryAdd(element,
                element.schedule.
                Execute(() => element.style.rotate = new Rotate(new(element.style.rotate.value.angle.value + speed))).
                Every(tick));

        public static void StopRotateAnimation(this VisualElement element)
        {
            if (rotatingElements.Remove(element, out var schedule))
            {
                schedule.Pause();
                element.style.rotate = default;
            }
        }

        #endregion

    }

}
