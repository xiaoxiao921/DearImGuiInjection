using System;
using System.Runtime.InteropServices;

namespace DearImGuiInjection.Windows;

[Flags]
public enum PRODUCT_SUITE : short
{
    /// <summary>
    /// Microsoft BackOffice components are installed.
    /// </summary>
    VER_SUITE_BACKOFFICE = 0x00000004,

    /// <summary>
    /// Windows Server 2003, Web Edition is installed.
    /// </summary>
    VER_SUITE_BLADE = 0x00000400,

    /// <summary>
    /// Windows Server 2003, Compute Cluster Edition is installed.
    /// </summary>
    VER_SUITE_COMPUTE_SERVER = 0x00004000,

    /// <summary>
    /// Windows Server 2008 Datacenter, Windows Server 2003, Datacenter Edition, or Windows 2000 Datacenter Server is installed.
    /// </summary>
    VER_SUITE_DATACENTER = 0x00000080,

    /// <summary>
    /// Windows Server 2008 Enterprise, Windows Server 2003, Enterprise Edition, or Windows 2000 Advanced Server is installed.
    /// </summary>
    VER_SUITE_ENTERPRISE = 0x00000002,

    /// <summary>
    /// Windows XP Embedded is installed.
    /// </summary>
    VER_SUITE_EMBEDDEDNT = 0x00000040,

    /// <summary>
    /// Windows Vista Home Premium, Windows Vista Home Basic, or Windows XP Home Edition is installed.
    /// </summary>
    VER_SUITE_PERSONAL = 0x00000200,

    /// <summary>
    /// Remote Desktop is supported, but only one interactive session is supported.
    /// This value is set unless the system is running in application server mode.
    /// </summary>
    VER_SUITE_SINGLEUSERTS = 0x00000100,

    /// <summary>
    /// Microsoft Small Business Server was once installed on the system, but may have been upgraded to another version of Windows.
    /// </summary>
    /// <remarks>
    ///  You should not rely solely on the <see cref="VER_SUITE_SMALLBUSINESS"/> flag to determine whether Small Business Server is currently installed.
    ///  Both this flag and the <see cref="VER_SUITE_SMALLBUSINESS_RESTRICTED"/> flag are set when this product suite is installed. If you upgrade this
    ///  installation to Windows Server, Standard Edition, the <see cref="VER_SUITE_SMALLBUSINESS_RESTRICTED"/> flag is cleared, but the
    ///  <see cref="VER_SUITE_SMALLBUSINESS"/> flag remains set, which, in this case, indicates that Small Business Server was previously installed on
    ///  this system. If this installation is further upgraded to Windows Server, Enterprise Edition, the <see cref="VER_SUITE_SMALLBUSINESS"/> flag
    ///  remains set.
    /// </remarks>
    VER_SUITE_SMALLBUSINESS = 0x00000001,

    /// <summary>
    /// Microsoft Small Business Server is installed with the restrictive client license in force.
    /// For more information about this flag bit, see the remarks for <see cref="VER_SUITE_SMALLBUSINESS"/> flag.
    /// </summary>
    VER_SUITE_SMALLBUSINESS_RESTRICTED = 0x00000020,

    /// <summary>
    /// Windows Storage Server 2003 R2 or Windows Storage Server 2003 is installed.
    /// </summary>
    VER_SUITE_STORAGE_SERVER = 0x00002000,

    /// <summary>
    /// Terminal Services is installed. This value is always set. If <see cref="VER_SUITE_TERMINAL"/> is set but <see cref="VER_SUITE_SINGLEUSERTS"/> is not set,
    /// the operating system is running in application server mode.
    /// </summary>
    VER_SUITE_TERMINAL = 0x00000010,

    /// <summary>
    /// Windows Home Server is installed.
    /// </summary>
    VER_SUITE_WH_SERVER = unchecked((short)0x00008000),
}

/// <summary>
/// The RTL_OSVERSIONINFOEXW structure contains operating system version information.
/// </summary>
public unsafe partial struct OSVERSIONINFOEX
{
    /// <summary>
    /// The size, in bytes, of an RTL_OSVERSIONINFOEXW structure.
    /// This member must be set before the structure is used with RtlGetVersion.
    /// </summary>
    public int dwOSVersionInfoSize;

    /// <summary>
    /// The major version number of the operating system. For example, for Windows 2000, the major version number is five.
    /// </summary>
    public int dwMajorVersion;

    /// <summary>
    /// The minor version number of the operating system. For example, for Windows 2000, the minor version number is zero.
    /// </summary>
    public int dwMinorVersion;

