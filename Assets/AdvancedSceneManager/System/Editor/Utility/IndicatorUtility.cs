using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    [InitializeOnLoad]
    static class IndicatorUtility
    {

        static IndicatorUtility() =>
            SceneManager.OnInitialized(() =>
                HierarchyGUIUtility.AddSceneGUI(s =>
                {

                    if (FallbackSceneUtility.IsFallbackScene(s))
                        DefaultSceneIndicator();
                    else if (s.ASMScene(out var scene))
                    {

                        AddressableIndicator(scene);
                        NetcodeIndicator(scene);
                        CollectionIndicator(scene);
                        PersistentIndicator(scene);
                        BindingIndicator(scene);
                        UntrackedIndicator(scene);
                        LockIndicator(scene);

                    }
                    else
                    {
                        UnimportedIndicator(s);
                        TestIndicator(s);
                    }

                }));

        #region Fallback scene

        static void DefaultSceneIndicator()
        {
            Label("Fallback scene", "This is the 'fallback scene', it makes sure scene management works well");
        }

        #endregion
        #region ASM scene

        static void CollectionIndicator(Scene scene)
        {
            if (SceneManager.openCollection && SceneManager.openCollection.Contains(scene))
                Label(SceneManager.openCollection.title, "This scene was opened by a collection");
        }

        static void PersistentIndicator(Scene scene)
        {
            if (Application.isPlaying && scene.isPersistent)
                Label("Persistent", "This scene is persistent, it will not be closed when collections close");
        }

        static void BindingIndicator(Scene scene)
        {
#if ENABLE_INPUT_SYSTEM && INPUTSYSTEM
            if (SceneBindingUtility.WasOpenedByBinding(scene))
                Label("Binding", "This scene was opened by a binding");
#endif
        }

        static void UntrackedIndicator(Scene scene)
        {
            if (!SceneManager.runtime.IsTracked(scene))
                Label("Untracked", "This scene is untracked, something went wrong somewhere.");
        }

        static void NetcodeIndicator(Scene scene)
        {
#if NETCODE
            if (SceneManager.settings.user.displaySyncedIndicator && scene.isSynced)
                Label("Synced", "This scene was synced by netcode.");
#endif
        }

        static void AddressableIndicator(Scene scene)
        {
#if ADDRESSABLES
            if (Application.isPlaying && scene.isAddressable)
                Label("Addressable", "This scene is addressible.");
#endif
        }

        static void LockIndicator(Scene scene)
        {

            if (SceneManager.settings.project.allowSceneLocking && GUILayout.Button(GetContent(scene), EditorStyles.iconButton))
                EditorApplication.delayCall += () =>
                {
                    LockUtility.Toggle(scene, true);
                    HierarchyGUIUtility.Repaint();
                };

        }

        static Texture lockedIcon;
        static Texture unlockedIcon;
        static GUIContent lockContent;
        static GUIContent unlockContent;
        static GUIContent modifiedContent;
        static GUIContent GetContent(Scene scene)
        {

            lockedIcon ??= EditorGUIUtility.IconContent("Locked@2x").image;
            unlockedIcon ??= EditorGUIUtility.IconContent("Unlocked@2x").image;
            modifiedContent ??= new(EditorGUIUtility.IconContent("d_P4_LockedLocal").image, "Locked:\nThis scene has been modified, you will be asked to save as new scene, or discard.\nClick to unlock.");

            lockContent ??= new(unlockedIcon, "Lock");
            unlockContent ??= new(lockedIcon, "Unlock");

            if (!scene.isLocked)
                return lockContent;
            else if (scene.internalScene?.isDirty ?? false)
                return modifiedContent;
            else if (!string.IsNullOrWhiteSpace(scene.lockMessage))
                return new(lockedIcon, "Unlock:\n" + scene.lockMessage);
            else
                return unlockContent;

        }

        #endregion
        #region Unity scene

        static void UnimportedIndicator(UnityEngine.SceneManagement.Scene scene)
        {

            if (SceneManager.runtime.dontDestroyOnLoad && SceneManager.runtime.dontDestroyOnLoad.internalScene?.path == scene.path)
                return;

            if (!SceneImportUtility.StringExtensions.IsValidSceneToImport(scene.path))
                return;

            Label("Unimported", "This scene is not imported.");

        }

        static void TestIndicator(UnityEngine.SceneManagement.Scene scene)
        {

            if (SceneImportUtility.StringExtensions.IsTestScene(scene.path))
                Label("TestRunner", "This scene makes sure the test runner, runs.");

        }

        #endregion

        static void Label(string text, string tooltip) =>
            GUILayout.Label(new GUIContent(text, tooltip), HierarchyGUIUtility.defaultStyle, GUILayout.ExpandWidth(false));

    }

}
