using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace SmartTraits
{
    public static class TraitProcessor
    {
        public static void ProcessTrait(GeneratorExecutionContext context, ClassDeclarationSyntax traitClass, Dictionary<string, ClassDeclarationSyntax> availableTraits, HashSet<string> generatedFiles)
        {
            StringBuilder sb = ProcessTraitInternal(context, traitClass, availableTraits);

            string sourceCodeToAdd = sb.ToString();

            if (!String.IsNullOrWhiteSpace(sourceCodeToAdd))
            {
                string generatedFileName = Utils.GetUniqueFileName(generatedFiles, Path.GetFileNameWithoutExtension(traitClass.SyntaxTree.FilePath));
                context.AddSource(generatedFileName, sourceCodeToAdd);

                generatedFiles.Add(generatedFileName);
            }
        }

        private static StringBuilder ProcessTraitInternal(GeneratorExecutionContext context, ClassDeclarationSyntax traitClass, Dictionary<string, ClassDeclarationSyntax> availableTraits)
        {
            // make sure it is abstract and partial class
            SyntaxTokenList modifiers = traitClass.Modifiers;
            if (!modifiers.Any(w => w.Kind() == SyntaxKind.PartialKeyword) || !modifiers.Any(w => w.Kind() == SyntaxKind.AbstractKeyword))
                return Utils.ReturnError($"in {traitClass.Identifier.ToString()}: Trait must be defined as an abstract and partial class");

            var sb = new StringBuilder();

            var namespaceNode = traitClass.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();

            string fullTraitClassName = (namespaceNode != null ? (namespaceNode.Name + ".") : "") + traitClass.Identifier;

            availableTraits[fullTraitClassName] = traitClass;

            if (traitClass.BaseList == null)
                return sb;

            var semanticModel = context.Compilation.GetSemanticModel(traitClass.SyntaxTree);

            if (Utils.IsStrictMode(traitClass))
            {
                // for strict method we need to skip for errors, because they are already added to the source
                // and also skip for method with the same name, but different signature
                if (!CheckStrictMode(traitClass, semanticModel, sb))
                    return sb;
            }

            AddProxy(traitClass, sb, semanticModel, namespaceNode);

            return sb;
        }

        private static bool CheckStrictMode(ClassDeclarationSyntax traitClass, SemanticModel semanticModel, StringBuilder sb)
        {
            InterfaceDeclarationSyntax traitInterface = Utils.GetTraitInterface(semanticModel, traitClass);
            if (traitInterface == null)
            {
                sb.AppendLine("#error in the strict mode you must have to implement an interface marked with TraitInterface attribute");
                return false;
            }

            List<MemberNodeInfo> interfaceMembers = Utils.GetAllMembersInfo(traitInterface);
            List<MemberNodeInfo> traitMembers = Utils.GetAllMembersInfo(traitClass);

            bool hasError = false;

            foreach (MemberNodeInfo traitMember in traitMembers)
            {
                if (!interfaceMembers.Any(w => w.Kind == traitMember.Kind
                                               && w.Name == traitMember.Name
                                               && w.ReturnType == traitMember.ReturnType
                                               && w.Arguments == traitMember.Arguments
                                               && w.GenericsCount == traitMember.GenericsCount)
                )
                {
                    hasError = true;
                    sb.AppendLine($"#error in the strict mode must have only members that implement a trait interface, but found {traitMember.Name} of kind {traitMember.Kind}");
                }
            }

            return !hasError;
        }

        private static void AddProxy(ClassDeclarationSyntax traitClass, StringBuilder sb, SemanticModel semanticModel, NamespaceDeclarationSyntax namespaceNode)
        {
            if (traitClass.BaseList == null)
                return;

            HashSet<string> addedProxies = new();

            if (!traitClass.BaseList.Types.Any())
                sb.AppendLine("#warning base list types is empty");

            foreach (var baseListType in traitClass.BaseList.Types)
            {
                TypeInfo typeInfo = ModelExtensions.GetTypeInfo(semanticModel, baseListType.Type);
                if (typeInfo.Type == null)
                    continue;

                SyntaxNode baseTypeNode = ModelExtensions.GetSymbolInfo(semanticModel, baseListType.Type).Symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                if (baseTypeNode == null)
                {
                    sb.AppendLine($"#error cannot find syntax node for {baseListType.Type}");
                    continue;
                }

                if (baseTypeNode.Kind() != SyntaxKind.InterfaceDeclaration)
                {
                    sb.AppendLine($"#error only interfaces are allowed for Trait base types, but got {baseListType.Type}");
                    continue;
                }

                ImmutableArray<AttributeData> typeAttrs = typeInfo.Type.GetAttributes();

                // don't add any proxy for the trait interface
                if (typeAttrs.Any(w => Consts.TraitInterfaceAttributes.Contains(w.AttributeClass?.Name)))
                    continue;

                foreach (ISymbol symbol in typeInfo.Type.GetMembers())
                {
                    if (addedProxies.Count == 0)
                    {
                        if (namespaceNode != null)
                            sb.AppendLine($"namespace {namespaceNode.Name} {{");

                        sb.AppendLine($"    {traitClass.Modifiers} class {traitClass.Identifier}: BaseProxy{traitClass.Identifier} {{ }}");
                        sb.AppendLine("");
                        sb.AppendLine($"    abstract class BaseProxy{traitClass.Identifier} {{");
                    }

                    SyntaxNode node = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                    if (node == null)
                        continue;

                    string proxyMember = "        public abstract " + node;

                    // might be already added as a part of another interface
                    if (addedProxies.Contains(proxyMember))
                        continue;

                    addedProxies.Add(proxyMember);

                    sb.AppendLine(proxyMember);
                }
            }

            if (addedProxies.Count > 0)
            {
                sb.AppendLine("}");

                if (namespaceNode != null)
                    sb.AppendLine("}");
            }
        }
    }
}
