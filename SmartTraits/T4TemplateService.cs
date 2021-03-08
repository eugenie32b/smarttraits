using Microsoft.VisualStudio.TextTemplating;
using Mono.TextTemplating;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartTraits
{
    public class T4TemplateService
    {
        private const string TEMPLATE_DIRECTORY_NAME = "T4Templates";
        private const string REFERENCES_DIRECTORY_NAME = "T4Refs";


        private readonly Dictionary<string, SgTemplateInfo> _templates = new();

        private TemplateGenerator _t4Generator;

        protected TemplateGenerator T4Generator
        {
            get
            {
                if (_t4Generator != null)
                    return _t4Generator;

                _t4Generator = new TemplateGenerator();

                return _t4Generator;
            }
        }




        public (string generatedCode, string outputExtension) ProcessTemplate(string searchTemplateFromPath, string templateName, Dictionary<string, object> sessionParams, T4GeneratorVerbosity verbosity, Action<LoggerInfo> logAction)
        {
            if (verbosity == T4GeneratorVerbosity.Debug)
            {
                logAction(new LoggerInfo(T4GeneratorVerbosity.Debug, "SGD0001", "ProcessTemplate searchTemplateFromPath", searchTemplateFromPath));
                logAction(new LoggerInfo(T4GeneratorVerbosity.Debug, "SGD0002", "ProcessTemplate name", searchTemplateFromPath));

                foreach (var param in sessionParams)
                {
                    logAction(new LoggerInfo(T4GeneratorVerbosity.Debug, "SGD0003", "ProcessTemplate sessionParams", $"{param.Key} = {param.Value}"));
                }
            }

            string templatePath = Utils.GetPath(searchTemplateFromPath, TEMPLATE_DIRECTORY_NAME);


            if (!Directory.Exists(templatePath))
                throw (new DirectoryNotFoundException($"Cannot find template directory ${TEMPLATE_DIRECTORY_NAME} for ${searchTemplateFromPath}"));

            T4Generator.IncludePaths.Clear();
            T4Generator.IncludePaths.Add(templatePath);

            string referencePath = Utils.GetPath(searchTemplateFromPath, REFERENCES_DIRECTORY_NAME);

            if (Directory.Exists(referencePath))
            {
                T4Generator.ReferencePaths.Clear();
                T4Generator.ReferencePaths.Add(referencePath);
            }



            CompiledTemplate t4gen = GetTemplate(templatePath, templateName, sessionParams, verbosity, logAction);

            string result = t4gen.Process();

            if (verbosity == T4GeneratorVerbosity.Debug)
            {
                logAction(new LoggerInfo(T4GeneratorVerbosity.Debug, "SGD10001", "Generated source code", result));
            }

            ShowErrors(templatePath, verbosity, logAction, templateName);

            return (result, T4Generator.OutputFile);
        }

        private CompiledTemplate GetTemplate(string templatePath, string name, Dictionary<string, object> sessionParams, T4GeneratorVerbosity verbosity, Action<LoggerInfo> logAction)
        {
            var session = T4Generator.GetOrCreateSession();

            if (sessionParams.Count > 0)
            {
                foreach (string key in sessionParams.Keys)
                {
                    session[key] = sessionParams[key];
                }
            }

            foreach (string key in session.Keys.ToArray())
            {
                if (session[key] == null)
                    session.Remove(key);
            }

            name = Path.GetFileNameWithoutExtension(name);

            if (!_templates.TryGetValue(name, out SgTemplateInfo template)
                || NeedReloadTemplate(template)
                || template.DependentOnTemplates.Any(NeedReloadTemplate))
            {
                string fileName = GetTemplateFileName(templatePath, name);

                if (!File.Exists(fileName))
                    throw (new FileNotFoundException($"Cannot find a template {fileName}"));

                string templateSourceCode = File.ReadAllText(fileName);

                FileInfo fi = new(fileName);

                T4Generator.UseInProcessCompiler();

                CompiledTemplate ct = T4Generator.CompileTemplate(templateSourceCode);

                if (ct == null)
                {
                    string errors = ShowErrors(templatePath, verbosity, logAction);

                    throw (new Exception("Cannot compile a template: " + errors));
                }

                template = new SgTemplateInfo(ct)
                {
                    FilePath = fileName,
                    FileSize = fi.Length,
                    FileModified = fi.LastWriteTime,
                };

                ShowErrors(templatePath, verbosity, logAction);

                // find all dependent templates
                var regEx = new Regex("<\\#@\\s+include\\s+file=\"([^\"]+)\"\\s+\\#>");

                foreach (Match match in regEx.Matches(templateSourceCode))
                {
                    string t = match.Groups[1].Value;

                    string dependentFilePath = templatePath + t;
                    FileInfo dfi = new FileInfo(dependentFilePath);

                    template.DependentOnTemplates.Add(new SgTemplateFileInfo()
                    {
                        FilePath = dependentFilePath,
                        FileSize = dfi.Length,
                        FileModified = dfi.LastWriteTime,
                    });
                }

                _templates[name] = template;
            }

            return template.CompiledTemplate;
        }

        private bool NeedReloadTemplate(SgTemplateFileInfo template)
        {
            if (template.FilePath == null)
                throw (new NullReferenceException());

            var fi = new FileInfo(template.FilePath);

            return (fi.Length != template.FileSize || fi.LastWriteTime != template.FileModified);
        }

        private string GetTemplateFileName(string templatePath, string name)
        {
            string fileName = $"{templatePath}/{name}";
            if (!Regex.IsMatch(name, @"\.[a-z0-9]+$", RegexOptions.IgnoreCase))
                fileName += ".tt";

            return fileName;
        }

        private string ShowErrors(string templatePath, T4GeneratorVerbosity verbosity, Action<LoggerInfo> logAction, string templateName = null)
        {
            StringBuilder errs = new();

            if (T4Generator.Errors.Count <= 0)
                return errs.ToString();

            foreach (var error in T4Generator.Errors.OfType<CompilerError>())
            {
                logAction(new LoggerInfo(T4GeneratorVerbosity.Error, "SGE00001", "Compiler error", error.ErrorText));

                errs.AppendLine("Compiler error: " + error.ErrorText);
            }

            if (verbosity == T4GeneratorVerbosity.Debug && templateName != null)
            {
                (string templateSourceCode, CompilerErrorCollection errors, string logs) = GetTemplateSourceCode(templatePath, templateName);

                if (logs != null)
                    logAction(new LoggerInfo(T4GeneratorVerbosity.Debug, "SGD20041", "Compilation logs", logs));

                if (templateSourceCode != null)
                    logAction(new LoggerInfo(T4GeneratorVerbosity.Debug, "SGD20001", "Template source code", templateSourceCode));

                if (errors != null)
                {
                    foreach (CompilerError compilerError in errors)
                    {
                        string errorMsg = $"Template generation of source code {(compilerError.IsWarning ? "warning" : "error")}";
                        logAction(new LoggerInfo(compilerError.IsWarning ? T4GeneratorVerbosity.Warning : T4GeneratorVerbosity.Error, "SGE22001", errorMsg, compilerError.ErrorText));

                        errs.AppendLine(errorMsg);
                        errs.AppendLine(compilerError.ErrorText);
                    }
                }
            }

            return errs.ToString();
        }

        public (string templateSourceCode, CompilerErrorCollection errors, string logs) GetTemplateSourceCode(string templatePath, string name)
        {
            string sourceCode = File.ReadAllText(GetTemplateFileName(templatePath, name));
            var host = T4Generator as ITextTemplatingEngineHost;

            ParsedTemplate pt = ParsedTemplate.FromText(sourceCode, host);
            if (pt.Errors.HasErrors)
            {
                host.LogErrors(pt.Errors);
                return (null, pt.Errors, null);
            }

            TemplateSettings settings = TemplatingEngine.GetSettings(T4Generator, pt);

            using (var logs = new StringWriter())
            {
                settings.Log = logs;

                CodeCompileUnit ccu = TemplatingEngine.GenerateCompileUnit(host, sourceCode, pt, settings);

                var opts = new CodeGeneratorOptions();
                using (var writer = new StringWriter())
                {
                    settings.Provider.GenerateCodeFromCompileUnit(ccu, writer, opts);
                    return (writer.ToString(), pt.Errors, logs.ToString());
                }
            }
        }


        public class SgTemplateInfo : SgTemplateFileInfo
        {
            public SgTemplateInfo(CompiledTemplate compiledTemplate)
            {
                CompiledTemplate = compiledTemplate;
            }

            public CompiledTemplate CompiledTemplate { get; set; }
            public List<SgTemplateFileInfo> DependentOnTemplates { get; set; } = new();
        }

        public class SgTemplateFileInfo
        {
            public string FilePath { get; set; }
            public long FileSize { get; set; }
            public DateTime FileModified { get; set; }
        }
    }
}
