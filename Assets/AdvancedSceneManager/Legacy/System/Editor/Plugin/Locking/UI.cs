#if ASM_PLUGIN_LOCKING

using AdvancedSceneManager.Editor.Utility;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Plugin.Locking
{

    public static class UI
    {

        /// <summary>Enables or disables buttons in UI.</summary>
        public static bool showButtons
        {
            get => EditorPrefs.GetBool("AdvancedSceneManager.Locking.ShowButtons", true);
            set => EditorPrefs.SetBool("AdvancedSceneManager.Locking.ShowButtons", value);
        }

        internal static void OnLoad()
        {
            SettingsTab.instance.Add(
                new Toggle("Display lock buttons:").
                    Setup(e =>
                    {
                        showButtons = e.newValue;
                        EditorApplication.RepaintHierarchyWindow();
                    },
                    defaultValue: showButtons,
                    tooltip: "Enable or disable lock buttons (does not disable functionality, saved in EditorPrefs)"
                    ),
                header: SettingsTab.instance.DefaultHeaders.Appearance);
        }

    }

}

#endif