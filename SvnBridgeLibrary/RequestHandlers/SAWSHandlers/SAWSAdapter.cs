#if SC_SAWS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SAWSSDKLib;

namespace SvnBridge.RequestHandlers
{
    public class SAWSAdapter : SAWAdapter
    {
        /// <summary>
        /// These varibles are required when we connect to SAWS.
        /// </summary>
        public SAWSAppObject sdkObject = new SAWSAppObject();
        public string strServerIP = "";
        public string strDBServerName = "";
        public string strDBName = "";
        public string strRepositoryName = "Default";
        public string strUserName = "";
        public string strPassword = "";
        public int nServerPort = 7777;
        public bool bConnected = false;
        public bool bLogin = false;
        public List<SAWItemCache> itemCache = new List<SAWItemCache>();

        public SAWSAdapter()
        {
            sdkObject.PromptCheckinUnchangedFile += new _ISHAppObjectEvents_PromptCheckinUnchangedFileEventHandler(sdkObject_PromptCheckinUnchangedFile);
            sdkObject.PromptLeaveOrReplaceWriteableFile += new _ISHAppObjectEvents_PromptLeaveOrReplaceWriteableFileEventHandler(sdkObject_PromptLeaveOrReplaceWriteableFile);
            sdkObject.PromptUndoCheckoutChangedFile += new _ISHAppObjectEvents_PromptUndoCheckoutChangedFileEventHandler(sdkObject_PromptUndoCheckoutChangedFile);
            sdkObject.PromptCheckedoutFile += new _ISHAppObjectEvents_PromptCheckedoutFileEventHandler(sdkObject_PromptCheckedoutFile);
        }

        void sdkObject_PromptCheckedoutFile()
        {
            Enum_CheckedoutFileHandlingType enumCheckoutFileHandlingType;
            string strInfo;
            sdkObject.GetPromptCheckedoutFile(out strInfo, out enumCheckoutFileHandlingType);

            switch (enumCheckoutFileHandlingType)
            {
                case Enum_CheckedoutFileHandlingType.Enum_enumCheckinOrUndoAnotherPC:
                    sdkObject.SetPromptCheckedoutFile(Enum_CheckedoutFileHandling.Enum_CheckinOrUndoAnotherPC, true);
                    break;
            }
        }

        void sdkObject_PromptUndoCheckoutChangedFile()
        {
            sdkObject.SetPromptUndoCheckoutChangedFile(Enum_UndoCheckOutChangedFileHandling.Enum_UndoCheckOutChangeFile, true);
        }

        void sdkObject_PromptLeaveOrReplaceWriteableFile()
        {
            sdkObject.SetPromptLeaveOrReplaceModifiedFile(Enum_ModifiedFileHandling.Enum_ReplaceModifiedFile, true);
        }

        void sdkObject_PromptCheckinUnchangedFile()
        {
            sdkObject.SetPromptCheckinOrUndoUnchangedFile(Enum_CheckinUnchangedFileHandling.Enum_CheckinUnchangedFile, true);
        }

        public string GetCurRepository()
        {
            return this.strRepositoryName;
        }

        public void SetRepository(string rep)
        {
            if (rep == "") return;
            bool bNew = rep != strRepositoryName;
            strRepositoryName = rep;
            if (bNew)
            {
                //sdkObject = new SAWSAppObject();
                //Connect(this.strServerIP, this.nServerPort);
                Login(strUserName, strPassword, strRepositoryName);
            }
        }
        /// <summary>
        /// Create object and set basic information.
        /// </summary>
        /// <param name="strServerIP">The Server IP</param>
        /// <param name="nServerPort">The SAWS Server Port</param>

        public bool Connect(string strServerIP, int nServerPort)
        {
            this.strServerIP = strServerIP;
            this.nServerPort = nServerPort;
            Enum_EncryptType EncryptType;
            Boolean OnlyTrial;
            int LeftTrialDays;
            Boolean RealServer;
            Boolean Cancelled;
            string ResultDescription;
            if (0 != sdkObject.ConnectToServer(strServerIP, nServerPort, Enum_ProxyType.Enum_NOPROXY,
                "", 0, "", "", false, "", out EncryptType, out OnlyTrial,
                out LeftTrialDays, out RealServer, out strDBServerName,
                out strDBName, out Cancelled, out ResultDescription))
            {
                bConnected = false;
                return false;
            }
            else
            {
                bConnected = true;
                return !Cancelled;
            }
        }

