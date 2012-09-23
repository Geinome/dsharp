// VenueFloor.cs
// Script#/Libraries/Microsoft/BingMaps
// This source code is subject to terms and conditions of the Apache License, Version 2.0.
//

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Maps.VenueMaps {

    [ScriptImport]
    [ScriptIgnoreNamespace]
    [ScriptName("Object")]
    public sealed class VenueFloor {

        private VenueFloor() {
        }

        [ScriptName("primitives")]
        [ScriptProperty]
        public VenueEntity[] Entities {
            get {
                return null;
            }
        }

        [ScriptProperty]
        public string Name {
            get {
                return null;
            }
        }

        [ScriptProperty]
        public int[] ZoomRange {
            get {
                return null;
            }
        }
    }
}
