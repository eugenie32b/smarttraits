using System;
using System.Collections.Generic;
using System.Text;
using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage4A
{
    [Trait]
    abstract partial class PersonalAddressTrait : AddressTrait, IName, IAddress
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        [Overrideable]
        public string GetFullName()
        {
            return $"{GetNamePrefix()} {FirstName} {LastName}";
        }

        public string GetFullAddress()
        {
            return $"{GetNamePrefix()} {FirstName} {LastName} \n {Address} \n {City}, {State} \t {ZipCode}";
        }


        [TraitIgnore]
        private string GetNamePrefix()
        {
            return null;
        }
    }
}
