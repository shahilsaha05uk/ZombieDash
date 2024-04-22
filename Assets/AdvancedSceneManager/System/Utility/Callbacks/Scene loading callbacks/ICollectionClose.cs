using AdvancedSceneManager.Models;
using UnityEngine;

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>
    /// <para>Callback for when a scene in a collection that a <see cref="MonoBehaviour"/> is contained within is closed.</para>
    /// <para>Called after loading screen has opened, if one is defined, or else just before collection is closed.</para>
    /// </summary>
    /// <remarks>See also: <see cref="ICollectionCloseAsync"/>.</remarks>
    public interface ICollectionClose : ISceneCallbacks
    {
        /// <inheritdoc cref="ICollectionClose"/>
        void OnCollectionClose(SceneCollection collection);
    }

}
