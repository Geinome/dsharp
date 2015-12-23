// ScriptGenerator.cs
// Script#/Core/Compiler
// This source code is subject to terms and conditions of the Apache License, Version 2.0.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ScriptSharp;
using ScriptSharp.ScriptModel;

namespace ScriptSharp.Generator
{
    internal sealed class ScriptGenerator
    {
        private ScriptTextWriter scriptTextWriter;
        private CompilerOptions compilerOptions;
        private SymbolSet symbolSet;

        private Stack<SymbolImplementation> implemenationStack;


        private List<TypeSymbol> types = new List<TypeSymbol>();
        private List<TypeSymbol> publicTypes = new List<TypeSymbol>();
        private List<TypeSymbol> internalTypes = new List<TypeSymbol>();
        private bool hasNonModuleInternalTypes = false;

        public ScriptGenerator(TextWriter writer, CompilerOptions options, SymbolSet symbols)
        {
            Debug.Assert(writer != null);
            Debug.Assert(symbolSet != null);

            scriptTextWriter = new ScriptTextWriter(writer);

            compilerOptions = options;
            symbolSet = symbols;

            implemenationStack = new Stack<SymbolImplementation>();
        }

        public SymbolImplementation CurrentImplementation
        {
            get
            {
                return implemenationStack.Peek();
            }
        }

        public CompilerOptions Options
        {
            get
            {
                return compilerOptions;
            }
        }

        public ScriptTextWriter Writer
        {
            get
            {
                return scriptTextWriter;
            }
        }

        public void EndImplementation()
        {
            Debug.Assert(implemenationStack.Count != 0);
            implemenationStack.Pop();
        }

        public void GenerateScript()
        {
            CollectTypes(symbolSet);
            SortTypes();

            bool initialIndent = false;
            if (!String.IsNullOrEmpty(compilerOptions.ScriptInfo.Template))
            {
                int scriptIndex = compilerOptions.ScriptInfo.Template.IndexOf("{script}");
                if ((scriptIndex > 0) && (compilerOptions.ScriptInfo.Template[scriptIndex - 1] == ' '))
                {
                    // Heuristic to turn on initial indent:
                    // The script template has a space prior to {script}, i.e. {script} is not the
                    // first thing on a line within the template.

                    initialIndent = true;

                }
            }

            if (initialIndent)
            {
                scriptTextWriter.IncrementIndent();
            }

            foreach (TypeSymbol type in types)
            {
                TypeGenerator.GenerateScript(this, type);
            }

            GenerateExports(symbolSet);

            foreach (TypeSymbol type in types)
            {
                if (type.Type == SymbolType.Class)
                {
                    TypeGenerator.GenerateClassConstructorScript(this, (ClassSymbol)type);
                }
            }

            if (compilerOptions.IncludeTests)
            {
                foreach (TypeSymbol type in types)
                {
                    ClassSymbol classSymbol = type as ClassSymbol;
                    if ((classSymbol != null) && classSymbol.IsTestClass)
                    {
                        TestGenerator.GenerateScript(this, classSymbol);
                    }
                }
            }

            if (initialIndent)
            {
                scriptTextWriter.DecrementIndent();
            }
        }

        private void GenerateExports(SymbolSet symbolSet)
        {
            if (publicTypes.Count <= 0 && (internalTypes.Count <= 0 || !hasNonModuleInternalTypes))
            {
                return;
            }

            scriptTextWriter.Write("var $exports = ss.module('");
            scriptTextWriter.Write(symbolSet.ScriptName);
            scriptTextWriter.Write("',");

            GenerateInternalExports();
            GeneratePublicExports();

            scriptTextWriter.WriteLine(");");
            scriptTextWriter.WriteLine();
        }

        private void GeneratePublicExports()
        {
            if (publicTypes.Count <= 0)
            {
                scriptTextWriter.Write(" null");
                return;
            }

            scriptTextWriter.WriteLine();
            scriptTextWriter.IncrementIndent();
            scriptTextWriter.WriteLine("{");
            scriptTextWriter.IncrementIndent();
            bool firstType = true;
            foreach (TypeSymbol type in publicTypes)
            {
                if ((type.Type == SymbolType.Class) &&
                    ((ClassSymbol)type).IsExtenderClass)
                {
                    continue;
                }

                if (firstType == false)
                {
                    scriptTextWriter.WriteLine(",");
                }
                TypeGenerator.GenerateRegistrationScript(this, type);
                firstType = false;
            }
            scriptTextWriter.DecrementIndent();
            scriptTextWriter.WriteLine();
            scriptTextWriter.Write("}");
            scriptTextWriter.DecrementIndent();
        }

