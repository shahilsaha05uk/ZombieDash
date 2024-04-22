using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Models
{

    /// <summary>A base class for <see cref="Profile"/>, <see cref="SceneCollection"/> and <see cref="Scene"/>.</summary>
    public abstract class ASMModel : ScriptableObject, INotifyPropertyChanged
    {

        [SerializeField] internal string m_id = Path.GetRandomFileName();

        /// <summary>Gets the id of this <see cref="ASMModel"/>.</summary>
        public string id => m_id;

        #region ScriptableObject

        /// <summary>Saves the scriptable object after modifying.</summary>
        /// <remarks>Only available in editor.</remarks>
        public virtual void Save()
        {
#if UNITY_EDITOR
            if (EditorApplication.isUpdating)
                EditorApplication.delayCall += Save;
            else
                ScriptableObjectUtility.Save(this);
#endif
        }

        /// <summary>Mark scriptable object as dirty after modifying.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void MarkAsDirty()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        #endregion
        #region INotifyPropertyChanged

        protected virtual void OnValidate()
        {
            OnPropertyChanged();
#if UNITY_EDITOR
            Editor.Utility.SceneImportUtility.Notify();
#endif
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new(propertyName));

        #endregion
        #region Find

        /// <summary>Gets if <paramref name="q"/> matches <see cref="name"/>.</summary>
        public virtual bool IsMatch(string q) =>
            !string.IsNullOrEmpty(q) && (IsNameMatch(q) || IsIDMatch(q));

        protected bool IsNameMatch(string q) =>
            name == q;

        protected bool IsIDMatch(string q) =>
             q == id;

        #endregion
        #region Name

        //Don't allow renaming from UnityEvent
        /// <inheritdoc cref="UnityEngine.Object.name"/>
        public new string name
        {
            get => this ? base.name : "(null)";
            protected set => Rename(value);
        }

        internal virtual void OnNameChanged()
        { }

        internal virtual void Rename(string newName)
        {
#if UNITY_EDITOR

            if (name == newName)
                return;

            base.name = newName;
            Save();

#endif
        }

        #endregion
        #region ToString

        /// <summary>Gets a text summarization of this model.</summary>
        public override string ToString() =>
            ToString(0);

        /// <inheritdoc cref="ToString()"/>
        /// <param name="indent">The indentation level, used for nested calls.</param>
        public virtual string ToString(int indent) =>
            $"{new string(' ', indent * 2)}";

        #endregion
        #region Create

        /// <summary>Creates a profile. Throws if name is invalid.</summary>
        protected static T CreateInternal<T>(string name) where T : ASMModel
        {

#if UNITY_EDITOR

            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "Name cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(nameof(name), "Name cannot be whitespace.");

            if (Path.GetInvalidFileNameChars().Any(name.Contains))
                throw new ArgumentException(nameof(name), "Name cannot contain invalid path chars.");

            var id = Path.GetRandomFileName();

            if (AssetDatabase.IsValidFolder(Assets.GetFolder<Profile>(id)))
                throw new InvalidOperationException("The generated id already exists.");

            //Windows / .net does not have an issue with paths over 260 chars anymore,
            //but unity still does, and it does not handle it gracefully, so let's have a check for that too
            //No clue how to make this cross-platform since we cannot even get the value on windows, so lets just hardcode it for now
            //This should be removed in the future when unity does handle it
            if (Path.GetFullPath(Assets.GetPath<Profile>(id, name)).Length > 260)
                throw new PathTooLongException("Path cannot exceed 260 characters in length.");

            var model = CreateInstance<T>();
            model.m_id = id;
            ((ScriptableObject)model).name = name;

            return model;

#else
            throw new InvalidOperationException("Cannot create instance outside of editor!");
#endif


        }

        #endregion

    }

}