        public bool Login(string strName, string strPassword, string rep)
        {
            if (rep.Length == 0)
            {
                bLogin = false;
                return false;
            }

            this.strUserName = strName;
            this.strPassword = strPassword;
            this.strRepositoryName = rep;
            var KeyFileSet = new SAWSKeyFileSet();
            Boolean MustChangePassword;
            int DaysOfExpiration;
            Boolean Cancelled;
            string ResultDescription;

            if (0 != sdkObject.Login(strName, strPassword, strDBName, strRepositoryName, KeyFileSet, out MustChangePassword,
                out DaysOfExpiration, out Cancelled, out ResultDescription))
            {
                bLogin = false;
                return false;
            }
            else
            {
                bLogin = true;
                return !Cancelled;
            }
        }

        public bool DeleteFile(string strRemotePath)
        {
            Boolean Cancelled;
            string ResultDescription;
            var OperationResultSet = new SAWSOperationResultSet();
            var fileset = new SAWSDeleteFileSet();
            fileset.Add(SAWCommon.ConvertPath(strRemotePath));
            if (0 != sdkObject.DeleteFiles(fileset, false, out Cancelled, out ResultDescription, out OperationResultSet))
            {/*
                if(ResultDescription.Contains("already"))
                    return true;
                else*/
                return false;
            }
            else
            {
                bool bSucceed = true;
                foreach (SAWSOperationResult result in OperationResultSet)
                {
                    if (result.OperationResult != 0)
                    {
                        bSucceed = false;
                        break;
                    }
                }

                return (bSucceed && !Cancelled);
            }
        }

        public bool AddFile(string strLocalPath, string strRemotePath, string Comment)
        {
            if (!(this.bConnected && this.bLogin)) return false;
            var AddFileSet = new SAWSAddFileSet();
            Boolean Cancelled;
            string ResultDescription;
            var OperationResultSet = new SAWSOperationResultSet();

            var file = new SAWSAddFile();
            file.FileType = Enum_FileType.Enum_Autodetect;
            file.LocalFileName = strLocalPath;
            file.RemoteFileName = SAWCommon.ConvertPath(strRemotePath);
            AddFileSet.Add(file);

            bool bAddSucceed = true;

            if (0 != sdkObject.AddFiles(AddFileSet, true, Comment, Enum_CompareFileBy.Enum_CompareFileByChecksum, Enum_SetLocalFileTime.Enum_SetLocalFileTimeCurrent,
                false, out Cancelled, out ResultDescription, out OperationResultSet))
            {
                bAddSucceed = false;
            }
            else
            {
                bAddSucceed = !Cancelled;
            }

            foreach (SAWSOperationResult result in OperationResultSet)
            {
                if (result.OperationResult != 0)
                {
                    bAddSucceed = false;
                    break;
                }
            }

            if (!bAddSucceed)
            {
                if (CheckOutFiles(strLocalPath, strRemotePath))
                {
                    bool ret = CheckInFiles(strLocalPath, strRemotePath, Comment);
                    if (!ret)
                    {
                        UndoCheckoutFiles(strRemotePath);
                    }
                    return ret;
                }
            }
            else
                return true;

            return false;
        }

        public string GetFileComment(string path)
        {
            Boolean Cancelled;
            string ResultDescription;
            Boolean IsMergeable;
            int RemoteVersionLow;
            int RemoteVersionHigh;
            System.DateTime LatestModified;
            int RemoteSizeLow;
            int RemoteSizeHigh;
            string Comment;
            path = SAWCommon.ConvertPath(path);

            if (0 != sdkObject.GetFileGeneralInfo(SAWCommon.ConvertPath(path), out IsMergeable, out RemoteVersionLow,
                out RemoteVersionHigh, out LatestModified, out RemoteSizeLow, out RemoteSizeHigh, out Comment,
                out Cancelled, out ResultDescription))
            {
                return "";
            }
            else
            {
                return Comment;
            }
        }

