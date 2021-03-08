using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage3
{
    [AddTrait(typeof(NameTrait))]
    [AddTrait(typeof(AddressTrait))]
    partial class ExampleB : BaseB, IName, IAddress
    {
        public string MiddleName { get; set;  }

        public string GetFullName()
        { 
            return $"{FirstName} {MiddleName} {LastName}";
        }
    }

    class BaseB { }
}
