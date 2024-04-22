using System.Collections;
using System.ComponentModel;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    public class SceneOverviewWindow : EditorWindow_UIElements<SceneOverviewWindow>, IUIToolkitEditor
    {

        public override string path => "AdvancedSceneManager/SceneOverview";

        public override bool autoReloadOnWindowFocus => false;

        bool IsExpanded(string key, bool? newValue)
        {

            if (newValue.HasValue)
            {
                _ = expanded.Set(key, newValue.Value);
                Save();
                ReloadOverview((Profile)profileField.value);
            }

            return expanded.GetValue(key, true);

        }

        [SerializeField] private SerializableStringBoolDict expanded = new SerializableStringBoolDict();

        bool hasSetProfileInitial;
        Profile profile;

        ObjectField profileField;
        VisualElement list;
        public override void OnEnable()
        {

            _ = Coroutine().StartCoroutine();
            IEnumerator Coroutine()
            {

                var json = SceneManager.settings.local.sceneOverviewWindow ?? JsonUtility.ToJson(this);
                JsonUtility.FromJsonOverwrite(json, this);

                base.OnEnable();

                ReloadContent();

                while (!isMainContentLoaded)
                    yield return null;

                AssetUtility.onAssetsChanged -= OnEnable;
                AssetUtility.onAssetsChanged += OnEnable;

                minSize = new Vector2(520, 200);

                list = rootVisualElement.Q("root");

                profileField = rootVisualElement.Q<ObjectField>("profileField");

                if (!hasSetProfileInitial)
                    profile = Profile.current;
                hasSetProfileInitial = true;

                profileField.SetValueWithoutNotify(profile);
                ReloadOverview(profile);

                yield return new WaitForSeconds(1);

                profileField.SetValueWithoutNotify(profile);
                profileField.viewDataKey = profile ? profile.name : "";
                _ = profileField.RegisterValueChangedCallback(e => ReloadOverview(e.newValue as Profile));

            }

        }

        void Profile_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            EditorApplication.delayCall += () =>
            ReloadOverview(profile);
        }

        void OnDisable() =>
            Save();

        void Save()
        {
            var json = JsonUtility.ToJson(this);
            SceneManager.settings.local.sceneOverviewWindow = json;
            SceneManager.settings.local.Save();
        }

        internal void ReloadOverview() =>
            ReloadOverview(profile);

        internal void ReloadOverview(Profile profile)
        {

            if (this.profile)
                this.profile.PropertyChanged -= Profile_PropertyChanged;

            if (profile)
                profile.PropertyChanged += Profile_PropertyChanged;

            this.profile = profile;
            profileField.SetValueWithoutNotify(profile);

            var element = SceneOverviewUtility.CreateSceneOverview(this, SceneManager.assets.allScenes.ToArray(), profile, isExpanded: IsExpanded, showAll: !profile);
            list.Clear();
            list.Add(element);

        }

    }

}
