using UnityEngine;

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>Callbacks for a <see cref="ScriptableObject"/> that has been set as extra data for a collection.</summary>
    /// <remarks>See also: <see cref="ICollectionExtraDataCallbacksAsync"/>.</remarks>
    public interface ICollectionExtraDataCallbacks : ICollectionOpenAsync, ICollectionCloseAsync
    { }

}
