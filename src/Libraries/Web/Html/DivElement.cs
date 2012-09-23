// DivElement.cs
// Script#/Libraries/Web
// This source code is subject to terms and conditions of the Apache License, Version 2.0.
//

using System;
using System.Runtime.CompilerServices;

namespace System.Html {

    [ScriptIgnoreNamespace]
    [ScriptImport]
    public sealed class DivElement : Element {

        private DivElement() {
        }

        [ScriptProperty]
        public string Align {
            get {
                return null;
            }
            set {
            }
        }

        [ScriptProperty]
        public bool NoWrap {
            get {
                return false;
            }
            set {
            }
        }
    }
}
