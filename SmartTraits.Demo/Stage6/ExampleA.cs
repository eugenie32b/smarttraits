using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage6
{
    [AddTrait(typeof(NameTrait))]
    partial class ExampleA : BaseA, IName
    {
    }

    class BaseA { }
}
