using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace SmartTraits
{
    public static class AddTraitProcessor
    {
        public static StringBuilder ProcessAddTrait(GeneratorExecutionContext context, HashSet<string> generatedFiles, HashSet<string> alreadyProcessedT4, T4Processor t4Processor, AttributeSyntax addTraitAttr, SemanticModel semanticModel, ClassDeclarationSyntax destClass, HashSet<string> alreadyProcessedTraits)
        {
            StringBuilder sb = new();

            var firstParam = addTraitAttr.ArgumentList?.Arguments.FirstOrDefault();

            var typeofTraitNode = firstParam?.DescendantNodes().OfType<TypeOfExpressionSyntax>().FirstOrDefault();
            if (typeofTraitNode == null)
                return Utils.ReturnError($"AddTrait param must be typeof(Type), but got {firstParam}");

            TypeInfo addTraitType = semanticModel.GetTypeInfo(typeofTraitNode.Type);
            string traitClassName = addTraitType.Type?.ToDisplayString();

            if (addTraitType.Type == null)
                return Utils.ReturnError($"the specified type \"{traitClassName}\" doesn't have any attributes");

            if (alreadyProcessedTraits.Contains(traitClassName))
                return Utils.ReturnError($"the trait \"{traitClassName}\" was already procesed, cannot have duplicate traits");

            alreadyProcessedTraits.Add(traitClassName);

            if (!addTraitType.Type.GetAttributes().Any(w => Consts.TraitAttributes.Contains(w.AttributeClass?.Name)))
                return Utils.ReturnError($"the specified type \"{traitClassName}\" doesn't have Trait attribute");


            SyntaxNode traitTypeSyntax = semanticModel.GetSymbolInfo(typeofTraitNode.Type).Symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

            if (traitTypeSyntax is not ClassDeclarationSyntax traitClassNode)
                return Utils.ReturnError($"the specified Trait \"{traitClassName}\" is not a class");

            var traitCompUnit = traitClassNode.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();

            if (traitCompUnit == null)
                return Utils.ReturnError($"cannot find computational unit for the class node {traitClassNode.Identifier}");


            string destinationGenerics = Utils.RemoveWhitespace(destClass.TypeParameterList?.ToString());
            string traitGenerics = Utils.RemoveWhitespace(traitClassNode.TypeParameterList?.ToString());

            if (traitGenerics != destinationGenerics)
                return Utils.ReturnError($"Generics definition should be exactly the same for Trait and destination classes, but got {Utils.RemoveNewLine(traitClassNode.TypeParameterList?.ToString())} and {Utils.RemoveNewLine(destClass.TypeParameterList?.ToString())}");

            string destConstraints = Utils.RemoveWhitespace(destClass.ConstraintClauses.ToString());
            string traitConstraints = Utils.RemoveWhitespace(traitClassNode.ConstraintClauses.ToString());

            if (traitConstraints != destConstraints)
                return Utils.ReturnError($"Constraints definitions should be exactly the same for Trait and destination classes, but got {Utils.RemoveNewLine(traitClassNode.ConstraintClauses.ToString())} and {Utils.RemoveNewLine(destClass.ConstraintClauses.ToString())}");


            bool hadUsings = false;
            foreach (var traitUsingDirective in traitCompUnit.Usings.Where(w => !Consts.IgnoreUsings.Contains(w.Name.ToString())))
            {
                sb.AppendLine(traitUsingDirective.ToString());
                hadUsings = true;
            }

            if (hadUsings)
                sb.AppendLine("");

            var traitNamespaceNode = traitClassNode.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();

            if (traitNamespaceNode != null)
            {
                sb.AppendLine($"namespace {traitNamespaceNode.Name} {{");

                hadUsings = false;
                foreach (var traitNamespaceUsing in traitNamespaceNode.Usings.Where(w => !Consts.IgnoreUsings.Contains(w.Name.ToString())))
                {
                    sb.AppendLine(traitNamespaceUsing.ToString());
                    hadUsings = true;
                }

                if (hadUsings)
                    sb.AppendLine("");
            }

            sb.AppendLine($"    {destClass.Modifiers} class {destClass.Identifier} {{");

            // process all destination class local T4 templates
            ProcessT4Local(destClass.Identifier.ToString(), destClass, destClass, context, t4Processor, alreadyProcessedT4, sb);

            // process all trait class local T4 templates
            ProcessT4Local(traitClassNode.Identifier.ToString(), traitClassNode, destClass, context, t4Processor, alreadyProcessedT4, sb);


            // process all destination members' nodes local T4 templates
            foreach (MemberDeclarationSyntax destMemberNode in destClass.Members)
            {
                ProcessT4Local(GetMemberNodeId(destMemberNode), destMemberNode, destClass, context, t4Processor, alreadyProcessedT4, sb);
            }

            List<MemberNodeInfo> destMembersInfo = Utils.GetAllMembersInfo(destClass);

            foreach (MemberDeclarationSyntax traitMemberNode in traitClassNode.Members)
            {
                AttributeSyntax[] traitMemberAttrs = traitMemberNode.AttributeLists.SelectMany(s => s.Attributes).ToArray();

                // process local T4 templates for trait members
                ProcessT4Local(GetMemberNodeId(traitMemberNode), traitMemberNode, destClass, context, t4Processor, alreadyProcessedT4, sb);

                // if has an ignore attribute, skip the member
                if (traitMemberAttrs.Any(w => Consts.TraitIgnoreAttributes.Contains(w.Name.ToString())))
                    continue;

                // if has process attribute, execute and add to the class or to a new file, depending on the attribute option
                AttributeSyntax processAttr = traitMemberAttrs.FirstOrDefault(w => Consts.TraitProcessAttributes.Contains(w.Name.ToString()));

                if (processAttr != null)
                {
                    if (CheckProcessMethod(sb, traitMemberNode, "Process", out MethodDeclarationSyntax methodNode))
                        ExecuteMethod(context, generatedFiles, destClass, traitClassName, methodNode, processAttr, sb);

                    continue;
                }

                bool canBeOverriden = traitMemberAttrs.Any(w => Consts.OverrideableAttributes.Contains(w.Name.ToString()));

                List<MemberNodeInfo> traitMemberInfos = Utils.GetMemberNodeInfo(traitMemberNode);

                MemberNodeInfo[] sameNameCandidates = destMembersInfo.Where(w => traitMemberInfos.Any(a => a.Name == w.Name)).ToArray();

                // check if destination class has members with the same name (and the same signature for methods)
                if (sameNameCandidates.Length > 0 && SkipToOverride(sameNameCandidates, traitMemberInfos, sb, traitMemberNode, canBeOverriden))
                    continue;

                var outputNode = traitMemberNode;

                if (canBeOverriden)
                    outputNode = Utils.RemoveAttributes(traitMemberNode, Consts.OverrideableAttributes);

                sb.Append(outputNode.ToFullString());
            }

            sb.AppendLine("    }");

            if (traitNamespaceNode != null)
                sb.AppendLine("}");

            return sb;
        }

        private static string GetMemberNodeId(MemberDeclarationSyntax memberNode)
        {
            if (memberNode is MethodDeclarationSyntax methodNode)
                return methodNode.Identifier.ToString();
            else if (memberNode is PropertyDeclarationSyntax propertyNode)
                return propertyNode.Identifier.ToString();
            else if (memberNode is FieldDeclarationSyntax fieldNode)
                return fieldNode.Declaration.Variables.ToString();

            return "";
        }

        private static void ProcessT4Local(string id, MemberDeclarationSyntax memberNode, ClassDeclarationSyntax classNode, GeneratorExecutionContext context, T4Processor t4Processor, HashSet<string> alreadyProcessedT4, StringBuilder sb)
        {
            if (memberNode.AttributeLists == null)
                return;

            foreach (AttributeSyntax t4Attr in memberNode.GetAttributesOfType(Consts.ApplyT4Attributes))
            {
                if (!t4Attr.HasAttributeParameter(Consts.T4TemplateScopeEnums, "Local"))
                    continue;

                context.CancellationToken.ThrowIfCancellationRequested();

                if (t4Attr.ArgumentList != null && t4Attr.ArgumentList.Arguments.Count > 0)
                {
                    string templateName = t4Attr.ArgumentList.Arguments[0].ToString().Trim('\"');

                    string processedName = (classNode.SyntaxTree.FilePath + "::" + id + "::" + templateName).ToLower();

                    // apply to the source only once
                    if (alreadyProcessedT4.Contains(processedName))
                        continue;

                    t4Processor.ProcessTemplate(context, t4Attr, classNode, sb);

                    alreadyProcessedT4.Add(processedName);
                }
            }
        }

        private static bool SkipToOverride(MemberNodeInfo[] sameNameCandidates, List<MemberNodeInfo> traitMemberInfos, StringBuilder sb, MemberDeclarationSyntax traitMemberNode, bool canBeOverriden)
        {
            // not the same kind 
            if (sameNameCandidates.Any(w => traitMemberInfos.Any(a => a.Kind != w.Kind)))
            {
                sb.AppendLine($"#error type of members doesn't match trait has {traitMemberInfos.First().Kind} and destination class has {sameNameCandidates.First().Kind}");
                return true;
            }

            // for methods, validate method signature
            if (traitMemberNode is MethodDeclarationSyntax)
            {
                // we know the memberInfos has only one entry for methods
                MemberNodeInfo methodInfo = traitMemberInfos.First();

                bool hasExactMatch = sameNameCandidates.Any(w => w.ReturnType == methodInfo.ReturnType
                                                                    && w.Arguments == methodInfo.Arguments
                                                                    && w.GenericsCount == methodInfo.GenericsCount);

                if (hasExactMatch)
                {
                    // add error message if could not be overriden
                    if (!canBeOverriden)
                        sb.AppendLine($"#error a method of kind {traitMemberNode.Kind()} has a member with the same name in destination class and trait's member is not marked as overrideable");

                    return true;
                }
            }
            else
            {
                // add error message if could not be overriden
                if (!canBeOverriden)
                    sb.AppendLine($"#error a trait member of kind {traitMemberNode.Kind()} has a member with the same name \"{traitMemberInfos.First().Name}\" in destination class and trait's member is not marked as overrideable");

                return true;
            }

            return false;
        }

        private static void ExecuteMethod(GeneratorExecutionContext context, HashSet<string> generatedFiles, ClassDeclarationSyntax destClass, string traitClassName, MethodDeclarationSyntax methodNode, AttributeSyntax processAttr, StringBuilder sb)
        {
            (string source, ImmutableArray<Diagnostic> diagnostics) = ExecutionProcessor.Process(traitClassName, methodNode, destClass);

            if (diagnostics != null)
            {
                foreach (Diagnostic diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }

            bool includeSourcesToClass = !Utils.HasAttributeParameter(processAttr, Consts.TraitProcessEnums, "Global");

            if (includeSourcesToClass)
            {
                sb.AppendLine(source);
            }
            else
            {
                Utils.AddToGeneratedSources(context, generatedFiles, null, sb, traitClassName + "." + methodNode?.Identifier);
            }
        }

        private static bool CheckProcessMethod(StringBuilder sb, MemberDeclarationSyntax memberNode, string typeName, out MethodDeclarationSyntax methodNode)
        {
            methodNode = null;

            if (memberNode.Kind() != SyntaxKind.MethodDeclaration)
            {
                sb.AppendLine($"#error can have {typeName} attribute only for a method");
                return false;
            }

            methodNode = memberNode as MethodDeclarationSyntax;
            if (methodNode == null)
                return false;

            if (!methodNode.Modifiers.Any(w => w.Kind() == SyntaxKind.StaticKeyword))
            {
                sb.AppendLine($"#error a {typeName} method {methodNode.Identifier} must be a static method");
                return false;
            }

            if (!methodNode.Modifiers.Any(w => w.Kind() == SyntaxKind.PublicKeyword))
            {
                sb.AppendLine($"#error a {typeName} method {methodNode.Identifier} must be a public method");
                return false;
            }

            return true;
        }
    }
}
