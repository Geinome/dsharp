using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ScriptSharp.ScriptModel;

namespace ScriptSharp.Generator
{
    internal class JavascriptGenerator
    {
        private readonly CompilerOptions compilerOptions;
        private readonly SymbolSet symbolSet;

        internal JavascriptGenerator(CompilerOptions compilerOptions, SymbolSet symbolSet)
        {
            this.compilerOptions = compilerOptions;
            this.symbolSet = symbolSet;
        }

        internal string Generate()
        {
            string script = GenerateScript();

            string template = compilerOptions.ScriptInfo.Template;
            if (String.IsNullOrEmpty(template))
            {
                return script;
            }

            template = PreprocessTemplate(template);

            StringBuilder requiresBuilder = new StringBuilder();
            StringBuilder dependenciesBuilder = new StringBuilder();
            StringBuilder depLookupBuilder = new StringBuilder();

            bool firstDependency = true;
            foreach (ScriptReference dependency in symbolSet.Dependencies)
            {
                if (dependency.DelayLoaded)
                {
                    continue;
                }

                if (firstDependency)
                {
                    depLookupBuilder.Append("var ");
                }
                else
                {
                    requiresBuilder.Append(", ");
                    dependenciesBuilder.Append(", ");
                    depLookupBuilder.Append(",\r\n    ");
                }

                string name = dependency.Name;
                if (name == "ss")
                {
                    // TODO: This is a hack... to make generated node.js scripts
                    //       be able to reference the 'scriptsharp' node module.
                    //       Fix this in a better/1st class manner by allowing
                    //       script assemblies to declare such things.
                    name = "scriptsharp";
                }

                requiresBuilder.Append("'" + dependency.Path + "'");
                dependenciesBuilder.Append(dependency.Identifier);

                depLookupBuilder.Append(dependency.Identifier);
                depLookupBuilder.Append(" = require('" + name + "')");

                firstDependency = false;
            }

            depLookupBuilder.Append(";");

            StringBuilder stringBuilder = new StringBuilder(template.TrimStart());
            stringBuilder.Replace("{name}", symbolSet.ScriptName);
            stringBuilder.Replace("{description}", compilerOptions.ScriptInfo.Description ?? String.Empty);
            stringBuilder.Replace("{copyright}", compilerOptions.ScriptInfo.Copyright ?? String.Empty);
            stringBuilder.Replace("{version}", compilerOptions.ScriptInfo.Version ?? String.Empty);
            stringBuilder.Replace("{compiler}", typeof(ScriptCompiler).Assembly.GetName().Version.ToString());
            stringBuilder.Replace("{description}", compilerOptions.ScriptInfo.Description);
            stringBuilder.Replace("{requires}", requiresBuilder.ToString());
            stringBuilder.Replace("{dependencies}", dependenciesBuilder.ToString());
            stringBuilder.Replace("{dependenciesLookup}", depLookupBuilder.ToString());
            stringBuilder.Replace("{script}", script);

            return stringBuilder.ToString();
        }

        private string GenerateScript()
        {
            StringWriter scriptWriter = new StringWriter();

            try
            {
                ScriptGenerator scriptGenerator = new ScriptGenerator(scriptWriter, compilerOptions, symbolSet);
                scriptGenerator.GenerateScript();
            }
            catch (Exception e)
            {
                Debug.Fail(e.ToString());
            }
            finally
            {
                scriptWriter.Flush();
            }

            return scriptWriter.ToString();
        }

        private string PreprocessTemplate(string template)
        {
            if (compilerOptions.IncludeResolver == null)
            {
                return template;
            }

            Regex includePattern = new Regex("\\{include:([^\\}]+)\\}", RegexOptions.Multiline | RegexOptions.CultureInvariant);
            return includePattern.Replace(template, ReplaceInclude);

        }

        private string ReplaceInclude(Match match)
        {
            string includedScript = String.Empty;

            if (match.Groups.Count == 2)
            {
                string includePath = match.Groups[1].Value;

                IStreamSource includeSource = compilerOptions.IncludeResolver.Resolve(includePath);
                if (includeSource != null)
                {
                    Stream includeStream = includeSource.GetStream();
                    StreamReader reader = new StreamReader(includeStream);

                    includedScript = reader.ReadToEnd();
                    includeSource.CloseStream(includeStream);
                }
            }

            return includedScript;
        }
    }
}
