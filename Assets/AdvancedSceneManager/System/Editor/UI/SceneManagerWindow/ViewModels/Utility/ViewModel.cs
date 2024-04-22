using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        protected abstract class ViewModel
        {

            public SceneManagerWindow window => SceneManagerWindow.window;
            public VisualElement rootVisualElement => SceneManagerWindow.rootVisualElement;
            public VisualElement element { get; set; }

            public virtual void OnCreateGUI(VisualElement element) { }
            public virtual void OnCreateGUI(VisualElement element, object param) { }

            public virtual void OnEnable() { }
            public virtual void OnDisable() { }
            public virtual void OnFocus() { }
            public virtual void OnLostFocus() { }
            public virtual void OnSizeChanged() { }
            public virtual void OnRemoved() { }

            public virtual void ApplyAppearanceSettings(VisualElement element) { }

        }

    }

}
