using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SvnBridge.RequestHandlers
{
    public class SAWCommon
    {
        public static string SourceControlServer = "";
        public static int SourceControlPort = 0;
        public static string SourceControlBridgeServer = "";
        public static int SourceControlBridgePort = 0;
        public static string TempPath = "E:\\Temp\\";
        public static string RepositoryName = "default";

        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def,
              System.Text.StringBuilder retVal, int size, string filePath);

        public static void LoadIni()
        {
            System.Text.StringBuilder temp = new System.Text.StringBuilder(256);
            string configfile = System.Environment.CurrentDirectory + "\\scconfig.ini";
        
        
#if SC_Hosted
            GetPrivateProfileString("SourceControl", "SCID", "100000", temp, 255, configfile);
            SourceControlServer = temp.ToString();
            GetPrivateProfileString("SourceControl", "SCPort", "443", temp, 255, configfile);
            SourceControlPort = int.Parse(temp.ToString());
#endif

#if SC_Std
            GetPrivateProfileString("SourceControl", "SCIP", "127.0.0.1", temp, 255, configfile);
            SourceControlServer = temp.ToString();
            GetPrivateProfileString("SourceControl", "SCPort", "7777", temp, 255, configfile);
            SourceControlPort = int.Parse(temp.ToString());
#endif
            GetPrivateProfileString("SourceControl", "Repository", "default", temp, 255, configfile);
            RepositoryName = temp.ToString();
            GetPrivateProfileString("SourceControl", "SCBIP", "192.168.1.212", temp, 255, configfile);
            SourceControlBridgeServer = temp.ToString();
            GetPrivateProfileString("SourceControl", "SCBPort", "8080", temp, 255, configfile);
            SourceControlBridgePort = int.Parse(temp.ToString());
            GetPrivateProfileString("SourceControl", "TempPath", "C:\\Temp\\", temp, 255, configfile);
            TempPath = temp.ToString();
            if (!TempPath.EndsWith("\\"))
                TempPath += "\\";
        }

        private static string Root = "$/";
        public static string ConvertPath(string path)
        {/*
            if (path == "/") path = "/" + strRepositoryName + "/";
            string[] res = path.Split('/');
            string repository;
            if (path.StartsWith("/"))
            {
                if (res.Length >= 2)
                {
                    repository = res[1];
                }
                else
                {
                    repository = "";
                }
                path = path.Substring(1 + repository.Length);
            }
            else
            {
                if (res.Length >= 1)
                {
                    repository = res[0];
                }
                else
                {
                    repository = "";
                }
                path = path.Substring(repository.Length);
            }
            if(path.StartsWith("/")) return string.Format("${0}", path);
            else if(!path.StartsWith("$/")) return "$/" + path;
            else return path;*/
            if (path.StartsWith("/"))
                path = string.Format("${0}", path);

            if (!path.StartsWith(Root))
            {
                path = string.Format("{0}{1}", Root, path);
            }

            if (path != Root)
            {
                if (path.EndsWith("/"))
                    path = path.Remove(path.Length - 1);
            }
            
            return path;
        }
    }
    public enum EnumActionType
    {
        Enum_ActionTypeNull = 0,
        Enum_ActionTypeAdd = 1,
        Enum_ActionTypeDelete = 2,
        Enum_ActionTypeRecover = 4,
        Enum_ActionTypeCheckin = 8,
        Enum_ActionTypeRename = 16,
        Enum_ActionTypeMoveOut = 32,
        Enum_ActionTypeMoveIn = 64,
        Enum_ActionTypeShare = 128,
        Enum_ActionTypeBranch = 256,
        Enum_ActionTypeMerge=512,
        Enum_ActionTypeRollback = 2048,
        //Enum_ActionTypeInheritedForVersionTracking = 4096,
        Enum_ActionTypeLabel = 16384,
        Enum_ActionTypePin = 131072,
        Enum_ActionTypeUnpin = 262144,
        Enum_ActionTypePurge = 524288,
        //Enum_ActionTypeForParentDisplay = 1048576,
        //Enum_ActionTypeAddForParentDisplay = 2097152,
        //Enum_ActionTypeDeleteForParentDisplay = 4194304,
        //Enum_ActionTypeRecoverForParentDisplay = 8388608,
        //Enum_ActionTypePurgeForParentDisplay = 16777216,
        //Enum_ActionTypeRenameForParentDisplay = 33554432,
        //Enum_ActionTypeShareForParentDisplay = 67108864,
        //Enum_ActionTypeBranchForParentDisplay = 134217728,
        //Enum_ActionTypeMoveInForParentDisplay = 268435456,
        //Enum_ActionTypeMoveOutForParentDisplay = 536870912,
    }


    public class SAWItemInfo
    {
        public string user;
        public string name;
        public DateTime date;
        public string comment;
        public int size;
        public int version;
        public EnumActionType type;
        public bool isdir;
    }

    public interface SAWAdapter
    {
        bool Connect(string strServerIP, int nServerPort);
        bool Login(string strName, string strPassword, string rep);
        string GetCurRepository();
        void SetRepository(string rep);
        bool DeleteFile(string strRemotePath);
        bool AddFile(string strLocalPath, string strRemotePath, string Comment);
        string GetFileComment(string path);
        List<SAWItemInfo> GetFileHistory(string path);
        Int64 GetFileSize(string path);
        bool CheckInProject(string strLocalPath, string strRemotePath, string Comment);
        bool CheckOutFiles(string strLocalPath, string strRemotePath);
        bool CheckOutProject(string strLocalPath, string strRemotePath, string Comment);
        bool CheckInFiles(string strLocalPath, string strRemotePath, string Comment);
        bool GetFileModifiedDate(string path, out System.DateTime date);
        SAWItemInfo GetFileInfo(string file, int ver);
        bool GetDateFromVersion(string path, int version, out System.DateTime date);
        int GetVersionFromDate(string path, System.DateTime date);
        bool FileExists(string strRemotePath, int ver);
        int GetLastestVersionNum(string strPath);
        List<SAWItemInfo> EnumItems(string strFolder, int ver);
        bool GetFile(int version, string strLocalPath, string strRemotePath);
        bool Lock(string strFile);
        bool Unlock(string strFile);
    }

    /// <summary>
    /// Class SAWSAdapter
    /// By Zju.Aegisys[0GiNr]
    /// Last Update: 2011-10-5
    /// </summary>
    public class SAWItemCache
    {
        public string strPath = "";
        public int version = -1;
        public List<SAWItemInfo> result = null;
    }
}