        public List<SAWItemInfo> GetFileHistory(string path)
        {
            bool Cancelled;
            string ResultDescription;
            var FileHistoryParam = new SAWSFileHistoryParam();
            bool IsPinned;
            var HistorySet = new SAWSHistorySet();
            bool bDir = false;
            if (0 != sdkObject.GetFileHistory(SAWCommon.ConvertPath(path), FileHistoryParam, out IsPinned, out HistorySet, out Cancelled, out ResultDescription))
            {
                var FolderHistoryParam = new SAWSProjectHistoryParam();
                if (0 != sdkObject.GetProjectHistory(SAWCommon.ConvertPath(path), FolderHistoryParam, out HistorySet, out Cancelled, out ResultDescription))
                {
                    return null;
                }
                bDir = true;
            }

            var set = new List<SAWItemInfo>();

            for (int i = 0; i < HistorySet.Count; i++)
            {
                SAWSHistory w = HistorySet.Item(i) as SAWSHistory;
                var his = new SAWItemInfo();
                his.comment = w.Comment == null ? "" : w.Comment;
                his.date = w.ActionDateTime;
                his.name = w.ItemName;
                his.size = w.FileSizeLow;
                his.user = w.UserName;
                his.version = w.VersionNumberLow;
                his.type = (EnumActionType)w.ActionType;
                his.isdir = bDir;
                set.Add(his);
            }
            return set;
        }

        public Int64 GetFileSize(string path)
        {
            Boolean Cancelled;
            string ResultDescription;
            Boolean IsMergeable;
            int RemoteVersionLow;
            int RemoteVersionHigh;
            System.DateTime LatestModified;
            int RemoteSizeLow;
            int RemoteSizeHigh;
            string Comment;
            path = SAWCommon.ConvertPath(path);

            if (0 != sdkObject.GetFileGeneralInfo(path, out IsMergeable, out RemoteVersionLow,
                out RemoteVersionHigh, out LatestModified, out RemoteSizeLow, out RemoteSizeHigh, out Comment,
                out Cancelled, out ResultDescription))
            {
                return 0;
            }
            else
            {
                Int64 ret = 0;
                ret = RemoteSizeLow;
                ret += RemoteSizeHigh * (1 << sizeof(long) * 8);
                return ret;
            }
        }

        public bool CheckInProject(string strLocalPath, string strRemotePath, string Comment)
        {
            if (!(this.bConnected && this.bLogin)) return false;

            Boolean Cancelled;
            String ResultDescription;
            String ProjectToCheckin = SAWCommon.ConvertPath(strRemotePath);
            String LocalDirectory = strLocalPath;
            var OperationResultSet = new SAWSOperationResultSet();
            var MergeParam = new SAWSDiffMergeParam();

            if (0 != sdkObject.CheckInProject(ProjectToCheckin, LocalDirectory, false, false, false, false,
                Enum_CompareFileBy.Enum_CompareFileByChecksum, Enum_EOL.Enum_EOLNative, false, Enum_SetLocalFileTime.Enum_SetLocalFileTimeCurrent,
                Enum_CheckinUnchangedFileHandling.Enum_AskCheckinUnchangedFile, Comment, MergeParam, out Cancelled, out ResultDescription, out OperationResultSet))
            {
                return false;
            }
            else
            {
                bool bSucceed = true;
                foreach (SAWSOperationResult result in OperationResultSet)
                {
                    if (result.OperationResult != 0)
                    {
                        bSucceed = false;
                        break;
                    }
                }

                return (bSucceed && !Cancelled);
            }
        }

        public bool Lock(string strFile)
        {
            return CheckOutFiles("", strFile);
        }
        public bool Unlock(string strFile)
        {
            return UndoCheckoutFiles(strFile);
        }

