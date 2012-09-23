// SlideOptions.cs
// Script#/Libraries/jQuery/UI
// This source code is subject to terms and conditions of the Apache License, Version 2.0.
//

using System;
using System.Runtime.CompilerServices;

namespace jQueryApi.UI.Effects {

    /// <summary>
    /// Options used to initialize or customize Slide.
    /// </summary>
    [ScriptImport]
    [ScriptIgnoreNamespace]
    [ScriptName("Object")]
    public sealed class SlideOptions {

        public SlideOptions() {
        }

        public SlideOptions(params object[] nameValuePairs) {
        }

        /// <summary>
        /// A callback function, executed when the effect completes
        /// </summary>
        [ScriptProperty]
        public Action Complete {
            get {
                return null;
            }
            set {
            }
        }

        /// <summary>
        /// The direction of the effect. Can be "left", "right", "up", "down".
        /// </summary>
        [ScriptProperty]
        public string Direction {
            get {
                return null;
            }
            set {
            }
        }

        /// <summary>
        /// The distance of the effect. Is set to either the height or width of the elemenet depending on the direction option. Can be set to any integer less than the width/height of the element.
        /// </summary>
        [ScriptProperty]
        public int Distance {
            get {
                return 0;
            }
            set {
            }
        }

        /// <summary>
        /// The number of ms the animation will run for
        /// </summary>
        [ScriptProperty]
        public int Duration {
            get {
                return 0;
            }
            set {
            }
        }

        /// <summary>
        /// The easing function to use
        /// </summary>
        [ScriptProperty]
        public string Easing {
            get {
                return null;
            }
            set {
            }
        }

        /// <summary>
        /// The mode of the effect. Can be "show" or "hide"
        /// </summary>
        [ScriptProperty]
        public string Mode {
            get {
                return null;
            }
            set {
            }
        }
    }
}
