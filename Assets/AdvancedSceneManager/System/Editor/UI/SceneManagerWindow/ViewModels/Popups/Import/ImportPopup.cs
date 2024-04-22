using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        [SerializeField] private VisualTreeAsset importSceneTemplate = null!;

        interface INotificationPopup
        {
            public void ReloadNotification();
        }

        abstract class ImportPopup<T, TSelf> : ViewModel, IPopup, INotificationPopup
            where TSelf : ViewModel, IPopup, new()
        {

            public class Item
            {
                public T value;
                public bool isChecked;
            }

            #region Notification

            public abstract string GetNotificationText(int count);

            string notificationID => GetType().FullName;

            public void ReloadNotification() =>
                SceneManager.OnInitialized(() =>
                {

                    SceneImportUtility.scenesChanged -= ReloadNotification;
                    SceneImportUtility.scenesChanged += ReloadNotification;

                    ReloadItems();

                    var count = items.Count();
                    var hasItems = count > 0;

                    Notify(hasItems && Profile.current, GetNotificationText(count));

                    void Notify(bool show, string message)
                    {

                        if (!SceneManagerWindow.window)
                            return;

                        SceneManagerWindow.window.notifications.Remove(notificationID);

                        if (show)
                            SceneManagerWindow.window.notifications.Notify(
                                                        message: message,
                                                        id: notificationID,
                                                        onClick: SceneManagerWindow.window.popups.Open<TSelf>,
                                                        canDismiss: false);

                    }

                });

            #endregion
            #region Header

            public abstract string headerText { get; }
            public abstract bool displayAutoImportField { get; }
            public virtual string subtitleText { get; }

            void SetupHeader()
            {

                element.Q<Label>("label-title").text = headerText;
                element.Q<Label>("label-subtitle").text = subtitleText;
                element.Q<Label>("label-subtitle").SetVisible(!string.IsNullOrWhiteSpace(subtitleText));

                SetupAutoImportOption();

            }

            #endregion
            #region Import option field

            void SetupAutoImportOption()
            {

                var importPopup = element.Q("import-option-field");
                importPopup.SetVisible(displayAutoImportField);

                importPopup.BindToSettings();
                importPopup.SetEnabled(true);
                importPopup.tooltip =
                    "Manual:\n" +
                    "Manually import each scene.\n\n" +
                    "SceneCreated:\n" +
                    "Import scenes when they are created.";

            }

            #endregion
            #region List

            ListView list;

            public Item[] items { get; private set; }

            public abstract IEnumerable<T> GetItems();

            public void ReloadItems()
            {
                items = GetItems().Select(i => new Item() { value = i, isChecked = items?.FirstOrDefault(i2 => EqualityComparer<T>.Default.Equals(i2.value, i))?.isChecked ?? true }).ToArray();
                if (list is not null)
                {
                    list.itemsSource = items;
                    element.Q("label-no-items").SetVisible(items.Length == 0);
                    list.Rebuild();
                }
            }

            public abstract void SetupItem(VisualElement element, Item item, int index, out string text);

            void SetupItem(VisualElement element, int index)
            {

                var item = items[index];

                var toggle = element.Q<Toggle>();

                element.RegisterCallback<ClickEvent>(e =>
                {
                    if (e.target is not Toggle)
                        toggle.value = !toggle.value;
                });

                toggle.SetValueWithoutNotify(item.isChecked);
                _ = toggle.RegisterValueChangedCallback(e =>
                {
                    item.isChecked = e.newValue;
                    ReloadButtons();
                });

                SetupItem(element, item, index, out var text);
                element.Q<Label>("label-item-text").text = text;

            }

            void SetupList()
            {

                list = element.Q<ListView>();
                list.itemsSource = items;
                list.makeItem = window.importSceneTemplate.Instantiate;

                list.bindItem = SetupItem;
                SceneManager.OnInitialized(ReloadItems);

            }

            #endregion
            #region Footer

            Button button1;
            Button button2;

            public abstract string button1Text { get; }
            public virtual string button2Text { get; }

            void SetupFooter()
            {
                SetupFooterButton("button-cancel", "Cancel", OnCancelClick);
                SetupFooterButton(ref button1, "button-1", button1Text, () => OnButton1Click(items.Where(i => i.isChecked).Select(i => i.value)));
                SetupFooterButton(ref button2, "button-2", button2Text, () => OnButton2Click(items.Where(i => i.isChecked).Select(i => i.value)));
            }

            void SetupFooterButton(string name, string text, Action click)
            {
                Button button = null;
                SetupFooterButton(ref button, name, text, click);
            }

            void SetupFooterButton(ref Button button, string name, string text, Action click)
            {

                button = element.Q<Button>(name);
                button.text = text;
                button.SetVisible(!string.IsNullOrWhiteSpace(text));

                button.clicked += () =>
                {
                    click.Invoke();
                    _ = window.popups.Close();
                    SceneManager.OnInitialized(ReloadNotification);
                };

            }

            public void ReloadButtons()
            {
                var isEnabled = items?.Count(i => i.isChecked) > 0;
                button1.SetEnabled(isEnabled);
                button2.SetEnabled(isEnabled);
            }

            public abstract void OnButton1Click(IEnumerable<T> items);
            public virtual void OnButton2Click(IEnumerable<T> items) { }
            public virtual void OnCancelClick() { }

            #endregion

            public override void OnCreateGUI(VisualElement element)
            {

                ReloadItems();
                SetupHeader();
                SetupFooter();
                SetupList();

                ReloadButtons();

                SceneImportUtility.scenesChanged += Reload;

            }

            public override void OnRemoved() =>
                SceneImportUtility.scenesChanged -= Reload;

            public override void OnEnable() =>
                Reload();

            public void Reload()
            {
                ReloadItems();
                SetupList();
                ReloadButtons();
            }

        }

    }

}
