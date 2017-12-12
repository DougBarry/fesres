using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DisplaySettingsAPI;

namespace FESRes
{
    /// <summary>
    /// Used to describe the operation desired on the command line
    /// </summary>
    enum OperationType
    {
        Query,
        SetRes,
        DetectAndSet,
        Detect,
        Current,
        Duplication,
        Emergency,
        ShowHelp,
        None
    }

    /// <summary>
    /// How verbose we should be
    /// </summary>
    enum OutputFormat
    {
        Silent = 0,
        Bare = 1,
        Errors = 2,
        Warnings = 3,
        Info = 4,
        Max = 5
    }

    /// <summary>
    /// Human readable description of screen rotation
    /// </summary>
    enum DisplayRotation
    {
        Default = NativeMethods.DMDO_DEFAULT,
        UpsideDown = NativeMethods.DMDO_180,
        AntiClockwise = NativeMethods.DMDO_90,
        Clockwise = NativeMethods.DMDO_270
    }


    enum DuplicationMode
    {
        Unchanged,
        Internal,
        External,
        Extend,
        Duplicate
    }
}
