#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.Window
{

    internal class CreateProfileGUI
    {

        public bool hasAutoBlacklistPaths =>
            blacklist != null &&
            !blacklist.isWhitelist &&
            possibleBlacklistPaths != null &&
            possibleBlacklistPaths.Any() &&
            possibleBlacklistPaths.SequenceEqual(blacklist.paths);

        public string autoBlacklistMessage =>
            "\n" + possibleBlacklistPaths.Count + " folders found that contain more than 50 scenes, these have been added to blacklist, as they may contain auto-generated scenes, which should not be included in ASM.\n\nYou probably want to add these folders as dynamic collections later, this will guarantee that the scenes are included in build.\n\nPlease verify whatever these are correct.\n";

        List<string> possibleBlacklistPaths;
        public CreateProfileGUI(Button doneButton, string profileName, BlacklistUtility.BlacklistModule blacklist = null, bool hideBlacklist = false)
        {

            this.doneButton = doneButton;
            this.profileName = profileName;
            this.hideBlacklist = hideBlacklist;
            this.blacklist = blacklist;

            element = new VisualElement();

            if (blacklist is null)
                ReloadBlacklist();

            Setup();

        }

        readonly Button doneButton;
        public BlacklistUtility.BlacklistModule blacklist { get; private set; }
        public string profileName { get; private set; }
        public bool hideBlacklist { get; }

        public VisualElement element { get; }
        public StyleSheet style { get; private set; }

        TextField profileNameField;
        IMGUIContainer blacklistContainer;
        VisualElement blacklistContainerParent;
        Label errorMessage;

        void Setup()
        {

            var template = Resources.Load<VisualTreeAsset>("AdvancedSceneManager/CreateProfileGUI/CreateProfileGUI");
            template.CloneTree(element);

            var uss = Resources.LoadAll("AdvancedSceneManager/Tabs/Welcome/Tab").OfType<StyleSheet>().Where(s => !s.name.Contains("inline")).FirstOrDefault();
            element.styleSheets.Add(uss);
            style = uss;

            profileNameField = element.Q<TextField>("profileName");
            blacklistContainer = element.Q<IMGUIContainer>("blacklistContainer");
            blacklistContainerParent = element.Q<VisualElement>("blacklistContainerParent");
            errorMessage = element.Q<Label>("errorMessage");

            ValidateName(profileName);
            profileNameField.SetValueWithoutNotify(profileName);
            profileNameField.RegisterCallback<ChangeEvent<string>>(e =>
            {
                ValidateName(e.newValue);
                profileName = e.newValue;
            });

            blacklistContainer.onGUIHandler = () =>
            {

                var extraMessage = "";
                if (hasAutoBlacklistPaths)
                    extraMessage = autoBlacklistMessage;

                var (_, _, height) = BlacklistUtility.DrawGUI(blacklist, extraMessage);
                blacklistContainerParent.style.minHeight = height;

            };

            if (hideBlacklist)
                element.Q("blacklistPanel").RemoveFromHierarchy();

        }

        void ValidateName(string name)
        {

            var (isError, message) = IsValid(name);
            errorMessage.text = message;
            errorMessage.visible = isError;

            doneButton.SetEnabled(!isError);

        }

        static (bool isError, string message) IsValid(string name)
        {

            var checks = new Func<string, (bool isError, string message)>[] { IsEmpty, CheckDuplicates };

            foreach (var check in checks)
            {
                var result = check.Invoke(name);
                if (result.isError)
                    return result;
            }

            return (isError: false, "");

        }

        static (bool isError, string message) IsEmpty(string value) =>
            (isError: string.IsNullOrEmpty(value), "");

        static (bool isError, string message) CheckDuplicates(string value) =>
            (isError: GetProfiles().Any(p => p.name.ToLower() == value?.ToLower()), "The name is already in use.");

        static IEnumerable<Profile> GetProfiles() =>
            AssetDatabase.FindAssets("t:" + typeof(Profile).FullName).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Profile>).Where(p => p);

        public void ReloadBlacklist()
        {
            blacklist = new BlacklistUtility.BlacklistModule();
            blacklist.isWhitelist = false;
            blacklist.paths = possibleBlacklistPaths = AssetDatabase.FindAssets("t:SceneAsset").Select(AssetDatabase.GUIDToAssetPath).GroupBy(Path.GetDirectoryName).Where(g => g.Count() > 50).Select(g => g.Key.Replace("\\", "/")).ToList();
        }

    }

}
#endif