        public bool UndoCheckoutFiles(string strRemoteFile)
        {
            if (!(this.bConnected && this.bLogin)) return false;

            bool Cancelled;
            string ResultDescription;
            var undoCheckoutFileSet = new SAWSUndoCheckoutFileSet();
            var undoCheckoutFile = new SAWSUndoCheckoutFile();
            var OperationResultSet = new SAWSOperationResultSet();
            undoCheckoutFile.FileToUndo = SAWCommon.ConvertPath(strRemoteFile);
            undoCheckoutFile.LocalFileName = "";
            undoCheckoutFileSet.Add(undoCheckoutFile);

            if (0 != sdkObject.UndoCheckoutFiles(undoCheckoutFileSet, Enum_LocalFileHandling.Enum_LeaveLocalFile, Enum_UndoCheckOutChangedFileHandling.Enum_UndoCheckOutChangeFile,
                Enum_CompareFileBy.Enum_CompareFileByChecksum, Enum_EOL.Enum_EOLNative, true, Enum_SetLocalFileTime.Enum_SetLocalFileTimeCurrent,
                out Cancelled, out ResultDescription, out OperationResultSet))
            {
                return false;
            }
            else
            {
                return !Cancelled;
            }
        }

        public bool CheckOutFiles(string strLocalPath, string strRemotePath)
        {
            if (!(this.bConnected && this.bLogin)) return false;

            bool Cancelled;
            string ResultDescription;
            var CheckoutFileSet = new SAWSCheckoutFileSet();
            var CheckoutFile = new SAWSCheckoutFile();
            var OperationResultSet = new SAWSOperationResultSet();
            var MergeParam = new SAWSDiffMergeParam();
            CheckoutFile.FileToCheckout = SAWCommon.ConvertPath(strRemotePath);
            CheckoutFile.LocalFileName = strLocalPath;
            CheckoutFileSet.Add(CheckoutFile);

            if (0 != sdkObject.CheckoutFiles(CheckoutFileSet, true, "lock", true, Enum_ModifiedFileHandling.Enum_ReplaceModifiedFile, Enum_EOL.Enum_EOLNative,
                Enum_CompareFileBy.Enum_CompareFileByChecksum, Enum_SetLocalFileTime.Enum_SetLocalFileTimeCurrent, MergeParam, out Cancelled, out ResultDescription, out OperationResultSet))
            {
                return false;
            }
            else
            {
                return !Cancelled;
            }
        }

        public bool CheckOutProject(string strLocalPath, string strRemotePath, string Comment)
        {
            if (!(this.bConnected && this.bLogin)) return false;

            Boolean Cancelled;
            String ResultDescription;
            String ProjectToCheckout = SAWCommon.ConvertPath(strRemotePath);
            String LocalDirectory = strLocalPath;
            var OperationResultSet = new SAWSOperationResultSet();
            var MergeParam = new SAWSDiffMergeParam();

            if (0 != sdkObject.CheckoutProject(ProjectToCheckout, LocalDirectory, Comment, false, false, false,
                Enum_ModifiedFileHandling.Enum_AskModifiedFile, Enum_EOL.Enum_EOLNative, Enum_CompareFileBy.Enum_CompareFileByChecksum,
                Enum_SetLocalFileTime.Enum_SetLocalFileTimeCurrent, MergeParam, out Cancelled, out ResultDescription, out OperationResultSet))
            {
                return false;
            }
            else
            {
                return !Cancelled;
            }
        }

        public bool CheckInFiles(string strLocalPath, string strRemotePath, string Comment)
        {
            bool Cancelled;
            string ResultDescription;
            var CheckinFileSet = new SAWSCheckinFileSet();
            var CheckinFile = new SAWSCheckinFile();
            var OperationResultSet = new SAWSOperationResultSet();
            var MergeParam = new SAWSDiffMergeParam();
            CheckinFile.FileToCheckin = SAWCommon.ConvertPath(strRemotePath);
            CheckinFile.LocalFileName = strLocalPath;
            CheckinFileSet.Add(CheckinFile);

            if (0 != sdkObject.CheckInFiles(CheckinFileSet, false, false, Enum_CompareFileBy.Enum_CompareFileByChecksum,
                Enum_EOL.Enum_EOLNative, false, true, Enum_SetLocalFileTime.Enum_SetLocalFileTimeCurrent, Enum_CheckinUnchangedFileHandling.Enum_CheckinUnchangedFile,
                Comment, MergeParam, out Cancelled, out ResultDescription, out OperationResultSet))
            {
                return false;
            }
            else
            {
                return !Cancelled;
            }
        }

