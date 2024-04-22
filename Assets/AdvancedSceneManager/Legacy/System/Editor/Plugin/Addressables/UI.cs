#if ASM_PLUGIN_ADDRESSABLES

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
using static AdvancedSceneManager.Plugin.Addressables.Editor.AddressablesListener;
using Scene = AdvancedSceneManager.Models.Scene;
using SettingsTab = AdvancedSceneManager.Editor.Window.SettingsTab;

namespace AdvancedSceneManager.Plugin.Addressables.Editor
{

    internal static class UI
    {

        [InitializeOnLoadMethod]
        internal static void OnLoad()
        {

            SceneManagerWindow.OnGUIEvent -= OnGUI;
            SceneManagerWindow.OnGUIEvent += OnGUI;

            GenerateButtons();
            AddSetting();

            PluginUtility.onBeforePluginDisabled += PluginUtility_onBeforePluginDisabled;

            EditorApplication.delayCall += () => RefreshButtons();

        }

        static void PluginUtility_onBeforePluginDisabled(PluginUtility.Plugin plugin)
        {
            if (plugin.dependency == "com.unity.addressables")
                BuildSceneListOverride.ResetBuildListBeforeDisable();
        }

        static Vector2 mousePos;
        static void OnGUI() =>
            mousePos = Event.current.mousePosition;

        #region Settings

        public static bool showButtons
        {
            get => EditorPrefs.GetBool("AdvancedSceneManager.Addressables.ShowButtons", true);
            set => EditorPrefs.SetBool("AdvancedSceneManager.Addressables.ShowButtons", value);
        }

        static void AddSetting() =>
            SettingsTab.Settings.Add(
           new Toggle("Display addressable buttons:").
           Setup(
               valueChanged: e => showButtons = e.newValue,
               defaultValue: showButtons,
               tooltip: "Enables or disables addressable buttons in scenes tab (does not disable functionality, saved in EditorPrefs)"),
               header: SettingsTab.Settings.DefaultHeaders.Appearance);

        #endregion
        #region Addressables

        /// <summary>Gets if all scenes are added to addressables.</summary>
        static bool IsAdded(params string[] paths)
        {

            if (paths == null || paths.Length == 0)
                return false;

            if (!settings)
                return false;

            var entries = settings.groups.SelectMany(g => g.entries?.Where(e => paths.Contains(e.AssetPath)));
            return paths.All(path => entries.Any(e => e.AssetPath == path));

        }

        /// <summary>Adds scene to addressables.</summary>
        static void AddScene(string scene)
        {
            if (SceneManager.assets.allScenes.TryFind(scene, out var s))
                s.IsAddressable(true);
        }

        /// <summary>Removes scene from addressables.</summary>
        static void RemoveScene(string scene)
        {
            if (SceneManager.assets.allScenes.TryFind(scene, out var s))
                s.IsAddressable(false);
        }

        #endregion
        #region Generate buttons

        static void GenerateButtons()
        {
            ScenesTab.AddExtraButton(GetCollectionAddressablesButton);
            ScenesTab.AddExtraButton(GetSceneAddressablesButton);
        }

        static VisualElement GetCollectionAddressablesButton(SceneCollection collection)
        {

            if (!showButtons || !collection || collection.scenes == null)
                return null;

            var paths = collection.scenes.Where(s => s).Select(s => s.path).ToArray();
            var button = Button(collection, "Addressable", 82, IsAdded(paths));

            _ = button.RegisterValueChangedCallback(value =>
            {

                if (value.newValue)
                {
                    var pathsToAdd = paths.Where(p => !IsAdded(p));
                    foreach (var path in pathsToAdd)
                        AddScene(path);
                }
                else
                    foreach (var group in settings.groups)
                        foreach (var entry in group.entries.ToArray())
                            if (paths.Contains(entry.AssetPath))
                                RemoveScene(entry.AssetPath);

                RefreshButtons();

            });

            return button;

        }

        static VisualElement GetSceneAddressablesButton(Scene scene)
        {

            if (!showButtons || !scene)
                return null;

            var button = Button(scene, "Addr.", 56, IsAdded(scene.path));

            _ = button.RegisterValueChangedCallback(value =>
               {

                   if (value.newValue)
                       AddScene(scene.path);
                   else
                       RemoveScene(scene.path);

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

            button.RegisterCallback<MouseEnterEvent>(e => button.style.backgroundColor = hoverBackground);
            button.RegisterCallback<MouseLeaveEvent>(e => button.style.backgroundColor = Color.clear);

            button.RegisterCallback<GeometryChangedEvent>(e =>
            {

                var pos = mousePos;
                if (button.worldBound.Contains(pos))
                    button.style.backgroundColor = new Color(0, 0, 0, 0.3f);

            });

            _ = buttons.Set(obj, button);

            return button;

        }

        #endregion
        #region Refresh buttons

        public static void RefreshButtons()
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
            RefreshButton(buttons.GetValue(collection), collection.scenes.Any() && collection.scenes.All(s => s.IsAddressable()));

        static void RefreshButton(Scene scene) =>
            RefreshButton(buttons.GetValue(scene), scene && scene.IsAddressable());

        static void RefreshButton(ToolbarToggle button, bool value)
        {

            if (button is null)
                return;

            button.style.opacity = value ? 1 : 0.4f;

            button.Q<Label>().style.color = value ? checkedColor : uncheckedColor;
            button.SetValueWithoutNotify(value);
            button.tooltip = value ? "Remove from addressables" : "Add to addressables";

        }

        #endregion

    }

}
#endif
