using System;
using System.Runtime.InteropServices;

namespace DearImGuiInjection.Windows;

public enum NtStatus : uint
{
    // Success
    Success = 0x00000000,
    Wait0 = 0x00000000,
    Wait1 = 0x00000001,
    Wait2 = 0x00000002,
    Wait3 = 0x00000003,
    Wait63 = 0x0000003f,
    Abandoned = 0x00000080,
    AbandonedWait0 = 0x00000080,
    AbandonedWait1 = 0x00000081,
    AbandonedWait2 = 0x00000082,
    AbandonedWait3 = 0x00000083,
    AbandonedWait63 = 0x000000bf,
    UserApc = 0x000000c0,
    KernelApc = 0x00000100,
    Alerted = 0x00000101,
    Timeout = 0x00000102,
    Pending = 0x00000103,
    Reparse = 0x00000104,
    MoreEntries = 0x00000105,
    NotAllAssigned = 0x00000106,
    SomeNotMapped = 0x00000107,
    OpLockBreakInProgress = 0x00000108,
    VolumeMounted = 0x00000109,
    RxActCommitted = 0x0000010a,
    NotifyCleanup = 0x0000010b,
    NotifyEnumDir = 0x0000010c,
    NoQuotasForAccount = 0x0000010d,
    PrimaryTransportConnectFailed = 0x0000010e,
    PageFaultTransition = 0x00000110,
    PageFaultDemandZero = 0x00000111,
    PageFaultCopyOnWrite = 0x00000112,
    PageFaultGuardPage = 0x00000113,
    PageFaultPagingFile = 0x00000114,
    CrashDump = 0x00000116,
    ReparseObject = 0x00000118,
    NothingToTerminate = 0x00000122,
    ProcessNotInJob = 0x00000123,
    ProcessInJob = 0x00000124,
    ProcessCloned = 0x00000129,
    FileLockedWithOnlyReaders = 0x0000012a,
    FileLockedWithWriters = 0x0000012b,

    // Informational
    Informational = 0x40000000,
    ObjectNameExists = 0x40000000,
    ThreadWasSuspended = 0x40000001,
    WorkingSetLimitRange = 0x40000002,
    ImageNotAtBase = 0x40000003,
    RegistryRecovered = 0x40000009,

    // Warning
    Warning = 0x80000000,
    GuardPageViolation = 0x80000001,
    DatatypeMisalignment = 0x80000002,
    Breakpoint = 0x80000003,
    SingleStep = 0x80000004,
    BufferOverflow = 0x80000005,
    NoMoreFiles = 0x80000006,
    HandlesClosed = 0x8000000a,
    PartialCopy = 0x8000000d,
    DeviceBusy = 0x80000011,
    InvalidEaName = 0x80000013,
    EaListInconsistent = 0x80000014,
    NoMoreEntries = 0x8000001a,
    LongJump = 0x80000026,
    DllMightBeInsecure = 0x8000002b,

