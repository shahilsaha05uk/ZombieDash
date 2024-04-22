#if ASM_PLUGIN_CROSS_SCENE_REFERENCES

using System;

namespace AdvancedSceneManager.Plugin.Cross_Scene_References
{

    /// <summary>A collection of <see cref="CrossSceneReference"/> for a scene.</summary>
    [Serializable]
    public class SceneReferenceCollection
    {
        public string scene;
        public CrossSceneReference[] references;
    }

}
#endif