using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        abstract class ViewManager<T> : ViewModel
        {

            public override void OnEnable() => activeView?.model?.OnEnable();
            public override void OnDisable() => activeView?.model?.OnDisable();
            public override void OnFocus() => activeView?.model?.OnFocus();
            public override void OnLostFocus() => activeView?.model?.OnLostFocus();
            public override void OnRemoved() => activeView?.model?.OnRemoved();

            public override void OnSizeChanged() =>
                activeView?.model?.OnSizeChanged();

            public (ViewModel model, VisualElement element)? activeView { get; private set; }

            protected VisualElement parent { get; private set; }
            protected VisualElement effectiveParent { get; private set; }
            protected Dictionary<Type, VisualTreeAsset> templates { get; private set; }

            protected abstract Dictionary<Type, VisualTreeAsset> GetTemplates();
            protected abstract VisualElement GetParent();

            protected virtual Task OnOpen(T model, VisualElement element, object parameter = null) => Task.CompletedTask;
            protected virtual Task OnClose(T model, VisualElement element, bool hasNewView) => Task.CompletedTask;

            public override void OnCreateGUI(VisualElement element)
            {

                parent = GetParent();
                templates = GetTemplates();

                effectiveParent = (parent?.Query().Children<ScrollView>() ?? parent?.Q("effectiveParent")) ?? parent;

                if (parent is not null)
                    parent.pickingMode = PickingMode.Ignore;

            }

            public async Task Close()
            {

                if (activeView != null)
                {
                    activeView.Value.model.OnRemoved();
                    await AnimateAndRemove((T)(object)activeView?.model, activeView?.element, false);
                    effectiveParent?.Clear();
                }

                if (parent is not null)
                    parent.pickingMode = PickingMode.Ignore;

                activeView = null;

            }

            protected bool TryOpen<TPage>(object param = null) =>
                TryOpen(typeof(TPage), param);

            protected bool TryOpen(Type type, object param = null)
            {

                if (type.IsAbstract || !typeof(ViewModel).IsAssignableFrom(type) || !typeof(T).IsAssignableFrom(type))
                    return false;

                OpenInternal(type, param);
                return true;

            }

            protected virtual async Task AnimateAndRemove(T model, VisualElement element, bool hasNewView)
            {
                if (element is not null)
                    await OnClose(model, element, hasNewView);
                element?.RemoveFromHierarchy();
            }

            public void Open<TModel>() where TModel : ViewModel, T, new() =>
                Open<TModel>(null);

            public void Open<TModel>(object parameter) where TModel : ViewModel, T, new() =>
                OpenInternal(typeof(TModel), parameter);

            async void OpenInternal(Type type, object parameter)
            {

                var model = (ViewModel)Activator.CreateInstance(type);
                var element = CreateElement();

                if (element is not null)
                    element.style.bottom = 0;

                parent.pickingMode = PickingMode.Position;

                var previousView = activeView;
                activeView = (model, element);

                if (previousView != null)
                    await AnimateAndRemove((T)(object)previousView?.model, previousView?.element, true);

                await OnOpen((T)(object)model, element, parameter);

                VisualElement CreateElement()
                {

                    var template = templates[type];
                    if (!template)
                        return null;

                    var element = template.Instantiate();
                    effectiveParent?.Add(element);

                    model.element = element;
                    model.OnCreateGUI(element);
                    model.OnCreateGUI(element, parameter);

                    return element;

                }

            }

        }

    }

}