        private void GenerateInternalExports()
        {
            if (internalTypes.Count <= 0 && !hasNonModuleInternalTypes)
            {
                scriptTextWriter.Write(" null,");
                return;
            }

            scriptTextWriter.WriteLine();
            scriptTextWriter.IncrementIndent();
            scriptTextWriter.WriteLine("{");
            scriptTextWriter.IncrementIndent();
            bool firstType = true;

            foreach (TypeSymbol type in internalTypes)
            {
                if ((type.Type == SymbolType.Class) &&
                    (((ClassSymbol)type).IsExtenderClass || ((ClassSymbol)type).IsModuleClass))
                {
                    continue;
                }

                if ((type.Type == SymbolType.Record) &&
                    ((RecordSymbol)type).Constructor == null)
                {
                    continue;
                }

                if (firstType == false)
                {
                    scriptTextWriter.WriteLine(",");
                }

                TypeGenerator.GenerateRegistrationScript(this, type);
                firstType = false;
            }

            scriptTextWriter.DecrementIndent();
            scriptTextWriter.WriteLine();
            scriptTextWriter.Write("},");
            scriptTextWriter.DecrementIndent();
        }

        private void SortTypes()
        {
            IComparer<TypeSymbol> typeComparer = new TypeComparer();
            types = types.OrderBy(t => t, typeComparer).ToList();
            publicTypes = publicTypes.OrderBy(t => t, typeComparer).ToList();
            internalTypes = internalTypes.OrderBy(t => t, typeComparer).ToList();
        }

        private void CollectTypes(SymbolSet symbolSet)
        {
            foreach (NamespaceSymbol namespaceSymbol in symbolSet.Namespaces)
            {
                if (!namespaceSymbol.HasApplicationTypes)
                {
                    continue;
                }

                foreach (TypeSymbol type in namespaceSymbol.Types)
                {
                    if (!type.IsApplicationType)
                    {
                        continue;
                    }

                    if (type.Type == SymbolType.Delegate)
                    {
                        // Nothing needs to be generated for delegate types.
                        continue;
                    }

                    if (type.IsTestType && !compilerOptions.IncludeTests)
                    {
                        continue;
                    }

                    if (type.Type == SymbolType.Enumeration &&
                        (!type.IsPublic || ((EnumerationSymbol)type).Constants))
                    {
                        // Internal enums can be skipped since their values have been inlined.
                        // Public enums marked as constants can also be skipped since their
                        // values will always be inlined.
                        continue;
                    }

                    types.Add(type);

                    if (type.IsPublic)
                    {
                        publicTypes.Add(type);
                    }
                    else
                    {
                        if (type.Type != SymbolType.Class || !((ClassSymbol)type).IsModuleClass)
                        {
                            hasNonModuleInternalTypes = true;
                        }

                        internalTypes.Add(type);
                    }
                }
            }
        }

        public void StartImplementation(SymbolImplementation implementation)
        {
            Debug.Assert(implementation != null);
            implemenationStack.Push(implementation);
        }

        private sealed class TypeComparer : IComparer<TypeSymbol>
        {
            public int Compare(TypeSymbol x, TypeSymbol y)
            {
                if (x.Type != y.Type)
                {
                    // If types are different, then use the symbol type to
                    // similar types of types together.
                    return (int)x.Type - (int)y.Type;
                }

                if (x.Type == SymbolType.Class)
                {
                    // For classes, sort by inheritance depth. This is a crude
                    // way to ensure the base class for a class is generated
                    // first, without specifically looking at the inheritance
                    // chain for any particular type. A parent class with lesser
                    // inheritance depth has to by definition come first.

                    return ((ClassSymbol)x).InheritanceDepth - ((ClassSymbol)y).InheritanceDepth;
                }

                return 0;
            }
        }
    }
}
