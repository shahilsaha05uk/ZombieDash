using System;
using System.Linq;
using AdvancedSceneManager.Utility;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AdvancedSceneManager.Models
{

    /// <summary>Specifies behavior of a scene when its associated collection is closed.</summary>
    public enum SceneCloseBehavior
    {
        /// <summary>Specifies that this scene should be closed when its associated collection is closed.</summary>
        Close,
        /// <summary>Specifies that this scene should be remain open when its associated collection is closed, if the newly opened collection also contains it.</summary>
        /// <remarks>Scene will be closed if its associated collection is closed, and no other collection is opened.</remarks>
        KeepOpenIfNextCollectionAlsoContainsScene,
        /// <summary>Specifies that the scene should not be automatically closed when its associated collection is closed.</summary>
        KeepOpenAlways
    }

    /// <summary>Specifies behavior of a scene when its associated collection is opened.</summary>
    public enum SceneOpenBehavior
    {
        /// <summary>Specifies that the scene will be opened when its associated collection is opened.</summary>
        OpenNormally,
        /// <summary>Specifies that the scene will not be opened when its associated collection is opened.</summary>
        DoNotOpenInCollection
    }

    /// <summary>Represents a list of <see cref="SceneTag"/>.</summary>
    [Serializable]
    public class TagList : SerializableDictionary<string, string>
    {

        public new SceneTag this[string path]
        {
            get => SceneTag.Find(this.GetValue(path, ""));
            set => this.Set(path, value.id);
        }

        public SceneTag this[Scene scene]
        {
            get => this[scene ? scene.path : ""];
            set => this[scene ? scene.path : ""] = value;
        }

        public bool TryGetValue(Scene scene, out SceneTag layer) =>
            TryGetValue(scene ? scene.path : "", out layer);

        public bool TryGetValue(string path, out SceneTag layer)
        {
            _ = base.TryGetValue(path, out var id);
            layer = SceneTag.Find(id);
            return layer != null;
        }

    }

    /// <summary>A layer that makes it easier to identify certain scenes.</summary>
    [Serializable]
    public class SceneTag
    {

        static Color RandomColor() =>
            new Color(Random.value, Random.value, Random.value);

        public readonly static SceneTag Default = new SceneTag("Default", Color.green, "Default");
        public readonly static SceneTag DoNotOpen = new SceneTag("DoNotOpen", Color.clear, "DoNotOpen") { openBehavior = SceneOpenBehavior.DoNotOpenInCollection };
        public readonly static SceneTag Persistent = new SceneTag("Persistent", Color.blue, "Persistent") { closeBehavior = SceneCloseBehavior.KeepOpenAlways, label = "P" };
        public readonly static SceneTag PersistIfPossible = new SceneTag("PersistIfPossible", Color.white, "PersistIfPossible") { closeBehavior = SceneCloseBehavior.KeepOpenIfNextCollectionAlsoContainsScene };

        [Obsolete]
        public SceneTag()
        { }

        public SceneTag(string title, Color? color = null, string id = null)
        {
            this.name = title;
            this.color = color ?? RandomColor();
            this.id = id ?? Guid.NewGuid().ToString();
        }

        public string name;
        public string label;
        public string id;
        public Color color;

        /// <summary>Specifies how the scene should behave when a <see cref="SceneCollection"/> is closed.</summary>
        public SceneCloseBehavior closeBehavior;

        /// <summary>Specifies how the scene should behave when a <see cref="SceneCollection"/> is opened.</summary>
        public SceneOpenBehavior openBehavior;

        public static SceneTag Find(string id) =>
            Profile.current
            ? Profile.current.tagDefinitions.FirstOrDefault(t => t.id == id) ?? Default
            : null;

        #region Equality

        public override bool Equals(object other) =>
            Equals(other as SceneTag);

        public override int GetHashCode() =>
            id.GetHashCode();

        public bool Equals(SceneTag layer) =>
            layer?.id == id;

        public static bool operator ==(SceneTag left, SceneTag right) =>
            left?.Equals(right) ?? false;

        public static bool operator !=(SceneTag left, SceneTag right) =>
            !(left?.Equals(right) ?? true);

        #endregion

        public override string ToString() =>
            name;

    }

}
