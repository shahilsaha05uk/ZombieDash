using System.Collections;

namespace AdvancedSceneManager.Callbacks
{

    /// <inheritdoc cref="ISceneClose"/>
    /// <remarks>Scene operation will wait for coroutine callback before continuing.</remarks>
    public interface ISceneCloseAsync : ISceneCallbacks
    {
        /// <inheritdoc cref="ISceneCloseAsync"/>
        IEnumerator OnSceneClose();
    }

}
