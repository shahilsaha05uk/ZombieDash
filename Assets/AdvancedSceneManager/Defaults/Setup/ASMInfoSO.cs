using System.IO;
using System.Linq;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Setup
{

    /// <summary>Contains info about the current version of ASM.</summary>
    public class ASMInfoSO : ScriptableObject
    {

#if UNITY_EDITOR

        public static ASMInfoSO instance
        {
            get
            {
                var obj = Resources.FindObjectsOfTypeAll<ASMInfoSO>().Where(o => o).FirstOrDefault();
                return obj;
            }
        }

        public void Save()
        {
            File.WriteAllText(@"Assets\AdvancedSceneManager/System/Resources/AdvancedSceneManager/version.txt", m_version + "\n" + m_patchNotes);
            AssetDatabase.Refresh();
            ScriptableObjectUtility.Save(this);
        }

#endif

        [Space]
        public string m_version;

        [Space]
        [TextArea(10, 1000)]
        public string m_patchNotes;

    }

}
