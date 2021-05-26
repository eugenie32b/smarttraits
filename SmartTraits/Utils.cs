using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TypeInfo = Microsoft.CodeAnalysis.TypeInfo;

namespace SmartTraits
{
    public static class Utils
    {
        public static DiagnosticSeverity ToSeverity(this T4GeneratorVerbosity verbosity)
        {
            return verbosity switch
            {
                T4GeneratorVerbosity.Critical => DiagnosticSeverity.Error,
                T4GeneratorVerbosity.Error => DiagnosticSeverity.Error,
                T4GeneratorVerbosity.Warning => DiagnosticSeverity.Warning,
                T4GeneratorVerbosity.Info => DiagnosticSeverity.Info,
                _ => DiagnosticSeverity.Hidden,
            };
        }

        public static string GetPath(string directoryPath, string directoryName)
        {
            if (Directory.Exists(directoryPath + "/" + directoryName))
                return directoryPath + "/" + directoryName + "/";

            DirectoryInfo parentInfo = Directory.GetParent(directoryPath);
            if (parentInfo == null)
                return null;

            return GetPath(parentInfo.FullName, directoryName);
        }

        public static (string mscorlibLocation, string netstandardLocation) GetNetStandardAssemblyLocactions(SyntaxNode node, DiagnosticDescriptor diagnostics)
        {
            string coreDir = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);
            if (coreDir == null)
                return (null, null);

            var mscorlibLocation = Path.Combine(coreDir, "mscorlib.dll");
            if (!File.Exists(mscorlibLocation))
            {
                var parentDir = Directory.GetParent(coreDir);

                string mscorlibLocation2 = parentDir.FullName + Path.DirectorySeparatorChar + "mscorlib.dll";
                if (!File.Exists(mscorlibLocation2))
                    return (null, null);

                mscorlibLocation = mscorlibLocation2;
            }

            var netstandardLocation = Path.Combine(coreDir, "netstandard.dll");
            if (!File.Exists(netstandardLocation))
                return (null, null);

            return (mscorlibLocation, netstandardLocation);
        }

        public static string RemoveNewLine(string s)
        {
            if (s == null)
                return "";

            return s.Replace("\n", " ").Replace("\r", " ");
        }

        public static string RemoveWhitespace(string s)
        {
            if (s == null)
                return "";

            return Regex.Replace(s, @"\s", "");
        }

        public static string GetUniqueFileName(HashSet<string> generatedFiles, string sourcefileName)
        {
            string fileName = $"SmartTraitsGenerator.{sourcefileName}.cs";
            if (!generatedFiles.Contains(fileName))
                return fileName;

            for (int i = 1; i < 1000; i++)
            {
                fileName = $"SmartTraitsGenerator.{sourcefileName}.{i}.cs";
                if (!generatedFiles.Contains(fileName))
                    return fileName;
            }

            throw (new Exception($"Cannot get unique file name for the {sourcefileName}"));
        }

        public static StringBuilder ReturnError(string errorMsg)
        {
            StringBuilder sb = new();

            sb.AppendLine($"#error {errorMsg}");

            return sb;
        }

        public static List<MemberNodeInfo> GetAllMembersInfo(TypeDeclarationSyntax node)
        {
            List<MemberNodeInfo> membersInfo = new();

            foreach (MemberDeclarationSyntax memberNode in node.Members)
            {
                if (memberNode is BaseFieldDeclarationSyntax fieldNode)
                {
                    foreach (VariableDeclaratorSyntax variableNode in fieldNode.Declaration.Variables)
                    {
                        membersInfo.Add(new MemberNodeInfo()
                        {
                            Kind = variableNode.Kind(),
                            Name = variableNode.Identifier.ToString(),
                        });
                    }
                }
                else
                {
                    var nodeInfo = GetMemberInfo(memberNode);
                    if (nodeInfo != null)
                        membersInfo.Add(nodeInfo);
                }
            }

            return membersInfo;
        }

        public static MemberNodeInfo GetMemberInfo(MemberDeclarationSyntax member)
        {
            if (member is ClassDeclarationSyntax classMember)
            {
                return new MemberNodeInfo()
                {
                    Name = classMember.Identifier.ToFullString(),
                    Kind = member.Kind(),
                };
            }

            if (member is MethodDeclarationSyntax methodMember)
            {
                return new MemberNodeInfo()
                {
                    Name = methodMember.Identifier.ToFullString(),
                    Kind = member.Kind(),
                    ReturnType = Utils.RemoveWhitespace(methodMember.ReturnType.ToFullString()),
                    GenericsCount = methodMember.TypeParameterList?.Parameters.Count() ?? 0,
                    Arguments = Utils.RemoveWhitespace(string.Join(";", methodMember.ParameterList.Parameters.Select(s => s.Type?.ToString()))),
                };
            }

            if (member is PropertyDeclarationSyntax propertyMember)
            {
                return new MemberNodeInfo()
                {
                    Name = propertyMember.Identifier.ToFullString(),
                    Kind = member.Kind(),
                    ReturnType = Utils.RemoveWhitespace(propertyMember.Type.ToString()),
                };
            }

            return null;
        }

        public static bool IsStrictMode(ClassDeclarationSyntax traitClass)
        {
            var classAttrs = traitClass.AttributeLists.SelectMany(s => s.Attributes).ToArray();
            var traitAttr = classAttrs.FirstOrDefault(w => Consts.TraitAttributes.Contains(w.Name.ToFullString()));

            return HasAttributeParameter(traitAttr, Consts.TraitOptionsEnums, "Strict");
        }

