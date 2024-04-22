using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    static class ContextMenuUtility
    {

        static readonly Dictionary<VisualElement, ContextualMenuManipulator> manipulators = new();

        public static void ClearContextMenu(VisualElement element)
        {
            if (manipulators.Remove(element, out var manipulator))
                element.RemoveManipulator(manipulator);
        }

        public static void ContextMenu(this VisualElement element, Action<ContextualMenuPopulateEvent> e)
        {
            ClearContextMenu(element);
            manipulators.Add(element, new ContextualMenuManipulator(e) { target = element });
        }

    }

}
