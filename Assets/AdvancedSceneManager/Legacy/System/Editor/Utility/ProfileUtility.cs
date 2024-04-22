using System.Collections;
using AdvancedSceneManager.Models;

namespace AdvancedSceneManager.Editor.Utility
{

    public static class ProfileUtility
    {

        /// <summary>Sets the profile to be used by ASM.</summary>
        public static IEnumerator SetProfileAndWaitForSceneGeneration(Profile profile)
        {
            Profile.SetProfile(profile, false);
            yield return AssetRefreshUtility.DoFullRefresh();
        }

    }

}
