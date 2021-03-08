using System;

namespace SmartTraitsDefs
{

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public class ApplyT4Attribute : Attribute
    {
        public const string DEFAULT_LOG_FILE = "%TEMP%/T4Trait.log";

        public string T4FileName { get; set; }
        public T4GeneratorVerbosity VerbosityLevel { get; set; }
        public string ExtraTag { get; set; }
        public string LogFile { get; set; } = DEFAULT_LOG_FILE;

        public T4TemplateScope Scope { get; set; } = T4TemplateScope.Global;

        public ApplyT4Attribute(string t4FileName)
        {
            T4FileName = t4FileName;
        }
    }

    public enum T4TemplateScope
    {
        Local = 1,
        Global = 2,
    }

    public enum T4GeneratorVerbosity
    {
        None = 0,
        Critical,
        Error,
        Warning,
        Info,
        Debug
    }

}
