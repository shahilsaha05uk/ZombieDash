#if UNITY_EDITOR
#endif

using AdvancedSceneManager.Core;
using AdvancedSceneManager.Utility;
using UnityEngine;

namespace AdvancedSceneManager.Models
{

    /// <summary>Represents a <see cref="Scene"/> that changes depending on active <see cref="Profile"/>.</summary>
    [CreateAssetMenu(menuName = "Advanced Scene Manager/Profile dependent scene")]
    public class ProfileDependentScene : ProfileDependent<Scene>
    {

        #region Code

        /// <inheritdoc cref="SceneManagerBase.Open"/>
        public SceneOperation<OpenSceneInfo> Open() => DoAction(s => s.Open());

        /// <inheritdoc cref="SceneManagerBase.Close(OpenSceneInfo)"/>
        public SceneOperation Close() => DoAction(s => s.Close());

        /// <inheritdoc cref="UtilitySceneManager.SetActive"/>
        public void SetActiveScene() => DoAction(s => s.SetActiveScene());

        /// <inheritdoc cref="StandaloneManager.OpenSingle"/>
        public SceneOperation<OpenSceneInfo> OpenSingle() => DoAction(s => s.OpenSingle());

        /// <inheritdoc cref="UtilitySceneManager.Reopen"/>
        public SceneOperation<OpenSceneInfo> Reopen() => DoAction(s => s.Reopen());

        /// <inheritdoc cref="UtilitySceneManager.Toggle"/>
        public SceneOperation Toggle() => DoAction(s => s.Toggle());

        /// <inheritdoc cref="UtilitySceneManager.Toggle"/>
        public SceneOperation Toggle(bool enabled) => DoAction(s => s.Toggle(enabled));

        /// <inheritdoc cref="StandaloneManager.Preload(Scene, bool)"/>
        public SceneOperation<PreloadedSceneHelper> Preload() => DoAction(s => s.Preload());

        #endregion
        #region Event

        /// <inheritdoc cref="SceneManagerBase.Open"/>
        public void OpenEvent() => Open();

        /// <inheritdoc cref="StandaloneManager.OpenSingle"/>
        public void OpenSingleEvent() => OpenSingle();

        /// <inheritdoc cref="SceneManagerBase.Reopen"/>
        public void ReopenEvent() => Reopen();

        /// <inheritdoc cref="SceneManagerBase.Toggle"/>
        public void ToggleEvent() => Toggle();

        /// <inheritdoc cref="SceneManagerBase.Toggle"/>
        public void ToggleEvent(bool enabled) => Toggle(enabled);

        /// <inheritdoc cref="SceneManagerBase.Close"/>
        public void CloseEvent() => Close();

        public void OpenWithLoadingScreenEvent(Scene loadingScene) => Open().WithLoadingScreen(loadingScene);

        #endregion

    }

}
