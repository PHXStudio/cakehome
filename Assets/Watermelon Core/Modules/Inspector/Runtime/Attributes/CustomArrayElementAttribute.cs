using System;

namespace Watermelon
{
    // Marks a [Serializable] class/struct as opting into per-element custom rendering when used
    // as an array/list element (SerializedArrayGUIRenderer.ElementTypeHasCustomAttributes) — e.g.
    // to enable a GetCustomArrayTitle(int) title override, which is otherwise never invoked under
    // Unity's default array drawing.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class CustomArrayElementAttribute : Attribute { }
}
