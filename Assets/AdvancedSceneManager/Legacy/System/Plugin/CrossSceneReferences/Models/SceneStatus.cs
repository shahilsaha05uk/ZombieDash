﻿#if ASM_PLUGIN_CROSS_SCENE_REFERENCES

namespace AdvancedSceneManager.Plugin.Cross_Scene_References
{

    /// <summary>Specifies the state of a scene.</summary>
    public enum SceneStatus
    {
        /// <summary>Cross-scene reference utility has not done anything to this scene.</summary>
        Default,
        /// <summary>Cross-scene reference utility has restored references in this scene.</summary>
        Restored,
        /// <summary>Cross-scene reference utility has cleared references in this scene.</summary>
        Cleared
    }

}
#endif