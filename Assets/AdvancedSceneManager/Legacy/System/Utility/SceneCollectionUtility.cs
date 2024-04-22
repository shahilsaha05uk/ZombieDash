using System.Linq;
using AdvancedSceneManager.Models;

#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
#endif

namespace AdvancedSceneManager.Utility
{

    /// <summary>An utility class to perform actions on collections.</summary>
    public static class SceneCollectionUtility
    {

        #region Create / remove

#if UNITY_EDITOR

        /// <summary>Creates a <see cref="SceneCollection"/>.</summary>
        /// <param name="title">The name of the collection.</param>
        /// <param name="profile">The profile to add this collection to. Defaults to <see cref="Profile.current"/>.</param>
        public static SceneCollection Create(string title, Profile profile = null)
        {

            if (!profile)
                profile = Profile.current;

            if (!profile)
                return null;

            return profile.CreateCollection(title);

        }

        /// <summary>Removes a <see cref="SceneCollection"/>.</summary>
        public static void Remove(SceneCollection collection) =>
            AssetUtility.Remove(collection);

        /// <summary>Removes all null scenes in the collection.</summary>
        public static void RemoveNullScenes(SceneCollection collection)
        {
            collection.m_scenes = collection.m_scenes.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
            collection.Save();
        }

#endif

        #endregion

    }

}
