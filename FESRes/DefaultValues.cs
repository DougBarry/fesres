using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FESRes
{

    /// <summary>
    /// Global default values to be used in error checking/bounding
    /// </summary>
    public static class DefaultValues
    {
        public const int RES_WIDTHMIN = 320;        //Px
        public const int RES_HEIGHTMIN = 200;       //Px
        public const int RES_WIDTHMAX = 4096;       //Px
        public const int RES_HEIGHTMAX = 4096;      //Px

        public const int RES_REFRESHRATEMIN = 24;   //Hz
        public const int RES_REFRESHRATEMAX = 180;  //Hz

        public static readonly int[] VALID_BITDEPTHS = { 4, 8, 15, 16, 24, 32 };
        public static readonly int[] VALID_ROTATIONS = { 0, 90, 180, 270 };

    }
}
