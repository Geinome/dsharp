// ScriptTextWriter.cs
// Script#/Core/Compiler
// This source code is subject to terms and conditions of the Apache License, Version 2.0.
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace ScriptSharp.Generator
{
    internal sealed class ScriptTextWriter : TextWriter
    {
        private TextWriter textWriter;

        private int indentLevel;
        private bool tabsPending;
        private string tabString;

        private TextWriter globalWriter;
        private int globalIndentLevel;
        private bool globalTabsPending;

        public ScriptTextWriter(TextWriter writer)
            : base(CultureInfo.InvariantCulture)
        {
            textWriter = writer;
            globalWriter = writer;

            tabString = "  ";
            indentLevel = 0;
            tabsPending = false;
        }

        public override Encoding Encoding
        {
            get { return textWriter.Encoding; }
        }

        public override string NewLine
        {
            get { return textWriter.NewLine; }
            set { textWriter.NewLine = value; }
        }

        public int Indent
        {
            get { return indentLevel; }
            private set
            {
                Debug.Assert(value >= 0);
                if (value < 0)
                {
                    value = 0;
                }

                indentLevel = value;
            }
        }

        public override void Close()
        {
            textWriter.Close();
        }

        public override void Flush()
        {
            textWriter.Flush();
        }

        private void OutputTabs()
        {
            if (tabsPending)
            {
                for (int i = 0; i < indentLevel; i++)
                {
                    textWriter.Write(tabString);
                }
                tabsPending = false;
            }
        }

        public void StartLocalWriting(TextWriter writer)
        {
            Debug.Assert(writer != null);
            Debug.Assert(textWriter == globalWriter);
            textWriter = writer;

            globalIndentLevel = indentLevel;
            indentLevel = 0;

            globalTabsPending = tabsPending;
            tabsPending = false;
        }

        public void StopLocalWriting()
        {
            textWriter = globalWriter;
            indentLevel = globalIndentLevel;
            tabsPending = globalTabsPending;
        }

        public override void Write(string s)
        {
            OutputTabs();
            textWriter.Write(s);
        }

        public override void Write(bool value)
        {
            OutputTabs();
            textWriter.Write(value);
        }

        public override void Write(char value)
        {
            OutputTabs();
            textWriter.Write(value);
        }

        public override void Write(char[] buffer)
        {
            OutputTabs();
            textWriter.Write(buffer);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            OutputTabs();
            textWriter.Write(buffer, index, count);
        }

        public override void Write(double value)
        {
            OutputTabs();
            textWriter.Write(value);
        }

        public override void Write(float value)
        {
            OutputTabs();
            textWriter.Write(value);
        }

        public override void Write(int value)
        {
            OutputTabs();
            textWriter.Write(value);
        }

        public override void Write(long value)
        {
            OutputTabs();
            textWriter.Write(value);
        }

        public override void Write(object value)
        {
            OutputTabs();
            textWriter.Write(value);
        }

        public override void Write(string format, object arg0)
        {
            OutputTabs();
            textWriter.Write(format, arg0);
        }

        public override void Write(string format, object arg0, object arg1)
        {
            OutputTabs();
            textWriter.Write(format, arg0, arg1);
        }

        public override void Write(string format, params object[] arg)
        {
            OutputTabs();
            textWriter.Write(format, arg);
        }

        public override void WriteLine()
        {
            textWriter.WriteLine();
            tabsPending = true;
        }

        public override void WriteLine(string s)
        {
            OutputTabs();
            textWriter.WriteLine(s);
            tabsPending = true;
        }

        public override void WriteLine(bool value)
        {
            OutputTabs();
            textWriter.WriteLine(value);
            tabsPending = true;
        }

        public override void WriteLine(char value)
        {
            OutputTabs();
            textWriter.WriteLine(value);
            tabsPending = true;
        }

        public override void WriteLine(char[] buffer)
        {
            OutputTabs();
            textWriter.WriteLine(buffer);
            tabsPending = true;
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            OutputTabs();
            textWriter.WriteLine(buffer, index, count);
            tabsPending = true;
        }

        public override void WriteLine(double value)
        {
            OutputTabs();
            textWriter.WriteLine(value);
            tabsPending = true;
        }

        public override void WriteLine(float value)
        {
            OutputTabs();
            textWriter.WriteLine(value);
            tabsPending = true;
        }

        public override void WriteLine(int value)
        {
            OutputTabs();
            textWriter.WriteLine(value);
            tabsPending = true;
        }

        public override void WriteLine(long value)
        {
            OutputTabs();
            textWriter.WriteLine(value);
            tabsPending = true;
        }

        public override void WriteLine(object value)
        {
            OutputTabs();
            textWriter.WriteLine(value);
            tabsPending = true;
        }

        public override void WriteLine(string format, object arg0)
        {
            OutputTabs();
            textWriter.WriteLine(format, arg0);
            tabsPending = true;
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            OutputTabs();
            textWriter.WriteLine(format, arg0, arg1);
            tabsPending = true;
        }

        public override void WriteLine(string format, params object[] arg)
        {
            OutputTabs();
            textWriter.WriteLine(format, arg);
            tabsPending = true;
        }

        public override void WriteLine(UInt32 value)
        {
            OutputTabs();
            textWriter.WriteLine(value);
            tabsPending = true;
        }

        public void IncrementIndent()
        {
            Indent++;
        }

        public void DecrementIndent()
        {
            Indent--;
        }
    }
}
