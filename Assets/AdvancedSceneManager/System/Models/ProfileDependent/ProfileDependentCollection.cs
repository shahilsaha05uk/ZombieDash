using AdvancedSceneManager.Core;
using AdvancedSceneManager.Utility;
using UnityEngine;

namespace AdvancedSceneManager.Models.Utility
{

    /// <summary>Represents a <see cref="SceneCollection"/> that changes depending on active <see cref="Profile"/>.</summary>
    [CreateAssetMenu(menuName = "Advanced Scene Manager/Profile dependent collection")]
    public class ProfileDependentCollection : ProfileDependent<SceneCollection>, ISceneCollection.IOpenable,
        SceneCollection.IMethods, SceneCollection.IMethods.IEvent
    {

        public static implicit operator SceneCollection(ProfileDependentCollection instance) =>
            instance.GetModel(out var scene) ? scene : null;

        #region IMethods

        public SceneOperation Open(bool openAll = false) => DoAction(c => c.Open(openAll));
        public SceneOperation OpenAdditive(bool openAll = false) => DoAction(c => c.OpenAdditive(openAll));
        public SceneOperation ToggleOpen(bool? openState = null, bool openAll = false) => DoAction(c => c.ToggleOpen(openState, openAll));
        public SceneOperation Close() => DoAction(c => c.Close());

        #endregion
        #region IMethods.IEvent

        public void _Open(bool openAll = false) =>
           SpamCheck.EventMethods.Execute(() => Open());

        public void _OpenAdditive(bool openAll = false) =>
            SpamCheck.EventMethods.Execute(() => OpenAdditive(openAll));

        public void _ToggleOpen(bool? openState = null) =>
            SpamCheck.EventMethods.Execute(() => ToggleOpen(openState));

        public void _ToggleOpenState() =>
            SpamCheck.EventMethods.Execute(() => ToggleOpen());

        public void _Close() =>
            SpamCheck.EventMethods.Execute(() => Close());

        #endregion

    }

}
