using System;

namespace SmartTraitsDefs
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class TraitAttribute : System.Attribute
    {
        public TraitOptions Options { get; set; }

        public TraitAttribute(TraitOptions options = TraitOptions.Normal)
        {
            Options = options;
        }
    }
}
