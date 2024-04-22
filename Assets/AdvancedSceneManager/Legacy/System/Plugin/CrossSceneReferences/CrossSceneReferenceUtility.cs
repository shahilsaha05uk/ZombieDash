#if ASM_PLUGIN_CROSS_SCENE_REFERENCES

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using Component = UnityEngine.Component;
using AdvancedSceneManager.Utility;

using scene = UnityEngine.SceneManagement.Scene;
using UnityEngine.Events;
using System.Reflection;
using AdvancedSceneManager.Models;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Plugin.Cross_Scene_References
{

    /// <summary>An utility for saving and restoring cross-scene references.</summary>
    public static partial class CrossSceneReferenceUtility
    {

#pragma warning disable CS0067
        internal static event Action OnSaved;
        internal static event Action OnSceneStatusChanged;
#pragma warning restore CS0067

        internal static void Initialize()
        {
            ResetAllScenes();
            resolvedReferences.Clear();
            sceneStatus.Clear();
        }

        #region Resolve status

        static readonly Dictionary<string, ResolvedCrossReference> resolvedReferences = new Dictionary<string, ResolvedCrossReference>();

        static void AddResolvedReference(ResolvedCrossReference reference) =>
            _ = resolvedReferences.Set(reference.reference.id, reference);

        /// <summary>Gets all references for all scenes.</summary>
        public static IEnumerable<ResolvedCrossReference> GetResolvedReferences() =>
            resolvedReferences.Values;

        /// <summary>Gets all references for this scene.</summary>
        public static IEnumerable<ResolvedCrossReference> GetResolvedReferences(scene scene) =>
            resolvedReferences.Values.Where(r => r.variable.resolve.scene == scene);

        /// <summary>Gets all references for this game object.</summary>
        public static IEnumerable<ResolvedCrossReference> GetResolvedReferences(GameObject obj) =>
            resolvedReferences.Values.Where(r => r.variable.resolve.gameObject == obj);

        /// <summary>Gets all references for this game object.</summary>
        public static IEnumerable<ResolvedCrossReference> GetResolvedReferencesValue(GameObject obj) =>
            resolvedReferences.Values.Where(r => r.value.resolve.gameObject == obj);

        /// <summary>Gets if the cross-scene references can be saved.</summary>
        /// <remarks>This would be if status: <see cref="SceneStatus.Restored"/> and no resolve errors.</remarks>
        public static bool CanSceneBeSaved(scene scene) =>
            GetResolvedReferences(scene).All(r => r.result == ResolveStatus.Succeeded);

        /// <summary>Get the resolve result for a cross scene reference, if it has been resolved.</summary>
        public static bool GetResolved(CrossSceneReference reference, out ResolvedCrossReference? resolved)
        {
            resolved = default;
            if (resolvedReferences.ContainsKey(reference.id))
                resolved = resolvedReferences[reference.id];
            return resolved.HasValue;
        }

        /// <summary>Get the resolve result for a cross scene reference, if it has been resolved.</summary>
        public static ResolvedCrossReference GetResolved(CrossSceneReference reference)
        {
            if (resolvedReferences.ContainsKey(reference.id))
                return resolvedReferences[reference.id];
            return default;
        }

        static void ClearStatusForScene(scene scene)
        {
            var references = GetResolvedReferences(scene);
            foreach (var reference in references.ToArray())
                if (reference.variable.resolve.scene == scene)
                    _ = resolvedReferences.Remove(reference.reference.id);
            OnSceneStatusChanged?.Invoke();
        }

        static readonly Dictionary<scene, SceneStatus> sceneStatus = new Dictionary<scene, SceneStatus>();

        static void SetSceneStatus(scene scene, SceneStatus state)
        {
            _ = sceneStatus.Set(scene, state);
            OnSceneStatusChanged?.Invoke();
        }

        public static SceneStatus GetSceneStatus(scene scene)
        {
            if (sceneStatus.ContainsKey(scene))
                return sceneStatus[scene];
            else
                return default;
        }

        #endregion
        #region Assets

        const string Key = "CrossSceneReferences";

        /// <summary>Loads cross-scene references for a scene.</summary>
        public static SceneReferenceCollection Load(Scene scene) =>
            scene
            ? SceneDataUtility.Get<SceneReferenceCollection>(scene, Key)
            : null;

        /// <summary>Loads cross-scene references for all scenes.</summary>
        public static SceneReferenceCollection[] Enumerate() =>
            SceneDataUtility.
            Enumerate<SceneReferenceCollection>(Key).
            Select(c => c.data).
            OfType<SceneReferenceCollection>().
            Where(c => c.references?.Any() ?? false).
            ToArray();

#if UNITY_EDITOR

        /// <summary>Save the cross-scene references for a scene. This removes all previously added references for this scene.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void Save(scene scene, params CrossSceneReference[] references) =>
            Save(new SceneReferenceCollection() { references = references, scene = scene.path }, scene);

        /// <summary>Saves a <see cref="CrossSceneReference"/>.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void Save(SceneReferenceCollection reference, scene scene)
        {

            if (!scene.IsValid())
                return;

            var s = SceneManager.assets.allScenes.Find(scene.path);
            if (s)
                SceneDataUtility.Set(s, Key, reference);
            else
                Debug.LogWarning($"Scene ('{scene.name}') was not valid, could not save cross-scene reference, please make sure it isn't blacklisted.");
            ClearStatusForScene(scene);
            OnSaved?.Invoke();

        }

        /// <summary>Removes all cross-scene references for this scene.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void Remove(scene scene) =>
            Remove(scene.Scene().scene);

        /// <summary>Removes all cross-scene references for this scene.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void Remove(Scene scene) =>
            SceneDataUtility.Unset(scene, Key);

        /// <summary>Removes all cross-scene references for this scene.</summary>
        /// <remarks>Only available in editor.</remarks>
        public static void Remove(CrossSceneReference reference)
        {

            if (reference == null)
                return;

            var collection = SceneDataUtility.Get<SceneReferenceCollection>(reference.variable.asmScene, Key);
            var list = collection.references;
            var i = Array.FindIndex(list, r => r.variable.ToString() == reference.variable.ToString());
            if (i == -1)
                return;

            ArrayUtility.RemoveAt(ref list, i);
            collection.references = list;

            SceneDataUtility.Set(reference.variable.asmScene, Key, collection);

            if (resolvedReferences.ContainsKey(reference.id))
            {
                var resolve = resolvedReferences[reference.id];
                _ = resolvedReferences.Remove(reference.id);
                ObjectReference.ResetValue(resolve.variable.resolve);
            }

        }


#endif

        #endregion
        #region Resolve / reset

        /// <summary>Resolves all scenes.</summary>
        /// <remarks>This runs within a single frame.</remarks>
        public static void ResolveAllScenes()
        {
            foreach (var scene in SceneUtility.GetAllOpenUnityScenes())
                _ = ResolveScene(scene).ToArray();
        }

        /// <summary>Resolves cross-scene references in the scene.</summary>
        public static IEnumerable<ResolvedCrossReference> ResolveScene(scene scene)
        {

            if (Load(SceneManager.assets.allScenes.Find(scene.path)) is SceneReferenceCollection references)
                foreach (var reference in references.references)
                {

                    if (resolvedReferences.ContainsKey(reference.id))
                    {
                        var r = resolvedReferences[reference.id];
                        if (resolvedReferences.Remove(reference.id) && r.result == ResolveStatus.Succeeded)
                            ObjectReference.ResetValue(r.variable.resolve);
                    }

                    var variable = reference.variable.Resolve();
                    var value = reference.value.Resolve();
                    var result = ObjectReference.SetValue(variable, value);

                    var resolved = new ResolvedCrossReference(variable, value, reference, result);
                    AddResolvedReference(resolved);
                    yield return resolved;

                };

            SetSceneStatus(scene, SceneStatus.Restored);

        }

        /// <summary>Resets all cross-scene references in all scenes.</summary>
        public static void ResetAllScenes()
        {
            foreach (var scene in SceneUtility.GetAllOpenUnityScenes())
                ResetScene(scene);
        }

        /// <summary>Resets all cross-scene references in scene.</summary>
        public static void ResetScene(scene scene)
        {
            foreach (var reference in GetResolvedReferences(scene).ToArray())
            {
                ObjectReference.ResetValue(reference.variable.resolve);
                _ = resolvedReferences.Remove(reference.reference.id);
            }
            SetSceneStatus(scene, SceneStatus.Cleared);
        }

        #endregion
        #region Find

        /// <summary>Finds all cross-scene references in the scenes.</summary>
        public static IEnumerable<CrossSceneReference> FindCrossSceneReferences(params scene[] scenes)
        {

            var components = FindComponents(scenes).
                Where(s => s.obj && s.scene.IsValid()).
                Select(c => (c.scene, c.obj, fields: c.obj.GetType()._GetFields().Where(IsSerialized).ToArray())).
                ToArray();

            foreach (var (scene, obj, fields) in components)
            {

                foreach (var field in fields.ToArray())
                {

                    var o = field.GetValue(obj);

                    if (o != null)
                    {

                        if (typeof(UnityEventBase).IsAssignableFrom(field.FieldType))
                        {
                            for (int i = 0; i < ((UnityEventBase)o).GetPersistentEventCount(); i++)
                            {
                                if (GetCrossSceneReference(o, scene, out var reference, unityEventIndex: i))
                                {
                                    var source = GetSourceCrossSceneReference(scene, obj, field, unityEventIndex: i);
                                    yield return new CrossSceneReference(source, reference);
                                }
                            }
                        }
                        else if (typeof(IList).IsAssignableFrom(field.FieldType))
                        {
                            for (int i = 0; i < ((IList)o).Count; i++)
                            {
                                if (GetCrossSceneReference(o, scene, out var reference, arrayIndex: i))
                                {
                                    var source = GetSourceCrossSceneReference(scene, obj, field, arrayIndex: i);
                                    yield return new CrossSceneReference(source, reference);
                                }
                            }
                        }
                        else if (GetCrossSceneReference(o, scene, out var reference))
                            yield return new CrossSceneReference(GetSourceCrossSceneReference(scene, obj, field), reference);

                    }

                }
            }
        }

        static bool IsSerialized(FieldInfo field) =>
             (field?.IsPublic ?? false) || field?.GetCustomAttribute<SerializeField>() != null;

        static IEnumerable<(scene scene, Component obj)> FindComponents(params scene[] scenes)
        {
            foreach (var scene in scenes)
                if (scene.isLoaded)
                    foreach (var rootObj in scene.GetRootGameObjects())
                        foreach (var obj in rootObj.GetComponentsInChildren<Component>(includeInactive: true))
                            yield return (scene, obj);
        }

        static bool GetCrossSceneReference(object obj, scene sourceScene, out ObjectReference reference, int unityEventIndex = -1, int arrayIndex = -1)
        {

            reference = null;

            if (obj is GameObject go && go && IsCrossScene(sourceScene.path, go.scene.path))
                reference = new ObjectReference(go.scene, GuidReferenceUtility.GetOrAddPersistent(go));

            else if (obj is Component c && c && c.gameObject && IsCrossScene(sourceScene.path, c.gameObject.scene.path))
                reference = new ObjectReference(c.gameObject.scene, GuidReferenceUtility.GetOrAddPersistent(c.gameObject)).With(c);

            else if (obj is UnityEvent ev)
                return GetCrossSceneReference(ev.GetPersistentTarget(unityEventIndex), sourceScene, out reference);

            else if (obj is IList list)
                return GetCrossSceneReference(list[arrayIndex], sourceScene, out reference);

            return reference != null;

        }

        static bool IsCrossScene(string srcScene, string scenePath)
        {
            var isPrefab = string.IsNullOrWhiteSpace(scenePath);
            var isDifferentScene = scenePath != srcScene;
            return isDifferentScene && !isPrefab;
        }

        static ObjectReference GetSourceCrossSceneReference(scene scene, Component obj, FieldInfo field, int? unityEventIndex = null, int? arrayIndex = null) =>
            new ObjectReference(scene, GuidReferenceUtility.GetOrAddPersistent(obj.gameObject), field).With(obj).With(unityEventIndex, arrayIndex);

        #endregion

    }

}
#endif
