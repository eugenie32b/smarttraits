using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage2
{
    [Trait]
    abstract partial class NameTrait: IName
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string GetFullName()
        {
            return $"{FirstName} {LastName}";
        }
    }
}