    /// <summary>
    /// The build number of the operating system.
    /// </summary>
    public int dwBuildNumber;

    /// <summary>
    /// The operating system platform. For Win32 on NT-based operating systems, RtlGetVersion returns the value
    /// VER_PLATFORM_WIN32_NT.
    /// </summary>
    public int dwPlatformId;

    /// <summary>
    /// The service-pack version string. This member contains a null-terminated string, such as "Service Pack 3", which
    /// indicates the latest service pack installed on the system. If no service pack is installed, RtlGetVersion might not
    /// initialize this string. Initialize szCSDVersion to zero (empty string) before the call to RtlGetVersion.
    /// </summary>
    public fixed char szCSDVersion[128];

    /// <summary>
    /// The major version number of the latest service pack installed on the system. For example, for Service Pack 3,
    /// the major version number is three. If no service pack has been installed, the value is zero.
    /// </summary>
    public short wServicePackMajor;

    /// <summary>
    /// The minor version number of the latest service pack installed on the system. For example, for Service Pack 3,
    /// the minor version number is zero.
    /// </summary>
    public short wServicePackMinor;

    /// <summary>
    /// The product suites available on the system. This member is set to zero or to the bitwise OR of one or more of
    /// the <see cref="PRODUCT_SUITE"/> values.
    /// </summary>
    public PRODUCT_SUITE wSuiteMask;

    /// <summary>
    /// The product type. This member contains additional information about the system.
    /// </summary>
    public OS_TYPE wProductType;

    /// <summary>
    /// Reserved for future use.
    /// </summary>
    public byte wReserved;

    /// <summary>
    /// Initializes a new instance of the <see cref="OSVERSIONINFOEX" /> struct
    /// with <see cref="dwOSVersionInfoSize" /> set to the correct value.
    /// </summary>
    /// <returns>
    /// A newly initialized instance of <see cref="OSVERSIONINFOEX"/>.
    /// </returns>
    public static OSVERSIONINFOEX Create() => new() { dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX) };
}

/// <summary>
/// The product type enumeration.
/// </summary>
public enum OS_TYPE : byte
{
    /// <summary>
    /// The operating system is Windows 8, Windows 7, Windows Vista, Windows XP Professional, Windows XP Home Edition, or Windows 2000 Professional.
    /// </summary>
    VER_NT_WORKSTATION = 0x00000001,

    /// <summary>
    /// The system is a domain controller and the operating system is Windows Server 2012 , Windows Server 2008 R2,
    /// Windows Server 2008, Windows Server 2003, or Windows 2000 Server.
    /// </summary>
    VER_NT_DOMAIN_CONTROLLER = 0x00000002,

    /// <summary>
    /// The operating system is Windows Server 2012, Windows Server 2008 R2, Windows Server 2008, Windows Server 2003, or Windows 2000 Server.
    /// </summary>
    /// <remarks>
    /// Note that a server that is also a domain controller is reported as <see cref="VER_NT_DOMAIN_CONTROLLER"/>, not <see cref="VER_NT_SERVER"/>.
    /// </remarks>
    VER_NT_SERVER = 0x00000003,
}

[Flags]
public enum VER_MASK : int
{
    /// <summary>
    /// dwBuildNumber
    /// </summary>
    VER_BUILDNUMBER = 0x0000004,

    /// <summary>
    /// dwBuildNumber
    /// </summary>
    VER_MAJORVERSION = 0x0000002,

    /// <summary>
    /// dwMinorVersion
    /// </summary>
    VER_MINORVERSION = 0x0000001,

    /// <summary>
    /// dwPlatformId
    /// </summary>
    VER_PLATFORMID = 0x0000008,

    /// <summary>
    /// wProductType
    /// </summary>
    VER_PRODUCT_TYPE = 0x0000080,

    /// <summary>
    /// wServicePackMajor
    /// </summary>
    VER_SERVICEPACKMAJOR = 0x0000020,

    /// <summary>
    /// wServicePackMinor
    /// </summary>
    VER_SERVICEPACKMINOR = 0x0000010,

    /// <summary>
    /// wSuiteMask
    /// </summary>
    VER_SUITENAME = 0x0000040,
}


public static class Ntdll
{
    [DllImport("ntdll.dll")]
    public static unsafe extern NtStatus RtlVerifyVersionInfo(
    OSVERSIONINFOEX* VersionInfo,
    VER_MASK TypeMask,
    long ConditionMask);
}
