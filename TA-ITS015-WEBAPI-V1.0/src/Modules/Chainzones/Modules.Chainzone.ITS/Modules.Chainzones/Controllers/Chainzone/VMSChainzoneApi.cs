using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Modules.Chainzones.Controllers.Chainzone
{
    public class VMSChainzoneApi
    {
        private String strVMS_IP = "169.254.10.49";
        private int iVMS_Port = 9520;
        private czinterface If;
        #region constant

        const String strVMS_COM = "COM13";
        const UInt32 VMS_COM_BAUDRATE = 19200;
        const UInt16 VMS_COM_GGUU = 0x0101;
        const UInt32 CZ_MODE_MOVEUP = 0x35;
        const UInt32 CZ_MODE_RAND = 0x2F;
        const UInt32 CZ_FONT_EN_14x8 = 0x32;
        const UInt32 CZ_FONT_ENB_16x12 = 0x50;
        const UInt32 CZ_FONT_EN_16x9 = 0x34;
        const UInt32 CZ_FONT_EN_7x6 = 0x31;
        const UInt32 CZ_FONT_EN_5x5 = 0x30;
        const UInt32 CZ_COLOR_AMBER = 0x33;
        const UInt32 CZ_MODE_STATIC = 0x30;
        const UInt32 CZ_FONT_CH_ST_16x16 = 0x35;
        const UInt32 TEST_IPCOUNT = 2;
        const UInt32 CZ_ERROR_OK = 0x0;
        const UInt32 MAX_ZONE_COUNT = 9;
        const Int32 PARK_SLEEP_TIME = 1000;
        const UInt32 MAX_PATH = 255;

        #endregion
        #region Interface

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czInitAPI(ref czinterface czif);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadCurrentState(ref CURRENT_STATE State, IntPtr ExtData, UInt32 DataSize);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czWriteSpecFile(String SpecialFile, String PCPath);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadSpecPathFile(String SpecialFile, String PCPath);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czImposaGetTileStat(UInt16 tileAddr, ref UInt32 rtCode);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czImposaGetResult(UInt32 rtCode, StringBuilder result, UInt32 size, ref UInt16 resultCode);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czShowFiles(String[] files, UInt32 numFiles, UInt32 mode, UInt32 stayTimeSec,
                                    String signIp, UInt32 signPort, UInt32 isStoreRam = 0);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czShowFilesEx(String[] files, UInt32 numFiles, UInt32 mode, UInt32 stayTimeSec,
                                    String signIp, UInt32 signPort, UInt32 isStoreRam = 0,
                                    czinterface.conn_type type = czinterface.conn_type.COMM_UDP, String comStr = "COM3", UInt32 baudrate = 9600, UInt32 dstAddr = 0x0101);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czShowFilesSpe(String[] files, UInt32 numFiles, UInt32 mode, UInt32 stayTimeSec,
                                    String signIp, UInt32 signPort, UInt32 isStoreRam = 0,
                                    czinterface.conn_type type = czinterface.conn_type.COMM_UDP, String comStr = "COM3",
                                    UInt32 baudrate = 9600, UInt32 dstAddr = 0x0101, Byte sendType = 0);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czShowPic(Byte[] bitmap, UInt32 bmpSize, UInt32 mode, UInt32 stayTimeSec,
                                    String signIp, UInt32 signPort, UInt32 isStoreRam = 0);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czShowPicEx(Byte[] bitmap, UInt32 bmpSize, UInt32 mode, UInt32 stayTimeSec,
                                    String signIp, UInt32 signPort, UInt32 isStoreRam = 0,
                                    czinterface.conn_type type = czinterface.conn_type.COMM_UDP, String comStr = "COM3", UInt32 baudrate = 9600, UInt32 dstAddr = 0x0101);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czShowMsg(Byte[] msg, UInt32 msgSize, UInt32 font, UInt32 color, UInt32 mode,
                                  UInt32 stayTimeSec, String signIp, UInt32 signPort, UInt32 isStoreRam = 0,
                                  UInt32 isSendPlaylist = 1);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czShowMsgEx(String msg, UInt32 msgSize, UInt32 font, UInt32 color, UInt32 mode,
                                    UInt32 stayTimeSec, String signIp, UInt32 signPort, UInt32 isStoreRam = 0,
                                    UInt32 isSendPlaylist = 1,
                                    czinterface.conn_type type = czinterface.conn_type.COMM_UDP, String comStr = "COM3", UInt32 baudrate = 9600, UInt32 dstAddr = 0x0101);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czShowFilesII(String[] files, UInt32 numFiles, Byte[] stay_time_sec, UInt32 mode,
                                    String signIp, UInt32 signPort, UInt32 isStoreRam = 0,
                                    czinterface.conn_type type = czinterface.conn_type.COMM_UDP, String comStr = "COM3",
                                    UInt32 baudrate = 9600, UInt32 dstAddr = 0x0101, Byte sendType = 0);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static IntPtr czErrorDesc(UInt32 errCode);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czBrightAdjust(UInt16 bright);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czBeginPixCheck();

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadPixResult(String PCPxlPath);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadSignalResult(String PCPxlPath);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czPixProgress(ref UInt16 resultCode);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czPxlChk(String PCPxlPath, String PCSignalPath);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static void czSleep(UInt32 msec);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czBlackScreen();

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadCommunicationSet(ref COMMUNICATICON_ST commSet);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czWriteCommunicationSet(ref COMMUNICATICON_ST commSet);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadCurScreentshot(String pcpath);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadBrightInfoExt(ref czBrightInfoExt BrightInfo);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czGetPlayingFileName(StringBuilder FileName, UInt32 NameLen);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czGetDirFile(string path, ref UInt32 num, ref DIRECTORY_ENTRY_STRUCT dirEntry, UInt32 size);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czSetDisplay(byte pageID, byte zoneNum, byte setMode, ZoneDisplaySet parameter, StringBuilder content);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czSetDisplay(byte pageID, byte zoneNum, byte setMode, ZoneDisplaySet parameter, byte[] content);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czSetDisplay(byte pageID, byte zoneNum, byte setMode, ZoneDisplaySet parameter, String content);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czDivideZone(byte pageID, byte flag, ZoneSetHead zoneSetHead, ZoneSet[] zoneSetArrs);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czSetContentDisplay(byte pageID, byte zoneNum, ZoneDisplayContentSet parameter, StringBuilder content);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czSetContentDisplay(byte pageID, byte zoneNum, ZoneDisplayContentSet parameter, String content);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czSetContentDisplay(byte pageID, byte zoneNum, ZoneDisplayContentSet parameter, byte[] content);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czGetZone(ref ZoneSetHead zoneSetHead, IntPtr zoneSetEntry, byte maxEntryCount, byte PageID = 1);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czPlaySoundFile(SoundST soundST, String soundpath);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czAdjustVolume(Byte soundVolume);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadDefDisplayStyle(ref DEFAULT_SET defaultSet);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czWriteDefDisplayStyle(ref DEFAULT_SET defaultSet);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czPLInit(String workPath, String playListName);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czPLSendToLED(Byte isSendChangedFiles);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadCurVolume(ref Byte soundVolume);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadWarningLog(ref CZLog_File_Head logFileHead, IntPtr logDataEntry, Byte maxEntryCount);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]

        public extern static UInt32 czTM_DecodeDosTime(UInt32 DosTime, Byte[] dateTimeBuf, Byte bufSize);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czPLSpeSendToLED(Byte playListIndex, Byte isSendChangedFiles);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czLoadSYSFromXML(String fileName);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czPlaySpePlaylist(Byte playListIndex);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadSpePlayListIndex(ref Byte playListIndex);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czGetBeacon(ref BEACON_ST beacon);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czGetMulitZoneSetting(ref Byte enabled, ref Byte pageCount);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czSetEnableMulitZone(Byte enabled);

        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czSetPageCount(Byte pageCount);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int vsprintf(StringBuilder buffer, string format, IntPtr ptr);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int _vscprintf(string format, IntPtr ptr);
        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadPictureFile(string Drive, string pictureName, string PCPath);
        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadBrightInfo(ref Byte BrightType, ref Byte Percent, ref Byte ADValue);
        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czEasyReadFile(string pc_file_path, string sign_file_path, string sign_ip, UInt32 sign_port);
        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czReadStats(Byte statsType, ref Byte buf, UInt32 bufSize);
        [DllImport("czJetFileII.dll", CharSet = CharSet.Ansi)]
        public extern static UInt32 czResetSystem();

        #endregion
        #region Struct

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DIRECTORY_ENTRY_STRUCT_JETFILEII
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            public byte[] badir_name;
            public byte bdir_attr;
            public byte bdir_rev;
            public byte bcrt_time_tecth;
            public UInt32 wcrt_time;
            public UInt32 wcrt_date;
            public UInt32 wlast_acc_time;
            public UInt32 wfst_clus_hi;
            public UInt32 wwrt_time;
            public UInt32 wwrt_date;
            public UInt32 wfst_clus_lo;
            public UInt32 dwfile_size;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct GRAY_TEST_PARAM
        {
            public uint Type;
            public uint Red;
            public uint Green;
            public uint Blue;
            public uint Level;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct czBrightInfoExt
        {
            public Byte BrightType;
            public Byte BrightPrecent1;
            public UInt16 BrightSensorAD1;
            public Byte Rev;
            public Byte BrightPrecent2;
            public UInt16 BrightSensorAD2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZoneSetHead
        {
            public Byte BGColor_R;
            public Byte BGColor_G;
            public Byte BGColor_B;
            public Byte ZoneNum;
            public UInt16 PageStayTime;
            public Byte PortraitMode;
            public Byte Rev;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZoneSet
        {
            public byte ZoneID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Rev;
            public UInt16 XPos;
            public UInt16 YPos;
            public UInt16 ZoneWidth;
            public UInt16 ZoneHeight;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZoneDisplaySet
        {
            public UInt16 ZoneSize;
            public Byte ZoneID;
            public Byte ZoneType;
            public Byte CodeType;
            public Byte InMode;
            public Byte OutMode;
            public Byte Align;
            public Byte FGColor_R;
            public Byte FGColor_G;
            public Byte FGColor_B;
            public Byte BGColor_R;
            public Byte BGColor_G;
            public Byte BGColor_B;
            public Byte AutoLine;
            public Byte AutoWidth;
            public UInt16 Speed;
            public Byte StayTime;
            public Byte Times;
            public Byte FontStyle;
            public Byte LineSpace;
            public Byte CloumnSpace;
            public Byte Rev;
            public UInt16 BlinkOnTime;
            public UInt16 BlinkOffTime;
            public UInt16 ContentSize;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] Rev2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZoneDisplayContentSet
        {

            public UInt16 ZoneSize;
            public Byte ZoneID;
            public Byte CodeType;
            public Byte ZoneType;
            public Byte Rev;
            public UInt16 ContentSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DEFAULT_SET
        {
            public UInt16 ID;
            public Byte PlayListLoc;
            public Byte TimePre0En;

            public Byte Ddrive;
            public Byte Dback_color;
            public Byte Dfont_color;
            public Byte Dhor_just;

            public Byte Dver_just;
            public Byte Dline_space;
            public Byte Dfont;
            public Byte Din_mode;

            public Byte Dout_mode;
            public Byte Dspeed;
            public Byte Dstay_time;
            public Byte Dwrap;

            public UInt32 Lstay_time;

            public Byte TimeFormat;
            public Byte HeadTailPlayMode;
            public Byte HeadTailMoveDirection;
            public Byte HeadTailMoveSpeed;

            public Byte HeadTailPauseTime;
            public Byte Play_num;
            public UInt16 Version;

            public UInt16 ShowDefaultBmpTime;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
            public Byte[] rev;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CZLog_File_Head
        {
            public UInt16 ID;
            public UInt16 RecordBytes;
            public UInt32 RecordNums;
            public UInt32 WriteLocation;
            public UInt32 MaxRecordNums;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CZLogDataST
        {
            public UInt32 DataTime;
            public Byte AlarmType;
            public Byte AlarmStatus;
            public UInt32 AlarmValue;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
            public Byte[] rev;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct czinterface
        {
            public enum conn_type
            {
                COMM_UDP,
                COMM_TCP,
                COMM_COM
            }

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void PreCommFun(ref czinterface inter);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void PostCommFun(ref czinterface inter);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void UpdateStatusFun(ref czinterface inter, UInt32 code, UInt32 precent);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate UInt32 ReadFun(IntPtr par, StringBuilder buf, UInt32 size, UInt32 timeoutms);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate UInt32 WriteFun(IntPtr par, String buf, UInt32 size);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void LogFun(ref czinterface inter, string file, int line, int level, string format, IntPtr args);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void CurShowFilterFun(ref czinterface inter, String srcfile, String dstfile, int frames, int delays);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void GetTemporaryPath(ref czinterface inter, String store_path, UInt32 max_path);

            public conn_type type;
            public UInt32 inited;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public String ip;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public String com;
            public UInt32 port;
            public UInt32 baudrate;
            public IntPtr signPointer;
            public IntPtr taskPointer;
            public UInt16 SrcAddr;
            public UInt16 DstAddr;
            public UInt32 Retry;
            public UInt32 Timeoutms;
            public UInt32 Stoped;
            public UInt16 totalSteps;
            public UInt16 curSteps;
            public UInt16 packetSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 26)]
            public String Rev;

            public PreCommFun pre_comm;
            public PostCommFun post_comm;
            public UpdateStatusFun update_status;
            public ReadFun read;
            public WriteFun write;
            public LogFun log;
            public CurShowFilterFun curShowFilter;
            public GetTemporaryPath getTemporaryPath;
        }

        /// <summary>
        /// Current State Struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CURRENT_STATE
        {
            public Byte sysState;
            public Byte inTemp;
            public Byte outTemp;
            public Byte AutoPower;
            public Byte Humidity;
            public Byte Samples;
            public Byte BrightLevel;
            public Byte Rev;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct COMMUNICATICON_ST
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public String ip;
            public UInt16 CustomPort;
            public Byte GroupAddr;
            public Byte UnitAddr;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public String GatewayAddr;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public String SubnetMaskAddr;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public String Rev;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DIRECTORY_ENTRY_STRUCT
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            public char[] badir_name;
            public byte bdir_attr;
            public byte bdir_rev;
            public byte bcrt_time_tecth;
            public UInt16 wcrt_time;
            public UInt16 wcrt_date;
            public UInt16 wlast_acc_time;
            public UInt16 wfst_clus_hi;
            public UInt16 wwrt_time;
            public UInt16 wwrt_date;
            public UInt16 wfst_clus_lo;
            public UInt32 dwfile_size;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SoundST
        {
            public Byte BuzzerSwitch;
            public Byte PlayTimes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public UInt16[] Rev;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BEACON_ST
        {
            Byte R;
            Byte Y;
            Byte G;
            Byte Mode;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            Byte[] Rev;
        }

        #endregion

        #region CallbackFunc

        private static czinterface.PreCommFun PreCommFunc = preComm;

        public static void preComm(ref czinterface If)
        {
            return;
        }

        private static czinterface.PostCommFun PostCommFunc = postComm;

        public static void postComm(ref czinterface If)
        {
            return;
        }

        private static czinterface.UpdateStatusFun UpdateStatusFunc = updateStatus;

        public static void updateStatus(ref czinterface inter, UInt32 code, UInt32 precent)
        {
            return;
        }

        private static czinterface.ReadFun ReadFunc = read;

        public static UInt32 read(IntPtr par, StringBuilder buf, UInt32 size, UInt32 timeoutms)
        {
            return 0;
        }

        private static czinterface.WriteFun WriteFunc = write;

        public static UInt32 write(IntPtr par, String buf, UInt32 size)
        {
            return 0;
        }

        private static czinterface.LogFun LogFunc = log;

        public static void log(ref czinterface inter, String file, int line, int level, String format, IntPtr args)
        {
            var bufferCapacity = _vscprintf(format, args);

            StringBuilder logMessage = new StringBuilder(256);
            var stringBuilder = new StringBuilder(bufferCapacity + 1);
            vsprintf(stringBuilder, format, args);
            Debug.WriteLine(stringBuilder.ToString());
        }

        #endregion

        private void InitIFStruct(ref czinterface If)
        {
            If.type = czinterface.conn_type.COMM_UDP;
            If.ip = strVMS_IP;
            If.com = strVMS_COM;
            If.signPointer = IntPtr.Zero;
            If.taskPointer = IntPtr.Zero;
            If.baudrate = VMS_COM_BAUDRATE;
            If.Retry = 5;
            If.Timeoutms = 3000;

            If.SrcAddr = 0;
            If.DstAddr = VMS_COM_GGUU;
            If.Retry = 3;

            If.pre_comm = PreCommFunc;
            If.post_comm = PostCommFunc;
            If.update_status = UpdateStatusFunc;
            If.log = LogFunc;
            If.totalSteps = 1;
            If.curSteps = 1;
            If.packetSize = 768;
        }

        private static void SetBufferZero(IntPtr buffer, int size)
        {
            byte[] zeroBytes = new byte[size];
            for (int i = 0; i < zeroBytes.Length; i++)
            {
                zeroBytes[i] = 0;
            }
            Marshal.Copy(zeroBytes, 0, buffer, size);
        }
        public bool fn_Init(string strIP, uint port)
        {
            strVMS_IP = strIP;

            If = new czinterface();
            If.port = port;

            InitIFStruct(ref If);
            return czInitAPI(ref If) == 0;
        }
        private void SingleZone(ZoneSetHead zoneSetHead, ZoneSet[] zoneSetEntry)
        {
            UInt32 RT, nPos;
            String content;
            byte[] msgStr = new byte[30];

            byte enabled = 0, pageCount = 0, colorR, colorG, colorB, zoneCount = 0, pageID = 0;

            colorR = 0xFF;
            colorG = 0xFF;
            colorB = 0xFF;
            //Get MulitZone Setting
            RT = czGetMulitZoneSetting(ref enabled, ref pageCount);

            //Four steps are necessary!
            //1.Enable Mulit Zone
            enabled = 1;
            RT = czSetEnableMulitZone(enabled);
            //2.Set the page number=1
            pageCount = 1;
            RT = czSetPageCount(pageCount);

            //Get MulitZone Setting
            RT = czGetMulitZoneSetting(ref enabled, ref pageCount);




            pageID = 1;
            zoneCount = zoneSetHead.ZoneNum;
            //3.reset all zone
            RT = czDivideZone(pageID, 1, zoneSetHead, zoneSetEntry);

            //4.divide one zone
            RT = czDivideZone(pageID, 0, zoneSetHead, zoneSetEntry);

            ZoneDisplaySet zoneDisplayEntry = new ZoneDisplaySet
            {
                ZoneSize = (UInt16)(zoneSetEntry[0].ZoneWidth * zoneSetEntry[0].ZoneHeight),
                ZoneID = zoneSetEntry[0].ZoneID,
                ZoneType = 0,
                CodeType = 0,
                InMode = 3,
                OutMode = 4,
                Align = 0,
                FGColor_R = colorR,
                FGColor_G = colorG,
                FGColor_B = colorB,
                BGColor_R = 0,
                BGColor_G = 0,
                BGColor_B = 0,
                AutoLine = 0,
                AutoWidth = 1,
                Speed = 5,
                StayTime = 2,
                Times = 0,
                FontStyle = (byte)CZ_FONT_EN_14x8,       //CZ_FONT_EN_7x6
                LineSpace = 1,
                CloumnSpace = 1,
                BlinkOnTime = 0x0A,
                BlinkOffTime = 0x0A
            };
            content = "Welcome";
            zoneDisplayEntry.ContentSize = (UInt16)content.Length;
            //display a message with property in zone
            Thread.Sleep((int)PARK_SLEEP_TIME);
            RT = czSetDisplay(pageID, zoneCount, 0, zoneDisplayEntry, content);

            ZoneDisplayContentSet zoneDisplayContentEntry = new ZoneDisplayContentSet
            {
                ZoneSize = (UInt16)(zoneSetEntry[0].ZoneWidth * zoneSetEntry[0].ZoneHeight),
                ZoneID = zoneSetEntry[0].ZoneID
            };
            content = "Hello";
            zoneDisplayContentEntry.ContentSize = (UInt16)content.Length;
            //display a message without property in zone
            Thread.Sleep((int)PARK_SLEEP_TIME);
            RT = czSetContentDisplay(pageID, zoneCount, zoneDisplayContentEntry, content);

            //display a message(English+chinese+English) without property in zone
            zoneDisplayContentEntry = new ZoneDisplayContentSet
            {
                ZoneSize = (UInt16)(zoneSetEntry[0].ZoneWidth * zoneSetEntry[0].ZoneHeight),
                ZoneID = zoneSetEntry[0].ZoneID
            };
            content = "123";
            nPos = 0;

            Array.Copy(Encoding.ASCII.GetBytes(content), 0, msgStr, nPos, content.Length);
            nPos += (uint)content.Length;
            msgStr[nPos++] = 0x08;
            msgStr[nPos++] = 0x31;
            msgStr[nPos++] = (byte)CZ_FONT_EN_5x5;
            content = "123";
            Array.Copy(Encoding.ASCII.GetBytes(content), 0, msgStr, nPos, content.Length);
            nPos += (uint)content.Length;
            msgStr[nPos++] = 0x08;              //switch font control code
            msgStr[nPos++] = 0x31;              //switch font control code
            msgStr[nPos++] = (byte)CZ_FONT_EN_16x9;    //font 7x6
            content = "123";
            Array.Copy(Encoding.ASCII.GetBytes(content), 0, msgStr, nPos, content.Length);
            nPos += (uint)content.Length;
            zoneDisplayContentEntry.ContentSize = (UInt16)nPos;
            Thread.Sleep((int)PARK_SLEEP_TIME);
            RT = czSetContentDisplay(pageID, zoneCount, zoneDisplayContentEntry, msgStr);

            //display a message(Picture+text) without property in zone
            zoneDisplayContentEntry = new ZoneDisplayContentSet
            {
                ZoneSize = (UInt16)(zoneSetEntry[0].ZoneWidth * zoneSetEntry[0].ZoneHeight),
                ZoneID = zoneSetEntry[0].ZoneID
            };
            msgStr = new byte[30];
            nPos = 0;
            msgStr[nPos++] = 0x08;              //display picture control code
            msgStr[nPos++] = 0x33;              //display picture control code
            msgStr[nPos++] = 0x01;              //display picture control code
            content = "Hello";
            Array.Copy(Encoding.ASCII.GetBytes(content), 0, msgStr, nPos, content.Length);
            nPos += (uint)content.Length;
            zoneDisplayContentEntry.ContentSize = (UInt16)nPos;
            Thread.Sleep((int)PARK_SLEEP_TIME);
            RT = czSetContentDisplay(pageID, zoneCount, zoneDisplayContentEntry, msgStr);
        }
        public void fn_SendPicture(string strPath)
        {
            UInt32 RT, nPos;

            //ZoneSetHead zoneSetHead = new ZoneSetHead();
            //ZoneSet[] zoneSetEntry = new ZoneSet[MAX_ZONE_COUNT];

            //IntPtr pt = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(ZoneSet)) * MAX_ZONE_COUNT));
            //SetBufferZero(pt, (int)(Marshal.SizeOf(typeof(ZoneSet)) * MAX_ZONE_COUNT));
            //RT = czGetZone(ref zoneSetHead, pt, (byte)MAX_ZONE_COUNT);

            //for (int i = 0; i < MAX_ZONE_COUNT; i++)
            //{
            //    IntPtr ptr = (IntPtr)((UInt32)pt + i * Marshal.SizeOf(typeof(ZoneSet)));
            //    zoneSetEntry[i] = (ZoneSet)Marshal.PtrToStructure(ptr, typeof(ZoneSet));
            //}
            //Marshal.FreeHGlobal(pt);

            var path = strPath;
            var tmp = "D:\\P\\1.bmp";
            RT = czWriteSpecFile(tmp, path);
            ////path = ".\\leftArrow.bmp";
            ////tmp = "D:\\P\\2.bmp";
            ////RT = czWriteSpecFile(tmp, path);
            ////path = ".\\rigthArrow.bmp";
            ////tmp = "D:\\P\\3.bmp";
            ////RT = czWriteSpecFile(tmp, path);

            //zoneSetEntry[0].ZoneID = 1;
            //zoneSetEntry[0].XPos = 0;
            //zoneSetEntry[0].YPos = 0;
            //zoneSetEntry[0].ZoneWidth = 128;
            //zoneSetEntry[0].ZoneHeight = 128;

            //byte enabled = 0, pageCount = 0, colorR, colorG, colorB, zoneCount = 0, pageID = 0;

            //colorR = 0xFF;
            //colorG = 0xFF;
            //colorB = 0x02;
            ////Get MulitZone Setting
            //RT = czGetMulitZoneSetting(ref enabled, ref pageCount);

            ////Four steps are necessary!
            ////1.Enable Mulit Zone
            //enabled = 1;
            //RT = czSetEnableMulitZone(enabled);
            ////2.Set the page number=1
            //pageCount = 1;
            //RT = czSetPageCount(pageCount);

            ////Get MulitZone Setting
            //RT = czGetMulitZoneSetting(ref enabled, ref pageCount);

            //zoneSetHead = new ZoneSetHead
            //{
            //    ZoneNum = 1
            //};

            //pageID = 1;
            //zoneCount = zoneSetHead.ZoneNum;
            ////3.reset all zone
            //RT = czDivideZone(pageID, 1, zoneSetHead, zoneSetEntry);

            ////4.divide one zone
            //RT = czDivideZone(pageID, 0, zoneSetHead, zoneSetEntry);

            //ZoneDisplaySet zoneDisplayEntry = new ZoneDisplaySet
            //{
            //    ZoneSize = (UInt16)(zoneSetEntry[0].ZoneWidth * zoneSetEntry[0].ZoneHeight),
            //    ZoneID = zoneSetEntry[0].ZoneID,
            //    ZoneType = 0,
            //    CodeType = 0,
            //    InMode = 0,
            //    OutMode = 0,
            //    Align = 0,
            //    FGColor_R = colorR,
            //    FGColor_G = colorG,
            //    FGColor_B = colorB,
            //    BGColor_R = 0,
            //    BGColor_G = 0,
            //    BGColor_B = 0,
            //    AutoLine = 0,
            //    AutoWidth = 1,
            //    Speed = 2,
            //    StayTime = 2,
            //    Times = 0,
            //    FontStyle = (byte)CZ_FONT_ENB_16x12,
            //    LineSpace = 1,
            //    CloumnSpace = 1,
            //    BlinkOnTime = 0x0A,
            //    BlinkOffTime = 0x0A
            //};

            //zoneDisplayEntry.ContentSize = (UInt16)(zoneSetEntry[0].ZoneWidth * zoneSetEntry[0].ZoneHeight);

            //ZoneDisplayContentSet zoneDisplayContentEntry = new ZoneDisplayContentSet
            //{
            //    ZoneSize = (UInt16)(zoneSetEntry[0].ZoneWidth * zoneSetEntry[0].ZoneHeight),
            //    ZoneID = zoneSetEntry[0].ZoneID
            //};

            //zoneDisplayContentEntry = new ZoneDisplayContentSet
            //{
            //    ZoneSize = (UInt16)(zoneSetEntry[0].ZoneWidth * zoneSetEntry[0].ZoneHeight),
            //    ZoneID = zoneSetEntry[0].ZoneID
            //};

            //byte[] msgStr = new byte[30];
            //nPos = 0;
            //msgStr[nPos++] = 0x08;              //display picture control code
            //msgStr[nPos++] = 0x33;              //display picture control code
            //msgStr[nPos++] = 0x01;              //display picture control code

            ////Array.Copy(Encoding.ASCII.GetBytes(content), 0, msgStr, nPos, content.Length);
            ////nPos += (uint)content.Length;
            //zoneDisplayContentEntry.ContentSize = (UInt16)nPos;
            //Thread.Sleep((int)PARK_SLEEP_TIME);
            //RT = czSetContentDisplay(pageID, zoneCount, zoneDisplayContentEntry, msgStr);

            //msgStr = new byte[30];
            //nPos = 0;
            //msgStr[nPos++] = 0x08;              //display picture control code
            //msgStr[nPos++] = 0x33;              //display picture control code
            //msgStr[nPos++] = 0x02;              //display picture control code

            ////Array.Copy(Encoding.ASCII.GetBytes(content), 0, msgStr, nPos, content.Length);
            ////nPos += (uint)content.Length;
            //zoneDisplayContentEntry.ContentSize = (UInt16)nPos;
            //Thread.Sleep((int)PARK_SLEEP_TIME);
            //RT = czSetContentDisplay(pageID, zoneCount, zoneDisplayContentEntry, msgStr);
        }
        public bool fn_AdjustBright(ushort bright)
        {
            return czBrightAdjust(bright) == 0;
        }
        public bool fn_ReadBrightInfo(ref byte BrightType, ref byte Percent, ref byte ADValue)
        {
            return czReadBrightInfo(ref BrightType, ref Percent, ref ADValue) == 0;
        }

        public bool fn_ReadPicture(string pc_file_path, string sign_file_path, string sign_ip, UInt32 sign_port)
        {
            return czEasyReadFile(pc_file_path, sign_file_path, sign_ip, sign_port) == 0;
        }

        public bool fn_ReadStats(Byte statsType, ref Byte buf, UInt32 bufSize)
        {
            return czReadStats(statsType, ref buf, bufSize) == 0;
        }
        public bool fn_ResetSystem()
        {
            return czResetSystem() == 0;
        }
    }
}
