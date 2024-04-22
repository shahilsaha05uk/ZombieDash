using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        [SerializeField] private VisualTreeAsset listItem;

        abstract class ListPopup<T> : ViewModel, IPopup where T : ASMModel
        {

            public abstract void OnAdd();
            public abstract void OnRemove(T item);
            public abstract void OnRename(T item);
            public abstract void OnSelected(T item);

            public abstract string noItemsText { get; }
            public abstract string headerText { get; }
            public abstract IEnumerable<T> items { get; }

            T[] list;

            VisualElement container;
            public override void OnCreateGUI(VisualElement container)
            {

                this.container = container;
                this.list = items.Where(o => o).ToArray();

                container.BindToSettings();

                container.Q<Label>("text-header").text = headerText;
                container.Q<Label>("text-no-items").text = noItemsText;

                container.Q<Button>("button-add").clicked += OnAdd;

                var list = container.Q<ListView>();

                list.makeItem = window.listItem.Instantiate;

                list.unbindItem = Unbind;
                list.bindItem = Bind;
                Reload();

            }

            public void Reload()
            {
                list = items.Where(o => o).ToArray();
                container.Q("text-no-items").SetVisible(!list.Any());
                container.Q<ListView>().itemsSource = list;
                container.Q<ListView>().Rebuild();
            }

            void Unbind(VisualElement element, int index)
            {
                element.Q<Button>("button-name").clickable = new(_ => { });
                element.Q<Button>("button-rename").clickable = new(_ => { });
                element.Q<Button>("button-remove").clickable = new(_ => { });
            }

            void Bind(VisualElement element, int index)
            {

                var item = list.ElementAt(index);
                var nameButton = element.Q<Button>("button-name");
                nameButton.text = item.name;

                nameButton.clicked += () => OnSelected(item);
                element.Q<Button>("button-rename").clicked += () => OnRename(item);
                element.Q<Button>("button-remove").clicked += () => OnRemove(item);

            }

        }

    }

}
