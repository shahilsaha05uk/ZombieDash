#if ASM_PLUGIN_LOCKING

using AdvancedSceneManager.Editor;
using AdvancedSceneManager.Editor.Window;
using AdvancedSceneManager.Models;
using Lazy.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Plugin.Locking
{

    /// <summary>
    /// <para>Locks collections from editing.</para>
    /// <para>This class is responsible for adding lock buttons to collection fields in <see cref="SceneManagerWindow"/>. Actual disabling of ui elements is done in <see cref="Editor.VisualElementExtensions"/> and uses predefined class 'lockable' in uxml files to determine what should be disabled and what shouldn't.</para>
    /// </summary>
    static class CollectionLock
    {

        public static void OnLoad() =>
            ScenesTab.AddExtraButton(GetCollectionLockButton, position: 99, isLockable: false);

        static VisualElement GetCollectionLockButton(SceneCollection collection)
        {

            if (!UI.showButtons && !LockUtility.IsLocked(collection))
                return null;

            var button = new Button();
            button.AddToClassList("Collection-template-header-Settings");
            button.styleSheets.Add(Resources.Load<StyleSheet>("AdvancedSceneManager/Plugin/Locking/LockButton"));
            button.style.unityFont = new StyleFont(Resources.Load<Font>("Fonts/Inter-Regular"));

            button.clicked += () =>
            {

                if (LockUtility.IsLocked(collection))
                    _ = LockUtility.PromptUnlock(collection);
                else
                    _ = LockUtility.PromptLock(collection);

                EditorUtility.SetDirty(collection);

                CoroutineUtility.Run(SceneManagerWindow.Reload, nextFrame: true);

            };

            ReloadLockButton();
            void ReloadLockButton()
            {
                button.AddToClassList(LockUtility.IsLocked(collection) ? "locked" : "unlocked");
                button.AddToClassList(SceneManagerWindow.IsDarkMode ? "dark" : "light");
                button.tooltip = LockUtility.GetTooltipString(collection);
            }

            return button;

        }

    }

}

#endif