    // Error
    Error = 0xc0000000,
    Unsuccessful = 0xc0000001,
    NotImplemented = 0xc0000002,
    InvalidInfoClass = 0xc0000003,
    InfoLengthMismatch = 0xc0000004,
    AccessViolation = 0xc0000005,
    InPageError = 0xc0000006,
    PagefileQuota = 0xc0000007,
    InvalidHandle = 0xc0000008,
    BadInitialStack = 0xc0000009,
    BadInitialPc = 0xc000000a,
    InvalidCid = 0xc000000b,
    TimerNotCanceled = 0xc000000c,
    InvalidParameter = 0xc000000d,
    NoSuchDevice = 0xc000000e,
    NoSuchFile = 0xc000000f,
    InvalidDeviceRequest = 0xc0000010,
    EndOfFile = 0xc0000011,
    WrongVolume = 0xc0000012,
    NoMediaInDevice = 0xc0000013,
    NoMemory = 0xc0000017,
    NotMappedView = 0xc0000019,
    UnableToFreeVm = 0xc000001a,
    UnableToDeleteSection = 0xc000001b,
    IllegalInstruction = 0xc000001d,
    AlreadyCommitted = 0xc0000021,
    AccessDenied = 0xc0000022,
    BufferTooSmall = 0xc0000023,
    ObjectTypeMismatch = 0xc0000024,
    NonContinuableException = 0xc0000025,
    BadStack = 0xc0000028,
    NotLocked = 0xc000002a,
    NotCommitted = 0xc000002d,
    InvalidParameterMix = 0xc0000030,
    ObjectNameInvalid = 0xc0000033,
    ObjectNameNotFound = 0xc0000034,
    ObjectNameCollision = 0xc0000035,
    ObjectPathInvalid = 0xc0000039,
    ObjectPathNotFound = 0xc000003a,
    ObjectPathSyntaxBad = 0xc000003b,
    DataOverrun = 0xc000003c,
    DataLate = 0xc000003d,
    DataError = 0xc000003e,
    CrcError = 0xc000003f,
    SectionTooBig = 0xc0000040,
    PortConnectionRefused = 0xc0000041,
    InvalidPortHandle = 0xc0000042,
    SharingViolation = 0xc0000043,
    QuotaExceeded = 0xc0000044,
    InvalidPageProtection = 0xc0000045,
    MutantNotOwned = 0xc0000046,
    SemaphoreLimitExceeded = 0xc0000047,
    PortAlreadySet = 0xc0000048,
    SectionNotImage = 0xc0000049,
    SuspendCountExceeded = 0xc000004a,
    ThreadIsTerminating = 0xc000004b,
    BadWorkingSetLimit = 0xc000004c,
    IncompatibleFileMap = 0xc000004d,
    SectionProtection = 0xc000004e,
    EasNotSupported = 0xc000004f,
    EaTooLarge = 0xc0000050,
    NonExistentEaEntry = 0xc0000051,
    NoEasOnFile = 0xc0000052,
    EaCorruptError = 0xc0000053,
    FileLockConflict = 0xc0000054,
    LockNotGranted = 0xc0000055,
    DeletePending = 0xc0000056,
    CtlFileNotSupported = 0xc0000057,
    UnknownRevision = 0xc0000058,
    RevisionMismatch = 0xc0000059,
    InvalidOwner = 0xc000005a,
    InvalidPrimaryGroup = 0xc000005b,
    NoImpersonationToken = 0xc000005c,
    CantDisableMandatory = 0xc000005d,
    NoLogonServers = 0xc000005e,
    NoSuchLogonSession = 0xc000005f,
    NoSuchPrivilege = 0xc0000060,
    PrivilegeNotHeld = 0xc0000061,
    InvalidAccountName = 0xc0000062,
    UserExists = 0xc0000063,
    NoSuchUser = 0xc0000064,
    GroupExists = 0xc0000065,
    NoSuchGroup = 0xc0000066,
    MemberInGroup = 0xc0000067,
    MemberNotInGroup = 0xc0000068,
    LastAdmin = 0xc0000069,
    WrongPassword = 0xc000006a,
    IllFormedPassword = 0xc000006b,
    PasswordRestriction = 0xc000006c,
    LogonFailure = 0xc000006d,
    AccountRestriction = 0xc000006e,
    InvalidLogonHours = 0xc000006f,
    InvalidWorkstation = 0xc0000070,
    PasswordExpired = 0xc0000071,
    AccountDisabled = 0xc0000072,
    NoneMapped = 0xc0000073,
    TooManyLuidsRequested = 0xc0000074,
    LuidsExhausted = 0xc0000075,
    InvalidSubAuthority = 0xc0000076,
    InvalidAcl = 0xc0000077,
    InvalidSid = 0xc0000078,
    InvalidSecurityDescr = 0xc0000079,
    ProcedureNotFound = 0xc000007a,
    InvalidImageFormat = 0xc000007b,
    NoToken = 0xc000007c,
    BadInheritanceAcl = 0xc000007d,
    RangeNotLocked = 0xc000007e,
    DiskFull = 0xc000007f,
    ServerDisabled = 0xc0000080,
    ServerNotDisabled = 0xc0000081,
    TooManyGuidsRequested = 0xc0000082,
    GuidsExhausted = 0xc0000083,
    InvalidIdAuthority = 0xc0000084,
    AgentsExhausted = 0xc0000085,
    InvalidVolumeLabel = 0xc0000086,
    SectionNotExtended = 0xc0000087,
    NotMappedData = 0xc0000088,
    ResourceDataNotFound = 0xc0000089,
    ResourceTypeNotFound = 0xc000008a,
    ResourceNameNotFound = 0xc000008b,
    ArrayBoundsExceeded = 0xc000008c,
    FloatDenormalOperand = 0xc000008d,
    FloatDivideByZero = 0xc000008e,
    FloatInexactResult = 0xc000008f,
    FloatInvalidOperation = 0xc0000090,
    FloatOverflow = 0xc0000091,
    FloatStackCheck = 0xc0000092,
    FloatUnderflow = 0xc0000093,
    IntegerDivideByZero = 0xc0000094,
    IntegerOverflow = 0xc0000095,
    PrivilegedInstruction = 0xc0000096,
    TooManyPagingFiles = 0xc0000097,
    FileInvalid = 0xc0000098,
    InstanceNotAvailable = 0xc00000ab,
    PipeNotAvailable = 0xc00000ac,
    InvalidPipeState = 0xc00000ad,
    PipeBusy = 0xc00000ae,
    IllegalFunction = 0xc00000af,
    PipeDisconnected = 0xc00000b0,
    PipeClosing = 0xc00000b1,
    PipeConnected = 0xc00000b2,
    PipeListening = 0xc00000b3,
    InvalidReadMode = 0xc00000b4,
    IoTimeout = 0xc00000b5,
    FileForcedClosed = 0xc00000b6,
    ProfilingNotStarted = 0xc00000b7,
    ProfilingNotStopped = 0xc00000b8,
    NotSameDevice = 0xc00000d4,
    FileRenamed = 0xc00000d5,
    CantWait = 0xc00000d8,
    PipeEmpty = 0xc00000d9,
    CantTerminateSelf = 0xc00000db,
    InternalError = 0xc00000e5,
    InvalidParameter1 = 0xc00000ef,
    InvalidParameter2 = 0xc00000f0,
    InvalidParameter3 = 0xc00000f1,
    InvalidParameter4 = 0xc00000f2,
    InvalidParameter5 = 0xc00000f3,
    InvalidParameter6 = 0xc00000f4,
    InvalidParameter7 = 0xc00000f5,
    InvalidParameter8 = 0xc00000f6,
    InvalidParameter9 = 0xc00000f7,
    InvalidParameter10 = 0xc00000f8,
    InvalidParameter11 = 0xc00000f9,
    InvalidParameter12 = 0xc00000fa,
    MappedFileSizeZero = 0xc000011e,
    TooManyOpenedFiles = 0xc000011f,
    Cancelled = 0xc0000120,
    CannotDelete = 0xc0000121,
    InvalidComputerName = 0xc0000122,
    FileDeleted = 0xc0000123,
    SpecialAccount = 0xc0000124,
    SpecialGroup = 0xc0000125,
    SpecialUser = 0xc0000126,
    MembersPrimaryGroup = 0xc0000127,
    FileClosed = 0xc0000128,
    TooManyThreads = 0xc0000129,
    ThreadNotInProcess = 0xc000012a,
    TokenAlreadyInUse = 0xc000012b,
    PagefileQuotaExceeded = 0xc000012c,
    CommitmentLimit = 0xc000012d,
    InvalidImageLeFormat = 0xc000012e,
    InvalidImageNotMz = 0xc000012f,
    InvalidImageProtect = 0xc0000130,
    InvalidImageWin16 = 0xc0000131,
    LogonServer = 0xc0000132,
    DifferenceAtDc = 0xc0000133,
    SynchronizationRequired = 0xc0000134,
    DllNotFound = 0xc0000135,
    IoPrivilegeFailed = 0xc0000137,
    OrdinalNotFound = 0xc0000138,
    EntryPointNotFound = 0xc0000139,
    ControlCExit = 0xc000013a,
    PortNotSet = 0xc0000353,
    DebuggerInactive = 0xc0000354,
    CallbackBypass = 0xc0000503,
    PortClosed = 0xc0000700,
    MessageLost = 0xc0000701,
    InvalidMessage = 0xc0000702,
    RequestCanceled = 0xc0000703,
    RecursiveDispatch = 0xc0000704,
    LpcReceiveBufferExpected = 0xc0000705,
    LpcInvalidConnectionUsage = 0xc0000706,
    LpcRequestsNotAllowed = 0xc0000707,
    ResourceInUse = 0xc0000708,
    ProcessIsProtected = 0xc0000712,
    VolumeDirty = 0xc0000806,
    FileCheckedOut = 0xc0000901,
    CheckOutRequired = 0xc0000902,
    BadFileType = 0xc0000903,
    FileTooLarge = 0xc0000904,
    FormsAuthRequired = 0xc0000905,
    VirusInfected = 0xc0000906,
    VirusDeleted = 0xc0000907,
    TransactionalConflict = 0xc0190001,
    InvalidTransaction = 0xc0190002,
    TransactionNotActive = 0xc0190003,
    TmInitializationFailed = 0xc0190004,
    RmNotActive = 0xc0190005,
    RmMetadataCorrupt = 0xc0190006,
    TransactionNotJoined = 0xc0190007,
    DirectoryNotRm = 0xc0190008,
    CouldNotResizeLog = 0xc0190009,
    TransactionsUnsupportedRemote = 0xc019000a,
    LogResizeInvalidSize = 0xc019000b,
    RemoteFileVersionMismatch = 0xc019000c,
    CrmProtocolAlreadyExists = 0xc019000f,
    TransactionPropagationFailed = 0xc0190010,
    CrmProtocolNotFound = 0xc0190011,
    TransactionSuperiorExists = 0xc0190012,
    TransactionRequestNotValid = 0xc0190013,
    TransactionNotRequested = 0xc0190014,
    TransactionAlreadyAborted = 0xc0190015,
    TransactionAlreadyCommitted = 0xc0190016,
    TransactionInvalidMarshallBuffer = 0xc0190017,
    CurrentTransactionNotValid = 0xc0190018,
    LogGrowthFailed = 0xc0190019,
    ObjectNoLongerExists = 0xc0190021,
    StreamMiniversionNotFound = 0xc0190022,
    StreamMiniversionNotValid = 0xc0190023,
    MiniversionInaccessibleFromSpecifiedTransaction = 0xc0190024,
    CantOpenMiniversionWithModifyIntent = 0xc0190025,
    CantCreateMoreStreamMiniversions = 0xc0190026,
    HandleNoLongerValid = 0xc0190028,
    NoTxfMetadata = 0xc0190029,
    LogCorruptionDetected = 0xc0190030,
    CantRecoverWithHandleOpen = 0xc0190031,
    RmDisconnected = 0xc0190032,
    EnlistmentNotSuperior = 0xc0190033,
    RecoveryNotNeeded = 0xc0190034,
    RmAlreadyStarted = 0xc0190035,
    FileIdentityNotPersistent = 0xc0190036,
    CantBreakTransactionalDependency = 0xc0190037,
    CantCrossRmBoundary = 0xc0190038,
    TxfDirNotEmpty = 0xc0190039,
    IndoubtTransactionsExist = 0xc019003a,
    TmVolatile = 0xc019003b,
    RollbackTimerExpired = 0xc019003c,
    TxfAttributeCorrupt = 0xc019003d,
    EfsNotAllowedInTransaction = 0xc019003e,
    TransactionalOpenNotAllowed = 0xc019003f,
    TransactedMappingUnsupportedRemote = 0xc0190040,
    TxfMetadataAlreadyPresent = 0xc0190041,
    TransactionScopeCallbacksNotSet = 0xc0190042,
    TransactionRequiredPromotion = 0xc0190043,
    CannotExecuteFileInTransaction = 0xc0190044,
    TransactionsNotFrozen = 0xc0190045,

