using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DisplaySettingsAPI;

namespace FESRes
{
    /// <summary>
    /// A helper object for keeping DEVMODE objects with a human readable format explanation string
    /// </summary>
    public class DisplayMode
    {
        /// <summary>
        /// API DEVMODE object containing display settings
        /// </summary>
        public DEVMODE DevMode;

        /// <summary>
        /// Human readable format version of DEVMODE
        /// </summary>
        public string HRMode;

        /// <summary>
        /// Construct object
        /// </summary>
        /// <param name="devMode">Win32 API DEVMODE</param>
        /// <param name="hrMode">Human readable display mode description</param>
        public DisplayMode(DEVMODE devMode, string hrMode)
        {
            this.DevMode = devMode;
            this.HRMode = hrMode;
        }
    }
}
