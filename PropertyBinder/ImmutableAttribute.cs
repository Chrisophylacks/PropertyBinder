using System;

namespace PropertyBinder
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class ImmutableAttribute : Attribute
    {
    }
}