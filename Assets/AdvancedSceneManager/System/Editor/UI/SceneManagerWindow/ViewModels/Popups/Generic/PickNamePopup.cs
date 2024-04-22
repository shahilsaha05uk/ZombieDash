using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        [SerializeField] private string m_pickNameValue;

        class PickNamePopup : ViewModel, IPopup
        {

            static bool isOpen;
            static bool isDone;
            public static async Task<string> Prompt(string value = null)
            {

                if (isOpen)
                    throw new InvalidOperationException("Cannot display multiple prompts at a time.");
                isOpen = true;
                isDone = false;

                SceneManagerWindow.window.popups.Open<PickNamePopup>(value);

                while (!isDone)
                    await Task.Delay(100);

                var result = SceneManagerWindow.window.m_pickNameValue;

                isOpen = false;
                isDone = false;

                await SceneManagerWindow.window.popups.Close();

                return result;

            }

            public override void OnCreateGUI(VisualElement element, object param)
            {

                if (!isOpen)
                {
                    _ = window.popups.Close();
                    return;
                }

                window.m_pickNameValue = param as string;

                var button = element.Q<Button>("button-continue");

                button.clicked += () => isDone = true;

                element.Bind(new(window));
                button.SetEnabled(false);

                element.Q<Button>("button-cancel").clicked += () =>
                {
                    window.m_pickNameValue = null;
                    isDone = true;
                };

                element.Q<TextField>().RegisterValueChangedCallback(e => button.SetEnabled(Validate()));

                var textBox = element.Q<TextField>("text-name");

                textBox.SelectAll();
                textBox.Focus();

                textBox.RegisterCallback<KeyDownEvent>(e =>
                {
                    if (e.keyCode is KeyCode.KeypadEnter or KeyCode.Return)
                        if (Validate())
                            isDone = true;
                });

            }

            public override void OnRemoved()
            {
                if (!isDone)
                    window.m_pickNameValue = null;
                isOpen = false;
            }

            bool Validate()
            {

                var name = window.m_pickNameValue;

                return
                    !string.IsNullOrWhiteSpace(name) &&
                    !name.StartsWith(' ') &&
                    !name.EndsWith(' ') &&
                    name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

            }

        }

    }

}
