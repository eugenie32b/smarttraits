using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartTraits
{
    public static class ExecutionProcessor
    {
        private static DiagnosticDescriptor _methodProcessorDescriptor = new(
            "ExecutionProcessor",
            "Process method compilation",
            "Process method compilation",
            "Codegen",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            "Process method compilation");

        private static int _assemblyCounter;

        private static Dictionary<string, CompilationCache> _cacheCompilations = new();

        public static (string, ImmutableArray<Diagnostic>) Process(string classType, MethodDeclarationSyntax traitMethodNode, ClassDeclarationSyntax destClassNode)
        {
            string methodName = traitMethodNode.Identifier.ToFullString();

            string sha1 = GetHashSHA1(Encoding.UTF8.GetBytes(traitMethodNode.ToFullString()));

            string classKey = classType + methodName;
            if (_cacheCompilations.TryGetValue(classKey, out CompilationCache cachedVersion))
            {
                if (cachedVersion.Sha1 == sha1)
                    return ExecuteMethod(cachedVersion.Assembly, cachedVersion.ProcessTypeName, methodName, destClassNode);

                _cacheCompilations.Remove(classKey);
            }

            var sb = new StringBuilder();

            _assemblyCounter++;

            string executionClassName = $"ExecutionWrapper{_assemblyCounter}";

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using System.Text.RegularExpressions;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Microsoft.CodeAnalysis;");
            sb.AppendLine("using Microsoft.CodeAnalysis.CSharp;");
            sb.AppendLine("using Microsoft.CodeAnalysis.CSharp.Syntax;");

            sb.AppendLine("");

            sb.AppendLine("namespace SmartTrait {");
            sb.AppendLine($"  public class {executionClassName} {{");
            sb.AppendLine($"  public {executionClassName}() {{ }}");

            // remove all attributes from the method
            var methodNodeWithoutAttrs = traitMethodNode;
            while (true)
            {
                var attr = methodNodeWithoutAttrs?.AttributeLists.FirstOrDefault();
                if (attr == null)
                    break;

                methodNodeWithoutAttrs = methodNodeWithoutAttrs.RemoveNode(attr, SyntaxRemoveOptions.KeepDirectives);
            }

            sb.AppendLine(methodNodeWithoutAttrs?.ToFullString());

            sb.AppendLine("  }");
            sb.AppendLine("}");

            var syntaxTree = CSharpSyntaxTree.ParseText(sb.ToString(), new CSharpParseOptions(LanguageVersion.Latest));

            string coreDir = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);
            if (coreDir == null)
                return (null, new ImmutableArray<Diagnostic>() { Diagnostic.Create(_methodProcessorDescriptor, traitMethodNode.GetLocation(), "Empty core directory") });

            var mscorlibLocation = Path.Combine(coreDir, "mscorlib.dll");
            if (!File.Exists(mscorlibLocation))
            {
                var parentDir = Directory.GetParent(coreDir);

                string mscorlibLocation2 = parentDir.FullName + Path.DirectorySeparatorChar + "mscorlib.dll";
                if (!File.Exists(mscorlibLocation2))
                    return (null, new ImmutableArray<Diagnostic>() { Diagnostic.Create(_methodProcessorDescriptor, traitMethodNode.GetLocation(), $"Cannot find neither {mscorlibLocation} nor {mscorlibLocation2}") });

                mscorlibLocation = mscorlibLocation2;
            }

            var netstandardLocation = Path.Combine(coreDir, "netstandard.dll");
            if (!File.Exists(netstandardLocation))
                return (null, new ImmutableArray<Diagnostic>() { Diagnostic.Create(_methodProcessorDescriptor, traitMethodNode.GetLocation(), $"Cannot find {netstandardLocation}") });

            List<PortableExecutableReference> refs = new()
            {
                MetadataReference.CreateFromFile(mscorlibLocation),
                MetadataReference.CreateFromFile(netstandardLocation),
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ClassDeclarationSyntax).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(SyntaxToken).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Regex).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IQueryable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MethodInfo).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ValueType).Assembly.Location),
            };

            CSharpCompilation compilation = CSharpCompilation.Create(
                $"SmartTrait.MethodProcessor{_assemblyCounter}",
                new[] { syntaxTree },
                refs,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


            using (var dllStream = new MemoryStream())
            {
                using (var pdbStream = new MemoryStream())
                {
                    EmitResult emitResult = compilation.Emit(dllStream, pdbStream);
                    if (!emitResult.Success)
                    {
                        return (null, emitResult.Diagnostics);
                    }


                    var assembly = Assembly.Load(dllStream.ToArray(), pdbStream.ToArray());

                    var versionInfo = new CompilationCache()
                    {
                        Sha1 = sha1,
                        Assembly = assembly,
                        ProcessTypeName = $"SmartTrait.{executionClassName}",
                    };

                    _cacheCompilations[classType + methodName] = versionInfo;

                    return ExecuteMethod(versionInfo.Assembly, versionInfo.ProcessTypeName, methodName, destClassNode);
                }
            }
        }

        private static (string, ImmutableArray<Diagnostic>) ExecuteMethod(Assembly assembly, string processTypeName, string methodName, ClassDeclarationSyntax targetClassNode)
        {
            var processObject = assembly.CreateInstance(processTypeName);

            MethodInfo methodInfo = processObject?.GetType().GetMethod(methodName);
            if (methodInfo == null)
                return ($"method {methodName} not found in {processTypeName}", new ImmutableArray<Diagnostic>());

            object result = methodInfo.Invoke(processObject, new object[] { targetClassNode });

            return ((result as string), new ImmutableArray<Diagnostic>());
        }

        private static string GetHashSHA1(byte[] data)
        {
            using (var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider())
            {
                return string.Concat(sha1.ComputeHash(data).Select(x => x.ToString("X2")));
            }
        }

        private class CompilationCache
        {
            public string Sha1;
            public string ProcessTypeName;
            public Assembly Assembly;

        }
    }
}
