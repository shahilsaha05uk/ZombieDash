using System.Collections;
using AdvancedSceneManager.Models;

namespace AdvancedSceneManager.Callbacks
{

    /// <inheritdoc cref="ICollectionOpen"/>
    /// <remarks>Scene operation will wait for coroutine callback before continuing.</remarks>
    public interface ICollectionOpenAsync : ISceneCallbacks
    {
        /// <inheritdoc cref="ICollectionOpenAsync"/>
        IEnumerator OnCollectionOpen(SceneCollection collection);
    }

}
