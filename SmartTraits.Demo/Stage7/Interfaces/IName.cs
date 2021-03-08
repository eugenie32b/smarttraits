using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage7
{
    [TraitInterface]
    interface IName
    {
        string FirstName { get; set; }
        string LastName { get; set; }
        string GetFullName();

    }
}