        public bool GetFileModifiedDate(string path, out System.DateTime date)
        {
            bool Cancelled;
            string ResultDescription;
            bool IsMergeable;
            int versionLow;
            int versionHigh;
            int sizeLow;
            int sizeHigh;
            string Comment = "";
            if (0 != sdkObject.GetFileGeneralInfo(SAWCommon.ConvertPath(path), out IsMergeable, out versionLow, out versionHigh, out date, out sizeLow, out sizeHigh, out Comment, out Cancelled, out ResultDescription))
            {
                return true;
            }
            else
            {
                return !Cancelled;
            }
        }

        public bool GetDateFromVersion(string path, int version, out System.DateTime date)
        {
            Boolean Cancelled;
            string ResultDescription;
            ulong ItemID = 0;
            ulong VersionNumber = (ulong)version;
            bool Project, Mergable, Deleted;
            DateTime CheckinTime, ModificationTime;
            long FileSize;
            Enum_ItemStatus ItemStatus;
            string CheckedOutUser;

            int iResult = sdkObject.GetItemInfo(path, ref ItemID, ref VersionNumber, out Project, out Mergable, out Deleted, out CheckinTime,
                out ModificationTime, out FileSize, out ItemStatus, out CheckedOutUser, out Cancelled, out ResultDescription);

            date = CheckinTime;

            return (0 == iResult);
        }

        public int GetVersionFromDate(string path, System.DateTime date)
        {
            Boolean Cancelled;
            string ResultDescription;
            ulong ItemID = 0;
            ulong VersionNumber = 0;

            int iResult = sdkObject.GetItemVersionByCheckinDate(path, ref ItemID, date, out VersionNumber, out Cancelled, out ResultDescription);
            return (int)VersionNumber;
        }


        public SAWItemInfo GetFileInfo(string file, int ver)
        {
            /*
            Boolean Cancelled;
            string ResultDescription;
            var set = new SAWSFileCommonInfoSet();
            var info = new SAWSFileCommonInfo();
            info.FileName = file;
            if (ver == -1) ver = GetLastestVersionNum(file);
            set.Add(info);
            if (0 == sdkObject.GetFilesCommonInfo(set, out Cancelled, out ResultDescription)
                && set.GetCount() != 0)
            {
                SAWSFileCommonInfo c = new SAWSFileCommonInfo();
                SAWItemInfo t = new SAWItemInfo();
                t.isdir = false;
                t.name = file;
                t.size = c.FileLengthLow;
                t.version = ver;
                t.date = c.ModificationDateTime;
                return t;
            }
            else
            {
                return null;
            }
             */
            return null;
        }

        public bool FileExists(string strRemotePath, int ver)
        {
            return GetLastestVersionNum(strRemotePath) != -1;
            /*
            Boolean Cancelled;
            string ResultDescription;
            int VersionLow;
            int VersionHigh;
            string FileName = SAWCommon.ConvertPath(strRemotePath);
            if (ver < 0)
            {
                if (0 != sdkObject.GetFileLatestVersionNumber(FileName, out VersionLow, out VersionHigh, out Cancelled, out ResultDescription))
                {
                    return false;
                }
                else
                {
                    return !Cancelled;
                }
            }
            return true;
             */
        }

        public int GetLastestVersionNum(string strPath)
        {
            Boolean Cancelled;
            string ResultDescription;
            strPath = SAWCommon.ConvertPath(strPath);
            ulong ItemID = 0;
            ulong VersionNumber = 0;
            bool Project, Mergable, Deleted;
            DateTime CheckinTime, ModificationTime;
            long FileSize;
            Enum_ItemStatus ItemStatus;
            string CheckedOutUser;

            int iResult = sdkObject.GetItemInfo(strPath, ref ItemID, ref VersionNumber, out Project, out Mergable, out Deleted, out CheckinTime,
                out ModificationTime, out FileSize, out ItemStatus, out CheckedOutUser, out Cancelled, out ResultDescription);

            return (ItemID == 0) ? -1 : (int)VersionNumber;
        }

