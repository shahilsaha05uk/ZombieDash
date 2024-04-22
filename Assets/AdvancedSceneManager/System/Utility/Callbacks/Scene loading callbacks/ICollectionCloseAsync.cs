using System.Collections;
using AdvancedSceneManager.Models;

namespace AdvancedSceneManager.Callbacks
{

    /// <inheritdoc cref="ICollectionClose"/>
    /// <remarks>Scene operation will wait for coroutine callback before continuing.</remarks>
    public interface ICollectionCloseAsync : ISceneCallbacks
    {
        /// <inheritdoc cref="ICollectionClose"/>
        IEnumerator OnCollectionClose(SceneCollection collection);
    }

}
