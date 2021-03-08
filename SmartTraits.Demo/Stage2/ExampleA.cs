using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage2
{
    [AddTrait(typeof(NameTrait))]
    partial class ExampleA : BaseA, IName
    {
    }

    class BaseA { }
}
