using AdvancedSceneManager.Core;
using UnityEngine;

namespace AdvancedSceneManager.Models
{

    /// <summary>Represents a <see cref="SceneCollection"/> that changes depending on active <see cref="Profile"/>.</summary>
    [CreateAssetMenu(menuName = "Advanced Scene Manager/Profile dependent collection")]
    public class ProfileDependentCollection : ProfileDependent<SceneCollection>
    {

        #region Code

        /// <inheritdoc cref="CollectionManager.Open"/>
        public SceneOperation Open() => DoAction(c => c.Open());

        /// <inheritdoc cref="SceneCollection.OpenOrReopen"/>
        public SceneOperation OpenOrReopen() => DoAction(c => c.OpenOrReopen());

        /// <inheritdoc cref="CollectionManager.Toggle"/>
        public SceneOperation Toggle() => DoAction(s => SceneManager.collection.Toggle(s));

        /// <inheritdoc cref="CollectionManager.Toggle"/>
        public SceneOperation Toggle(bool enabled) => DoAction(c => c.Toggle(enabled));

        /// <inheritdoc cref="CollectionManager.Reopen"/>
        public SceneOperation Reopen() => DoAction(c => c.Reopen());

        /// <inheritdoc cref="CollectionManager.Close"/>
        public SceneOperation Close() => DoAction(c => Close());

        /// <inheritdoc cref="CollectionManager.IsOpen"/>
        public bool IsOpen() => DoAction(s => SceneManager.collection.IsOpen(s));

        #endregion
        #region Event

        /// <inheritdoc cref="CollectionManager.Open"/>
        public void OpenEvent() => Open();

        /// <inheritdoc cref="CollectionManager.Toggle"/>
        public void ToggleEvent() => Toggle();

        /// <inheritdoc cref="CollectionManager.Toggle"/>
        public void ToggleEvent(bool enabled) => Toggle(enabled);

        /// <inheritdoc cref="CollectionManager.Reopen"/>
        public void ReopenEvent() => Reopen();

        /// <inheritdoc cref="CollectionManager.Close"/>
        public void CloseEvent() => Close();

        #endregion

    }

}
