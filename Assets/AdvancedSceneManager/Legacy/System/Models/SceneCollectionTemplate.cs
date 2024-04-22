using UnityEngine;

#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
using UnityEditor;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>Represents a template for a <see cref="SceneCollection"/>.</summary>
    public class SceneCollectionTemplate : SceneCollection
    {

        /// <inheritdoc cref="SceneCollection.name"/>
        public new string name => base.name;

        /// <inheritdoc cref="SceneCollection.title"/>
        public new string title
        {
            get => m_title;
            set => m_title = value;
        }

#if UNITY_EDITOR

        /// <summary>Creates a <see cref="SceneCollection"/> from this <see cref="SceneCollectionTemplate"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public SceneCollection CreateCollection(Profile profile) =>
            profile.CreateCollection(
                title,
                initializeBeforeSave: collection => JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(this), collection));

        /// <summary>Applies a <see cref="SceneCollectionTemplate"/> on this <see cref="SceneCollection"/>.</summary>
        /// <remarks>Only available in editor. Not reversible.</remarks>
        public void Apply(SceneCollection collection)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(this), collection);
            collection.MarkAsDirty();
        }

        /// <summary>Creates <see cref="SceneCollectionTemplate"/> from the specified <see cref="SceneCollection"/>, using the currently open folder in the project view.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static SceneCollectionTemplate CreateTemplateInCurrentFolder(SceneCollection collection)
        {
            var template = CreateInstance<SceneCollectionTemplate>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(collection), template);
            ProjectWindowUtil.CreateAsset(template, collection.title + ".asset");
            return template;
        }

        /// <summary>Creates <see cref="SceneCollectionTemplate"/> from the specified <see cref="SceneCollection"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static SceneCollectionTemplate CreateTemplate(string path, SceneCollection collection)
        {

            var template = CreateInstance<SceneCollectionTemplate>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(collection), template);
            EditorFolderUtility.EnsureFolderExists(path);
            AssetDatabase.CreateAsset(template, path);

            return template;

        }

#endif

    }

}
