using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static AdvancedSceneManager.Editor.SceneManagerWindow;
using Selection = AdvancedSceneManager.Editor.SceneManagerWindow.Selection;

namespace AdvancedSceneManager.Editor.Window
{

    public static class TagsTab
    {

        const string tagTemplate = "AdvancedSceneManager/Templates/Tag";

        public static void OnEnable(VisualElement element)
        {
            SceneManagerWindow.window.LoadContent(tagTemplate, element, loadStyle: true);
            PopulateList(element.Q("tag-list"));
        }

        static VisualElement list;
        static void PopulateList(VisualElement list)
        {

            if (list == null)
                list = TagsTab.list;
            if (list != null)
            {

                var elements = new List<(VisualElement header, VisualElement body)>();

                TagsTab.list = list;
                list.Clear();
                if (Profile.current)
                    foreach (var layer in Profile.current.tagDefinitions)
                        if (layer.id != SceneTag.Default.id)
                        {

                            var element = CreateItem(layer, list);
                            if (element == null)
                                continue;

                            var header = element.Q(className: "Tag-template-header");
                            var body = element.Q("Tag-template-content");
                            elements.Add((header, body));

                        }

                RoundedCornerHelper.Add(elements.ToArray());

            }

            RoundedCornerHelper.Update();

        }

        static VisualElement CreateItem(SceneTag tag, VisualElement list)
        {

            var element = Resources.Load<VisualTreeAsset>(tagTemplate).CloneTree();
            list.Add(element);

            var content = element.Q("Tag-template-content");

            #region Expander

            var expander = element.Q<ToolbarToggle>("Tag-template-expander");
            expander.SetValueWithoutNotify(SceneManagerWindow.window.openTagExpanders.GetValue(tag.id));

            element.Q<Label>("Tag-template-header-Label").RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == 0 && !e.ctrlKey)
                    expander.value = !expander.value;
            });

            _ = expander.RegisterValueChangedCallback(b => OnChecked());
            OnChecked();

            void OnChecked()
            {
                expander.text = expander.value ? "▼" : "►";
                _ = SceneManagerWindow.window.openTagExpanders.Set(tag.id, expander.value);
                content.EnableInClassList("hidden", !expander.value);
                element.Q(className: "Tag-template-header").EnableInClassList("expanded", expander.value);
                RoundedCornerHelper.Update();
            }

            #endregion
            #region Title

            var title = element.Q<Label>("Tag-template-header-Label");

            ReloadTitle();
            void ReloadTitle() =>
                title.text = tag.name;

            #endregion
            #region Tag properties

            _ = element.Q<TextField>("Tag-template-Title").Setup(tag, nameof(tag.name), () => { ReloadTitle(); Save(); });

            _ = element.Q<EnumField>("CloseBehavior").Setup(tag, nameof(tag.closeBehavior), Save);
            _ = element.Q<EnumField>("OpenBehavior").Setup(tag, nameof(tag.openBehavior), Save);

            #endregion
            #region Color and label

            //These have been hidden, we may remove them, but we keep them here if someone misses them

            //ReloadColor();
            //void ReloadColor() =>
            //    element.Q(className: "Tag-ColorIndicator").style.backgroundColor = tag.color;

            //element.Q<TextField>("Tag-template-Label").Setup(tag, nameof(tag.label), Save);
            //element.Q<ColorField>("Tag-template-Color").Setup(tag, nameof(tag.color), () => { ReloadColor(); Save(); });

            #endregion

            //Remove tag button
            element.Q<Button>("Tag-template-header-Remove").clicked += () => Remove(tag);

            DragAndDropReorder.RegisterList(list, dragButtonName: "tag-drag-button", itemRootName: "tag-drag-root");
            element.Q("tag-drag-root").AddManipulator(new Selection.Manipulator(tag));
            element.Q("tag-drag-root").AddManipulator(new ContextualMenuManipulator(Menu));

            return element;

        }

        static void Menu(ContextualMenuPopulateEvent e)
        {
            Selection.ClearWhenGUIReturns();
            e.menu.AppendAction("Remove", _ => Remove(Selection.tags.ToArray()));
        }

        static void Save() =>
            SceneManagerWindow.Save(Profile.current);

        static void Remove(params SceneTag[] tags)
        {
            foreach (var tag in tags)
                ArrayUtility.Remove(ref Profile.current.tagDefinitions, tag);
            Selection.Reset();
            Save();
            ReopenTab();
        }

        public static void OnReorderEnd(DragAndDropReorder.DragElement element, int newIndex)
        {

            //We're hiding SceneLayer.None, which is always index 0, so we need to add one here since reorder only uses visual chilren
            var oldIndex = element.index;
            oldIndex += 1;
            newIndex += 1;

            var item = Profile.current.tagDefinitions[oldIndex];
            ArrayUtility.RemoveAt(ref Profile.current.tagDefinitions, oldIndex);
            ArrayUtility.Insert(ref Profile.current.tagDefinitions, newIndex, item);

            SceneManagerWindow.Save(Profile.current);

        }

        #region Footer

        public static FooterItem[] FooterButtons { get; } = new FooterItem[]
        {
            FooterItem.Create().OnRight().Button("Restore default", RestoreTagsToDefault),
            FooterItem.Create().OnRight().Button("New tag", CreateNewTag),
        };

        static void RestoreTagsToDefault()
        {

            if (!EditorUtility.DisplayDialog("Restoring tags...", "This will remove all existing tags, are you sure?", ok: "Cancel", cancel: "Restore"))
            {

                Profile.current.tagDefinitions = new[]
                {
                    SceneTag.Default,
                    SceneTag.Persistent,
                    SceneTag.PersistIfPossible,
                    SceneTag.DoNotOpen,
                };

                Save();

            }

            ReopenTab();

        }

        static void CreateNewTag()
        {
            ArrayUtility.Add(ref Profile.current.tagDefinitions, new SceneTag("New Tag"));
            Save();
            ReopenTab();
        }

        #endregion

    }

}
