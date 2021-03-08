using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage4
{
    [AddTrait(typeof(NameTrait))]
    [AddTrait(typeof(AddressTrait))]
    partial class ExampleB : BaseB, IName, IAddress, IAddrLabel
    {
        public string MiddleName { get; set; }

        private string GetNamePrefix()
        {
            return "sir/madam ";
        }

        public string GetLabel()
        {
            return "Address Label: ";
        }
    }

    class BaseB { }
}
