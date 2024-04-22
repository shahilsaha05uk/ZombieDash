#if UNITY_EDITOR

using System;
using System.Linq;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor.SceneManagement;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>An utility class to automatically open persistent scenes in editor.</summary>
    /// <remarks>Only available in editor.</remarks>
    public static class PersistentSceneInEditorUtility
    {

        public enum OpenInEditorOption
        {
            Never, AnySceneOpens, WhenAnyOfTheFollowingScenesOpen, WhenAnySceneOpensExcept
        }

        [Serializable]
        public struct OpenInEditorSetting
        {
            public OpenInEditorOption option;
            public string[] list;
        }

        /// <summary>Saves settings.</summary>
        public static void Update(string sceneAssetID, OpenInEditorSetting setting)
        {

            if (SceneManager.settings.local.editorPersistentScenes == null)
                SceneManager.settings.local.editorPersistentScenes = new SerializableDictionary<string, OpenInEditorSetting>();

            SceneManager.settings.local.editorPersistentScenes.Set(sceneAssetID, setting);
            SceneManager.settings.local.Save();

        }

        /// <summary>Gets the persistent option of a scene.</summary>
        public static OpenInEditorSetting GetPersistentOption(Scene scene)
        {
            if (SceneManager.settings.local.editorPersistentScenes?.ContainsKey(scene.assetID) ?? false)
                return SceneManager.settings.local.editorPersistentScenes[scene.assetID];
            else
                return new OpenInEditorSetting();
        }

        /// <summary>Open all scenes that are flagged to open when the specified scene is opened.</summary>
        /// <param name="promptSave">If <see langword="true"/>, then the user will be prompted to save any unsaved scenes before opening the scenes.</param>
        public static void OpenAssociatedPersistentScenes(Scene scene, bool promptSave = false)
        {

            if (!scene)
                return;

            if (promptSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scenes = GetAssociatedScenes(scene);
            foreach (var persistentScene in scenes)
                _ = SceneManager.editor.Open(persistentScene, promptSave: false);

        }

        /// <summary>Gets the scenes flagged to open when the specified scene is opened.</summary>
        public static Scene[] GetAssociatedScenes(Scene scene) =>
            SceneManager.assets.allScenes.Where(s =>
            {

                var option = GetPersistentOption(s);

                if (option.option == OpenInEditorOption.AnySceneOpens)
                    return true;
                else if (option.option == OpenInEditorOption.WhenAnyOfTheFollowingScenesOpen)
                    return option.list.Contains(scene.path);
                else if (option.option == OpenInEditorOption.WhenAnySceneOpensExcept)
                    return !option.list.Contains(scene.path);

                return false;

            }).ToArray();

        public static bool IsPersistent(UnityEngine.SceneManagement.Scene scene)
        {

            var asmScene = Scene.Find(scene.path);
            if (!asmScene)
                return false;

            var option = GetPersistentOption(asmScene);
            if (option.option == OpenInEditorOption.AnySceneOpens)
                return true;
            else if (option.option == OpenInEditorOption.WhenAnyOfTheFollowingScenesOpen)
                return SceneUtility.GetAllOpenUnityScenes().Any(s => option.list.Contains(s.path));
            else if (option.option == OpenInEditorOption.WhenAnySceneOpensExcept)
                return !SceneUtility.GetAllOpenUnityScenes().Any(s => option.list.Contains(s.path));

            return false;

        }

    }

}
#endif
