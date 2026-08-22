using System;

namespace Watermelon
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AdsEditorContainerAttribute : Attribute
    {
        public Type ContainerType { get; }

        public AdsEditorContainerAttribute(Type containerType)
        {
            ContainerType = containerType;
        }
    }
}
