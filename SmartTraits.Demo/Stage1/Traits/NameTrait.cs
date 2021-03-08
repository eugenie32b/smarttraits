using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage1
{
    [Trait]
    abstract partial class NameTrait
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string GetFullName()
        {
            return $"{FirstName} {LastName}";
        }
    }
}
