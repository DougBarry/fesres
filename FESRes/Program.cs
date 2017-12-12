//-----------------------------------------------------------------------------
// FESRES v0.3
// Douglas Barry (bd59@gre.ac.uk) University of Greenwich
// 
// A command line utility to detect, report and set display mode properties on Windows 7 and above.
//
// Capabilities:
//  Detect highest available display mode on any number of active displays
//  Detect/Set specific display mode on any number of active displays
//  Set specific rotation on any number of displays
//  Report current display mode information to stdout, optionally in a script suited format
//
// TODO:
//  Include ability to set display brightness (PoC code exists)
//-----------------------------------------------------------------------------

using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using DisplaySettingsAPI;
using System.Threading.Tasks;
using System.Diagnostics;

namespace FESRes
{
    class Program
    {
        /// <summary>
        /// Operation to be perfomed after command line options analysis
        /// </summary>
        private static OperationType _operation = OperationType.None;

        /// <summary>
        /// Limit operations to the Primary (0) display
        /// </summary>
        private static bool _onlyPrimary = false;

        /// <summary>
        /// The format for the output to stdout
        /// </summary>
        private static OutputFormat _outputFormat = OutputFormat.Info;

        /// <summary>
        /// A cache for the display count
        /// </summary>
        private static int _displayCount = 0;

        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="args">Incomming command line argments</param>
        /// <returns>Return value (Error level)</returns>
        static int Main(string[] args)
        {
            string[] arguments = Environment.GetCommandLineArgs();

            Dictionary<int, TargetDisplayMode> displaySettingsTargets = new Dictionary<int, TargetDisplayMode>();
            DuplicationMode targetDuplication = DuplicationMode.Unchanged;

            for (int i = 1; i < arguments.Length; i++)
            {
                if (arguments[i].ToLower() == "-?")
                {
                    _operation = OperationType.ShowHelp;
                    break;
                }
            }

            if (_operation == OperationType.ShowHelp)
            {
                ShowCLIHelp();
                KeyWait();
                return 0;
            }

            _displayCount = GetDisplayCount();
            List<string> unknownOptions = new List<string>();

            // process command line args
            for (int i = 1; i < arguments.Length; i++)
            {

                // Verbosity
                if (arguments[i].ToLower() == "-v")
                {
                    _outputFormat = OutputFormat.Max;
                    continue;
                }

                // For piping
                if (arguments[i].ToLower() == "-b")
                {
                    _outputFormat = OutputFormat.Bare;
                    continue;
                }

                // Silent
                if (arguments[i].ToLower() == "-t")
                {
                    _outputFormat = OutputFormat.Max;
                    continue;
                }

                // Limit operations to primary
                if (arguments[i].ToLower() == "-p")
                {
                    _onlyPrimary = true;
                    continue;
                }

                // Duplication modes
                if (arguments[i].ToLower() == "-l")
                {
                    if (_operation != OperationType.None)
                    {
                        OutputError("Parameter: -l is exclusive and cannot be combined with other oprerations");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 9;
                    }

                    _operation = OperationType.Duplication;

                    // Duplication parameter
                    i++;
                    string paramListLong;

                    try
                    {
                        paramListLong = arguments[i];
                    }
                    catch (Exception e)
                    {
                        // probably no arguments
                        OutputError("Duplication switch used, but no duplication definition followed. Please specify.");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 2;
                    }

                    try
                    {
                        // Param from enum

                        if (!Enum.TryParse<DuplicationMode>(paramListLong.ToLower(), true,out targetDuplication))

                        {
                            OutputError("Duplication type " + paramListLong + " unknown. Please specify known duplication mode.");
                            ShowCLIHelpHint(true);
                            KeyWait();
                            return 2;
                        }
                    }
                    catch (Exception e)
                    {
                        OutputError("Unable to process target display settings, please check syntax.");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 8;
                    }

                    continue;
                }

                // Report current display modes
                if (arguments[i].ToLower() == "-g")
                {
                    if (_operation != OperationType.None)
                    {
                        OutputError("Parameter: -g is exclusive and cannot be combined with other oprerations");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 9;
                    }

                    _operation = OperationType.Current;
                    continue;
                }

                // Report current display modes bare format
                if (arguments[i].ToLower() == "-c")
                {
                    if (_operation != OperationType.None)
                    {
                        OutputError("Parameter: -c is exclusive and cannot be combined with other oprerations");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 9;
                    }

                    _operation = OperationType.Current;
                    _outputFormat = OutputFormat.Bare;

                    continue;
                }

                // Emergency restore to compatible
                if (arguments[i].ToLower() == "-e")
                {
                    if (_operation != OperationType.None)
                    {
                        OutputError("Parameter: -e is exclusive and cannot be combined with other oprerations");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 9;
                    }

                    _operation = OperationType.Emergency;

                    continue;
                }

                // Report available display settings
                if (arguments[i].ToLower() == "-q")
                {
                    if (_operation != OperationType.None)
                    {
                        OutputError("Parameter: -q is exclusive and cannot be combined with other oprerations");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 9;
                    }

                    _operation = OperationType.Query;

                    continue;
                }

                // Report available display settings in bare format
                if (arguments[i].ToLower() == "-w")
                {
                    if (_operation != OperationType.None)
                    {
                        OutputError("Parameter: -w is exclusive and cannot be combined with other oprerations");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 9;
                    }

                    _operation = OperationType.Query;
                    _outputFormat = OutputFormat.Bare;

                    continue;
                }

                // Detect and report best available display settings
                if (arguments[i].ToLower() == "-d")
                {
                    if (_operation != OperationType.None)
                    {
                        OutputError("Parameter: -d is exclusive and cannot be combined with other oprerations");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 9;
                    }

                    _operation = OperationType.Detect;

                    continue;
                }

                // Detect best available display settings and apply them
                if (arguments[i].ToLower() == "-x")
                {
                    if (_operation != OperationType.None)
                    {
                        OutputError("Parameter: -x is exclusive and cannot be combined with other oprerations");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 9;
                    }

                    _operation = OperationType.DetectAndSet;

                    continue;
                }

                // Set specific display settings
                if (arguments[i].ToLower() == "-s")
                {
                    if ((_operation != OperationType.None) && (_operation != OperationType.SetRes))
                    {
                        OutputError("Parameter: -s is partly exclusive and cannot be combined with oprerations other than -r");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 9;
                    }

                    _operation = OperationType.SetRes;

                    // resolution parameters
                    i++;
                    string paramListLong;

                    try
                    {
                        paramListLong = arguments[i];
                    }
                    catch (Exception e)
                    {
                        // probably no arguments
                        OutputError("Set resolution switch used, but no resolutions definition followed. Please specify.");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 2;
                    }

                    string[] paramList = paramListLong.Split(',');

                    if (paramList.Length == 0)
                    {
                        OutputError("Set resolution switch used, but no resolutions definition followed. Please specify.");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 2;
                    }

                    if (paramList.Length < 5)
                    {
                        OutputError("Set resolution switch used, but incorrect resolutions definition followed.");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 2;
                    }

                    // multiple of 5 check
                    if ((paramList.Length % 5) != 0)
                    {
                        OutputError("Set resolution switch used, but incorrect resolutions definition followed. Comma seperated argument count must be a multiple of 5.");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 2;
                    }

                    bool finished = false;
                    int ii = 0;

                    int targetDisplay = 0;

                    do
                    {
                        try
                        {

                            // param0 = target display
                            // param1 = width
                            // param2 = height
                            // param3 = bitdepth
                            // param4 = refreshrate

                            int targetWidth, targetHeight, targetBitDepth, targetRefreshRate;
                            int.TryParse(paramList[ii++], out targetDisplay);
                            int.TryParse(paramList[ii++], out targetWidth);
                            int.TryParse(paramList[ii++], out targetHeight);
                            int.TryParse(paramList[ii++], out targetBitDepth);
                            int.TryParse(paramList[ii++], out targetRefreshRate);

                            // pre check validity

                            if (targetDisplay > _displayCount)
                            {
                                OutputError("Target display: " + targetDisplay + " outside known display count: " + _displayCount);
                                ShowCLIHelpHint(true);
                                KeyWait();
                                return 3;
                            }
                            if ((targetWidth < DefaultValues.RES_WIDTHMIN) || (targetWidth > DefaultValues.RES_WIDTHMAX))
                            {
                                OutputError("Target width: " + targetWidth + " outside range " + DefaultValues.RES_WIDTHMIN + "-" + DefaultValues.RES_WIDTHMAX + ".");
                                ShowCLIHelpHint(true);
                                KeyWait();
                                return 4;
                            }
                            if ((targetHeight < DefaultValues.RES_HEIGHTMIN) || (targetHeight > DefaultValues.RES_HEIGHTMAX))
                            {
                                OutputError("Target height: " + targetWidth + " outside range " + DefaultValues.RES_HEIGHTMIN + "-" + DefaultValues.RES_HEIGHTMAX + ".");
                                ShowCLIHelpHint(true);
                                KeyWait();
                                return 5;
                            }
                            if (!DefaultValues.VALID_BITDEPTHS.Contains(targetBitDepth))
                            {
                                OutputError("Target bit depth: " + targetBitDepth + " outside range: [" + string.Join(",", DefaultValues.VALID_BITDEPTHS) + "]");
                                ShowCLIHelpHint(true);
                                KeyWait();
                                return 6;
                            }
                            if ((targetRefreshRate < DefaultValues.RES_REFRESHRATEMIN) || (targetRefreshRate > DefaultValues.RES_REFRESHRATEMAX))
                            {
                                OutputError("Target refresh rate: " + targetWidth + " outside range " + DefaultValues.RES_REFRESHRATEMIN + "-" + DefaultValues.RES_REFRESHRATEMAX + ".");
                                ShowCLIHelpHint(true);
                                KeyWait();
                                return 7;
                            }


                            if (displaySettingsTargets.Keys.Contains(targetDisplay))
                            {
                                // already setting a rotation for this display, set resolution also
                                displaySettingsTargets[targetDisplay].TargetWidth = targetWidth;
                                displaySettingsTargets[targetDisplay].TargetHeight = targetHeight;
                                displaySettingsTargets[targetDisplay].TargetBitDepth = targetBitDepth;
                                displaySettingsTargets[targetDisplay].TargetRefreshRate = targetRefreshRate;
                            }
                            else
                            {
                                // should get current display mode rotation and apply resolution
                                //FIXME

                                displaySettingsTargets.Add(targetDisplay, new TargetDisplayMode(targetWidth, targetHeight, targetBitDepth, targetRefreshRate));
                            }

                        }
                        catch (Exception e)
                        {
                            OutputError("Unable to process target display settings, please check syntax.");
                            ShowCLIHelpHint(true);
                            KeyWait();
                            return 8;
                        }

                        if (ii >= paramList.Length)
                        {
                            finished = true;
                            i++;
                        }
                    } while (!finished);
                    continue;
                }

                // Set specific display rotations
                if (arguments[i].ToLower() == "-r")
                {
                    if (_operation != OperationType.SetRes)
                    {
                        if (_operation != OperationType.None)
                        {
                            OutputError("Parameter: -r is partly exclusive and cannot be combined with oprerations other than -s");
                            ShowCLIHelpHint(true);
                            KeyWait();
                            return 9;
                        }
                    }

                    _operation = OperationType.SetRes;

                    // resolution parameters
                    i++;
                    string paramListLong;

                    try
                    {
                        paramListLong = arguments[i];
                    }
                    catch (Exception e)
                    {
                        // probably no arguments
                        OutputError("Set rotations switch used, but no rotations definition followed. Please specify.");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 2;
                    }

                    string[] paramList = paramListLong.Split(',');

                    if (paramList.Length == 0)
                    {
                        OutputError("Set rotations switch used, but no rotations definition followed. Please specify.");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 2;
                    }

                    if (paramList.Length < 2)
                    {
                        OutputError("Set rotations switch used, but incorrect rotations definition followed.");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 2;
                    }

                    // multiple of 5 check
                    if ((paramList.Length % 2) != 0)
                    {
                        OutputError("Set rotations switch used, but incorrect rotations definition followed. Comma seperated argument count must be a multiple of 2.");
                        ShowCLIHelpHint(true);
                        KeyWait();
                        return 2;
                    }

                    bool finished = false;
                    int ii = 0;

                    int targetDisplay = 0;

                    do
                    {
                        try
                        {

                            // param0 = target display
                            // param1 = rotation (deg) 0,90,180,270

                            int targetRotation;
                            int.TryParse(paramList[ii++], out targetDisplay);
                            int.TryParse(paramList[ii++], out targetRotation);

                            DisplayRotation rotation;

                            // pre check validity

                            if (targetDisplay > _displayCount)
                            {
                                OutputError("Target display: " + targetDisplay + " outside known display count: " + _displayCount);
                                ShowCLIHelpHint(true);
                                KeyWait();
                                return 3;
                            }

                            switch (targetRotation)
                            {
                                case 0:
                                    rotation = DisplayRotation.Default;
                                    break;
                                case 90:
                                    rotation = DisplayRotation.Clockwise;
                                    break;
                                case 180:
                                    rotation = DisplayRotation.UpsideDown;
                                    break;
                                case 270:
                                    rotation = DisplayRotation.AntiClockwise;
                                    break;
                                default:
                                    OutputError("Target rotation: " + targetRotation + " outside range: [" + string.Join(",", DefaultValues.VALID_ROTATIONS) + "]");
                                    ShowCLIHelpHint(true);
                                    return 4;
                            }

                            if (displaySettingsTargets.Keys.Contains(targetDisplay))
                            {
                                // already setting a resolution for this display, set rotation also
                                displaySettingsTargets[targetDisplay].TargetRotation = rotation;
                            }
                            else
                            {
                                // get current display mode and apply rotation
                                DEVMODE dm = GetCurrentMode(targetDisplay);
                                TargetDisplayMode targetMode = new TargetDisplayMode(dm);
                                targetMode.TargetRotation = rotation;
                                displaySettingsTargets.Add(targetDisplay, targetMode);
                            }

                        }
                        catch (Exception e)
                        {
                            OutputError("Unable to process target display rotation settings, please check syntax.");
                            ShowCLIHelpHint(true);
                            KeyWait();
                            return 5;
                        }

                        if (ii >= paramList.Length)
                            finished = true;
                    } while (!finished);
                    continue;
                }

                // Unknown option handling
                unknownOptions.Add(arguments[i]);
            }

            ShowCLIHeader();

            if (unknownOptions.Count > 0)
            {
                OutputError("Unknown options: " + string.Join(" ", unknownOptions.ToArray()));
                ShowCLIHelpHint(true);
                KeyWait();
                return 10;
            }

            OutputMessage("Detected display count: " + _displayCount, OutputFormat.Info);

            if (_onlyPrimary)
                OutputMessage("Limiting to primary output device.", OutputFormat.Warnings);

            int val = 0;

            switch (_operation)
            {
                default:
                case OperationType.ShowHelp:
                    ShowCLIHelp();
                    KeyWait();
                    return 0;
                case OperationType.Query:
                    val = ReportAllDisplayModes();
                    KeyWait();
                    return val;
                case OperationType.Current:
                    val = ReportCurrentDisplay();
                    KeyWait();
                    return val;
                case OperationType.Detect:
                    val = ShowDisplayDetect();
                    KeyWait();
                    return val;
                case OperationType.DetectAndSet:
                    val = DetectAndSet();
                    KeyWait();
                    return val;
                case OperationType.SetRes:
                    val = SetSpecificResolution(displaySettingsTargets);
                    KeyWait();
                    return val;
                case OperationType.Duplication:
                    val = (SetDisplayDuplicationMode(targetDuplication) ? 1 : 0);
                    KeyWait();
                    return val;
                case OperationType.Emergency:
                    val = SetEmergencyRes();
                    KeyWait();
                    return val;
            }
        }

