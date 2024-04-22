using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        class NotificationView : ViewModel
        {

            VisualElement list;
            public override void OnCreateGUI(VisualElement element) =>
                list = element;

            readonly Dictionary<string, VisualElement> notifications = new();

            public string Notify(string message, Action onClick, Action onDismiss = null, bool canDismiss = true)
            {

                if (list is null)
                    return default;

                var id = GUID.Generate().ToString();
                Notify(message, id, onClick, onDismiss, canDismiss);

                return id;

            }

            public void Notify(string message, string id, Action onClick, Action onDismiss = null, bool canDismiss = true)
            {

                Remove(id);

                if (list is null)
                    return;

                Button element = null;
                element = new Button(() => Dismiss(onClick));

                element.Add(new Label(message));
                var spacer = new VisualElement();
                spacer.AddToClassList("spacer");
                element.Add(spacer);

                if (canDismiss)
                    element.Add(new Button(() => Dismiss(onDismiss)) { text = "x" });

                element.AddToClassList("notification");
                list.Add(element);

                notifications.Add(id, element);

                void Dismiss(Action callback)
                {
                    if (canDismiss)
                        element.RemoveFromHierarchy();
                    callback?.Invoke();
                }

            }

            public void Remove(string id)
            {
                if (notifications.Remove(id, out var element))
                    element.RemoveFromHierarchy();
            }

        }

    }

}
