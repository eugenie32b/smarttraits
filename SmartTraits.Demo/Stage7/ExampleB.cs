using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage7
{
    [AddTrait(typeof(NameTrait))]
    [AddTrait(typeof(AddressTrait))]
    [ApplyT4("DemoTemplate", Scope = T4TemplateScope.Local, VerbosityLevel = T4GeneratorVerbosity.Debug )]
    partial class ExampleB : BaseB, IName, IAddress
    {
    }

    class BaseB { }
}
