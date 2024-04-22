#if UNITY_EDITOR

using System;
using System.Linq;
using System.Threading.Tasks;
using AdvancedSceneManager.Setup;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Provides methods for working with packages.</summary>
    internal static class PluginUtility
    {

        #region Plugins

#pragma warning disable CS0067 //Unused event
        public static event Action<Plugin> onBeforePluginDisabled;
#pragma warning restore CS0067

        public abstract class Item
        {

            public string title { get; set; }
            public string tooltip { get; set; }
            public bool meetsMinimumUnityVersion { get; set; } = true;

            public Button button { get; set; }
            public ProgressSpinner progress { get; set; }
            public abstract string initialButtonText { get; }

            public abstract void ToggleEnabled();

        }

        public class Plugin : Item
        {

            public string pragma { get; set; }
            public string dependency { get; set; }
            public bool hasDependency => !string.IsNullOrWhiteSpace(dependency);
            public bool isExperiment { get; set; }

            public override string initialButtonText => isEnabled ? "Disable" : "Enable";

            public bool isEnabled => ScriptingDefineUtility.IsSet(pragma);
            public bool isBusy { get; set; }

            public override void ToggleEnabled()
            {
                if (!isEnabled)
                    Install();
                else
                    Uninstall();
            }

#if UNITY_2019
            public void OnInitialize()
            { }
#else
            public async void OnInitialize()
            {


                if (!ScriptingDefineUtility.IsSet(pragma) && await IsDependencyInstalled())
                {
                    ScriptingDefineUtility.Set(pragma);
                    EditorApplication.delayCall += UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation;
                }
                else if (ScriptingDefineUtility.IsSet(pragma) && !await IsDependencyInstalled())
                {
                    ScriptingDefineUtility.Unset(pragma);
                    EditorApplication.delayCall += UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation;
                }

            }
#endif

#if UNITY_2019
            public virtual void Install() =>
                ScriptingDefineUtility.Set(pragma);
#else
            public virtual async void Install()
            {
                if (hasDependency && !await IsDependencyInstalled())
                {
                    UnityEditor.PackageManager.UI.Window.Open("");
                    EditorApplication.delayCall += () =>
                       UnityEditor.PackageManager.UI.Window.Open(dependency);
                }
                else
                    ScriptingDefineUtility.Set(pragma);
            }
#endif

            public virtual void Uninstall()
            {
#if !UNITY_2019
                if (hasDependency)
                    UnityEditor.PackageManager.UI.Window.Open(dependency);
                else
#endif
                    ScriptingDefineUtility.Unset(pragma);
            }

            async Task<bool> IsDependencyInstalled()
            {

                if (!hasDependency)
                    return true;

                var request = await DoRequest(Client.List(offlineMode: true));
                return request.Result.Any(p => p.name == dependency);

            }

            async Task<T> DoRequest<T>(T request) where T : Request
            {
                isBusy = true;
                while (request != null && !request.IsCompleted)
                    await Task.Delay(10);
                isBusy = false;
                return request;
            }

        }

        public class ExperimentalPlugin : Plugin
        { }

        public class Example : Item
        {
            public string uri { get; set; }
            public override string initialButtonText => "View on github";
            public override void ToggleEnabled() => Application.OpenURL(uri);
        }

        public static Item[] items { get; } = new Item[]
        {

            //Auto plugins
            new Plugin()
            {
                dependency = "com.unity.addressables",
                title = "Addressables support",
                pragma = "ASM_PLUGIN_ADDRESSABLES",
                tooltip = "Provides support for addressables in ASM.",
            },
//            new ExperimentalPlugin()
//            {
//#if UNITY_2021_1_OR_NEWER
//                meetsMinimumUnityVersion = true,
//#else
//                meetsMinimumUnityVersion = false,
//#endif
//                dependency = "com.unity.netcode.gameobjects",
//                title = "Netcode support",
//                pragma = "ASM_PLUGIN_NETCODE",
//                tooltip = "Provides support for netcode in ASM.",
//            },

            //Plugins
            new Plugin()
            {
                title = "Lock collections and scenes",
                pragma = "ASM_PLUGIN_LOCKING",
                tooltip = "Provides the ability to lock collections and scenes to prevent them from being modified.\n\nNote this is only applies to ui and will not prevent editing files on disk."
            },

            //Experiments
            new ExperimentalPlugin()
            {
                title = "Cross-scene references",
                pragma = "ASM_PLUGIN_CROSS_SCENE_REFERENCES",
                tooltip = "Provides the ability to create cross-scene references. While certainly useful, it can slow down performance of the editor.\n\nCross-scene references is experimental because it has a tendency to break, we're trying our best to fix any bugs that pops up, but the reality is that this is a hack at best and it may never reach a status where it would not be considered experimental, unless Unity adds or changes APIs that makes this easier to implement."
            },

            //Examples
            new Example()
            {
                title = "Preloading",
                uri = "https://github.com/Lazy-Solutions/example.asm.preloading",
                tooltip = "An example that shows how to setup preloading."
            },
            new Example()
            {
                title = "Level select",
                uri = "https://github.com/Lazy-Solutions/example.asm.level-select",
                tooltip = "An example that shows how to setup a scene streaming level."
            },
            new Example()
            {
                title = "Streaming",
                uri = "https://github.com/Lazy-Solutions/example.asm.streaming",
                tooltip = "An example that shows how to setup a simple level select menu."
            },
            //new Example()
            //{
            //    title = "Netcode",
            //    uri = "https://github.com/Lazy-Solutions/example.asm.netcode",
            //    tooltip  = "An example that shows how to setup a project with netcode."
            //},

        };

        #endregion
        #region UI   

        static void SetupUI()
        {

            SetupDescription();

            foreach (var item in items)
                if (item.meetsMinimumUnityVersion)
                    SettingsTab.instance.Add(header: GetHeader(item), callback: CreateElement(item));

        }

        static string GetHeader(Item item = null)
        {
            if (item is Example)
                return SettingsTab.instance.DefaultHeaders.PluginsAndExamples_Examples;
            else if (item is ExperimentalPlugin)
                return SettingsTab.instance.DefaultHeaders.PluginsAndExamples_Experiments;
            else if (item is Plugin)
                return SettingsTab.instance.DefaultHeaders.PluginsAndExamples_Plugins;
            else
                return SettingsTab.instance.DefaultHeaders.PluginsAndExamples;
        }

        static Label versionLabel;
        static void SetupVersion()
        {

            if (versionLabel is null)
            {
                versionLabel = new Label();
                SettingsTab.instance.AddHeaderContent(versionLabel, SettingsTab.instance.DefaultHeaders.PluginsAndExamples);
            }

            versionLabel.text = "Version: " + ASMInfo.GetVersionInfo().version;
            versionLabel.style.opacity = 0.5f;
            versionLabel.pickingMode = PickingMode.Ignore;
            versionLabel.style.position = Position.Absolute;
            versionLabel.style.right = 0;
            versionLabel.style.height = new Length(100, LengthUnit.Percent);
            versionLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            versionLabel.style.SetMargin(horizontal: 6);

        }

        static void SetupDescription()
        {

            var description = new Label(
#if UNITY_2019
                "Plugins can add new features or support for other packages.\n\n" +
                "If a plugin has a dependency then you'll need to install that\n" +
                "before enabling, please disable plugin before uninstalling\n" +
                "dependency." +
                "\n\n" +
                "If you get compilation errors you may have to enable or disable\n" +
                "scripting defines manually in player settings. Scripting defines\n" +
                "can be found in tooltips."
#else
                "Plugins can add new features or support for other packages.\n\n" +
                "If a plugin has a dependency, then package manager will be opened,\n" +
                "where you can press install on the package. Plugin will be\n" +
                "automatically enabled when package is installed (note that you\n" +
                "may have to press Enable twice to get package manager to select\n" +
                "correct package)." +
                "\n\n" +
                "If you get compilation errors you may have to enable or disable\n" +
                "scripting defines manually in player settings. Scripting defines\n" +
                "can be found in tooltips."
#endif
                );

            description.style.alignSelf = Align.Center;
            description.style.opacity = 0.5f;
            description.style.SetMargin(bottom: 16);
            SettingsTab.instance.Add(header: GetHeader(), callback: description);

        }

        static VisualElement CreateElement(Item item)
        {

            var label = new Label(item.title) { tooltip = item.tooltip };
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.tooltip = item.tooltip + (item is Plugin p ? "\n\nScripting define: " + p.pragma : "");

            var button = new Button(item.ToggleEnabled) { text = item.initialButtonText };
            button.style.width = 100;

            item.button = button;

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;

            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Row;
            element.Add(label);
            element.Add(spacer);
            element.Add(button);
            if (item is Plugin plugin)
            {

                button.style.display = DisplayStyle.None;

                var progressbar = new ProgressSpinner();
                item.progress = progressbar;
                progressbar.style.width = 100;
                element.Add(progressbar);

            }

            return element;

        }

        public class ProgressSpinner : TextElement
        {

            public ProgressSpinner()
            {

                text = "◠";
                style.height = 19;
                style.fontSize = 26;
                style.unityTextAlign = TextAnchor.MiddleCenter;

                RegisterCallback<AttachToPanelEvent>(e =>
                {

                    _ = schedule.Execute(() =>
                    {
                        var rotation = worldTransform.rotation.eulerAngles;
                        rotation.z += 4;
                        transform.rotation = Quaternion.Euler(rotation);
                    }).Every(10);

                });

            }

        }

        #endregion

        static bool isInitialized;
        internal static void Initialize()
        {

            SetupVersion();
            InitializePlugins();
            if (isInitialized)
                return;
            isInitialized = true;
            SetupUI();
            SetupAuto();

        }

        static void SetupAuto()
        {

#if !UNITY_2019

            Events.registeringPackages += Events_registeringPackages;
            Events.registeredPackages += Events_registeredPackages;

            void Events_registeredPackages(PackageRegistrationEventArgs e)
            {
                foreach (var plugin in items.OfType<Plugin>().Where(p => p.hasDependency))
                    plugin.OnInitialize();
            }

            void Events_registeringPackages(PackageRegistrationEventArgs e)
            {
                foreach (var plugin in items.OfType<Plugin>().Where(p => p.hasDependency))
                    plugin.OnInitialize();
            }

#endif

        }

        static void InitializePlugins()
        {

            foreach (var plugin in items.OfType<Plugin>().Where(p => p.hasDependency))
                plugin.OnInitialize();

            EditorApplication.update += Update;

            void Update()
            {
                foreach (var plugin in items.OfType<Plugin>().Where(p => p.button != null))
                {

                    plugin.button.SetEnabled(!plugin.isBusy && !EditorApplication.isCompiling);

                    plugin.button.style.display = plugin.isBusy ? DisplayStyle.None : DisplayStyle.Flex;
                    plugin.progress.style.display = plugin.isBusy ? DisplayStyle.Flex : DisplayStyle.None;

                }
            }

        }

    }

}
#endif
