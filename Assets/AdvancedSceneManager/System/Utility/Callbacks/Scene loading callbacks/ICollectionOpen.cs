using AdvancedSceneManager.Models;
using UnityEngine;

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>
    /// <para>Callback for when a scene in a collection that a <see cref="MonoBehaviour"/> is contained within is opened.</para>
    /// <para>Called before loading screen is hidden, if one is defined, or else just when collection has opened.</para>
    /// </summary>
    /// <remarks>See also: <see cref="ICollectionOpenAsync"/>.</remarks>
    public interface ICollectionOpen : ISceneCallbacks
    {
        /// <inheritdoc cref="ICollectionOpen"/>
        void OnCollectionOpen(SceneCollection collection);
    }

}
