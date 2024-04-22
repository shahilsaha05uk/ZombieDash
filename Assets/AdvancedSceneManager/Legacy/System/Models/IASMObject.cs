using System.ComponentModel;

namespace AdvancedSceneManager.Models
{

    /// <summary>Identifies either <see cref="SceneCollection"/>, <see cref="Scene"/> or <see cref="Profile"/>.</summary>
    public interface IASMObject
#if UNITY_EDITOR
        : INotifyPropertyChanged
#endif
    {

        /// <summary>Should be called after changing a property.</summary>
        /// <remarks>Only available in editor. Possible removal in ASM 2.0, if it is no longer needed.</remarks>
        void OnPropertyChanged();

        /// <summary>Matches this ASM object against a string (i.e., where applicable: path / name / asset id).</summary>
        /// <remarks>See <see cref="Scene.Find(string, SceneCollection, Profile)"/>.</remarks>
        bool Match(string name);

    }

}
