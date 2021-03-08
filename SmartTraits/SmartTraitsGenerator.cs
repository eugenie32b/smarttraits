using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartTraits
{
    [Generator]
    public class SmartTraitsGenerator : ISourceGenerator
    {
        private T4Processor _t4Processor;
        private T4Processor T4Processor
        {
            get
            {
                if (_t4Processor == null)
                    _t4Processor = new T4Processor();

                return _t4Processor;
            }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            HashSet<string> generatedFiles = new();

            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            if (!receiver.AddTraitCandidates.Any())
                Utils.AddToGeneratedSources(context, generatedFiles, null, new StringBuilder("// no any candidates"));

            try
            {
                HashSet<string> alreadyProcessedT4 = new();
                Dictionary<string, ClassDeclarationSyntax> availableTraits = new();

                foreach (ClassDeclarationSyntax traitClass in receiver.TraitCandidates)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    TraitProcessor.ProcessTrait(context, traitClass, availableTraits, generatedFiles);
                }

                foreach (ClassDeclarationSyntax destClass in receiver.AddTraitCandidates)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    var sb = new StringBuilder();

                    // make sure it is partial class
                    if (destClass.Modifiers.Any(w => w.Kind() == SyntaxKind.PartialKeyword))
                    {
                        HashSet<string> alreadyProcessedTraits = new();

                        var semanticModel = context.Compilation.GetSemanticModel(destClass.SyntaxTree);

                        foreach (AttributeSyntax addTraitAttr in destClass.AttributeLists.SelectMany(s => s.Attributes).Where(w => Consts.AddTraitAttributes.Contains(w.Name.ToString())))
                        {
                            context.CancellationToken.ThrowIfCancellationRequested();

                            StringBuilder processResult = AddTraitProcessor.ProcessAddTrait(context, generatedFiles, alreadyProcessedT4, T4Processor, addTraitAttr, semanticModel, destClass, alreadyProcessedTraits);
                            sb.Append(processResult);
                        }
                    }
                    else
                    {
                        sb.AppendLine($"#error in {destClass.Identifier}: to add a trait, the class must be defined as a partial");
                    }

                    Utils.AddToGeneratedSources(context, generatedFiles, destClass, sb);
                }

                foreach (MemberDeclarationSyntax memberCandidate in receiver.T4Candidates)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    var sb = new StringBuilder();

                    foreach (var attr in memberCandidate.AttributeLists.SelectMany(s => s.Attributes).Where(w => Consts.ApplyT4Attributes.Contains(w.Name.ToString())))
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();

                        // for local T4, we process them in the AddTrait
                        if (attr.HasAttributeParameter(Consts.T4TemplateScopeEnums, "Local"))
                        {
                            var classNode = memberCandidate.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                            if (classNode == null)
                            {
                                sb.AppendLine($"#error cannot find a class for T4 transformation marked as local");
                            }
                            else if (!classNode.HasAttributeOfType(Consts.AddTraitAttributes))
                            {
                                sb.AppendLine($"#error a class for T4 transformation marked as local, must have AddTrait attribute");
                            }

                            continue;
                        }

                        T4Processor.ProcessTemplate(context, attr, memberCandidate, sb);

                        Utils.AddToGeneratedSources(context, generatedFiles, memberCandidate, sb);
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder sb = Utils.LogExceptionAsComments(ex, $"cannot generate sources, please check logs. Exception {Utils.RemoveNewLine(ex.GetType().FullName)}");
                if (ex.InnerException != null)
                {
                    sb.AppendLine(Utils.LogExceptionAsComments(ex.InnerException, $"    inner exception . Exception {Utils.RemoveNewLine(ex.InnerException.GetType().FullName)}").ToString());
                }

                Utils.AddToGeneratedSources(context, generatedFiles, null, sb, "Exceptions");
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public class SyntaxReceiver : ISyntaxReceiver
        {
            public HashSet<ClassDeclarationSyntax> AddTraitCandidates { get; } = new();
            public HashSet<ClassDeclarationSyntax> TraitCandidates { get; } = new();
            public HashSet<MemberDeclarationSyntax> T4Candidates { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is not MemberDeclarationSyntax member)
                    return;

                AttributeSyntax[] attrs = member.AttributeLists.SelectMany(s => s.Attributes).ToArray();

                if (attrs.Any(w => Consts.ApplyT4Attributes.Contains(w.Name.ToString())))
                    T4Candidates.Add(member);

                if (syntaxNode is not ClassDeclarationSyntax declarator)
                    return;

                bool hasTraitAttribute = declarator.AttributeLists.Any(a => a.Attributes.Any(w => Consts.TraitAttributes.Contains(w.Name.ToString())));

                if (hasTraitAttribute)
                {
                    TraitCandidates.Add(declarator);
                }
                else
                {
                    bool hasAddTraitAttribute = declarator.AttributeLists.Any(a => a.Attributes.Any(w => Consts.AddTraitAttributes.Contains(w.Name.ToString())));

                    if (hasAddTraitAttribute)
                        AddTraitCandidates.Add(declarator);
                }
            }
        }
    }
}
