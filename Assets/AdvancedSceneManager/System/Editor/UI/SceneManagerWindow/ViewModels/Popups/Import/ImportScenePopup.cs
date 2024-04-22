using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        class ImportScenePopup : ImportPopup<string, ImportScenePopup>
        {

            public override string headerText => "Unimported scenes:";
            public override string button1Text => "Import";
            public override string subtitleText => "<i>* Right click a scene to access blacklist options</i>";
            public override bool displayAutoImportField => true;

            public override string GetNotificationText(int count) =>
                $"You have {count} scenes ready to be imported, click here to import them now...";

            public override IEnumerable<string> GetItems() =>
                SceneImportUtility.unimportedScenes.Except(SceneImportUtility.dynamicScenes);

            public override void SetupItem(VisualElement element, Item item, int index, out string text)
            {

                text = item.value;

                element.ContextMenu((e) =>
                {

                    e.menu.AppendAction("View SceneAsset...", e => EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(item.value)));
                    e.menu.AppendSeparator();

                    //Blacklist
                    var segments = item.value.Split("/");
                    var paths = segments.Select((s, i) => string.Join("\\", segments.Take(i))).Skip(1);

                    foreach (var path in paths)
                        e.menu.AppendAction("Blacklist/" + path, (e) => AddToBlacklist(path));

                    e.menu.AppendAction("Blacklist/" + item.value.Replace("/", "\\").TrimEnd('/'), (e) => AddToBlacklist(item.value));

                });

                void AddToBlacklist(string path)
                {
                    SceneImportUtility.Blacklist.Add(path);
                    Reload();
                }

            }

            public override void OnButton1Click(IEnumerable<string> items) =>
                SceneImportUtility.Import(items);

        }

    }

}