        public List<SAWItemInfo> EnumItems(string strFolder, int version)
        {
            if (version == -1) version = GetLastestVersionNum(strFolder);

            // Lookup cache.
            /*foreach(SAWItemCache v in this.itemCache)
            {
                if(strFolder == v.strPath && version == v.version)
                {
                    return v.result;
                }
            }*/

            List<SAWItemInfo> result = new List<SAWItemInfo>();
            SAWSHistorySet subProject, subFile;
            Boolean Cancelled;
            string ResultDescription;
            if (0 != sdkObject.GetProjectTreeByVersion(SAWCommon.ConvertPath(strFolder), version, out subProject, out subFile, out Cancelled, out ResultDescription))
            {
                return null;
            }

            foreach (SAWSHistory v in subProject)
            {
                SAWItemInfo his = new SAWItemInfo();
                his.comment = v.Comment;
                his.name = v.ItemName;
                his.size = v.FileSizeLow;
                his.user = v.UserName;
                his.version = v.VersionNumberLow;
                his.date = v.ModificationDateTime;
                his.date = System.DateTime.Now;
                his.isdir = true;
                result.Add(his);
            }

            foreach (SAWSHistory v in subFile)
            {
                SAWItemInfo his = new SAWItemInfo();
                his.comment = v.Comment;
                his.name = v.ItemName;
                his.size = v.FileSizeLow;
                his.user = v.UserName;
                his.version = v.VersionNumberLow;
                his.date = v.ModificationDateTime;
                his.isdir = false;
                result.Add(his);
            }
            /*
            SAWItemCache c = new SAWItemCache();
            c.strPath = strFolder;
            c.version = version;
            c.result = result;
            itemCache.Add(c);*/
            return result;
        }

        public List<string> EnumFiles(string strFolder)
        {
            SAWSFileListSet FileListSet = new SAWSFileListSet();
            Boolean Cancelled;
            string ResultDescription;
            strFolder = SAWCommon.ConvertPath(strFolder);
            if (0 != sdkObject.GetFileListFromProject(strFolder, out FileListSet, out Cancelled, out ResultDescription))
            {
                return null;
            }
            else
            {
                List<string> res = new List<string>();
                for (int i = 0; i < FileListSet.Count; i++)
                {
                    res.Add(FileListSet.Item(i));
                }
                return res;
            }
        }

        public List<string> EnumFolders(string strFolder)
        {
            SAWSProjectListSet ProjectListSet = new SAWSProjectListSet();
            Boolean Cancelled;
            string ResultDescription;
            strFolder = SAWCommon.ConvertPath(strFolder);
            if (0 != sdkObject.GetProjectListFromProject(strFolder, out ProjectListSet, out Cancelled, out ResultDescription))
            {
                return null;
            }
            else
            {
                List<string> res = new List<string>();
                for (int i = 0; i < ProjectListSet.Count; i++)
                {
                    res.Add(ProjectListSet.Item(i));
                }
                return res;
            }
        }

        public bool GetFile(int version, string strLocalPath, string strRemotePath)
        {
            if (!(this.bConnected && this.bLogin)) return false;
            Boolean Cancelled;
            string ResultDescription;
            string FileNameOnServer = SAWCommon.ConvertPath(strRemotePath);
            int VersionNumberLow = version;
            string LocalFileName = strLocalPath;
            var MergeParam = new SAWSDiffMergeParam();

            if (0 != sdkObject.GetOldVersionFile(FileNameOnServer, VersionNumberLow, 0, LocalFileName, false,
                Enum_ModifiedFileHandling.Enum_ReplaceModifiedFile, Enum_EOL.Enum_EOLNative,
                Enum_CompareFileBy.Enum_CompareFileByChecksum, Enum_SetLocalFileTime.Enum_SetLocalFileTimeCurrent,
                MergeParam, out Cancelled, out ResultDescription))
            {
                return false;
            }
            else
            {
                return !Cancelled;
            }
        }
    }
}

#endif