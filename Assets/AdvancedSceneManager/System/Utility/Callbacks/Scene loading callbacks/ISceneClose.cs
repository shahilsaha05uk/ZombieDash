using UnityEngine;

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>Callback for when the scene that a <see cref="MonoBehaviour"/> is contained within is closed.</summary>
    /// <remarks>See also: <see cref="ISceneCloseAsync"/>.</remarks>
    public interface ISceneClose : ISceneCallbacks
    {
        /// <inheritdoc cref="ISceneClose"/>
        void OnSceneClose();
    }

}
