using System;

namespace SmartTraitsDefs
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class OverrideableAttribute: System.Attribute
    {
    }
}
