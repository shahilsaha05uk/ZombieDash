#pragma warning disable IDE0051 // Remove unused private members

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AdvancedSceneManager.Utility
{

    [ExecuteAlways, ExecuteInEditMode]
    /// <summary>Represents a persistent reference to the <see cref="GameObject"/> that this is attached to, see also <see cref="GuidReferenceUtility"/> .</summary>
    public class GuidReference : MonoBehaviour
    {

        public string guid = GuidReferenceUtility.GenerateID();

        void OnValidate()
        {

            if (!enabled)
                enabled = true;

            //Why is this the only reliable callback outside of playmode? Start() or Awake() does not seem to be called after domain reload?
            //Constructor cannot be used, too many unity apis are called when registering
            Register();

        }

        void Start() => Register();
        void Awake() => Register();

        void Register()
        {
            //Debug.Log("registered: " + guid);
            GuidReferenceUtility.Add(this);
        }

        void OnDestroy()
        {
            //Debug.Log("unregistered: " + guid);
            GuidReferenceUtility.Remove(this);
        }

#if UNITY_EDITOR

        [CustomEditor(typeof(GuidReference))]
        public class Editor : UnityEditor.Editor
        {

            public override void OnInspectorGUI()
            { }

            public override bool UseDefaultMargins() =>
                false;

        }

#endif

    }

}
