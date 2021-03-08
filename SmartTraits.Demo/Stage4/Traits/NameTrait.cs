using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage4
{
    [Trait]
    abstract partial class NameTrait : IName
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        [Overrideable]
        public string GetFullName()
        {
            return $"{GetNamePrefix()} {FirstName} {LastName}";
        }

        [TraitIgnore]
        private string GetNamePrefix()
        {
            return null;
        }
    }
}
