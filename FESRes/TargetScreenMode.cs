using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DisplaySettingsAPI;

namespace FESRes
{
    /// <summary>
    /// A helper object to store information describing a desired screenmode.
    /// </summary>
    class TargetDisplayMode
    {

        public int TargetWidth, TargetHeight, TargetBitDepth, TargetRefreshRate;
        public DisplayRotation TargetRotation;
        public bool SetRotation = false;
        public bool Success;

        /// <summary>
        /// Construct with width/height/bit depth and refresh rate parameters
        /// </summary>
        /// <param name="targetWidth"></param>
        /// <param name="targetHeight"></param>
        /// <param name="targetBitDepth"></param>
        /// <param name="targetRefreshRate"></param>
        public TargetDisplayMode(int targetWidth, int targetHeight, int targetBitDepth, int targetRefreshRate)
        {
            this.TargetWidth = targetWidth;
            this.TargetHeight = targetHeight;
            this.TargetBitDepth = targetBitDepth;
            this.TargetRefreshRate = targetRefreshRate;
        }

        /// <summary>
        /// Construct with width/heigh/bit depth/refresh rate and rotation parameters
        /// </summary>
        /// <param name="targetWidth"></param>
        /// <param name="targetHeight"></param>
        /// <param name="targetBitDepth"></param>
        /// <param name="targetRefreshRate"></param>
        /// <param name="targetRotation"></param>
        public TargetDisplayMode(int targetWidth, int targetHeight, int targetBitDepth, int targetRefreshRate, DisplayRotation targetRotation)
            : this(targetWidth, targetHeight, targetBitDepth, targetRefreshRate)
        {
            this.TargetRotation = targetRotation;
            this.SetRotation = true;
        }

        /// <summary>
        /// Construct object using a Win32 API DEVMODE descriptor
        /// </summary>
        /// <param name="devMode"></param>
        public TargetDisplayMode(DEVMODE devMode)
            : this(devMode.dmPelsWidth, devMode.dmPelsHeight, devMode.dmBitsPerPel, devMode.dmDisplayFrequency, (DisplayRotation)devMode.dmDisplayOrientation)
        {
        }

        /// <summary>
        /// Return a human readable format describing the desired screenmode
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return TargetWidth.ToString() +
                " x " + TargetHeight.ToString() +
                ", " + TargetBitDepth.ToString() +
                " bits, " +
                TargetRefreshRate.ToString() + " Hz" +
                " (Orientation: " + TargetRotation.ToString() + ")";
        }
    }
}
