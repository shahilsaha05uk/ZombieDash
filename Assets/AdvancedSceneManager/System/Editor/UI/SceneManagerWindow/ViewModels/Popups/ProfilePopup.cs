using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Internal;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        class ProfilePopup : ListPopup<Profile>
        {

            public override string noItemsText { get; } = "No profiles, you can create one using + button.";
            public override string headerText { get; } = "Profiles";
            public override IEnumerable<Profile> items => SceneManager.assets.profiles;

            public override bool displayRenameButton => true;
            public override bool displayRemoveButton => true;
            public override bool displayDuplicateButton => true;

            public override async void OnAdd()
            {

                var name = await PickNamePopup.Prompt();
                if (string.IsNullOrEmpty(name))
                    window.popups.Open<ProfilePopup>();
                else
                {

                    window.StartProgressSpinner();

                    try
                    {
                        Profile.SetProfile(Profile.Create(name));
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError(e);
                    }

                    await window.popups.Close();
                    window.StopProgressSpinner();

                }

            }

            public override async void OnSelected(Profile profile)
            {
                window.StartProgressSpinner();
                Profile.SetProfile(profile);
                await window.popups.Close();
                window.StopProgressSpinner();
            }

            public override async void OnRename(Profile profile)
            {

                var name = await PickNamePopup.Prompt(profile.name);
                if (!string.IsNullOrEmpty(name))
                {
                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(profile), name + ".asset");
                    Profile.SetProfile(profile);
                }

                window.popups.Open<ProfilePopup>();

            }

            public override void OnRemove(Profile profile)
            {

                if (!PromptUtility.PromptDelete("profile"))
                    return;

                Profile.Delete(profile);
                Reload();

            }

            public override void OnDuplicate(Profile profile)
            {

                var names = SceneManager.assets.profiles.Select(p => p.name).ToArray();
                var path = $"Assets/AdvancedSceneManager/{ObjectNames.GetUniqueName(names, profile.name)}.asset";
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(profile), path))
                    Debug.LogError("Something went wrong when duplicating profile.");

                var obj = AssetDatabase.LoadAssetAtPath<Profile>(path);
                if (!obj)
                    Debug.LogError("Something went wrong when duplicating profile.");

                obj.m_id = ASMModel.GenerateID();

                Assets.Add(obj);

                Reload();

            }

        }

    }

}
