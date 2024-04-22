using System.Collections;

namespace AdvancedSceneManager.Callbacks
{

    /// <inheritdoc cref="ISceneOpen"/>
    /// <remarks>Scene operation will wait for coroutine callback before continuing.</remarks>
    public interface ISceneOpenAsync : ISceneCallbacks
    {
        /// <inheritdoc cref="ISceneOpenAsync"/>
        IEnumerator OnSceneOpen();
    }

}
