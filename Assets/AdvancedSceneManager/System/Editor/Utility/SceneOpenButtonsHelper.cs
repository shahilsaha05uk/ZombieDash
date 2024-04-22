using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Editor.Utility
{

    static class SceneOpenButtonsHelper
    {

        static SceneOpenButtonsHelper()
        {
            EditorSceneManager.sceneSaved += EditorSceneManager_sceneSaved;
            SceneImportUtility.scenesChanged += SceneImportUtility_scenesChanged;
        }

        static void SceneImportUtility_scenesChanged() =>
            ReinitializeScenePickers();

        static void EditorSceneManager_sceneSaved(UnityEngine.SceneManagement.Scene scene) =>
            ReinitializeScenePickers();

        static void ReinitializeScenePickers()
        {
            foreach (var callback in callbacksOnSceneSaved.Values.ToArray())
                callback.Invoke();
        }

        const string additiveClosed = "";
        const string additiveOpen = "";

        public static void AddButtons(this VisualElement parent, Func<Scene> sceneFunc, int insertAt = 0) =>
            AddButtons(parent, sceneFunc, out _, out _, out _, insertAt);

        public static void AddButtons(this VisualElement parent, Func<Scene> sceneFunc, out Action refresh, int insertAt = 0) =>
            AddButtons(parent, sceneFunc, out _, out _, out refresh, insertAt);

        public static void AddButtons(this VisualElement parent, Func<Scene> sceneFunc, out Button buttonOpen, out Button buttonAdditive, out Action refresh, int insertAt = 0)
        {

            buttonOpen = new Button(OpenScene) { text = "", tooltip = "Open scene", name = "button-open" };
            buttonAdditive = new Button(OpenSceneAdditive) { text = additiveClosed, tooltip = "Open scene additively", name = "button-additive" };

            buttonOpen.AddToClassList("scene-open-button");
            buttonAdditive.AddToClassList("scene-open-button");
            buttonOpen.AddToClassList("fontAwesome");
            buttonAdditive.AddToClassList("fontAwesome");
            buttonAdditive.style.marginRight = 6;

            parent.Insert(insertAt, buttonOpen);
            parent.Insert(insertAt + 1, buttonAdditive);

            var b1 = buttonOpen;
            var b2 = buttonAdditive;
            refresh = () => RefreshButtons(b1, b2);
            EditorApplication.delayCall += refresh.Invoke;

            SceneManager.runtime.startedWorking += refresh;
            SceneManager.runtime.stoppedWorking += refresh;

            void OpenScene()
            {
                if (sceneFunc.Invoke() is Scene scene && scene)
                    SceneManager.runtime.CloseAll().Open(scene);
            }

            void OpenSceneAdditive()
            {
                if (sceneFunc.Invoke() is Scene scene && scene)
                    SceneManager.runtime.ToggleOpen(scene);
            }

            void RefreshButtons(Button buttonOpen, Button buttonAdditive)
            {

                var scene = sceneFunc.Invoke();

#if COROUTINES
                buttonOpen?.SetEnabled(scene && !SceneManager.runtime.isBusy);
                buttonAdditive?.SetEnabled(scene && !SceneManager.runtime.isBusy);
#else

            buttonOpen?.SetEnabled(false);
            buttonAdditive?.SetEnabled(false);

            buttonOpen.tooltip = "Editor coroutines needed to use this feature.";
            buttonAdditive.tooltip = "Editor coroutines needed to use this feature.";

#endif

                buttonAdditive.text = scene && scene.isOpen ? additiveOpen : additiveClosed;

            }

        }

        static readonly Dictionary<DropdownField, Action> callbacksOnSceneSaved = new();
        public static void SetupSceneDropdown(this DropdownField dropdown, Func<IEnumerable<Scene>> getScenes, Func<Scene> getValue, Action<Scene> setValue, bool allowNull = true) =>
            SetupSceneDropdown(dropdown, getScenes, getValue, setValue, null, allowNull);

        static void SetupSceneDropdown(this DropdownField dropdown, Func<IEnumerable<Scene>> getScenes, Func<Scene> getValue, Action<Scene> setValue, Action buttonRefresh, bool allowNull = true)
        {

            if (buttonRefresh == null)
            {
                dropdown.RegisterValueChangedCallback(OnValueChanged);
                AddButtons(dropdown, () => getScenes().ElementAtOrDefault(dropdown.index - (allowNull ? 1 : 0)), out buttonRefresh, 1);
                dropdown.RegisterCallback<DetachFromPanelEvent>(e => callbacksOnSceneSaved.Remove(dropdown));
                callbacksOnSceneSaved.Set(dropdown, () => SetupSceneDropdown(dropdown, getScenes, getValue, setValue, buttonRefresh, allowNull));
            }

            LoadingScreenUtility.RefreshSpecialScenes();

            var scenes = getScenes().ToList();
            dropdown.Q(className: "unity-base-field__input").SetEnabled(scenes.Count > 0);

            dropdown.choices = scenes.NonNull().Select(s => s.name).ToList();
            dropdown.index = scenes.IndexOf(getValue());

            if (allowNull)
            {
                dropdown.choices.Insert(0, "None");
                dropdown.index += 1;
            }

            void OnValueChanged(ChangeEvent<string> e)
            {
                var i = dropdown.index;
                var scene = getScenes().ElementAtOrDefault(i - (allowNull ? 1 : 0));
                if (getValue() != scene)
                    setValue(scene);
                EditorApplication.delayCall += buttonRefresh.Invoke;
            }

        }

    }

}
