using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage3
{
    [AddTrait(typeof(NameTrait))]
    partial class ExampleA : BaseA, IName
    {
    }

    class BaseA { }
}
