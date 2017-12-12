//-------------------------------------------------------------------------- 
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: NativeMethods.cs 
//			C# file for the DisplaySettingsSample application
// 
//-------------------------------------------------------------------------- 

// This file includes some changes
//
using System;
using System.Runtime.InteropServices;

namespace DisplaySettingsAPI
{
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
	public struct DEVMODE 
	{
		[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]
		public string dmDeviceName;
		
		public short  dmSpecVersion;
		public short  dmDriverVersion;
		public short  dmSize;
		public short  dmDriverExtra;
		public int    dmFields;
		public int    dmPositionX;
		public int    dmPositionY;
		public int    dmDisplayOrientation;
		public int    dmDisplayFixedOutput;
		public short  dmColor;
		public short  dmDuplex;
		public short  dmYResolution;
		public short  dmTTOption;
		public short  dmCollate;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string dmFormName;

		public short  dmLogPixels;
		public short  dmBitsPerPel;
		public int    dmPelsWidth;
		public int    dmPelsHeight;
		public int    dmDisplayFlags;
		public int    dmDisplayFrequency;
		public int    dmICMMethod;
		public int    dmICMIntent;
		public int    dmMediaType;
		public int    dmDitherType;
		public int    dmReserved1;
		public int    dmReserved2;
		public int    dmPanningWidth;
		public int    dmPanningHeight;
	};


    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;

        public DISPLAY_DEVICE(int flags)
        {
            cb = 0;
            StateFlags = flags;
            DeviceName = new string((char)32, 32);
            DeviceString = new string((char)32, 128);
            DeviceID = new string((char)32, 128);
            DeviceKey = new string((char)32, 128);
            cb = Marshal.SizeOf(this);
        }
    };

	public class NativeMethods
	{
        // PInvoke declaration for EnumDisplayDevices (2) Win32 API
        [DllImport("User32.dll")]
        public static extern bool EnumDisplayDevices(
            IntPtr lpDevice, int iDevNum,
            ref DISPLAY_DEVICE lpDisplayDevice, int dwFlags);

		// PInvoke declaration for EnumDisplaySettings Win32 API
		[DllImport("user32.dll", CharSet=CharSet.Ansi)]
		public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);         

		// PInvoke declaration for ChangeDisplaySettings Win32 API
		[DllImport("user32.dll", CharSet=CharSet.Ansi)]
		public static extern int ChangeDisplaySettings(ref DEVMODE lpDevMode, int dwFlags);


        // Added
        // PInvoke declaration for changeDisplaySettingsEx Win32 API
        [DllImport("user32.dll")]
        public static extern int ChangeDisplaySettingsEx(
            string lpszDeviceName, ref DEVMODE lpDevMode,
            IntPtr hwnd, uint dwflags, IntPtr lParam);



        // helper for creating an initialized DEVMODE structure
		public static DEVMODE CreateDevmode()
		{
			DEVMODE dm = new DEVMODE();
			dm.dmDeviceName = new String(new char[32]);
			dm.dmFormName = new String(new char[32]);
			dm.dmSize = (short)Marshal.SizeOf(dm);
			return dm;
		}

		// constants
		public const int ENUM_CURRENT_SETTINGS = -1;
		public const int DISP_CHANGE_SUCCESSFUL = 0;
		public const int DISP_CHANGE_BADDUALVIEW = -6;
		public const int DISP_CHANGE_BADFLAGS = -4;
		public const int DISP_CHANGE_BADMODE = -2;
		public const int DISP_CHANGE_BADPARAM = -5;
		public const int DISP_CHANGE_FAILED = -1;
		public const int DISP_CHANGE_NOTUPDATED = -3;
		public const int DISP_CHANGE_RESTART = 1;
		public const int DMDO_DEFAULT = 0;
		public const int DMDO_90 = 1;
		public const int DMDO_180 = 2;
		public const int DMDO_270 = 3;

        public const uint CDS_UPDATEREGISTRY = 0x1;
        public const uint CDS_TEST = 0x2;
        public const uint CDS_FULLSCREEN = 0x4;
        public const uint CDS_GLOBAL = 0x8;
        public const uint CDS_SET_PRIMARY = 0x10;
        public const uint CDS_RESET = 0x40000000;
        public const uint CDS_SETRECT = 0x20000000;
        public const uint CDS_NORESET = 0x10000000;
        
	}
}
