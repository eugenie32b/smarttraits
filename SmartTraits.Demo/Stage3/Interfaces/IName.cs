using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage3
{
    [TraitInterface]
    interface IName
    {
        string FirstName { get; set; }
        string LastName { get; set; }
        string GetFullName();

    }
}
