using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AdvancedSceneManager.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if INPUTSYSTEM
using AdvancedSceneManager.Utility;
#endif

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        [SerializeField] private Scene m_scenePopupScene;
        [SerializeField] private SceneCollection m_scenePopupCollection;
        [SerializeField] private bool m_scenePopupIsStandalone;

        class ScenePopup : ViewModel, IPopup
        {

            Scene scene;
            ISceneCollection collection;

            public void OnOpen(VisualElement element, object parameter)
            {

                //Call have to be delayed since Profile.current may be null first frame
                EditorApplication.delayCall += () =>
                {

                    if (!IsParameterValid(parameter, out scene, out collection))
                    {
                        _ = window.popups.Close();
                        return;
                    }

                    SetupCollectionOptions();
                    SetupStandaloneOptions();
                    SetupSceneLoaderToggles();
                    SetupSceneOptions();
                    SetupEditorOptions();

                };

            }

            #region Popup 

            bool IsParameterValid(object parameter, out Scene scene, out ISceneCollection collection)
            {

                scene = null;
                collection = null;

                if (parameter is ValueTuple<Scene, ISceneCollection> param)
                {
                    scene = param.Item1;
                    collection = param.Item2;
                }

                if (!scene)
                {
                    scene = window.m_scenePopupScene;
                    if (Profile.current)
                        this.collection = window.m_scenePopupIsStandalone ? Profile.current.standaloneScenes : window.m_scenePopupCollection;
                }

                if (!scene)
                    return false;

                window.m_scenePopupScene = scene;
                window.m_scenePopupCollection = collection is SceneCollection c ? c : null;
                window.m_scenePopupIsStandalone = collection is StandaloneCollection;

                return true;

            }

            async void IPopup.OnClose(VisualElement element)
            {

                window.m_scenePopupScene = null;
                window.m_scenePopupCollection = null;
                window.m_scenePopupIsStandalone = false;

#if ENABLE_INPUT_SYSTEM && INPUTSYSTEM
                SceneBindingUtility.CancelListenForInput();
#endif

                await Task.Delay(250);

                if (collection is SceneCollection c)
                    c.Save();
                else if (collection is StandaloneCollection)
                    Profile.current.Save();

                if (scene)
                    scene.Save();

            }

            #endregion
            #region SceneCollection

            void SetupCollectionOptions()
            {

                if (collection is SceneCollection c)
                {
                    element.Q("group-collection").SetVisible(true);
                    var toggle = element.Q<Toggle>("toggle-dontOpen");
                    toggle.SetValueWithoutNotify(c.AutomaticallyOpenScene(scene));
                    toggle.RegisterValueChangedCallback(e => c.AutomaticallyOpenScene(scene, e.newValue));
                }

            }

            #endregion
            #region Standalone

            void SetupStandaloneOptions()
            {

                if (collection is StandaloneCollection c)
                {

                    element.Q("group-standalone").Bind(new(scene));
                    element.Q("group-standalone").SetVisible(true);
                    SetupBinding(c);
                }

            }

            void SetupBinding(StandaloneCollection c)
            {
                var section = element.Q<TemplateContainer>("SceneBinding");
                section.SetVisible(true);

#if ENABLE_INPUT_SYSTEM && INPUTSYSTEM
                SceneBindingUtility.SetupBindingField(section, scene);
#else
                section.SetEnabled(false);
#endif
            }

            #endregion
            #region Scene

            void SetupSceneOptions()
            {
                element.Q("group-scene").Bind(new SerializedObject(scene));
                SetupSceneLoaderToggles();
                SetupHalfPersistent();
            }

            void SetupSceneLoaderToggles()
            {

                var list = element.Q("group-scene-loader-toggles");
                scene.PropertyChanged += OnPropertyChanged;
                element.RegisterCallback<DetachFromPanelEvent>(e => scene.PropertyChanged -= OnPropertyChanged);

                void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == nameof(scene.sceneLoader))
                        Reload();
                }

                Reload();

                void Reload()
                {

                    list.Clear();

                    foreach (var loader in SceneManager.runtime.GetToggleableSceneLoaders().ToArray())
                    {

                        var button = new Toggle();
                        button.label = loader.sceneToggleText;
                        button.SetValueWithoutNotify(scene.sceneLoader == loader.Key);
                        button.RegisterValueChangedCallback(e =>
                        {
                            if (e.newValue)
                                scene.sceneLoader = loader.Key;
                            else
                                scene.ClearSceneLoader();
                            scene.Save();
                        });

                        list.Add(button);

                    }

                }

            }

            void SetupHalfPersistent()
            {

                SetupToggle(element.Q<RadioButton>("toggle-remain-open"), false);
                SetupToggle(element.Q<RadioButton>("toggle-re-open"), true);

                void SetupToggle(RadioButton toggle, bool invert)
                {
                    toggle.SetValueWithoutNotify(invert ? !scene.keepOpenWhenNewCollectionWouldReopen : scene.keepOpenWhenNewCollectionWouldReopen);
                    toggle.RegisterValueChangedCallback(e => scene.keepOpenWhenNewCollectionWouldReopen = invert ? !e.newValue : e.newValue);
                }

            }

            #endregion
            #region Editor

            void SetupEditorOptions()
            {

#if UNITY_2022_1_OR_NEWER
                element.Q("group-editor").Bind(new(scene));

                var list = element.Q<ListView>("list-auto-open-scenes");
                var enumField = element.Q<UnityEngine.UIElements.EnumField>("enum-auto-open-in-editor");
                list.makeItem = () => new SceneField();

                list.bindItem = (element, i) =>
                {

                    var field = (SceneField)element;

                    if (scene.autoOpenInEditorScenes.ElementAtOrDefault(i) is Scene s && s)
                        field.SetValueWithoutNotify(s);
                    else
                        field.SetValueWithoutNotify(null);

                    field.RegisterValueChangedCallback(e => scene.autoOpenInEditorScenes[i] = e.newValue);

                };

                enumField.RegisterValueChangedCallback(e => UpdateListVisible());

                UpdateListVisible();
                void UpdateListVisible() =>
                    list.SetVisible(scene.autoOpenInEditor == Models.Enums.EditorPersistentOption.WhenAnyOfTheFollowingScenesAreOpened);

#endif

            }

            #endregion

        }

    }

}

