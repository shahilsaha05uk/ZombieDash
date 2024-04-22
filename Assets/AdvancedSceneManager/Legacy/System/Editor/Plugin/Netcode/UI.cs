#if ASM_PLUGIN_NETCODE && UNITY_2021_1_OR_NEWER

using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Editor;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Editor.Window;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SettingsTab = AdvancedSceneManager.Editor.Window.SettingsTab;

namespace AdvancedSceneManager.Plugin.Netcode.Editor
{

    //Adds 'Netcode' buttons to scenes and collections in scene manager window.

    [InitializeOnLoad]
    static class UI
    {

        static UI()
        {

            SceneManagerWindow.OnGUIEvent -= OnGUI;
            SceneManagerWindow.OnGUIEvent += OnGUI;

            ScenesTab.AddExtraButton(GetCollectionNetcodeButton);
            ScenesTab.AddExtraButton(GetSceneNetcodeButton);

            SettingsTab.Settings.Add(
                new Toggle("Display netcode buttons:").
                Setup(
                    valueChanged: e => showButtons = e.newValue,
                    defaultValue: showButtons,
                    tooltip: "Enables or disables netcode buttons in scenes tab (does not disable functionality, saved in EditorPrefs)"),
                    header: SettingsTab.Settings.DefaultHeaders.Appearance);

            SettingsTab.Settings.Add(
                new Toggle("Enable ASM scene validation:").
                Setup(
                    valueChanged: e => SceneValidator.enable = e.newValue,
                    defaultValue: SceneValidator.enable,
                    tooltip: "Enables or disables ASM netcode plugin scene validation, disable this if you want to use your own override. (saved in project settings)"),
                    header: SettingsTab.Settings.DefaultHeaders.Options_Project);

        }

        public static bool showButtons
        {
            get => EditorPrefs.GetBool("AdvancedSceneManager.Netcode.ShowButtons", true);
            set => EditorPrefs.SetBool("AdvancedSceneManager.Netcode.ShowButtons", value);
        }

        static Vector2 mousePos;
        static void OnGUI() =>
            mousePos = Event.current.mousePosition;

        static VisualElement GetCollectionNetcodeButton(SceneCollection collection)
        {

            if (!showButtons || !collection || collection.scenes == null)
                return null;

            var scenes = collection.scenes.Where(s => s).ToArray();
            var button = Button(collection, "Netcode", 82, scenes.Any(SceneExtensions.IsNetcode));

            _ = button.RegisterValueChangedCallback(e =>
            {

                foreach (var scene in scenes)
                    scene.NetcodeState(e.newValue);
                RefreshButtons();

            });

            return button;

        }

        static VisualElement GetSceneNetcodeButton(Scene scene)
        {

            if (!showButtons || !scene)
                return null;

            var button = Button(scene, "Netcode", 56, scene.IsNetcode());

            _ = button.RegisterValueChangedCallback(e =>
            {
                scene.NetcodeState(e.newValue);
                RefreshButtons();
            });

            return button;

        }

        static readonly Color hoverBackground = new Color(0, 0, 0, 0.3f);

        static Color checkedColor =>
            SceneManagerWindow.IsDarkMode
            ? darkCheckedColor
            : lightCheckedColor;

        static Color uncheckedColor =>
            SceneManagerWindow.IsDarkMode
            ? darkUncheckedColor
            : lightUncheckedColor;

        static readonly Color darkCheckedColor = new Color32(85, 246, 98, 255);
        static readonly Color darkUncheckedColor = Color.white;

        static readonly Color lightCheckedColor = new Color32(0, 150, 8, 255);
        static readonly Color lightUncheckedColor = Color.black;

        static readonly Dictionary<IASMObject, ToolbarToggle> buttons = new Dictionary<IASMObject, ToolbarToggle>();
        static ToolbarToggle Button(IASMObject obj, string text, float width, bool value)
        {

            var button = new ToolbarToggle();
            button.style.alignSelf = Align.Center;
            button.style.marginLeft = 2;
            button.style.SetBorderWidth(0);
            button.style.width = width;
            button.text = text;

            button.AddToClassList("StandardButton");
            button.AddToClassList("no-checkedBackground");
            button.style.backgroundColor = Color.clear;
            button.SetValueWithoutNotify(value);

            RefreshButton(button, value);
            _ = button.RegisterValueChangedCallback(e => RefreshButton(button, e.newValue));

            button.RegisterCallback<MouseEnterEvent>(e => { button.style.backgroundColor = hoverBackground; });
            button.RegisterCallback<MouseLeaveEvent>(e => { button.style.backgroundColor = Color.clear; });

            button.RegisterCallback<GeometryChangedEvent>(e =>
            {

                var pos = mousePos;
                if (button.worldBound.Contains(pos))
                    button.style.backgroundColor = new Color(0, 0, 0, 0.3f);

            });

            _ = buttons.Set(obj, button);

            return button;

        }

        static void RefreshButtons()
        {
            foreach (var button in buttons)
            {
                if (button.Key is SceneCollection collection)
                    RefreshButton(collection);
                else if (button.Key is Scene scene)
                    RefreshButton(scene);
            }
        }

        static void RefreshButton(SceneCollection collection) =>
            RefreshButton(buttons.GetValue(collection), collection.scenes.Where(s => s).Any(SceneExtensions.IsNetcode));

        static void RefreshButton(Scene scene) =>
            RefreshButton(buttons.GetValue(scene), scene && scene.IsNetcode());

        static void RefreshButton(ToolbarToggle button, bool value)
        {

            button.style.opacity = value ? 1 : 0.4f;

            button.Q<Label>().style.color = value ? checkedColor : uncheckedColor;
            button.SetValueWithoutNotify(value);
            button.tooltip = value ? "Remove from addressables" : "Add to addressables";

        }

    }

}
#endif
