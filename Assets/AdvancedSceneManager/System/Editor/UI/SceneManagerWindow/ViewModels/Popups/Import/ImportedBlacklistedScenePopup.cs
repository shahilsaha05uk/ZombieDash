using System.Collections.Generic;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Internal;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        class ImportedBlacklistedScenePopup : ImportPopup<Scene, ImportedBlacklistedScenePopup>
        {

            public override string headerText => "Blacklisted scenes:";
            public override string button1Text => "Unimport";
            public override bool displayAutoImportField => false;

            public override string GetNotificationText(int count) =>
                $"You have {count} imported scenes that are blacklisted, click here to fix now...";

            public override IEnumerable<Scene> GetItems() =>
                SceneImportUtility.importedBlacklistedScenes;

            public override void SetupItem(VisualElement element, Item item, int index, out string text) =>
                text = $"{item.value.name} ({item.value.id})";

            public override void OnButton1Click(IEnumerable<Scene> items) =>
                Assets.Remove(items);

        }

    }

}
