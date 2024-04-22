using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    public class EnumField
#if UNITY_2022_1_OR_NEWER
: UnityEngine.UIElements.EnumField
#else
: UnityEditor.UIElements.EnumField
#endif
    {

        public new class UxmlFactory : UxmlFactory<EnumField, UxmlTraits>
        {
            public override string uxmlNamespace => "AdvancedSceneManager";
        }

    }

}
