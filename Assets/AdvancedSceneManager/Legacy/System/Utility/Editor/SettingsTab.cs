#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Provides the ability to add settings to advanced scene manager window settings.</summary>
    /// <remarks>Only available in editor.</remarks>
    public class SettingsTab
    {

        public static SettingsTab instance { get; } = new SettingsTab();

        public SettingsTab()
        {
            order = new List<string>()
            {
                DefaultHeaders.Profile,
                DefaultHeaders.Options,
                DefaultHeaders.Options_Profile,
                DefaultHeaders.Options_Project,
                DefaultHeaders.Options_SpamChecking,
                DefaultHeaders.Options_Local,
                DefaultHeaders.Options_Log,
                DefaultHeaders.Options_InGameToolbar,
                DefaultHeaders.Options_Advanced,
                DefaultHeaders.Appearance,
                DefaultHeaders.Appearance_Hierarchy,
                DefaultHeaders.Appearance_ScenesTab,
                DefaultHeaders.Appearance_WindowHeader,
                DefaultHeaders.Appearance_CollectionHeader,
                DefaultHeaders.Assets,
                DefaultHeaders.DynamicCollections,
                DefaultHeaders.PluginsAndExamples,
                DefaultHeaders.PluginsAndExamples_Plugins,
                DefaultHeaders.PluginsAndExamples_Experiments,
                DefaultHeaders.PluginsAndExamples_Examples,
            };
        }

        #region Headers

        public class _DefaultHeaders
        {
            public string Appearance { get; } = "Appearance";
            public string Appearance_ScenesTab { get; } = "Appearance_Scenes Tab";
            public string Appearance_WindowHeader { get; } = "Appearance_Window Header";
            public string Appearance_CollectionHeader { get; } = "Appearance_Collection Header";
            public string Appearance_Hierarchy { get; } = "Appearance_Hierarchy";
            public string Options { get; } = "Options";
            public string Options_Project { get; } = "Options_Project";
            public string Options_Profile { get; } = "Options_Profile";
            public string Options_Local { get; } = "Options_Local";
            public string Options_InGameToolbar { get; } = "Options_In-game Toolbar";
            public string Options_Log { get; } = "Options_Log";
            public string Options_SpamChecking { get; } = "Options_Spam Checking";
            public string Options_Advanced { get; } = "Options_Advanced";
            public string PluginsAndExamples { get; } = "Plugins and examples";
            public string PluginsAndExamples_Plugins { get; } = "Plugins and examples_Plugins";
            public string PluginsAndExamples_Experiments { get; } = "Plugins and examples_Experimental";
            public string PluginsAndExamples_Examples { get; } = "Plugins and examples_Examples";
            public string Assets { get; } = "Assets";
            public string DynamicCollections { get; } = "Dynamic Collections";
            public string Profile { get; } = "Profile";
        }

        readonly List<string> order;

        public void SetHeaderOrder(string header, int order)
        {
            var i = this.order.IndexOf(header);
            this.order.Insert(order, header);
            if (i != 1)
                this.order.RemoveAt(i);
        }

        public int GetHeaderOrder(string header)
        {

            if (!order.Contains(header))
                order.Add(header);

            return order.IndexOf(header);

        }

        public void AddHeaderContent(VisualElement callback, string header)
        {

            if (!headerContent.ContainsKey(header))
                headerContent.Add(header, new List<VisualElement>());

            if (!headerContent[header].Contains(callback))
                headerContent[header].Add(callback);

        }

        public void RemoveHeaderContent(VisualElement callback)
        {
            foreach (var key in headerContent.Keys)
                _ = headerContent[key].Remove(callback);
        }

        public _DefaultHeaders DefaultHeaders { get; } = new _DefaultHeaders();
        internal readonly Dictionary<string, List<VisualElement>> headerContent = new Dictionary<string, List<VisualElement>>();

        #endregion
        #region Content

        internal readonly Dictionary<string, List<VisualElement>> settings = new Dictionary<string, List<VisualElement>>();

        internal void Insert(VisualElement callback, string header, int index)
        {

            if (header is null)
                return;

            if (!settings.ContainsKey(header))
                settings.Add(header, new List<VisualElement>());

            if (!settings[header].Contains(callback))
                settings[header].Insert(index, callback);

        }

        /// <summary>Add field to settings tab.</summary>
        /// <param name="callback">The callback that is called when your field is to be constructed. An element of type <see cref="VisualElement"/> is expected to be returned.</param>
        /// <param name="header">The header to place the field under, see also <see cref="DefaultHeaders"/>, for the default headers.</param>
        public void Add(VisualElement callback, string header)
        {

            if (header is null)
                return;

            if (!settings.ContainsKey(header))
                settings.Add(header, new List<VisualElement>());

            if (!settings[header].Contains(callback))
                settings[header].Add(callback);

        }

        internal void Spacer(string header, int index) =>
            Insert(null, header, index);

        public void Spacer(string header) =>
            Add(null, header);

        /// <summary>Removes the setting field.</summary>
        public void Remove(VisualElement callback)
        {
            foreach (var key in settings.Keys)
                _ = settings[key].Remove(callback);
        }

        #endregion

    }

}
#endif
