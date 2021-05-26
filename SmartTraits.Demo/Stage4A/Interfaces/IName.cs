using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage4A
{
    [TraitInterface]
    interface IName
    {
        string FirstName { get; set; }
        string LastName { get; set; }
        string GetFullName();

    }
}
