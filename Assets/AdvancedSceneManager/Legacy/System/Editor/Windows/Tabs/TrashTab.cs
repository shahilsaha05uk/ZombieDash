using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.Window
{

    static class TrashTab
    {

        public static void OnEnable(VisualElement element)
        {

            var imguiContainer = element.Q<IMGUIContainer>();
            imguiContainer.onGUIHandler = () =>
            {

                foreach (var collection in Profile.current.removedCollections)
                {

                    if (!collection)
                        continue;

                    GUILayout.BeginHorizontal();

                    GUILayout.Label(collection.title);

                    if (GUILayout.Button("Restore", GUILayout.ExpandWidth(false))) Restore(collection);
                    if (GUILayout.Button("Remove permanently", GUILayout.ExpandWidth(false))) Remove(collection);

                    GUILayout.EndHorizontal();

                }

                GUILayout.Space(22);
                GUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
                    Clear();

                GUILayout.EndHorizontal();

            };

            void Restore(SceneCollection collection)
            {
                Profile.current.Restore(collection);
                GoBackIfEmpty();
            }

            void Remove(SceneCollection collection)
            {
                if (!EditorUtility.DisplayDialog("Removing collections...", "Are you sure you wish to permanently remove the collection? This is not reversible.", "Cancel", "Remove permanently"))
                    AssetUtility.Remove(collection);
                GoBackIfEmpty();
            }

            void Clear()
            {

                if (!EditorUtility.DisplayDialog("Removing collections...", "Are you sure you wish to permanently remove all collections in this list? This is not reversible.", "Cancel", "Clear"))
                    foreach (var collection in Profile.current.removedCollections)
                        if (collection)
                            AssetUtility.Remove(collection);

                GoBackIfEmpty();

            }

            void GoBackIfEmpty()
            {
                if (Profile.current.removedCollections.Where(c => c).Count() == 0)
                {
                    SceneManagerWindow.OpenTab(SceneManagerWindow.Tab.Scenes);
                    //Remove nulls
                    Profile.current.ClearRemovedCollections();
                }
            }

        }

    }

}
