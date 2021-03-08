using Microsoft.CodeAnalysis.CSharp.Syntax;
using SmartTraitsDefs;

namespace SmartTraits.Tests.Stage6
{
    [Trait]
    abstract partial class NameTrait : IName
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        [Overrideable]
        public string GetFullName()
        {
            return $"{FirstName} {LastName}";
        }

        [TraitProcess]
        public static string DemoProcessFeature(ClassDeclarationSyntax classNode)
        {
            return $"public string Get{classNode.Identifier}() {{ return \"{classNode.Identifier}\"; }}";
        }
    }
}
