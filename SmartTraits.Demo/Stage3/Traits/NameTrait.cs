using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage3
{
    [Trait]
    abstract partial class NameTrait: IName
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        [Overrideable]
        public string GetFullName()
        {
            return $"{FirstName} {LastName}";
        }
    }
}
