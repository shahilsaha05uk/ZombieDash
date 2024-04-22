#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    /// <summary>Object field with type property that can be set from uxml. Also has <see cref="isReadOnly"/> property to allow selecting or opening value, but not allow changing value.</summary>
    public class ObjectField : UnityEditor.UIElements.ObjectField
    {

        bool m_isReadOnly;
        public bool isReadOnly
        {
            get => m_isReadOnly;
            set { m_isReadOnly = value; ApplyReadOnly(); }
        }

        public ObjectField() : base() =>
            ApplyReadOnly();

        void ApplyReadOnly() =>
            this.Q(className: "unity-object-field__selector").style.display = isReadOnly ? DisplayStyle.None : DisplayStyle.Flex;

        #region Factory

        public new class UxmlFactory : UxmlFactory<ObjectField, UxmlTraits>
        { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            readonly UxmlStringAttributeDescription m_label = new UxmlStringAttributeDescription() { name = "label" };
            readonly UxmlStringAttributeDescription m_type = new UxmlStringAttributeDescription() { name = "type" };
            readonly UxmlBoolAttributeDescription m_allowSceneObjects = new UxmlBoolAttributeDescription() { name = "allowSceneObjects" };
            readonly UxmlBoolAttributeDescription m_isReadOnly = new UxmlBoolAttributeDescription() { name = "isReadOnly" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {

                base.Init(ve, bag, cc);

                var element = ve as ObjectField;
                element.label = m_label.GetValueFromBag(bag, cc);

                var typeName = m_type.GetValueFromBag(bag, cc);
                if (AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName == typeName) is Type type)
                    element.objectType = type;

                element.allowSceneObjects = m_allowSceneObjects.GetValueFromBag(bag, cc);
                element.isReadOnly = m_isReadOnly.GetValueFromBag(bag, cc);

            }

        }

        #endregion

    }

}
