using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage4
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
