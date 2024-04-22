#pragma warning disable IDE1006 // Naming Styles

using AdvancedSceneManager.Models;
using System.Collections;
using UnityEngine;

namespace AdvancedSceneManager.Callbacks
{

    /// <summary>Base interface for <see cref="ISceneOpen"/>, <see cref="ISceneClose"/>, <see cref="ICollectionOpen"/>, <see cref="ICollectionClose"/>. Does nothing on its own, used by <see cref="CallbackUtility"/>.</summary>
    public interface ISceneManagerCallbackBase
    { }

    /// <summary>Callback for when the scene that a <see cref="MonoBehaviour"/> is contained within is opened.</summary>
    public interface ISceneOpen : ISceneManagerCallbackBase
    {

        /// <inheritdoc cref="ISceneOpen"/>
        IEnumerator OnSceneOpen();

#if !UNITY_2019
        public interface Coroutine : ISceneManagerCallbackBase
        {

            /// <inheritdoc cref="ISceneOpen"/>
            IEnumerator OnSceneOpen();

            public interface EvenWhenDisabled : ISceneManagerCallbackBase
            { IEnumerator OnSceneOpen(); }

        }

        public interface Void : ISceneManagerCallbackBase
        {

            void OnSceneOpen();

            public interface EvenWhenDisabled : ISceneManagerCallbackBase
            { void OnSceneOpen(); }

        }
#endif

    }

    /// <summary>Callback for when the scene that a <see cref="MonoBehaviour"/> is contained within is closed.</summary>
    public interface ISceneClose : ISceneManagerCallbackBase
    {

        /// <inheritdoc cref="ISceneClose"/>
        IEnumerator OnSceneClose();

#if !UNITY_2019
        public interface Coroutine : ISceneManagerCallbackBase
        {

            IEnumerator OnSceneClose();

            public interface EvenWhenDisabled : ISceneManagerCallbackBase
            { IEnumerator OnSceneClose(); }

        }

        public interface Void : ISceneManagerCallbackBase
        {

            void OnSceneClose();

            public interface EvenWhenDisabled : ISceneManagerCallbackBase
            { void OnSceneClose(); }

        }
#endif

    }

    /// <summary>
    /// <para>Callback for when a scene in a collection that a <see cref="MonoBehaviour"/> is contained within is opened.</para>
    /// <para>Called before loading screen is hidden, if one is defined, or else just when collection has opened.</para>
    /// </summary>
    public interface ICollectionOpen : ISceneManagerCallbackBase
    {

        IEnumerator OnCollectionOpen(SceneCollection collection);

#if !UNITY_2019
        public interface Coroutine : ISceneManagerCallbackBase
        {

            IEnumerator OnCollectionOpen(SceneCollection collection);

            public interface EvenWhenDisabled : ISceneManagerCallbackBase
            { IEnumerator OnCollectionOpen(SceneCollection collection); }

        }

        public interface Void : ISceneManagerCallbackBase
        {

            void OnCollectionOpen(SceneCollection collection);

            public interface EvenWhenDisabled : ISceneManagerCallbackBase
            { void OnCollectionOpen(SceneCollection collection); }

        }
#endif

    }

    /// <summary>
    /// <para>Callback for when a scene in a collection that a <see cref="MonoBehaviour"/> is contained within is closed.</para>
    /// <para>Called after loading screen has opened, if one is defined, or else just before collection is closed.</para>
    /// </summary>
    public interface ICollectionClose : ISceneManagerCallbackBase
    {

        IEnumerator OnCollectionClose(SceneCollection collection);

#if !UNITY_2019
        public interface Coroutine : ISceneManagerCallbackBase
        {

            IEnumerator OnCollectionClose(SceneCollection collection);

            public interface EvenWhenDisabled : ISceneManagerCallbackBase
            { IEnumerator OnCollectionClose(SceneCollection collection); }

        }

        public interface Void : ISceneManagerCallbackBase
        {

            void OnCollectionClose(SceneCollection collection);

            public interface EvenWhenDisabled : ISceneManagerCallbackBase
            { void OnCollectionClose(SceneCollection collection); }

        }
#endif

    }

    /// <summary>Callbacks for a <see cref="ScriptableObject"/> that has been set as extra data for a collection.</summary>
    public interface ICollectionExtraData : ICollectionOpen, ICollectionClose
    { }

}
