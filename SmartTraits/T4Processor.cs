using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SmartTraits
{
    public class T4Processor
    {
        private static bool _isAssembliesUpdated;

        static T4Processor()
        {
            // we need to have several dependent dlls, so we will write them to the same folder as current dll, to be able to load when needed
            if (!_isAssembliesUpdated)
            {
                string dir = Path.GetDirectoryName(typeof(T4Processor).Assembly.Location);

                Logger.WriteToLog("Updated assembly files in " + dir);

                string codeDomPath = Path.Combine(dir, "System.CodeDom.dll");
                if (!File.Exists(codeDomPath))
                    File.WriteAllBytes(codeDomPath, ExternalResources.System_CodeDom);

                string monoPath = Path.Combine(dir, "Mono.TextTemplating.dll");
                if (!File.Exists(monoPath))
                    File.WriteAllBytes(monoPath, ExternalResources.Mono_TextTemplating);

                string monoRoslynPath = Path.Combine(dir, "Mono.TextTemplating.Roslyn.dll");
                if (!File.Exists(monoRoslynPath))
                    File.WriteAllBytes(monoRoslynPath, ExternalResources.Mono_TextTemplating_Roslyn);

                _isAssembliesUpdated = true;
            }
        }

        private readonly T4TemplateService _templateService = new();

        public void ProcessTemplate(GeneratorExecutionContext context, AttributeSyntax attr, MemberDeclarationSyntax memberCandidate, StringBuilder sb)
        {
            if (attr.ArgumentList == null || attr.ArgumentList.Arguments.Count < 1)
                throw (new Exception("Cannot find a template name argument"));

            string templateName = attr.ArgumentList.Arguments[0].ToString().Trim('\"');
            string extraTag = null;
            Logger.LogFile = Logger.DEFAULT_LOG_FILE;
            T4GeneratorVerbosity verbosity = T4GeneratorVerbosity.None;

            try
            {
                for (int i = 1; i < attr.ArgumentList.Arguments.Count; i++)
                {
                    AttributeArgumentSyntax arg = attr.ArgumentList.Arguments[i];
                    switch (arg.NameEquals?.Name.ToString())
                    {
                        case "ExtraTag":
                            extraTag = arg.Expression.ToFullString().Trim('\"');
                            break;

                        case "LogFile":
                            Logger.LogFile = arg.Expression.ToFullString().Trim('\"');
                            break;

                        case "VerbosityLevel":
                            if (arg.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression.ToString() == "T4GeneratorVerbosity")
                                verbosity = (T4GeneratorVerbosity)Enum.Parse(typeof(T4GeneratorVerbosity), memberAccess.Name.ToString());
                            break;
                    }
                }

                ClassDeclarationSyntax classNode = memberCandidate as ClassDeclarationSyntax;
                MethodDeclarationSyntax methodNode = null;
                PropertyDeclarationSyntax propertyNode = null;

                if (memberCandidate is MethodDeclarationSyntax)
                {
                    methodNode = memberCandidate as MethodDeclarationSyntax;
                    classNode = memberCandidate.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                }
                else if (memberCandidate is PropertyDeclarationSyntax)
                {
                    propertyNode = memberCandidate as PropertyDeclarationSyntax;
                    classNode = memberCandidate.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                }

                string filePath = memberCandidate.SyntaxTree.FilePath;

                (string sourceCode, _) = _templateService.ProcessTemplate(filePath, templateName, new Dictionary<string, object>()
                    {
                        {"ClassNode", classNode},
                        {"MethodNode", methodNode},
                        {"PropertyNode", propertyNode},
                        {"SyntaxNode", memberCandidate},
                        {"ExtraTag", extraTag},
                        {"FilePath", filePath}
                    },
                    verbosity, (info) =>
                    {
                        if (verbosity == T4GeneratorVerbosity.Debug)
                            Logger.WriteToLog(info.Message);

                        var diagnostic = new DiagnosticDescriptor(info.Id, info.Title, info.Message, "SmartTraits", verbosity.ToSeverity(), true);

                        context.ReportDiagnostic(Diagnostic.Create(diagnostic, null));
                    });

                sb.AppendLine(sourceCode);
            }
            finally
            {
                Logger.LogFile = Logger.DEFAULT_LOG_FILE;
            }
        }

    }
}
