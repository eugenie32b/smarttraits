using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage4A
{
    [AddTrait(typeof(PersonalAddressTrait))]
    [AddTrait(typeof(AddressTrait))]
    partial class ExampleC : BaseC, IName, IAddress
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

        public string GetFullLable()
        {
            return "Full Address Label: \n" + GetFullAddress(); 
        }
    }

    class BaseC { }
}
