using UnityEngine;

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>Callbacks for a <see cref="ScriptableObject"/> that has been set as extra data for a collection.</summary>
    /// <remarks>Scene operation will wait for coroutine callback before continuing.</remarks>
    public interface ICollectionExtraDataCallbacksAsync : ICollectionOpenAsync, ICollectionCloseAsync
    { }

}
