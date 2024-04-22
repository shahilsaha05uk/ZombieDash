using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Utility;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        class ExtraCollectionPopup : ListPopup<SceneCollectionTemplate>
        {

            public override string noItemsText { get; } = "No templates, you can create one using + button.";
            public override string headerText { get; } = "Collection templates";
            public override IEnumerable<SceneCollectionTemplate> items => SceneManager.assets.templates;

            public override void OnCreateGUI(VisualElement container)
            {

                base.OnCreateGUI(container);

                var group = new GroupBox();
                var button = new Button(CreateDynamicCollection) { text = "Create dynamic collection" };
                group.Add(button);
                container.Insert(0, group);

            }

            void CreateDynamicCollection()
            {
                Profile.current.CreateDynamicCollection();
                window.collections.Reload();
            }

            public override async void OnAdd()
            {

                var name = await PickNamePopup.Prompt();
                if (string.IsNullOrEmpty(name))
                    window.popups.Open<ProfilePopup>();
                else
                {
                    SceneCollectionTemplate.CreateTemplate(name);
                    window.popups.Open<ExtraCollectionPopup>();
                }

            }

            public override async void OnSelected(SceneCollectionTemplate template)
            {
                Profile.current.CreateCollection(template);
                await window.popups.Close();
            }

            public override async void OnRename(SceneCollectionTemplate template)
            {

                var name = await PickNamePopup.Prompt(template.title);
                if (!string.IsNullOrWhiteSpace(name))
                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(template), name);
                window.popups.Open<ExtraCollectionPopup>();

            }

            public override async void OnRemove(SceneCollectionTemplate template)
            {

                if (PromptUtility.PromptDelete("template"))
                {

                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(template));

                    if (!items.Where(o => o).Any())
                        await window.popups.Close();
                    else
                        Reload();

                }

            }

        }

    }

}