    MaximumNtStatus = 0xffffffff
}

public static class User32
{


    [Flags]
    public enum KeyFlag : int
    {
        /// <summary>
        /// Manipulates the extended key flag.
        /// </summary>
        KF_EXTENDED = 0x0100,
        /// <summary>
        /// Manipulates the dialog mode flag, which indicates whether a dialog box is active.
        /// </summary>
        KF_DLGMODE = 0x0800,
        /// <summary>
        /// Manipulates the menu mode flag, which indicates whether a menu is active.
        /// </summary>
        KF_MENUMODE = 0x1000,
        /// <summary>
        /// Manipulates the ALT key flag, which indicated if the ALT key is pressed.
        /// </summary>
        KF_ALTDOWN = 0x2000,
        /// <summary>
        /// Manipulates the repeat count.
        /// </summary>
        KF_REPEAT = 0x4000,
        /// <summary>
        /// Manipulates the transition state flag.
        /// </summary>
        KF_UP = 0x8000
    }

    [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi)]
    public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

    [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi)]
    public static extern IntPtr GetThreadDpiAwarenessContext();

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("User32.dll")]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

    // size of a device name string
    private const int CCHDEVICENAME = 32;

    /// <summary>
    /// The MONITORINFOEX structure contains information about a display monitor.
    /// The GetMonitorInfo function stores information into a MONITORINFOEX structure or a MONITORINFO structure.
    /// The MONITORINFOEX structure is a superset of the MONITORINFO structure. The MONITORINFOEX structure adds a string member to contain a name
    /// for the display monitor.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MonitorInfoEx
    {
        /// <summary>
        /// The size, in bytes, of the structure. Set this member to sizeof(MONITORINFOEX) (72) before calling the GetMonitorInfo function.
        /// Doing so lets the function determine the type of structure you are passing to it.
        /// </summary>
        public int Size;

        /// <summary>
        /// A RECT structure that specifies the display monitor rectangle, expressed in virtual-screen coordinates.
        /// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
        /// </summary>
        public RectStruct Monitor;

        /// <summary>
        /// A RECT structure that specifies the work area rectangle of the display monitor that can be used by applications,
        /// expressed in virtual-screen coordinates. Windows uses this rectangle to maximize an application on the monitor.
        /// The rest of the area in rcMonitor contains system windows such as the task bar and side bars.
        /// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
        /// </summary>
        public RectStruct WorkArea;

        /// <summary>
        /// The attributes of the display monitor.
        ///
        /// This member can be the following value:
        ///   1 : MONITORINFOF_PRIMARY
        /// </summary>
        public uint Flags;

        /// <summary>
        /// A string that specifies the device name of the monitor being used. Most applications have no use for a display monitor name,
        /// and so can save some bytes by using a MONITORINFO structure.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string DeviceName;

        public void Init()
        {
            this.Size = 40 + 2 * CCHDEVICENAME;
            this.DeviceName = string.Empty;
        }
    }

    /// <summary>
    /// The RECT structure defines the coordinates of the upper-left and lower-right corners of a rectangle.
    /// </summary>
    /// <see cref="http://msdn.microsoft.com/en-us/library/dd162897%28VS.85%29.aspx"/>
    /// <remarks>
    /// By convention, the right and bottom edges of the rectangle are normally considered exclusive.
    /// In other words, the pixel whose coordinates are ( right, bottom ) lies immediately outside of the the rectangle.
    /// For example, when RECT is passed to the FillRect function, the rectangle is filled up to, but not including,
    /// the right column and bottom row of pixels. This structure is identical to the RECTL structure.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct RectStruct
    {
        /// <summary>
        /// The x-coordinate of the upper-left corner of the rectangle.
        /// </summary>
        public int Left;

        /// <summary>
        /// The y-coordinate of the upper-left corner of the rectangle.
        /// </summary>
        public int Top;

        /// <summary>
        /// The x-coordinate of the lower-right corner of the rectangle.
        /// </summary>
        public int Right;

        /// <summary>
        /// The y-coordinate of the lower-right corner of the rectangle.
        /// </summary>
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowUnicode(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetProcessDPIAware();

    /// <summary>
    /// The services requested. This member can be a combination of the following values.
    /// </summary>
    /// <seealso cref="http://msdn.microsoft.com/en-us/library/ms645604%28v=vs.85%29.aspx"/>
    [Flags]
    public enum TMEFlags : uint
    {
        /// <summary>
        /// The caller wants to cancel a prior tracking request. The caller should also specify the type of tracking that it wants to cancel. For example, to cancel hover tracking, the caller must pass the TME_CANCEL and TME_HOVER flags.
        /// </summary>
        TME_CANCEL = 0x80000000,
        /// <summary>
        /// The caller wants hover notification. Notification is delivered as a WM_MOUSEHOVER message.
        /// If the caller requests hover tracking while hover tracking is already active, the hover timer will be reset.
        /// This flag is ignored if the mouse pointer is not over the specified window or area.
        /// </summary>
        TME_HOVER = 0x00000001,
        /// <summary>
        /// The caller wants leave notification. Notification is delivered as a WM_MOUSELEAVE message. If the mouse is not over the specified window or area, a leave notification is generated immediately and no further tracking is performed.
        /// </summary>
        TME_LEAVE = 0x00000002,
        /// <summary>
        /// The caller wants hover and leave notification for the nonclient areas. Notification is delivered as WM_NCMOUSEHOVER and WM_NCMOUSELEAVE messages.
        /// </summary>
        TME_NONCLIENT = 0x00000010,
        /// <summary>
        /// The function fills in the structure instead of treating it as a tracking request. The structure is filled such that had that structure been passed to TrackMouseEvent, it would generate the current tracking. The only anomaly is that the hover time-out returned is always the actual time-out and not HOVER_DEFAULT, if HOVER_DEFAULT was specified during the original TrackMouseEvent request.
        /// </summary>
        TME_QUERY = 0x40000000,
    }

    [DllImport("user32.dll")]
    public static extern int TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);
    [StructLayout(LayoutKind.Sequential)]
    public struct TRACKMOUSEEVENT
    {
        public Int32 cbSize;    // using Int32 instead of UInt32 is safe here, and this avoids casting the result  of Marshal.SizeOf()
        [MarshalAs(UnmanagedType.U4)]
        public TMEFlags dwFlags;
        public IntPtr hWnd;
        public UInt32 dwHoverTime;

        public TRACKMOUSEEVENT(TMEFlags dwFlags, IntPtr hWnd, UInt32 dwHoverTime)
        {
            this.cbSize = Marshal.SizeOf(typeof(TRACKMOUSEEVENT));
            this.dwFlags = dwFlags;
            this.hWnd = hWnd;
            this.dwHoverTime = dwHoverTime;
        }
    }

    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    public static extern IntPtr SetCapture(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetCapture();

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(POINT p);

    [DllImport("user32.dll")]
    public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    /// <summary>
    ///     Retrieves a handle to the foreground window (the window with which the user is currently working). The system
    ///     assigns a slightly higher priority to the thread that creates the foreground window than it does to other threads.
    ///     <para>See https://msdn.microsoft.com/en-us/library/windows/desktop/ms633505%28v=vs.85%29.aspx for more information.</para>
    /// </summary>
    /// <returns>
    ///     C++ ( Type: Type: HWND )<br /> The return value is a handle to the foreground window. The foreground window
    ///     can be NULL in certain circumstances, such as when a window is losing activation.
    /// </returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    public enum VirtualKey : int
    {
        VK_LBUTTON = 0x01,
        VK_RBUTTON = 0x02,
        VK_CANCEL = 0x03,
        VK_MBUTTON = 0x04,
        //
        VK_XBUTTON1 = 0x05,
        VK_XBUTTON2 = 0x06,
        //
        VK_BACK = 0x08,
        VK_TAB = 0x09,
        //
        VK_CLEAR = 0x0C,
        VK_RETURN = 0x0D,
        //
        VK_SHIFT = 0x10,
        VK_CONTROL = 0x11,
        VK_MENU = 0x12,
        VK_PAUSE = 0x13,
        VK_CAPITAL = 0x14,
        //
        VK_KANA = 0x15,
        VK_HANGEUL = 0x15,  /* old name - should be here for compatibility */
        VK_HANGUL = 0x15,
        VK_JUNJA = 0x17,
        VK_FINAL = 0x18,
        VK_HANJA = 0x19,
        VK_KANJI = 0x19,
        //
        VK_ESCAPE = 0x1B,
        //
        VK_CONVERT = 0x1C,
        VK_NONCONVERT = 0x1D,
        VK_ACCEPT = 0x1E,
        VK_MODECHANGE = 0x1F,
        //
        VK_SPACE = 0x20,
        VK_PRIOR = 0x21,
        VK_NEXT = 0x22,
        VK_END = 0x23,
        VK_HOME = 0x24,
        VK_LEFT = 0x25,
        VK_UP = 0x26,
        VK_RIGHT = 0x27,
        VK_DOWN = 0x28,
        VK_SELECT = 0x29,
        VK_PRINT = 0x2A,
        VK_EXECUTE = 0x2B,
        VK_SNAPSHOT = 0x2C,
        VK_INSERT = 0x2D,
        VK_DELETE = 0x2E,
        VK_HELP = 0x2F,
        //
        VK_LWIN = 0x5B,
        VK_RWIN = 0x5C,
        VK_APPS = 0x5D,
        //
        VK_SLEEP = 0x5F,
        //
        VK_NUMPAD0 = 0x60,
        VK_NUMPAD1 = 0x61,
        VK_NUMPAD2 = 0x62,
        VK_NUMPAD3 = 0x63,
        VK_NUMPAD4 = 0x64,
        VK_NUMPAD5 = 0x65,
        VK_NUMPAD6 = 0x66,
        VK_NUMPAD7 = 0x67,
        VK_NUMPAD8 = 0x68,
        VK_NUMPAD9 = 0x69,
        VK_MULTIPLY = 0x6A,
        VK_ADD = 0x6B,
        VK_SEPARATOR = 0x6C,
        VK_SUBTRACT = 0x6D,
        VK_DECIMAL = 0x6E,
        VK_DIVIDE = 0x6F,
        VK_F1 = 0x70,
        VK_F2 = 0x71,
        VK_F3 = 0x72,
        VK_F4 = 0x73,
        VK_F5 = 0x74,
        VK_F6 = 0x75,
        VK_F7 = 0x76,
        VK_F8 = 0x77,
        VK_F9 = 0x78,
        VK_F10 = 0x79,
        VK_F11 = 0x7A,
        VK_F12 = 0x7B,
        VK_F13 = 0x7C,
        VK_F14 = 0x7D,
        VK_F15 = 0x7E,
        VK_F16 = 0x7F,
        VK_F17 = 0x80,
        VK_F18 = 0x81,
        VK_F19 = 0x82,
        VK_F20 = 0x83,
        VK_F21 = 0x84,
        VK_F22 = 0x85,
        VK_F23 = 0x86,
        VK_F24 = 0x87,
        //
        VK_NUMLOCK = 0x90,
        VK_SCROLL = 0x91,
        //
        VK_OEM_NEC_EQUAL = 0x92,   // '=' key on numpad
                                   //
        VK_OEM_FJ_JISHO = 0x92,   // 'Dictionary' key
        VK_OEM_FJ_MASSHOU = 0x93,   // 'Unregister word' key
        VK_OEM_FJ_TOUROKU = 0x94,   // 'Register word' key
        VK_OEM_FJ_LOYA = 0x95,   // 'Left OYAYUBI' key
        VK_OEM_FJ_ROYA = 0x96,   // 'Right OYAYUBI' key
                                 //
        VK_LSHIFT = 0xA0,
        VK_RSHIFT = 0xA1,
        VK_LCONTROL = 0xA2,
        VK_RCONTROL = 0xA3,
        VK_LMENU = 0xA4,
        VK_RMENU = 0xA5,
        //
        VK_BROWSER_BACK = 0xA6,
        VK_BROWSER_FORWARD = 0xA7,
        VK_BROWSER_REFRESH = 0xA8,
        VK_BROWSER_STOP = 0xA9,
        VK_BROWSER_SEARCH = 0xAA,
        VK_BROWSER_FAVORITES = 0xAB,
        VK_BROWSER_HOME = 0xAC,
        //
        VK_VOLUME_MUTE = 0xAD,
        VK_VOLUME_DOWN = 0xAE,
        VK_VOLUME_UP = 0xAF,
        VK_MEDIA_NEXT_TRACK = 0xB0,
        VK_MEDIA_PREV_TRACK = 0xB1,
        VK_MEDIA_STOP = 0xB2,
        VK_MEDIA_PLAY_PAUSE = 0xB3,
        VK_LAUNCH_MAIL = 0xB4,
        VK_LAUNCH_MEDIA_SELECT = 0xB5,
        VK_LAUNCH_APP1 = 0xB6,
        VK_LAUNCH_APP2 = 0xB7,
        //
        VK_OEM_1 = 0xBA,   // ';:' for US
        VK_OEM_PLUS = 0xBB,   // '+' any country
        VK_OEM_COMMA = 0xBC,   // ',' any country
        VK_OEM_MINUS = 0xBD,   // '-' any country
        VK_OEM_PERIOD = 0xBE,   // '.' any country
        VK_OEM_2 = 0xBF,   // '/?' for US
        VK_OEM_3 = 0xC0,   // '`~' for US
                           //
        VK_OEM_4 = 0xDB,  //  '[{' for US
        VK_OEM_5 = 0xDC,  //  '\|' for US
        VK_OEM_6 = 0xDD,  //  ']}' for US
        VK_OEM_7 = 0xDE,  //  ''"' for US
        VK_OEM_8 = 0xDF,
        //
        VK_OEM_AX = 0xE1,  //  'AX' key on Japanese AX kbd
        VK_OEM_102 = 0xE2,  //  "<>" or "\|" on RT 102-key kbd.
        VK_ICO_HELP = 0xE3,  //  Help key on ICO
        VK_ICO_00 = 0xE4,  //  00 key on ICO
                           //
        VK_PROCESSKEY = 0xE5,
        //
        VK_ICO_CLEAR = 0xE6,
        //
        VK_PACKET = 0xE7,
        //
        VK_OEM_RESET = 0xE9,
        VK_OEM_JUMP = 0xEA,
        VK_OEM_PA1 = 0xEB,
        VK_OEM_PA2 = 0xEC,
        VK_OEM_PA3 = 0xED,
        VK_OEM_WSCTRL = 0xEE,
        VK_OEM_CUSEL = 0xEF,
        VK_OEM_ATTN = 0xF0,
        VK_OEM_FINISH = 0xF1,
        VK_OEM_COPY = 0xF2,
        VK_OEM_AUTO = 0xF3,
        VK_OEM_ENLW = 0xF4,
        VK_OEM_BACKTAB = 0xF5,
        //
        VK_ATTN = 0xF6,
        VK_CRSEL = 0xF7,
        VK_EXSEL = 0xF8,
        VK_EREOF = 0xF9,
        VK_PLAY = 0xFA,
        VK_ZOOM = 0xFB,
        VK_NONAME = 0xFC,
        VK_PA1 = 0xFD,
        VK_OEM_CLEAR = 0xFE
    }


    [DllImport("USER32.dll")]
    public static extern short GetKeyState(VirtualKey nVirtKey);

    [DllImport("user32.dll", SetLastError = false)]
    public static extern IntPtr GetMessageExtraInfo();

    [DllImport("user32.dll")]
    public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    [DllImport("user32.dll")]
    public static extern IntPtr SetCursor(IntPtr handle);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    public static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
            return GetWindowLongPtr64(hWnd, nIndex);
        else
            return GetWindowLongPtr32(hWnd, nIndex);
    }

    public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        else
            return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    public static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    public static extern IntPtr CallWindowProc(IntPtr previousWindowProc, IntPtr windowHandle, WindowMessage message, IntPtr wParam, IntPtr lParam);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate IntPtr WndProcDelegate(IntPtr windowHandle, WindowMessage message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetCursorPos(int x, int y);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public Int32 X;
        public Int32 Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}
