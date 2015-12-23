// ScriptCompiler.cs
// Script#/Core/Compiler
// This source code is subject to terms and conditions of the Apache License, Version 2.0.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ScriptSharp.CodeModel;
using ScriptSharp.Compiler;
using ScriptSharp.Generator;
using ScriptSharp.Importer;
using ScriptSharp.ResourceModel;
using ScriptSharp.ScriptModel;
using ScriptSharp.Validator;

namespace ScriptSharp
{
    /// <summary>
    /// The Script# compiler.
    /// </summary>
    public sealed class ScriptCompiler : IErrorHandler
    {

        private CompilerOptions compilerOptions;
        private IErrorHandler errorHandler;

        private ParseNodeList compilationUnitList;
        private SymbolSet symbolSet;
        private ICollection<TypeSymbol> importedSymbols;
        private ICollection<TypeSymbol> appSymbols;
        private bool hasErrors;

#if DEBUG
        private string testOutput;
#endif

        public ScriptCompiler()
            : this(null)
        {
        }

        public ScriptCompiler(IErrorHandler errorHandler)
        {
            this.errorHandler = errorHandler;
        }

        public bool Compile(CompilerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            compilerOptions = options;

            hasErrors = false;
            symbolSet = new SymbolSet();

            ImportMetadata();
            if (hasErrors)
            {
                return false;
            }

            BuildCodeModel();
            if (hasErrors)
            {
                return false;
            }

            BuildMetadata();
            if (hasErrors)
            {
                return false;
            }

            BuildImplementation();
            if (hasErrors)
            {
                return false;
            }

            GenerateScript();
            if (hasErrors)
            {
                return false;
            }

            return true;
        }

        private void ImportMetadata()
        {
            MetadataImporter metadataImporter = new MetadataImporter(compilerOptions, this);

            importedSymbols = metadataImporter.ImportMetadata(compilerOptions.References, symbolSet);
        }

        private void BuildCodeModel()
        {
            compilationUnitList = new ParseNodeList();

            CodeModelBuilder codeModelBuilder = new CodeModelBuilder(compilerOptions, this);
            CodeModelValidator codeModelValidator = new CodeModelValidator(this);
            CodeModelProcessor validationProcessor = new CodeModelProcessor(codeModelValidator, compilerOptions);

            foreach (IStreamSource source in compilerOptions.Sources)
            {
                CompilationUnitNode compilationUnit = codeModelBuilder.BuildCodeModel(source);
                if (compilationUnit != null)
                {
                    validationProcessor.Process(compilationUnit);

                    compilationUnitList.Append(compilationUnit);
                }
            }
        }

        private void BuildMetadata()
        {
            if (compilerOptions.Resources != null && compilerOptions.Resources.Count != 0)
            {
                ResourcesBuilder resourcesBuilder = new ResourcesBuilder(symbolSet);
                resourcesBuilder.BuildResources(compilerOptions.Resources);
            }

            MetadataBuilder mdBuilder = new MetadataBuilder(this);
            appSymbols = mdBuilder.BuildMetadata(compilationUnitList, symbolSet, compilerOptions);

            // Check if any of the types defined in this assembly conflict.
            Dictionary<string, TypeSymbol> types = new Dictionary<string, TypeSymbol>();
            CheckForDuplicateTypes(types);

            // Capture whether there are any test types in the project
            // when not compiling the test flavor script. This is used to determine
            // if the test flavor script should be compiled in the build task.

            if (compilerOptions.IncludeTests == false)
            {
                foreach (TypeSymbol appType in appSymbols)
                {
                    if (appType.IsApplicationType && appType.IsTestType)
                    {
                        compilerOptions.HasTestTypes = true;
                    }
                }
            }

#if DEBUG
            if (compilerOptions.InternalTestType == "metadata")
            {
                StringWriter testWriter = new StringWriter();

                testWriter.WriteLine("Metadata");
                testWriter.WriteLine("================================================================");

                SymbolSetDumper symbolDumper = new SymbolSetDumper(testWriter);
                symbolDumper.DumpSymbols(symbolSet);

                testWriter.WriteLine();
                testWriter.WriteLine();

                testOutput = testWriter.ToString();
            }
#endif

            ISymbolTransformer transformer = null;
            if (compilerOptions.Minimize)
            {
                transformer = new SymbolObfuscator();
            }
            else
            {
                transformer = new SymbolInternalizer();
            }

            if (transformer != null)
            {
                SymbolSetTransformer symbolSetTransformer = new SymbolSetTransformer(transformer);
                ICollection<Symbol> transformedSymbols = symbolSetTransformer.TransformSymbolSet(symbolSet, /* useInheritanceOrder */ true);

#if DEBUG
                if (compilerOptions.InternalTestType == "minimizationMap")
                {
                    StringWriter testWriter = new StringWriter();

                    testWriter.WriteLine("Minimization Map");
                    testWriter.WriteLine("================================================================");

                    List<Symbol> sortedTransformedSymbols = new List<Symbol>(transformedSymbols);
                    sortedTransformedSymbols.Sort(delegate(Symbol s1, Symbol s2)
                    {
                        return String.Compare(s1.Name, s2.Name);
                    });

                    foreach (Symbol transformedSymbol in sortedTransformedSymbols)
                    {
                        Debug.Assert(transformedSymbol is MemberSymbol);
                        testWriter.WriteLine("    Member '" + transformedSymbol.Name + "' renamed to '" + transformedSymbol.GeneratedName + "'");
                    }

                    testWriter.WriteLine();
                    testWriter.WriteLine();

                    testOutput = testWriter.ToString();
                }
#endif
            }
        }