        /// <summary>
        /// Hold the command window open when debugging, until keypress or timeout.
        /// </summary>
        [Conditional("DEBUG")]
        private static void KeyWait()
        {
            Task.Factory.StartNew(() => Console.ReadKey()).Wait(TimeSpan.FromSeconds(30.0));
        }

        /// <summary>
        /// Set all display adaptor resolutions to 800x600, 16bit, 60Hz and no rotation
        /// </summary>
        /// <returns>Error level</returns>
        private static int SetEmergencyRes()
        {
            Dictionary<int, TargetDisplayMode> resTargets = new Dictionary<int, TargetDisplayMode>();

            for (int targetDisplay = 0; targetDisplay < _displayCount; targetDisplay++)
                resTargets.Add(targetDisplay, new TargetDisplayMode(800, 600, 16, 60, 0));

            return SetSpecificResolution(resTargets);
        }

        /// <summary>
        /// Reports the current resoltion and rotation settings of all available displays to stdout
        /// </summary>
        /// <returns>Error level</returns>
        private static int ReportCurrentDisplay()
        {
            for (int i = 0; i < _displayCount; i++)
            {
                DEVMODE dm = GetCurrentMode(i);
                OutputMessage(i + "," + DevmodeToString(dm, true, (_outputFormat == OutputFormat.Bare)), 0);
            }

            return 0;
        }

