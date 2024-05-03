using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        class AssetsPage : SettingsPage
        {

            public override string Header => "Assets";

            public override void OnCreateGUI(VisualElement element)
            {

                element.BindToSettings();
                SetupAssetMove();
                SetupBlocklist();
            }

            void SetupBlocklist()
            {
                SetupBlocklist("list-blacklist", SceneManager.settings.project.m_blacklist);
                SetupBlocklist("list-whitelist", SceneManager.settings.project.m_whitelist);
            }

            void SetupBlocklist(string name, List<string> setting)
            {

                var list = element.Q<ListView>(name);
                list.makeItem += () => new TextField();

                list.itemsAdded += (e) =>
                {
                    setting[e.First()] = Normalize(GetCurrentFolder());
                    SaveAndNotify();
                };

                list.itemsRemoved += (e) =>
                {
                    foreach (var i in e)
                        setting.RemoveAt(i);
                    SaveAndNotify();
                };

                list.bindItem += (element, i) =>
                {

                    list.ClearSelection();
                    element.userData = i;
                    element.RegisterCallback<ClickEvent>(OnClick);

                    var text = (TextField)element;
                    text.value = setting[i];
                    text.RegisterCallback<FocusOutEvent>(OnChange);
                    element.Query<BindableElement>().ForEach(e => e.SetEnabled(true));

                };

                list.unbindItem += (element, i) =>
                {

                    list.ClearSelection();

                    var text = ((TextField)element);

                    text.UnregisterCallback<ClickEvent>(OnClick);
                    text.UnregisterCallback<FocusOutEvent>(OnChange);
                    text.value = null;

                };

                element.Query<BindableElement>().ForEach(e => e.SetEnabled(true));

                void OnClick(ClickEvent e)
                {
                    var index = ((int)((VisualElement)e.target).userData);
                    list.SetSelection(index);
                }

                void OnChange(FocusOutEvent e)
                {

                    var text = (TextField)e.target;
                    var index = (int)text.userData;

                    setting[index] = Normalize(text.value);
                    SceneManager.settings.project.Save();
                    SceneImportUtility.Notify();

                }

                string GetCurrentFolder()
                {
                    var projectWindowUtilType = typeof(ProjectWindowUtil);
                    var getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
                    var obj = getActiveFolderPath.Invoke(null, Array.Empty<object>());
                    return obj.ToString() + "/";
                }

                void SaveAndNotify()
                {
                    SceneManager.settings.project.Save();
                    SceneImportUtility.Notify();
                }

                string Normalize(string path) =>
                    SceneImportUtility.Blacklist.Normalize(path);

            }

            void SetupAssetMove()
            {

                var textField = element.Q<TextField>("text-path");
                var cancelButton = element.Q<Button>("button-cancel");
                var applyButton = element.Q<Button>("button-apply");

                textField.value = SceneManager.settings.project.assetPath;
                UpdateEnabledStatus();

                cancelButton.clicked += Cancel;
                applyButton.clicked += Apply;
                textField.RegisterValueChangedCallback(e => UpdateEnabledStatus());

                void Cancel()
                {
                    SceneManager.settings.project.assetPath = textField.value;
                    UpdateEnabledStatus();
                }

                void Apply()
                {

                    var profilePath = Assets.GetFolder<Profile>();
                    var scenePath = Assets.GetFolder<Scene>();

                    if (!AssetDatabaseUtility.CreateFolder(textField.value))
                    {
                        Debug.LogError("An error occurred when creating specified folder.");
                        return;
                    }

                    SceneManager.settings.project.assetPath = textField.value;

                    AssetDatabase.MoveAsset(profilePath, Assets.GetFolder<Profile>());
                    AssetDatabase.MoveAsset(scenePath, Assets.GetFolder<Scene>());

                    UpdateEnabledStatus();

                }

                void UpdateEnabledStatus()
                {

                    var isValid =
                        textField.value != SceneManager.settings.project.assetPath &&
                        textField.value.ToLower().StartsWith("assets/") &&
                        !string.IsNullOrEmpty(textField.value) &&
                        !Path.GetInvalidPathChars().Any(textField.value.Contains) &&
                        !Path.GetInvalidFileNameChars().Any(textField.value.Replace("/", "").Contains);

                    cancelButton.SetEnabled(isValid);
                    applyButton.SetEnabled(isValid);

                }

            }

        }

    }

}
