using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage1
{
    [AddTrait(typeof(NameTrait))]
    [AddTrait(typeof(AddressTrait))]
    partial class ExampleB : BaseB
    {
    }

    class BaseB { }
}
