// CodeModelProcessor.cs
// Script#/Core/Compiler
// This source code is subject to terms and conditions of the Apache License, Version 2.0.
//

using System;
using System.Diagnostics;
using System.Reflection;

namespace ScriptSharp.CodeModel
{
    internal sealed class CodeModelProcessor
    {
        private IParseNodeHandler parseNodeHandler;
        private object context;
        private bool notifyChildren;

        public CodeModelProcessor(IParseNodeHandler nodeHandler, object context)
        {
            parseNodeHandler = nodeHandler;
            this.context = context;
            notifyChildren = nodeHandler.RequiresChildrenGrouping;
        }

        private void EndChildren()
        {
            if (notifyChildren)
            {
                parseNodeHandler.EndChildren();
            }
        }

        public void Process(ParseNode node)
        {
            Visit(node);
        }

        private bool ProcessNode(ParseNode node)
        {
            return parseNodeHandler.HandleNode(node, context);
        }

        private void StartChildren(string identifier)
        {
            if (notifyChildren)
            {
                parseNodeHandler.StartChildren(identifier);
            }
        }

        private void Visit(ParseNode node)
        {
            bool recurse = ProcessNode(node);

            if (recurse)
            {
                StartChildren(String.Empty);

                Type nodeType = node.GetType();
                foreach (PropertyInfo propertyInfo in nodeType.GetProperties())
                {
                    string propertyName = propertyInfo.Name;
                    if (propertyName.Equals("NodeType"))
                    {
                        continue;
                    }
                    if (propertyName.Equals("Parent"))
                    {
                        continue;
                    }
                    if (propertyName.Equals("Token"))
                    {
                        continue;
                    }

                    Visit(node, propertyInfo);
                }

                EndChildren();
            }
        }

        private void Visit(ParseNode node, PropertyInfo propertyInfo)
        {
            string name = propertyInfo.Name;
            object value = propertyInfo.GetValue(node, null);

            string text = name + " (" + propertyInfo.PropertyType.Name + ")";

            if (value != null)
            {
                if (value is ParseNodeList)
                {
                    ParseNodeList nodeList = (ParseNodeList)value;

                    if (nodeList.Count == 0)
                    {
                        text += " : Empty";
                    }
                    else
                    {
                        text += " : " + nodeList.Count.ToString();
                    }

                    StartChildren(text);
                    foreach (ParseNode nodeItem in nodeList)
                    {
                        Visit(nodeItem);
                    }
                    EndChildren();
                }
                else if (value is ParseNode)
                {
                    StartChildren(text);
                    Visit((ParseNode)value);
                    EndChildren();
                }
                else
                {
                    if (value is string)
                    {
                        text += " : \"" + (string)value + "\"";
                    }
                    else
                    {
                        text += " : " + value.ToString();
                    }

                    StartChildren(text);
                    EndChildren();
                }
            }
            else
            {
                text += " : null";
                StartChildren(text);
                EndChildren();
            }
        }
    }
}
