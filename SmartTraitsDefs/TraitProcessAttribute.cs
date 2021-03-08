using System;

namespace SmartTraitsDefs
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class TraitProcessAttribute : Attribute
    {
        public ProcessOptions Options { get; set; }

        public TraitProcessAttribute(ProcessOptions options = ProcessOptions.Include)
        {
            Options = options;
        }
    }

    public enum ProcessOptions
    {
        Include = 1,
        Global = 2,
    }
}