        /// <summary>
        /// Sets the display duplication settings
        /// </summary>
        /// <param name="duplicationMode">Enum for setting the target duplication type</param>
        /// <returns>Success</returns>
        private static bool SetDisplayDuplicationMode(DuplicationMode duplicationMode)
        {
            Process dispswitch = new Process();
            dispswitch.StartInfo.FileName = "DisplaySwitch.exe";
            switch (duplicationMode)
            {
                case DuplicationMode.External:
                    dispswitch.StartInfo.Arguments = "/external";
                    break;
                case DuplicationMode.Internal:
                    dispswitch.StartInfo.Arguments = "/internal";
                    break;
                case DuplicationMode.Extend:
                    dispswitch.StartInfo.Arguments = "/extend";
                    break;
                case DuplicationMode.Duplicate:
                    dispswitch.StartInfo.Arguments = "/clone";
                    break;
                default:
                case DuplicationMode.Unchanged:
                    // Do nothing
                    break;
            }
            return dispswitch.Start();
        }


        /// <summary>
        /// Apply specified resolutions (and optionally rotations) to displays
        /// </summary>
        /// <param name="targets">Target display mode definitions</param>
        /// <returns>Error level</returns>
        private static int SetSpecificResolution(Dictionary<int, TargetDisplayMode> targets)
        {
            int newHeight = 0, newWidth = 0;
            DisplayRotation newOrientation = DisplayRotation.Default;

            foreach (KeyValuePair<int, TargetDisplayMode> target in targets)
            {

                if (_onlyPrimary && target.Key != 0)
                    break;

                try
                {

                    string displayName = @"\\.\DISPLAY" + (target.Key + 1);

                    // work out new resolutions, then apply rotations

                    Dictionary<int, List<DisplayMode>> allModes = GetAllResolutions();

                    foreach (KeyValuePair<int, List<DisplayMode>> deviceModes in allModes)
                    {
                        if (deviceModes.Key != target.Key)
                        {
                            continue;
                        }

                        List<DisplayMode> availableModes = deviceModes.Value;

                        foreach (DisplayMode mode in availableModes)
                        {
                            if ((mode.DevMode.dmBitsPerPel == target.Value.TargetBitDepth) &&
                                (mode.DevMode.dmDisplayFrequency == target.Value.TargetRefreshRate) &&
                                    (mode.DevMode.dmPelsWidth == target.Value.TargetWidth) &&
                                    (mode.DevMode.dmPelsHeight == target.Value.TargetHeight))
                            {
                                // we have a match for our desired mode, this display adaptor supports it.

                                // Work out if we need to swap the width/height and set the rotation in the devmode struct.
                                newOrientation = target.Value.TargetRotation;

                                DisplayRotation currentOrientation = (DisplayRotation)mode.DevMode.dmDisplayOrientation;

                                switch (currentOrientation)
                                {
                                    case DisplayRotation.Default:
                                    case DisplayRotation.UpsideDown:
                                        switch (newOrientation)
                                        {
                                            case DisplayRotation.UpsideDown:
                                            case DisplayRotation.Default:
                                                // No need to swap width/height
                                                newHeight = mode.DevMode.dmPelsHeight;
                                                newWidth = mode.DevMode.dmPelsWidth;
                                                break;
                                            case DisplayRotation.Clockwise:
                                            case DisplayRotation.AntiClockwise:
                                                // need to swap em
                                                newHeight = mode.DevMode.dmPelsWidth;
                                                newWidth = mode.DevMode.dmPelsHeight;
                                                break;
                                        }
                                        break;
                                    case DisplayRotation.AntiClockwise:
                                    case DisplayRotation.Clockwise:
                                        switch (newOrientation)
                                        {
                                            case DisplayRotation.UpsideDown:
                                            case DisplayRotation.Default:
                                                // swapping time
                                                newHeight = mode.DevMode.dmPelsWidth;
                                                newWidth = mode.DevMode.dmPelsHeight;
                                                break;
                                            case DisplayRotation.Clockwise:
                                            case DisplayRotation.AntiClockwise:
                                                // need to swap em
                                                newHeight = mode.DevMode.dmPelsHeight;
                                                newWidth = mode.DevMode.dmPelsWidth;
                                                break;
                                        }
                                        break;
                                }

                                // determine new orientation
                                switch (target.Value.TargetRotation)
                                {
                                    case DisplayRotation.AntiClockwise:
                                        // swap width and height
                                        mode.DevMode.dmDisplayOrientation = NativeMethods.DMDO_270;
                                        break;
                                    case DisplayRotation.UpsideDown:
                                        mode.DevMode.dmDisplayOrientation = NativeMethods.DMDO_180;
                                        break;
                                    case DisplayRotation.Clockwise:
                                        // swap width and height
                                        mode.DevMode.dmDisplayOrientation = NativeMethods.DMDO_90;
                                        break;
                                    case DisplayRotation.Default:
                                        mode.DevMode.dmDisplayOrientation = NativeMethods.DMDO_DEFAULT;
                                        break;
                                    default:
                                        // unknown orientation value
                                        // add exception handling here
                                        break;
                                }

                                mode.DevMode.dmPelsWidth = newWidth;
                                mode.DevMode.dmPelsHeight = newHeight;

                                int returnCode = NativeMethods.ChangeDisplaySettingsEx(displayName, ref mode.DevMode, IntPtr.Zero, NativeMethods.CDS_UPDATEREGISTRY & NativeMethods.CDS_TEST, IntPtr.Zero);
                                if (returnCode != NativeMethods.DISP_CHANGE_SUCCESSFUL)
                                {
                                    // error?
                                    Console.Error.WriteLine("Error setting display: " + target.Key + " to display mode: " + returnCode.ToString());
                                    return returnCode;
                                }
                                target.Value.Success = true;

                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error querying devices: " + e.Message);
                    return 1;
                }

            }
            foreach (KeyValuePair<int, TargetDisplayMode> target in targets)
            {
                if (!target.Value.Success)
                {
                    Console.Error.WriteLine("Display mode requested could not be matched: " + target.Value.ToString());
                }
            }


            return 0;
        }

        /// <summary>
        /// Reports all available display modes to stdout
        /// </summary>
        /// <returns>Error level</returns>
        private static int ReportAllDisplayModes()
        {
            try
            {
                Dictionary<int, List<DisplayMode>> allModes = GetAllResolutions();
                foreach (KeyValuePair<int, List<DisplayMode>> deviceModes in allModes)
                {
                    List<DisplayMode> availableModes = deviceModes.Value;

                    OutputMessage("Display: " + deviceModes.Key, OutputFormat.Info);

                    foreach (DisplayMode mode in availableModes)
                    {
                        if (_outputFormat == OutputFormat.Bare)
                        {
                            // pipeable
                            Console.WriteLine(deviceModes.Key + "," + DevmodeToString(mode.DevMode, false, true));
                        }
                        else
                        {
                            Console.WriteLine(DevmodeToString(mode.DevMode, true));
                        }
                    }
                    if (_onlyPrimary)
                        break;
                }

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error querying devices: " + e.Message);
                return 1;
            }

            return -1;
        }

        /// <summary>
        /// Send detected display information to stdout
        /// </summary>
        /// <returns>Error level</returns>
        private static int ShowDisplayDetect()
        {

            Dictionary<int, DisplayMode> highestModes = GetHighestResolution();

            foreach (KeyValuePair<int, DisplayMode> deviceMode in highestModes)
            {
                OutputMessage("Display: " + deviceMode.Key, OutputFormat.Info);
                OutputMessage("Highest compatible screen mode: " + deviceMode.Value.HRMode, OutputFormat.Info);

                if (_outputFormat == OutputFormat.Bare)
                {
                    // pipeable
                    Console.WriteLine(deviceMode.Key + "," + DevmodeToString(deviceMode.Value.DevMode, false, true));
                }

                if (_onlyPrimary)
                    break;
            }

            return -1;
        }

        /// <summary>
        /// Detect best available (highest) display mode for each display and apply
        /// </summary>
        /// <returns>Error level</returns>
        private static int DetectAndSet()
        {
            Dictionary<int, DisplayMode> highestModes = GetHighestResolution();

            foreach (KeyValuePair<int, DisplayMode> deviceModes in highestModes)
            {
                OutputMessage("Display: " + deviceModes.Key, OutputFormat.Info);
                OutputMessage("Highest compatible screen mode: " + deviceModes.Value.HRMode, OutputFormat.Info);
                OutputMessage("Setting display mode...", OutputFormat.Info);

                string displayName = @"\\.\DISPLAY" + (deviceModes.Key + 1);

                int returnCode = NativeMethods.ChangeDisplaySettingsEx(displayName, ref deviceModes.Value.DevMode, IntPtr.Zero, NativeMethods.CDS_UPDATEREGISTRY & NativeMethods.CDS_TEST, IntPtr.Zero);
                if (returnCode != NativeMethods.DISP_CHANGE_SUCCESSFUL)
                {
                    // error?
                    OutputError("Error setting display mode: " + returnCode.ToString());
                    return returnCode;
                }

                if (_onlyPrimary)
                    break;
            }

            return -1;
        }

        /// <summary>
        /// Get total available display count
        /// </summary>
        /// <returns>Number of available displays</returns>
        private static int GetDisplayCount()
        {
            Dictionary<int, List<DisplayMode>> allModes = GetAllResolutions();
            return allModes.Count;
        }

        /// <summary>
        /// Gets all available display resolutions for all available displays
        /// </summary>
        /// <returns>Dictionary containtg resolutions available on displays</returns>
        private static Dictionary<int, List<DisplayMode>> GetAllResolutions()
        {
            Dictionary<int, DISPLAY_DEVICE> devices = EnumDevices();
            Dictionary<int, List<DisplayMode>> allDevicesAndModes = new Dictionary<int, List<DisplayMode>>();


            if (devices.Count < 1)
            {
                //ShowError("No display adaptors found.");
                return null;
            }

            List<DisplayMode> availableModes = new List<DisplayMode>();

            foreach (KeyValuePair<int, DISPLAY_DEVICE> device in devices)
            {
                availableModes = EnumModes(device.Key);
                if (availableModes.Count == 0) continue;
                allDevicesAndModes.Add(device.Key, availableModes);
            }

            return allDevicesAndModes;
        }

        /// <summary>
        /// Finds the highest resolution for a given display
        /// </summary>
        /// <returns>Dictionary contatining highest resolution on display</returns>
        private static Dictionary<int, DisplayMode> GetHighestResolution()
        {
            Dictionary<int, List<DisplayMode>> allDevicesAndModes = GetAllResolutions();

            // discard anything non 32 bit and non 60Hz
            Dictionary<int, List<DisplayMode>> allDevicesAnd32BitModes = new Dictionary<int, List<DisplayMode>>();

            List<DisplayMode> availableModes = new List<DisplayMode>();

            foreach (KeyValuePair<int, List<DisplayMode>> deviceModes in allDevicesAndModes)
            {
                availableModes = deviceModes.Value;

                List<DisplayMode> modes = new List<DisplayMode>();

                foreach (DisplayMode mode in availableModes)
                {
                    if ((mode.DevMode.dmBitsPerPel == 32) && (mode.DevMode.dmDisplayFrequency == 60))
                    {
                        modes.Add(mode);
                    }
                }

                allDevicesAnd32BitModes.Add(deviceModes.Key, modes);

            }

            // find highest res per monitor
            Dictionary<int, DisplayMode> allDevicesAnd32BitModesHighestRes = new Dictionary<int, DisplayMode>();

            foreach (KeyValuePair<int, List<DisplayMode>> deviceModes in allDevicesAnd32BitModes)
            {
                availableModes = deviceModes.Value;

                DisplayMode highestRes = null;
                int highX = 0;
                int highY = 0;
                int highMult = 0;
                DEVMODE highDEVMODE = new DEVMODE();

                foreach (DisplayMode mode in availableModes)
                {
                    if ((mode.DevMode.dmPelsWidth * mode.DevMode.dmPelsHeight) >= highMult)
                    {
                        highMult = (mode.DevMode.dmPelsWidth * mode.DevMode.dmPelsHeight);
                        highX = mode.DevMode.dmPelsWidth;
                        highY = mode.DevMode.dmPelsHeight;
                        highDEVMODE = mode.DevMode;
                        highestRes = mode;
                    }
                }

                if (highestRes != null)
                {
                    allDevicesAnd32BitModesHighestRes.Add(deviceModes.Key, highestRes);
                }

            }

            return allDevicesAnd32BitModesHighestRes;
        }

        /// <summary>
        /// Enumerate display modes on a given display
        /// </summary>
        /// <param name="devNum">Device number</param>
        /// <returns>List of DisplayMode objects</returns>
        private static List<DisplayMode> EnumModes(int devNum)
        {
            List<DisplayMode> modes = new List<DisplayMode>();

            string devName = GetDeviceName(devNum);
            int modeNum = 0;
            bool result = true;
            do
            {
                DEVMODE devMode = new DEVMODE();
                result = NativeMethods.EnumDisplaySettings(devName,
                    modeNum, ref devMode);

                if (result)
                {
                    modes.Add(new DisplayMode(devMode, DevmodeToString(devMode, true)));
                }
                modeNum++;
            } while (result);

            return modes;
        }

        /// <summary>
        /// Helper method to retrieve populated DEVMODE struct from the Win32 API for a given mode on a given display
        /// </summary>
        /// <param name="devNum">Display number</param>
        /// <param name="modeNum">Mode number</param>
        /// <returns>Populated DEVMODE struct</returns>
        private static DEVMODE GetDevmode(int devNum, int modeNum)
        { //populates DEVMODE for the specified device and mode
            DEVMODE devMode = new DEVMODE();
            string devName = GetDeviceName(devNum);
            NativeMethods.EnumDisplaySettings(devName, modeNum, ref devMode);
            return devMode;
        }

        /// <summary>
        /// Retrieve DEVMODE struct describing the current mode for a given display
        /// </summary>
        /// <param name="devNum">Display number</param>
        /// <returns>Populated DEVMODE struct</returns>
        private static DEVMODE GetCurrentMode(int devNum)
        {
            return GetDevmode(devNum, -1);
        }

        /// <summary>
        /// Create a human/script readable form of a given populated DEVMODE struct
        /// </summary>
        /// <param name="devMode">DEVMODE struct</param>
        /// <param name="includeRotation">Output rotation information</param>
        /// <param name="bareFormat">Generate pipable style output</param>
        /// <returns>Humare readable string</returns>
        private static string DevmodeToString(DEVMODE devMode, bool includeRotation = false, bool bareFormat = false)
        {
            if (!bareFormat)
            {
                string ret = "";

                ret += devMode.dmPelsWidth.ToString() +
                " x " + devMode.dmPelsHeight.ToString() +
                ", " + devMode.dmBitsPerPel.ToString() +
                " bits, " +
                devMode.dmDisplayFrequency.ToString() + " Hz";
                if (includeRotation)
                    ret += " (Orientation: " + ((DisplayRotation)devMode.dmDisplayOrientation).ToString() + ")";
                return ret;
            }
            else
            {
                string ret = "";
                ret += devMode.dmPelsWidth.ToString() + "," + devMode.dmPelsHeight.ToString() + "," + devMode.dmBitsPerPel.ToString() + "," + devMode.dmDisplayFrequency.ToString();
                if (includeRotation)
                    ret += ":" + ScreenRotationToDegrees((DisplayRotation)devMode.dmDisplayOrientation).ToString();
                return ret;
            }
        }

        /// <summary>
        /// Helper method to convert DisplayRotation enum to human readable degrees
        /// </summary>
        /// <param name="screenRotation">DisplayRotation enum to convert</param>
        /// <returns></returns>
        private static int ScreenRotationToDegrees(DisplayRotation screenRotation)
        {
            switch (screenRotation)
            {
                default:
                case DisplayRotation.Default:
                    return 0;
                case DisplayRotation.UpsideDown:
                    return 180;
                case DisplayRotation.AntiClockwise:
                    return 90;
                case DisplayRotation.Clockwise:
                    return 270;
            }
        }

        /// <summary>
        /// Enumerate display devices via Win32 API
        /// </summary>
        /// <returns></returns>
        private static Dictionary<int, DISPLAY_DEVICE> EnumDevices()
        {
            //populates Display Devices list
            Dictionary<int, DISPLAY_DEVICE> devices = new Dictionary<int, DISPLAY_DEVICE>();

            int devNum = 0;
            bool result;
            do
            {
                DISPLAY_DEVICE d = new DISPLAY_DEVICE(0);

                result = NativeMethods.EnumDisplayDevices(IntPtr.Zero,
                    devNum, ref d, 0);

                if (result)
                {
                    string item = devNum.ToString() +
                        ". " + d.DeviceString.Trim();
                    if ((d.StateFlags & 4) != 0) item += " - primary";
                    //_availableModes.Add(dev);
                    devices.Add(devNum, d);
                }
                devNum++;
            } while (result);

            return devices;
        }

        /// <summary>
        /// Get device name from device number
        /// </summary>
        /// <param name="devNum">Device number</param>
        /// <returns></returns>
        private static string GetDeviceName(int devNum)
        {
            DISPLAY_DEVICE d = new DISPLAY_DEVICE(0);
            bool result = NativeMethods.EnumDisplayDevices(IntPtr.Zero,
                devNum, ref d, 0);
            return (result ? d.DeviceName.Trim() : "#error#");
        }

        /// <summary>
        /// Analyse device and work out if its the primary display
        /// </summary>
        /// <param name="devNum"></param>
        /// <returns></returns>
        private static bool MainDevice(int devNum)
        { //whether the specified device is the main device
            DISPLAY_DEVICE d = new DISPLAY_DEVICE(0);
            if (NativeMethods.EnumDisplayDevices(IntPtr.Zero, devNum, ref d, 0))
            {
                return ((d.StateFlags & 4) != 0);
            } return false;
        }

        /// <summary>
        /// Send header to stdout
        /// </summary>
        /// <param name="force">Force output even if bare formatting options are specified</param>
        private static void ShowCLIHeader(bool force = false)
        {
            if (!force)
            {
                if (_outputFormat <= OutputFormat.Bare)
                    return;
            }

            OutputMessage(GetExecutableName() + " by Doug Barry @ UOG FES 20150924");
        }

        /// <summary>
        /// Helper method to get exe name without extra stuff
        /// </summary>
        /// <returns>Exe name</returns>
        private static string GetExecutableName()
        {
            return Path.GetFileNameWithoutExtension(Path.GetFileName(Environment.GetCommandLineArgs()[0]));
        }

        /// <summary>
        /// Send help information to stdout
        /// </summary>
        /// <param name="force">Send even if bare formatting options are in place</param>
        private static void ShowCLIHelp(bool force = false)
        {
            if (!force)
            {
                if (_outputFormat <= OutputFormat.Bare)
                    return;
            }

            string screenDuplicationModes = "";
            screenDuplicationModes = string.Join(",", Enum.GetNames(typeof(DuplicationMode)));

            Console.WriteLine("Usage: " + GetExecutableName() + " <-q|-d|-x|-s [0,w,h,b,r,... n,w,h,b,r] -r [0,d,... n,d]> -l <mode> <-p>");
            Console.WriteLine("");
            Console.WriteLine(" -? \tDisplay this help screen\n");
            Console.WriteLine(" -g \tReport current display modes on all available displays.\n");
            Console.WriteLine(" -c \tReport current display modes on all available displays in bare format.\n");
            Console.WriteLine(" -e \tEmergency restore to compatible defaults. 800x600x16x60 no rotation on all display.\n");
            Console.WriteLine(" -q \tReport all available screen modes on all available displays.\n\t(WARNING: long) This option is exclusive.\n");
            Console.WriteLine(" -w \tReport all available screen modes on all available displays in bare format. This option is exclusive.\n");
            Console.WriteLine(" -d \tDetect highest available mode on all available displays and report.\n");
            Console.WriteLine(" -x \tDetect and set highest available mode on all available displays.\n\tWARNING on VGA outputs, this may render the display useless due\n\tto some video cards not detecting maximum resolutions correctly.\n");
            Console.WriteLine(" -p \tRestrict operations to primary output device.\n");
            Console.WriteLine(" -b \tRestrict console output to bare formats for piping.\n");
            Console.WriteLine("");
            Console.WriteLine(" -s MODES \tSet specific resolution to specfied displays, comma seperated\n\targuments. Cannot be used in conjunction with -x, -d, -q\n");
            Console.WriteLine(" -r MODES \tSet specific rotations on specfied displays, comma seperated\n\targuments. Cannot be used in conjunction with -x, -d, -q\n");
            Console.WriteLine(" -l MODE \tSet screen duplication mode. Supported modes: " + screenDuplicationModes + "\n");
            Console.WriteLine("");
            Console.WriteLine("Notes:\tDuring set operation, monitors without a specified resolution will be\n\tunaltered. Output devices are enumerated beginning with 0.");
            Console.WriteLine("\n");

            Console.WriteLine("Example: Set the primary display outputs resolution to the\n\tmaximum reported for the connected display.");
            Console.WriteLine("\t" + GetExecutableName() + " -p -x\n\n");

            Console.WriteLine("Example: Set the primary display outputs resolution to the\n\t1024x768 32 bit depth, 60Hz refresh rate and the second display\n\tto 1920x1200 32bit depth and 75Hz refresh rate.");
            Console.WriteLine("\t" + GetExecutableName() + " -s 0,1024,768,32,60,1,1920,1200,32,75\n\n");

            Console.WriteLine("Example: Set the primary display rotation to 90 degrees and the second displays\n\trotation to 180, and primary screen resolution to 1024x768x32x75.");
            Console.WriteLine("\t" + GetExecutableName() + " -r 0,90,1,180 -s 0,1024,768,32,75\n\n");
        }

        /// <summary>
        /// Show a help option hint
        /// </summary>
        /// <param name="force">Show even if bare formatting options are in place</param>
        private static void ShowCLIHelpHint(bool force = false)
        {
            if (!force)
            {
                if (_outputFormat <= OutputFormat.Bare)
                    return;
            }

            Console.WriteLine("Use -? for help.");
        }

        /// <summary>
        /// Send message to stdout
        /// </summary>
        /// <param name="message">Message</param>
        private static void OutputMessage(string message)
        {
            // Force output
            OutputMessage(message, 0);
        }

        /// <summary>
        /// Send message to stdout dependant on verbosity
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="verbosity">Verbosity required</param>
        private static void OutputMessage(string message, OutputFormat verbosity)
        {
            if (verbosity <= _outputFormat)
                Console.WriteLine(message);
        }

        /// <summary>
        /// Send message to stderr
        /// </summary>
        /// <param name="message"></param>
        private static void OutputError(string message)
        {
            if (_outputFormat != OutputFormat.Silent)
                Console.Error.WriteLine(message);
        }


    }
}