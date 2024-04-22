using AdvancedSceneManager.Models;
using UnityEditor;

namespace AdvancedSceneManager.Editor
{

    [CustomEditor(typeof(ASMUserSettings))]
    class ASMUserSettingsEditor : UnityEditor.Editor
    {

        public override void OnInspectorGUI()
        {

            var size = EditorStyles.boldLabel.fontSize;
            EditorStyles.boldLabel.fontSize = 16;
            base.OnInspectorGUI();
            EditorStyles.boldLabel.fontSize = size;

        }

    }

}
