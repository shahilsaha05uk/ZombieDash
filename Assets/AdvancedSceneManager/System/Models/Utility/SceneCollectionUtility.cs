using System;
using System.Linq;
using AdvancedSceneManager.Utility;
using UnityEngine;

#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>Provides utility methods for working with <see cref="SceneCollection"/>.</summary>
    public static class SceneCollectionExtensions
    {

        /// <summary>Saves the associated <see cref="ScriptableObject"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        static void Save<T>(this T collection) where T : ISceneCollection
        {
            if (collection is ScriptableObject so)
                so.Save();
            else
                foreach (var profile in SceneManager.assets.profiles)
                    profile.Save();
        }

        /// <summary>Finds the index of <paramref name="scene"/>.</summary>
        /// <remarks>Returns -1 if it does not exist.</remarks>
        public static int IndexOf<T>(this T collection, Scene scene) where T : ISceneCollection =>
            Array.IndexOf(collection.scenes.ToArray(), scene);

        #region ISceneCollection.IEditable

#if UNITY_EDITOR

        /// <summary>Adds an empty scene field to this <see cref="SceneCollection"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void AddEmptyScene<T>(this T collection) where T : ISceneCollection, ISceneCollection.IEditable
        {
            collection.sceneList.Add(null);
            collection.Save();
            collection.OnPropertyChanged(nameof(collection.scenes));
        }

        /// <summary>Adds a scene to this <see cref="SceneCollection"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void Add<T>(this T collection, params Scene[] scenes) where T : ISceneCollection, ISceneCollection.IEditable
        {

            var didAdd = false;
            foreach (var scene in scenes)
                if (!collection.Contains(scene))
                {
                    collection.sceneList.Add(scene);
                    didAdd = true;
                }

            if (didAdd)
            {
                collection.Save();
                BuildUtility.UpdateSceneList();
                collection.OnPropertyChanged(nameof(collection.scenes));
            }

        }

        /// <summary>Replaces a scene at the specified index.</summary>
        /// <remarks>Only available in editor.</remarks>
        /// <returns><see langword="true"/> if replace was successful.</returns>
        public static bool Replace<T>(this T collection, int index, Scene scene) where T : ISceneCollection, ISceneCollection.IEditable
        {

            if (index > collection.count - 1)
                return false;

            var oldIndex = collection.sceneList.IndexOf(scene);
            if (oldIndex != -1)
                collection.sceneList[oldIndex] = null;

            collection.sceneList[index] = scene;
            collection.Save();
            BuildUtility.UpdateSceneList();

            return true;

        }

        public static void Insert<T>(this T collection, int index, Scene scene) where T : ISceneCollection, ISceneCollection.IEditable
        {

            index = Math.Clamp(index, 0, collection.count);
            collection.sceneList.Insert(index, scene);
            collection.Save();
            BuildUtility.UpdateSceneList();
            collection.OnPropertyChanged(nameof(collection.scenes));

        }

        /// <summary>Removes a scene from this <see cref="SceneCollection"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void Remove<T>(this T collection, Scene scene) where T : ISceneCollection, ISceneCollection.IEditable
        {
            if (collection.sceneList.Remove(scene))
            {
                collection.Save();
                BuildUtility.UpdateSceneList();
                collection.OnPropertyChanged(nameof(collection.scenes));
            }
        }

        /// <summary>Removes a scene at the specified index from this <see cref="SceneCollection"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void RemoveAt<T>(this T collection, int index) where T : ISceneCollection, ISceneCollection.IEditable
        {
            collection.sceneList.RemoveAt(index);
            collection.Save();
            BuildUtility.UpdateSceneList();
            collection.OnPropertyChanged(nameof(collection.scenes));
        }

        /// <summary>Moves a scene field to a new index.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void Move<T>(this T collection, int oldIndex, int newIndex) where T : ISceneCollection, ISceneCollection.IEditable
        {

            var item = collection.sceneList[oldIndex];
            collection.sceneList.RemoveAt(oldIndex);

            collection.sceneList.Insert(newIndex, item);
            collection.Save();
            collection.OnPropertyChanged(nameof(collection.scenes));

        }

#endif

        #endregion

    }

}
