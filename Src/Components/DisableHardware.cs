﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace PicoGAUpdate.Components
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class DisableHardware
    {
        static DisableHardware()
        {
            DisableHardware.DEVPKEY_Device_DeviceDesc = new DEVPROPKEY();
            DEVPKEY_Device_DeviceDesc.fmtid = new Guid(
                    0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67,
                    0xd1, 0x46, 0xa8, 0x50, 0xe0);
            DEVPKEY_Device_DeviceDesc.pid = 2;

            DEVPKEY_Device_HardwareIds = new DEVPROPKEY();
            DEVPKEY_Device_HardwareIds.fmtid = new Guid(
                0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67,
                0xd1, 0x46, 0xa8, 0x50, 0xe0);
            DEVPKEY_Device_HardwareIds.pid = 3;
        }

        public static void DisableDevice(Func<string, bool> filter, bool disable = true)
        {
            IntPtr info = IntPtr.Zero;
            Guid NullGuid = Guid.Empty;
            try
            {
                info = SetupDiGetClassDevsW(
                    ref NullGuid,
                    null,
                    IntPtr.Zero,
                    DIGCF_ALLCLASSES);
                CheckError("SetupDiGetClassDevs");

                SP_DEVINFO_DATA devdata = new SP_DEVINFO_DATA();
                devdata.cbSize = (UInt32)Marshal.SizeOf(devdata);

                // Get first device matching device criterion.
                for (uint i = 0; ; i++)
                {
                    SetupDiEnumDeviceInfo(info,
                        i,
                        out devdata);
                    // if no items match filter, throw
                    if (Marshal.GetLastWin32Error() == ERROR_NO_MORE_ITEMS)
                        CheckError("No device found matching filter.", 0xcffff);
                    CheckError("SetupDiEnumDeviceInfo");

                    string devicepath = GetStringPropertyForDevice(info,
                                               devdata, 1); // SPDRP_HARDWAREID

                    // Uncomment to print name/path
                    //Console.WriteLine(GetStringPropertyForDevice(info,
                    //                         devdata, DEVPKEY_Device_DeviceDesc));
                    //Console.WriteLine("   {0}", devicepath);
                    if (devicepath != null && filter(devicepath)) break;
                }

                SP_CLASSINSTALL_HEADER header = new SP_CLASSINSTALL_HEADER();
                header.cbSize = (UInt32)Marshal.SizeOf(header);
                header.InstallFunction = DIF_PROPERTYCHANGE;

                SP_PROPCHANGE_PARAMS propchangeparams = new SP_PROPCHANGE_PARAMS();
                propchangeparams.ClassInstallHeader = header;
                propchangeparams.StateChange = disable ? DICS_DISABLE : DICS_ENABLE;
                propchangeparams.Scope = DICS_FLAG_GLOBAL;
                propchangeparams.HwProfile = 0;

                SetupDiSetClassInstallParams(info,
                    ref devdata,
                    ref propchangeparams,
                    (UInt32)Marshal.SizeOf(propchangeparams));
                CheckError("SetupDiSetClassInstallParams");

                SetupDiChangeState(
                    info,
                    ref devdata);
                CheckError("SetupDiChangeState");
            }
            finally
            {
                if (info != IntPtr.Zero)
                    SetupDiDestroyDeviceInfoList(info);
            }
        }

        private const uint DICS_DISABLE = 2;
        private const uint DICS_ENABLE = 1;

        // disable device
        private const uint DICS_FLAG_GLOBAL = 1;

        private const uint DIF_PROPERTYCHANGE = 0x12;

        // not profile-specific
        private const uint DIGCF_ALLCLASSES = 4;

        private const uint DIGCF_PRESENT = 2;
        private const uint ERROR_ELEMENT_NOT_FOUND = 1168;
        private const uint ERROR_INVALID_DATA = 13;
        private const uint ERROR_NO_MORE_ITEMS = 259;

        // ReSharper disable once NotAccessedField.Local
        private static DEVPROPKEY DEVPKEY_Device_DeviceDesc;

        // ReSharper disable once NotAccessedField.Local
        private static DEVPROPKEY DEVPKEY_Device_HardwareIds;

        private static void CheckError(string message, int lasterror = -1)
        {
            int code = lasterror == -1 ? Marshal.GetLastWin32Error() : lasterror;
            if (code != 0)
                throw new ApplicationException(
                    String.Format("Error disabling hardware device (Code {0}): {1}",
                        code, message));
        }

        private static string GetStringPropertyForDevice(IntPtr info, SP_DEVINFO_DATA devdata,
            uint propId)
        {
            // ReSharper disable once NotAccessedVariable
            uint proptype, outsize;
            IntPtr buffer = IntPtr.Zero;
            try
            {
                uint buflen = 512;
                buffer = Marshal.AllocHGlobal((int)buflen);
                outsize = 0;
                // CHANGE #2 - Use this instead of SetupDiGetDeviceProperty
                SetupDiGetDeviceRegistryPropertyW(
                    info,
                    ref devdata,
                    propId,
                    out proptype,
                    buffer,
                    buflen,
                    ref outsize);
                byte[] lbuffer = new byte[outsize];
                Marshal.Copy(buffer, lbuffer, 0, (int)outsize);
                int errcode = Marshal.GetLastWin32Error();
                if (errcode == ERROR_INVALID_DATA) return null;
                CheckError("SetupDiGetDeviceProperty", errcode);
                return Encoding.Unicode.GetString(lbuffer);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiChangeState(
            IntPtr deviceInfoSet,
            [In] ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr handle);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet,
            UInt32 memberIndex,
            [Out] out SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevsW(
            [In] ref Guid ClassGuid,
            [MarshalAs(UnmanagedType.LPWStr)]
string Enumerator,
            IntPtr parent,
            UInt32 flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiGetDevicePropertyW(
                IntPtr deviceInfoSet,
                [In] ref SP_DEVINFO_DATA DeviceInfoData,
                [In] ref DEVPROPKEY propertyKey,
                [Out] out UInt32 propertyType,
                IntPtr propertyBuffer,
                UInt32 propertyBufferSize,
                out UInt32 requiredSize,
                UInt32 flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiGetDeviceRegistryPropertyW(
          IntPtr DeviceInfoSet,
          [In] ref SP_DEVINFO_DATA DeviceInfoData,
          UInt32 Property,
          [Out] out UInt32 PropertyRegDataType,
          IntPtr PropertyBuffer,
          UInt32 PropertyBufferSize,
          [In, Out] ref UInt32 RequiredSize
        );

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiSetClassInstallParams(
            IntPtr deviceInfoSet,
            [In] ref SP_DEVINFO_DATA deviceInfoData,
            [In] ref SP_PROPCHANGE_PARAMS classInstallParams,
            UInt32 ClassInstallParamsSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVPROPKEY
        {
            public Guid fmtid;
            public UInt32 pid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_CLASSINSTALL_HEADER
        {
            public UInt32 cbSize;
            public UInt32 InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public UInt32 cbSize;
            public Guid classGuid;
            public UInt32 devInst;
            public IntPtr reserved;     // CHANGE #1 - was UInt32
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_PROPCHANGE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader;
            public UInt32 StateChange;
            public UInt32 Scope;
            public UInt32 HwProfile;
        }
    }
}
