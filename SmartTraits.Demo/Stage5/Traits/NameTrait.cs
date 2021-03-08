using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage5
{
    [Trait(TraitOptions.Strict)]
    abstract partial class NameTrait : IName
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        [Overrideable]
        public string GetFullName()
        {
            return $"{FirstName} {LastName}";
        }

        // if uncomment the method, it will product an error, because it is not in IName and NameTrait is in the Strict mode.
        //public void NameExtraMethod()
        //{

        //}
    }
}
