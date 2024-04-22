using System;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using UnityEditor;
using UnityEngine.UIElements;

#if !UNITY_2022_1_OR_NEWER
using UnityEditor.UIElements;
#endif

namespace AdvancedSceneManager.Editor
{

    public class EditCollectionPopup : Popup<EditCollectionPopup>
    {

        public override string path => "AdvancedSceneManager/Popups/EditCollection/Popup";

        SceneCollection collection;
        Action<string> onTitlePreview;
        Action onStartChanged;

        public void Refresh(SceneCollection collection, Action<string> onTitlePreview = null, Action onStartChanged = null)
        {

            if (!Profile.current)
                return;

            this.collection = collection;
            this.onTitlePreview = onTitlePreview;
            this.onStartChanged = onStartChanged;

            rootVisualElement.SetLocked(AssetDatabase.GetAssetPath(collection));
            rootVisualElement.style.width = 350;

            Title();
            ExtraData();
            ActiveScene();
            UnloadUnusedAssets();
            StartupOption();
            LoadingPriority();
            LoadingScreen();

        }

        void Title()
        {

            if (rootVisualElement.Q<TextField>("Collection-title") is TextField titleField)
            {

                titleField.SetValueWithoutNotify(collection.title);

                _ = titleField.UnregisterValueChangedCallback(OnValueChanged);
                titleField.UnregisterCallback<FocusOutEvent>(OnFocusOut);
                titleField.UnregisterCallback<KeyDownEvent>(OnKeyDown);

                _ = titleField.RegisterValueChangedCallback(OnValueChanged);
                titleField.RegisterCallback<FocusOutEvent>(OnFocusOut);
                titleField.RegisterCallback<KeyDownEvent>(OnKeyDown);

                void OnValueChanged(ChangeEvent<string> e)
                {
                    var sd = titleField.text;
                    titleField.Q("unity-text-input").EnableInClassList("invalidInput", string.IsNullOrWhiteSpace(titleField.text));
                    onTitlePreview?.Invoke(titleField.text);
                }

                void OnFocusOut(FocusOutEvent e) => DoRename();

                void OnKeyDown(KeyDownEvent e)
                {
                    if (e.keyCode == UnityEngine.KeyCode.Return || e.keyCode == UnityEngine.KeyCode.KeypadEnter)
                        DoRename();
                }

                void DoRename()
                {

                    if (string.IsNullOrWhiteSpace(titleField.text))
                        return;

                    SceneManagerWindow.preventReload = true;
                    AssetUtility.Rename(collection, titleField.text);
                    SceneManagerWindow.preventReload = false;

                }

            }

        }

        void ExtraData() =>
            _ = rootVisualElement.Q<ObjectField>("Collection-Extra-Data").Setup(collection, nameof(SceneCollection.userData));

        void ActiveScene()
        {

            var activeScene = rootVisualElement.Q<SceneField>("Collection-activeSceneEnum");
            activeScene.labelFilter = collection.label;
            activeScene.RegisterValueChangedCallback(e => { collection.activeScene = e.newValue; SceneManagerWindow.Save(collection); RefreshActiveScene(); });

            activeScene.OnSceneOpen += Close;

            RefreshActiveScene();
            void RefreshActiveScene() =>
                activeScene.SetValueWithoutNotify(collection.activeScene);

        }

        void UnloadUnusedAssets() =>
            _ = rootVisualElement.Q<Toggle>("Collection-UnloadUnusedAssets").Setup(collection, nameof(SceneCollection.unloadUnusedAssets));

        void StartupOption() =>
            _ = rootVisualElement.Q<EnumField>("Collection-StartupOption").Setup(collection, nameof(collection.startupOption), () => onStartChanged?.Invoke());

        void LoadingPriority() => _ =
            rootVisualElement.Q<EnumField>("Collection-loadingPriority").
                SetEnabledExt(Profile.current.enableChangingBackgroundLoadingPriority).
                Setup(collection, nameof(collection.loadingPriority), tooltip: "The thread priority to use for the loading thread when opening this collection.\n\nHigher equals faster loading, but more processing time used, and will as such produce lag ingame.\n\nSo using high during loading screen, and low during background loading gameplay, is recommended.\n\nAuto will attempt to automatically decide.");

        void LoadingScreen()
        {

            _ = rootVisualElement.Q<EnumField>("Collection-loadingScreenEnum").Setup(collection, nameof(collection.loadingScreenUsage), () => { UpdateLoadingScreen(collection); SetPosition(); });
            UpdateLoadingScreen(collection, setup: true);

            void UpdateLoadingScreen(SceneCollection collection, bool setup = false)
            {

                var loadingSceneField = rootVisualElement.Q<SceneField>("Collection-loadingScreen");
                loadingSceneField.labelFilter = "ASM:LoadingScreen";

                loadingSceneField.visible = collection.loadingScreenUsage == LoadingScreenUsage.Override;
                loadingSceneField.EnableInClassList("hidden", !loadingSceneField.visible);

                if (setup)
                {
                    loadingSceneField.OnSceneOpen += Close;
                    _ = loadingSceneField.Setup(collection, nameof(collection.loadingScreen));
                }

            }


        }

        protected override void OnReopen(EditCollectionPopup newPopup) =>
            newPopup.Refresh(collection, onTitlePreview, onStartChanged);

    }

}
