#pragma warning disable IDE0017 // Simplify object initialization
#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.Linq;
using AdvancedSceneManager.Editor.Utility;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    public class GenericPopup : Popup<GenericPopup>
    {

        public override string path => "AdvancedSceneManager/Popups/PickTag/Popup";

        /// <summary>Represents an <see cref="Item"/> separator. default keyword can also be used.</summary>
        public static Item Separator => default;

        public class Item
        {

            public static Item Separator => new Item();

            public string name { get; set; }
            public bool isChecked { get; set; }
            public Action<bool> onClick { get; set; }
            public bool isCheckable { get; set; }
            public bool isEnabled { get; set; }
            public bool isVisible { get; set; } = true;
            public bool isBold { get; set; }

            public bool isSeparator => Equals(this, Separator);

            public static Item Create(string name) =>
                new Item() { name = name, isEnabled = true };

            public static Item Create(string name, Action onClick) =>
                Create(name).WhenClicked(onClick);

            public Item AsCheckable() => Set(item => item.isCheckable = true);
            public Item AsCheckable(Action<bool> onCheckedChanged) => Set(item => { item.isCheckable = true; item.onClick = onCheckedChanged; });
            public Item WithCheckedStatus(bool isChecked) => Set(item => item.isChecked = isChecked);
            public Item WithEnabledState(bool isEnabled) => Set(item => item.isEnabled = isEnabled);
            public Item WhenClicked(Action action) => Set(item => item.onClick = (_) => action?.Invoke());
            public Item WithVisibleState(bool isVisible) => Set(item => item.isVisible = isVisible);
            public Item WithBoldState(bool isBold) => Set(item => item.isBold = isBold);

            Item Set(Action<Item> action)
            {
                action.Invoke(this);
                return this;
            }

        }

        Item[] items;
        public void Refresh(params Item[] items)
        {

            var list = items.OfType<Item>().Where(i => i.isSeparator || i.isVisible);
            if (Equals(list.FirstOrDefault(), Item.Separator))
                list = list.Skip(1);
            if (Equals(list.LastOrDefault(), Item.Separator))
                list = list.Reverse().Skip(1).Reverse();

            this.items = list.ToArray();
            rootVisualElement.Clear();
            foreach (var item in this.items)
            {

                if (string.IsNullOrWhiteSpace(item.name))
                {

                    if (!item.isVisible)
                        continue;

                    var separator = new VisualElement();
                    separator.style.height = 2;
                    separator.style.SetMargin(vertical: 2);
                    separator.style.backgroundColor = Color.gray;
                    rootVisualElement.Add(separator);

                }
                else
                {
                    var button = new ToolbarToggle();
                    button.AddToClassList("MenuItem");
                    button.text = item.name;
                    button.SetEnabled(item.isEnabled);

                    if (item.isBold)
                        button.style.unityFontStyleAndWeight = FontStyle.Bold;

                    _ = button.RegisterValueChangedCallback(e =>
                    {
                        if (!item.isCheckable)
                            button.SetValueWithoutNotify(false);
                        Close();
                        item.onClick?.Invoke(e.newValue);
                    });

                    rootVisualElement.Add(button);

                }

            }
        }

        protected override void OnReopen(GenericPopup newPopup) =>
            newPopup.Refresh(items);

    }

}
