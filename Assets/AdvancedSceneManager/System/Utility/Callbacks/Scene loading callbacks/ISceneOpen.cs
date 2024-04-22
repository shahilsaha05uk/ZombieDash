using UnityEngine;

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>Callback for when the scene that a <see cref="MonoBehaviour"/> is contained within is opened.</summary>
    /// <remarks>See also: <see cref="ISceneOpenAsync"/>.</remarks>
    public interface ISceneOpen : ISceneCallbacks
    {
        /// <inheritdoc cref="ISceneOpen"/>
        void OnSceneOpen();
    }

}
