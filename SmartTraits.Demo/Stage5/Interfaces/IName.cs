using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage5
{
    [TraitInterface]
    interface IName
    {
        string FirstName { get; set; }
        string LastName { get; set; }
        string GetFullName();

    }
}
