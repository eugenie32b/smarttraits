using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage4A
{
    [AddTrait(typeof(NameTrait))]
    partial class ExampleA : BaseA, IName
    {
        private string GetNamePrefix()
        {
            return "mr/mrs. ";
        }
    }

    class BaseA { }
}
