using System;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEngine.UIElements;
using static AdvancedSceneManager.Editor.Utility.PersistentSceneInEditorUtility;

#if !UNITY_2022_1_OR_NEWER
using UnityEditor.UIElements;
#endif

namespace AdvancedSceneManager.Editor
{

    public partial class OpenInEditorPopup : Popup<OpenInEditorPopup>
    {

        public override string path => "AdvancedSceneManager/Popups/OpenInEditor/Popup";

        public static float height { get; private set; }

        Scene scene;

        OpenInEditorSetting setting;
        public OpenInEditorPopup Refresh(Scene scene, Action onChange = null)
        {

            this.scene = scene;
            setting = GetPersistentOption(scene);
            rootVisualElement.Q<EnumField>("enum").Init(setting.option);
            _ = rootVisualElement.Q<EnumField>("enum").RegisterValueChangedCallback(e => { setting.option = (OpenInEditorOption)e.newValue; OnOptionChanged(); });

            OnOptionChanged(update: false);
            void OnOptionChanged(bool update = true)
            {

                var isList = setting.option == OpenInEditorOption.WhenAnySceneOpensExcept || setting.option == OpenInEditorOption.WhenAnyOfTheFollowingScenesOpen;

                var list = rootVisualElement.Q("list");
                list.EnableInClassList("hidden", !isList);

                if (isList)
                {

                    if (setting.list == null)
                        setting.list = Array.Empty<string>();

                    list.Clear();

                    for (int i = 0; i < setting.list.Length; i++)
                        CreateSceneItem(setting.list[i], i, list, () => OnOptionChanged());

                    var addButton = new Button() { text = "+" };
                    addButton.AddToClassList("Scene-template-header-Remove");
                    addButton.style.alignSelf = Align.FlexEnd;
                    addButton.style.marginRight = 2;
                    addButton.clicked += () => { ArrayUtility.Add(ref setting.list, null); OnOptionChanged(); };
                    list.Add(addButton);

                }
                else
                    list.Clear();

                if (update)
                {
                    PersistentSceneInEditorUtility.Update(scene.assetID, setting);
                    scene.OnPropertyChanged();
                    onChange?.Invoke();
                }

            }

            height = rootVisualElement.worldBound.height;
            return this;

        }

        void CreateSceneItem(string path, int index, VisualElement list, Action onChanged)
        {

            var listScene = SceneManager.assets.allScenes.Find(path);

            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;

            var sceneField = new SceneField();
            _ = sceneField.SetValueWithoutNotify(listScene);
            sceneField.RegisterValueChangedCallback(e => { setting.list[index] = e.newValue ? e.newValue.path : ""; onChanged?.Invoke(); });

            var removeButton = new Button() { text = "-" };
            removeButton.AddToClassList("Scene-template-header-Remove");
            removeButton.clicked += () => { ArrayUtility.RemoveAt(ref setting.list, index); onChanged?.Invoke(); };

            item.Add(sceneField);
            item.Add(removeButton);

            list.Add(item);

        }

        protected override void OnReopen(OpenInEditorPopup newPopup) =>
            newPopup.Refresh(scene);

    }

}