        private void CheckForDuplicateTypes(Dictionary<string, TypeSymbol> types)
        {
            foreach (TypeSymbol appType in appSymbols)
            {
                if ((appType.IsApplicationType == false) || (appType.Type == SymbolType.Delegate))
                {
                    // Skip the check for types that are marked as imported, as they
                    // aren't going to be generated into the script.
                    // Delegates are implicitly imported types, as they're never generated into
                    // the script.
                    continue;
                }

                if ((appType.Type == SymbolType.Class) &&
                    (((ClassSymbol)appType).PrimaryPartialClass != appType))
                {
                    // Skip the check for partial types, since they should only be
                    // checked once.
                    continue;
                }

                // TODO: We could allow conflicting types as long as both aren't public
                //       since they won't be on the exported types list. Internal types that
                //       conflict could be generated using full name.

                string name = appType.GeneratedName;
                if (types.ContainsKey(name))
                {
                    string error = "The type '" + appType.FullName + "' conflicts with with '" + types[name].FullName + "' as they have the same name.";
                    ((IErrorHandler)this).ReportError(error, null);
                }
                else
                {
                    types[name] = appType;
                }
            }
        }

        private void BuildImplementation()
        {
            CodeBuilder codeBuilder = new CodeBuilder(compilerOptions, this);
            ICollection<SymbolImplementation> implementations = codeBuilder.BuildCode(symbolSet);

            if (compilerOptions.Minimize)
            {
                foreach (SymbolImplementation impl in implementations)
                {
                    if (impl.Scope == null)
                    {
                        continue;
                    }

                    SymbolObfuscator obfuscator = new SymbolObfuscator();
                    SymbolImplementationTransformer transformer = new SymbolImplementationTransformer(obfuscator);

                    transformer.TransformSymbolImplementation(impl);
                }
            }
        }

        private void GenerateScript()
        {
            Stream outputStream = null;
            TextWriter outputWriter = null;

            try
            {
                outputStream = compilerOptions.ScriptFile.GetStream();
                if (outputStream == null)
                {
                    ((IErrorHandler)this).ReportError("Unable to write to file " + compilerOptions.ScriptFile.FullName,
                                                      compilerOptions.ScriptFile.FullName);
                    return;
                }

                outputWriter = new StreamWriter(outputStream, new UTF8Encoding(false));

#if DEBUG
                if (compilerOptions.InternalTestMode)
                {
                    if (testOutput != null)
                    {
                        outputWriter.Write(testOutput);
                        outputWriter.WriteLine("Script");
                        outputWriter.WriteLine("================================================================");
                        outputWriter.WriteLine();
                        outputWriter.WriteLine();
                    }
                }
#endif // DEBUG

                JavascriptGenerator javascriptGenerator = new JavascriptGenerator(compilerOptions, symbolSet);
                string script = javascriptGenerator.Generate();
                outputWriter.Write(script);
            }
            catch (Exception e)
            {
                Debug.Fail(e.ToString());
            }
            finally
            {
                if (outputWriter != null)
                {
                    outputWriter.Flush();
                }
                if (outputStream != null)
                {
                    compilerOptions.ScriptFile.CloseStream(outputStream);
                }
            }
        }

        void IErrorHandler.ReportError(string errorMessage, string location)
        {
            hasErrors = true;

            if (errorHandler != null)
            {
                errorHandler.ReportError(errorMessage, location);
                return;
            }

            if (String.IsNullOrEmpty(location) == false)
            {
                Console.Error.Write(location);
                Console.Error.Write(": ");
            }

            Console.Error.WriteLine(errorMessage);
        }
    }
}