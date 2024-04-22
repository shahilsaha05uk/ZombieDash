using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        const double undoTimeout = 10;
        [SerializeField] private VisualTreeAsset undoTemplate = null!;

        class UndoView : ViewModel
        {

            readonly Dictionary<ISceneCollection, (ProgressBar progressBar, float timeAdded)> undoTimeouts = new();

            ListView list;
            public override void OnCreateGUI(VisualElement element)
            {

                Profile.onProfileChanged += Reload;
                EditorApplication.update += Update;

                list = (ListView)element;
                if (Profile.current)
                    Reload();

            }

            public void Reload()
            {

                if (Profile.current)
                {
                    list.bindItem = BindCollection;
                    list.makeItem = window.undoTemplate.Instantiate;
                }
                else
                    list.Unbind();

                list.itemsSource = Profile.current ? Profile.current.removedCollections.ToArray() : Array.Empty<SceneCollection>();
                list.Rebuild();

            }

            void BindCollection(VisualElement element, int index)
            {

                if (Profile.current.removedCollections.ElementAtOrDefault(index) is not ISceneCollection collection)
                    return;

                if (collection is SceneCollection c)
                    element.Bind(new(c));
                element.userData = collection;

                element.Q<Label>("label-name").text = collection.title;

                var buttonUndo = element.Q<Button>("button-undo");
                var buttonDelete = element.Q<Button>("button-delete");

                buttonUndo.clickable = null;
                buttonDelete.clickable = null;

                var progressBar = element.Q<ProgressBar>();
                progressBar.lowValue = 0;
                progressBar.highValue = 1;

                if (!undoTimeouts.ContainsKey(collection))
                    undoTimeouts.Add(collection, (progressBar, (float)EditorApplication.timeSinceStartup));

                buttonUndo.clicked += () =>
                {

                    undoTimeouts.Remove(collection);
                    Profile.current.Restore(collection);
                    window.collections.Reload();
                    Reload();

                };

                buttonDelete.clicked += () =>
                    Remove(collection);

            }

            void Remove(ISceneCollection collection)
            {
                undoTimeouts.Remove(collection);
                Profile.current.Delete(collection);
                Reload();
            }

            void Update()
            {

                foreach (var (collection, (progressBar, timeAdded)) in undoTimeouts.ToArray())
                {

                    var value = ((EditorApplication.timeSinceStartup - timeAdded) / undoTimeout);
                    if (value >= 1)
                        Remove(collection);
                    else
                        progressBar.value = (float)value;

                }

            }

        }

    }

}
