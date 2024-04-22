using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    [CanEditMultipleObjects]
    [CustomEditor(typeof(SceneAsset))]
    public class SceneAssetEditor : UnityEditor.Editor, IUIToolkitEditor
    {

        public VisualElement rootVisualElement { get; private set; }
        public Rect position { get; set; }

        protected override void OnHeaderGUI()
        { }

        public override VisualElement CreateInspectorGUI()
        {

            var scenes = targets.OfType<SceneAsset>().ToArray();

            rootVisualElement = new VisualElement();
            rootVisualElement.style.marginTop = 22;
            rootVisualElement.style.height = Screen.height;
            rootVisualElement.style.marginRight = -28;

            rootVisualElement.Add(SceneOverviewUtility.CreateSceneOverview(
                editor: this, scenes, profile: Profile.current, popupOffset: new Vector2(16, 17), showAll: true));

            var path = "AdvancedSceneManager/SceneOverview";
            var items = Resources.LoadAll(path);
            var style = items.OfType<StyleSheet>().Where(s => !s.name.Contains("inline")).FirstOrDefault();
            var tree = items.OfType<VisualTreeAsset>().FirstOrDefault();

            if (style && !rootVisualElement.styleSheets.Contains(style))
                rootVisualElement.styleSheets.Add(style);

            return rootVisualElement;

        }

    }

}
