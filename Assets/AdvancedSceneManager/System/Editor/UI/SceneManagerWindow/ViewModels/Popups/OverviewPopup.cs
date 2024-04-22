using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {
        class OverviewPopup : ViewModel, IPopup
        {

            ScrollView scrollView;
            Foldout importedFoldout;
            Foldout unimportedFoldout;
            public override void OnCreateGUI(VisualElement element)
            {

                scrollView = element.Q<ScrollView>();
                importedFoldout = element.Q<Foldout>("foldout-imported");
                unimportedFoldout = element.Q<Foldout>("foldout-unimported");
                Reload();
                SceneImportUtility.scenesChanged += Reload;

                element.style.paddingTop = 12;
                element.style.paddingBottom = 12;

            }

            void Reload()
            {

                importedFoldout.Clear();
                unimportedFoldout.Clear();

                var importedScenes = SceneManager.assets.scenes.Where(s => s).OrderBy(s => s.name).ToArray();
                var unimportedScenes = SceneImportUtility.unimportedScenes.Select(AssetDatabase.LoadAssetAtPath<SceneAsset>).Where(s => s).OrderBy(s => s.name).ToArray();

                foreach (var scene in importedScenes)
                    importedFoldout.Add(SceneField(scene));

                foreach (var scene in unimportedScenes)
                    unimportedFoldout.Add(SceneField(scene));

                unimportedFoldout.Q<Toggle>().SetVisible(unimportedScenes.Length != 0);
                importedFoldout.Q<Toggle>().SetVisible(unimportedScenes.Length != 0);

            }

            VisualElement SceneField(Scene scene)
            {
                var element = new SceneField();
                element.style.marginBottom = 8;
                element.value = scene;
                element.Q(className: "unity-object-field__input").SetEnabled(false);
                return element;
            }

            VisualElement SceneField(SceneAsset scene)
            {

                var element = new ObjectField();
                element.value = scene;
                element.Q(className: "unity-object-field__input").SetEnabled(false);

                var button = new Button(() => SceneImportUtility.Import(AssetDatabase.GetAssetPath(scene))) { text = "Import..." };
                button.style.width = new StyleLength(StyleKeyword.Auto);
                element.Insert(0, button);
                element.style.marginBottom = 8;

                return element;

            }

        }

    }

}
