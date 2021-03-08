using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage5
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
