using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage2
{
    [AddTrait(typeof(NameTrait))]
    [AddTrait(typeof(AddressTrait))]
    partial class ExampleB : BaseB, IName, IAddress
    {
    }

    class BaseB { }
}
