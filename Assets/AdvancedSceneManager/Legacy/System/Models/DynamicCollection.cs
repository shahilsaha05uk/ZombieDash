using System;
using System.Collections.Generic;

namespace AdvancedSceneManager.Models
{

    /// <summary>Represents a dynamic scene collection.</summary>
    [Serializable]
    public class DynamicCollection
    {

        /// <summary>The title of this dynamic collection.</summary>
        public string title;

        /// <summary>Gets if this dynamic collection is automatically managed. This means ASM will clear scene list and re-populate it based on a source path.</summary>
        public bool isAuto;

        /// <summary>Gets the scene list.</summary>
        /// <remarks>Scenes are stored using path.</remarks>
        public List<string> scenes = new List<string>();

        internal bool isStandalone => string.IsNullOrWhiteSpace(title);
        internal bool isASM => title?.EndsWith("AdvancedSceneManager/System/Defaults") ?? false;
        internal string m_title => GetTitle();
        internal string m_description => GetDescription();

        string GetTitle() =>
            (isStandalone ? "Standalone" : null) ??
            (isASM ? "Advanced Scene Manager defaults" : null) ??
            (title);

        string GetDescription() =>
            (isStandalone ? "Standalone scenes (and other dynamic collection scenes) are guaranteed to be included build, even if they are not contained in a normal collection." : null) ??
            (isASM ? "These are scenes that ASM provides out-of-the-box as a convinience, these are listed here to make sure they are included in build by default.\n\nIf you aren't using any of these, you may remove this dynamic collection in settings." : null) ??
            null;

        #region Standalone
#if UNITY_EDITOR

        internal void AddEmptySceneField()
        {

            if (!isStandalone)
                return;

            scenes.Add(string.Empty);

        }

        internal void RemoveSceneField(int index)
        {

            if (!isStandalone)
                return;

            scenes.RemoveAt(index);

        }

        internal void SetField(Scene scene, int index)
        {
            if (scenes.Count > index)
                scenes[index] = scene ? scene.path : string.Empty;
        }

#endif
        #endregion

    }

}
