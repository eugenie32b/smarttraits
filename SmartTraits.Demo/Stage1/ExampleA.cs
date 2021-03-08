using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage1
{
    [AddTrait(typeof(NameTrait))]
    partial class ExampleA : BaseA
    {
    }

    class BaseA { }
}
