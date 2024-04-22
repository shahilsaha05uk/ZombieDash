#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.Utility
{

    /// <summary>Prompts the user to select an option.</summary>
    public class PickOptionPrompt : GenericPrompt<string, PickOptionPrompt>
    {

        public static (bool successful, string selectedValue) Prompt(string title, string message, string[] options, string OkButton = "Ok", string cancelButton = "Cancel", float maxContentHeight = 400)
        {

            content = new GUIContent(message);
            _title = title;
            _okButton = OkButton;
            _cancelButton = cancelButton;
            _maxContentHeight = maxContentHeight;
            _options = options;
            var value = Prompt(defaultValue: "_");
            return (value.successful, value.value);

        }

        static GUIContent content;
        static Vector2 size;
        static string _title;
        static string _okButton;
        static string _cancelButton;
        static float _maxContentHeight;
        static string[] _options;
        public override float extraHeight => size.y;
        public override float width => size.x + 56;

        public override string title => _title;
        public override string cancelButton => _cancelButton;
        public override string okButton => _okButton;

        int index;

        Vector2 scrollPos;
        public override void OnContentGUI(ref string value)
        {

            size = GUI.skin.label.CalcSize(content);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(_maxContentHeight));
            EditorGUILayout.LabelField(content, GUILayout.ExpandHeight(true));

            index = EditorGUILayout.Popup(index, _options);
            value = _options[index];

            EditorGUILayout.EndScrollView();

        }

    }

}
#endif