        public static InterfaceDeclarationSyntax GetTraitInterface(SemanticModel semanticModel, ClassDeclarationSyntax classNode)
        {
            if (classNode.BaseList == null)
                return null;

            foreach (var baseListType in classNode.BaseList.Types)
            {
                TypeInfo typeInfo = semanticModel.GetTypeInfo(baseListType.Type);
                if (typeInfo.Type == null)
                    return null;

                SyntaxNode typeNode = semanticModel.GetSymbolInfo(baseListType.Type).Symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                if (typeNode == null)
                    continue;

                if (typeNode is InterfaceDeclarationSyntax interfaceNode)
                {
                    if (interfaceNode.AttributeLists.SelectMany(s => s.Attributes).Any(w => Consts.TraitInterfaceAttributes.Contains(w.Name.ToString())))
                        return interfaceNode;
                }
            }

            return null;
        }

        public static StringBuilder LogExceptionAsComments(Exception ex, string msg)
        {
            StringBuilder sb = new();

            sb.AppendLine($"#error {Utils.RemoveNewLine(msg)}");
            sb.AppendLine("");
            sb.AppendLine("// Exception message:");

            foreach (string s in ex.Message.Split('\n'))
            {
                sb.AppendLine($"// {Utils.RemoveNewLine(s)}");
            }

            sb.AppendLine("");
            sb.AppendLine("// Exception stack trace:");

            foreach (string s in ex.StackTrace.Split('\n'))
            {
                sb.AppendLine($"// {Utils.RemoveNewLine(s)}");
            }

            return sb;
        }

        public static bool HasAttributeOfType(this MemberDeclarationSyntax member, string[] possibleTypes)
        {
            return member.AttributeLists.Any(w => w.Attributes.Any(a => possibleTypes.Contains(a.Name.ToString())));
        }

        public static bool HasAttributeParameter(this AttributeSyntax attr, string[] possibleTypes, string name)
        {
            if (attr?.ArgumentList == null)
                return false;

            return attr.ArgumentList.Arguments
                .Where(w => w.Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
                .Select(s => s.Expression)
                .OfType<MemberAccessExpressionSyntax>()
                .Any(w => possibleTypes.Contains(w.Expression.ToString()) && w.Name.ToString() == name);
        }

        public static IEnumerable<AttributeSyntax> GetAttributesOfType(this MemberDeclarationSyntax memberNode, string[] possibleTypes)
        {
            return memberNode.AttributeLists.SelectMany(s => s.Attributes).Where(w => possibleTypes.Contains(w.Name.ToString()));
        }

        public static List<MemberNodeInfo> GetMemberNodeInfo(MemberDeclarationSyntax memberNode)
        {
            List<MemberNodeInfo> result = new();
            if (memberNode is BaseFieldDeclarationSyntax fieldNodes)
            {
                foreach (var variableNode in fieldNodes.Declaration.Variables)
                {
                    result.Add(new MemberNodeInfo()
                    {
                        Kind = variableNode.Kind(),
                        Name = variableNode.Identifier.ToString(),
                    });
                }
            }
            else
            {
                MemberNodeInfo nodeInfo = Utils.GetMemberInfo(memberNode);
                if (nodeInfo != null)
                    result.Add(nodeInfo);
            }

            return result;
        }

        public static MemberDeclarationSyntax RemoveAttributes(MemberDeclarationSyntax memberNode, string[] removeAttributes)
        {
            while (true)
            {
                if (memberNode == null)
                    break;

                var removeAttr = memberNode.AttributeLists.SelectMany(s => s.Attributes.Where(w => removeAttributes.Contains(w.Name.ToString()))).FirstOrDefault();
                if (removeAttr == null)
                    break;

                memberNode = memberNode.RemoveNode(removeAttr.Ancestors().OfType<AttributeListSyntax>().First(), SyntaxRemoveOptions.KeepNoTrivia);
            }

            return memberNode;
        }

        public static void AddToGeneratedSources(GeneratorExecutionContext context, HashSet<string> generatedFiles, MemberDeclarationSyntax memberNode, StringBuilder sb, string defaultFileName = "Common", AttributeSyntax addTraitAttr = null, SemanticModel semanticModel = null)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            string fileName = defaultFileName;
            if (memberNode != null)
                fileName = Path.GetFileNameWithoutExtension(memberNode.SyntaxTree.FilePath);
            if (semanticModel != null && addTraitAttr != null)
            {
                var firstParam = addTraitAttr.ArgumentList?.Arguments.FirstOrDefault();

                var typeofTraitNode = firstParam?.DescendantNodes().OfType<TypeOfExpressionSyntax>().FirstOrDefault();
                if (typeofTraitNode == null)
                    return;

                TypeInfo addTraitType = semanticModel.GetTypeInfo(typeofTraitNode.Type);
                string traitClassName = addTraitType.Type?.ToDisplayString();
                fileName = traitClassName + "." + fileName;
            }

            string generatedFileName = Utils.GetUniqueFileName(generatedFiles, fileName);
            var x = sb.ToString();
            if (x.Contains("ExampleC"))
                System.Diagnostics.Debug.WriteLine("Utils.");
            context.AddSource(generatedFileName, sb.ToString());

            generatedFiles.Add(generatedFileName);
        }
    }
}
