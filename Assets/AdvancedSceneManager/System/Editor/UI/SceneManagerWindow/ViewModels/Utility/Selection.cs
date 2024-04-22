using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Models;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        [Serializable]
        class CollectionScenePair
        {
            public SceneCollection collection;
            public int sceneIndex;
        }

        [SerializeField] private List<CollectionScenePair> m_selectedScenes = new();
        [SerializeField] private List<SceneCollection> m_selectedCollections = new();

        class Selection
        {

            public void OnEnable()
            {
                rootVisualElement.UnregisterCallback<PointerDownEvent>(MouseDown);
                rootVisualElement.RegisterCallback<PointerDownEvent>(MouseDown);
            }

            void MouseDown(PointerDownEvent e)
            {
                if (((VisualElement)e.target).GetAncestor<ObjectField>() is not ObjectField)
                    Clear();
            }

            public IEnumerable<CollectionScenePair> scenes => window.m_selectedScenes;
            public IEnumerable<SceneCollection> collections => window.m_selectedCollections;

            public void Add(SceneItem item) => SetSelection(item, true);
            public void Add(CollectionItem item) => SetSelection(item, true);

            public void Remove(SceneItem item) => SetSelection(item, false);
            public void Remove(CollectionItem item) => SetSelection(item, false);

            public void SetSelection(SceneItem item, bool value)
            {
                if (IsSelected(item) != value)
                    ToggleSelection(item);
            }

            public void SetSelection(CollectionItem item, bool value)
            {
                if (IsSelected(item) != value)
                    ToggleSelection(item);
            }

            public void ToggleSelection(SceneItem item)
            {

                if (item.collection is not SceneCollection c)
                    return;

                var existingItem = window.m_selectedScenes.FirstOrDefault(i => i.collection == c && i.sceneIndex == item.index);
                if (!window.m_selectedScenes.Remove(existingItem))
                    window.m_selectedScenes.Add(new() { collection = c, sceneIndex = item.index });

            }

            public void ToggleSelection(CollectionItem item)
            {

                if (item.collection is not SceneCollection c)
                    return;

                if (!window.m_selectedCollections.Remove(c))
                    window.m_selectedCollections.Add(c);

            }

            public bool IsSelected(SceneItem item) =>
                window.m_selectedScenes.Any(i => i.collection == item.collection as SceneCollection && i.sceneIndex == item.index);

            public bool IsSelected(CollectionItem item) =>
                window.m_selectedCollections.Contains(item.collection as SceneCollection);

            public void Clear()
            {

                if (!window.m_selectedCollections.Any() && !window.m_selectedScenes.Any())
                    return;

                window.m_selectedCollections.Clear();
                window.m_selectedScenes.Clear();
                window.collections.Reload();

            }

        }

    }

}
