using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.dynamsoft.scm.SCMSDK;
using com.dynamsoft.scm.SCMSDK.Framework;
using com.dynamsoft.scm.client.pub.common.basedataobjects;
using com.dynamsoft.scm.client.sc.common.framework;
using com.dynamsoft.scm.client.sc.common;

namespace SvnBridge.RequestHandlers
{
    // TODO: code here.
    public class SCMSAdapter : SAWAdapter
    {
        /// <summary>
        /// These varibles are required when we connect to SAWS.
        /// </summary>
        public SCMSSDK sdkObject = new SCMSSDK();
        public string strServerIP = "";
        public string strDBServerName = "";
        public string strDBName = "";
        public string strRepositoryName = "Default";
        public string strUserName = "";
        public string strPassword = "";
        public int nServerPort = 7771;
        public bool bConnected = false;
        public bool bLogin = false;
        public List<SAWItemCache> itemCache = new List<SAWItemCache>();


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

        public string GetCurRepository()
        {
            return this.strRepositoryName;
        }
        public bool Connect(string strServerIP, int nServerPort)
        {
            this.strServerIP = strServerIP;
            this.nServerPort = nServerPort;
            this.bConnected = true;
            return true;
        }
        public bool Login(string strName, string Password, string rep)
        {
            this.strUserName = strName;
            this.strPassword = Password;
            this.strRepositoryName = rep;
            BoolObject bTrial = new BoolObject();
            IntObject nTrial = new IntObject();
            IntObject nPasswordLeft = new IntObject();
            StringObject error = new StringObject();
            if (0 != sdkObject.ConnectToServer(strServerIP, nServerPort, false, strName, Password, 0, "", 0, "", "", bTrial, nTrial, nPasswordLeft, error))
            {
                this.bLogin = false;
                return false;
            }
            else 
            {
                this.bLogin = true;
                return true;
            }
        }
        public bool DeleteFile(string strRemotePath)
        {
            if (!this.bLogin) 
                return false;
            List<string> toDelete = new List<string>();
            toDelete.Add(SAWCommon.ConvertPath(strRemotePath));
            StringObject error = new StringObject();
            List<SDKItemOperatorResult> results = new List<SDKItemOperatorResult>();
            if (0 != sdkObject.DeleteFiles(strRepositoryName, toDelete, false, ref results, error))  //DestoryPermanently is set as false
            {
                return false;
            }
            else
            {
                if (OperatorResultsHaveError(results))  //File is already deleted but not Purged.
                    return false;
                else
                    return true;
            }
        }

        public bool AddFile(string strLocalPath, string strRemotePath, string Comment)
        {
            if (!this.bLogin)
                return false;
            List<string> Local = new List<string>();
            Local.Add(strLocalPath);
            List<string> Remote = new List<string>();
            Remote.Add(SAWCommon.ConvertPath(strRemotePath));
            List<SDKItemOperatorResult> results = new List<SDKItemOperatorResult>();
            StringObject error = new StringObject();
            if (0 != sdkObject.AddFiles(strRepositoryName, Remote, Local, false, Comment, ref results, error))
            {
                return false;
            }
            else
            {
                if (!OperatorResultsHaveError(results))
                { 
                    return true; 
                }
                else
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
                    return false;
                }
            }

        }
        public string GetFileComment(string path)
        {
            SAWItemInfo itemInfo = GetFileInfo(path, -1);
            string strComment = "";
            if (itemInfo != null)
            {
                strComment = itemInfo.comment;
            }
            return strComment;
        }

        public List<SAWItemInfo> GetFileHistory(string path)
        {
            List<SDKHistory> history = new List<SDKHistory>();
            StringObject error = new StringObject();
            path = SAWCommon.ConvertPath(path);
            List<string> listUsers = new List<string>();
            List<SHistorySortOrderBy> listFilterHistorySortOrderBy = new List<SHistorySortOrderBy>();
            //SHistorySortOrderBy HistorySortOrderBy = new SHistorySortOrderBy();
            //listFilterHistorySortOrderBy.Add(HistorySortOrderBy);
            if (0 != sdkObject.GetFileHistory(strRepositoryName, path, listUsers, int.MaxValue, false, "", listFilterHistorySortOrderBy, ref history, true, error))
            {
                if (error.GetValue().Contains("The operation can not be performed on folder"))
                {
                    List<string> listFileExtensions = new List<string>();
                    List<string> listFileSubStrings = new List<string>();
                    if (0 != sdkObject.GetFolderHistoryByItem(strRepositoryName, path, false, listUsers, listFileExtensions, listFileSubStrings, int.MaxValue, false, "", listFilterHistorySortOrderBy, new System.DateTime(), System.DateTime.Now, ref history, error))
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            List<SAWItemInfo> ItemHistoryInfoset = new List<SAWItemInfo>();
            foreach (SDKHistory ItemHistory in history)
            {
                SAWItemInfo ItemInfo = new SAWItemInfo();
                ItemInfo.comment = ItemHistory.m_strComment;
                ItemInfo.name = ItemHistory.m_strItemFullName;
                ItemInfo.size = (int)ItemHistory.m_lFileSize;
                ItemInfo.version = (int)ItemHistory.m_lChangeSetID; //GetLastestVersionNum(ItemHistory.m_strItemFullName);
                ItemInfo.date = ItemHistory.m_dtCheckin;
                ItemInfo.isdir = IsItemAFolder(ItemHistory.m_enumItemType);
                ItemInfo.type = ConvertActionType(ItemHistory.m_enumActionType);
                ItemInfo.user = ItemHistory.m_strUserName;
                ItemHistoryInfoset.Add(ItemInfo);
            }
            return ItemHistoryInfoset;
        }


        public Int64 GetFileSize(string path)
        {
            Int64 size = -1;
            SAWItemInfo itemInfo = GetFileInfo(path, -1);
            if (itemInfo != null)
            {
                size = itemInfo.size;
            }
            return size;
        }
        public bool CheckInProject(string strLocalPath, string strRemotePath, string Comment)   //no use
        {
            List<SDKItemOperatorResult> results = new List<SDKItemOperatorResult>();
            BoolObject bConflict = new BoolObject();
            StringObject error = new StringObject();
            return 0 == sdkObject.CheckInFolder("", strRemotePath, true, Comment, null, false, 2, ref results, bConflict, error);
        }
        public bool CheckOutFiles(string strLocalPath, string strRemotePath)
        {
            if (! this.bLogin)
                return false;
            List<string> listCheckoutServerFileFullName = new List<string>();
            List<string> listCheckoutLocalFileFullName = new List<string>();
            string strComment = "lock";
            bool bDoNotGetLocalCopy = true;
            int iEnumExclusiveCheckout = EnumCheckoutLockType.enumExclusiveLock;
            int iEnumEOL = EnumEOL.enumEOLNative;
            int iEnumSetLocalFileTime = EnumSetLocalFileTime.enumCurrentFileTime;
            int iEnumModifiedFileHandling = EnumModifiedFileHandling.enumReplaceModifiedFile;
            SDKDiffMergeParameter stDiffMergeParameter = new SDKDiffMergeParameter();
            List<SDKItemOperatorResult> listItemOperateResults = new List<SDKItemOperatorResult>();
            StringObject strobjError = new StringObject();
            strRemotePath = SAWCommon.ConvertPath(strRemotePath);
            listCheckoutServerFileFullName.Add(strRemotePath);
            listCheckoutLocalFileFullName.Add(strLocalPath);
            if (0 != sdkObject.CheckoutFiles(strRepositoryName, listCheckoutServerFileFullName, listCheckoutLocalFileFullName, strComment, bDoNotGetLocalCopy, iEnumExclusiveCheckout, iEnumEOL, iEnumSetLocalFileTime, iEnumModifiedFileHandling, stDiffMergeParameter, ref listItemOperateResults, strobjError))
            {
                return false;
            }
            else
            {
                if (!OperatorResultsHaveError(listItemOperateResults))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public bool CheckOutProject(string strLocalPath, string strRemotePath, string Comment)  //no use
        {
            return true;
        }
        public bool CheckInFiles(string strLocalPath, string strRemotePath, string Comment)
        {
            List<string> listFilesFullNameToCheckIn = new List<string>();
            bool bKeepCheckout = false;
            bool bRemoveLocalCopy = true;
            string strComment = Comment;
            List<long> listIssueID = new List<long>();
            bool bUseReadOnly = false;
            int iEnumCheckInUnChangedFileHandling = EnumCheckinUnchangedFileHandling.enumUndoCheckinUnchangedFile;
            List<SDKItemOperatorResult> listItemOperateResults = new List<SDKItemOperatorResult>();
            BoolObject bConflictExists = new BoolObject();
            StringObject strobjError = new StringObject();
            strRemotePath = SAWCommon.ConvertPath(strRemotePath);
            listFilesFullNameToCheckIn.Add(strRemotePath);
            if (0 != sdkObject.CheckInFiles(strRepositoryName,listFilesFullNameToCheckIn,bKeepCheckout,bRemoveLocalCopy,strComment,listIssueID,bUseReadOnly,iEnumCheckInUnChangedFileHandling,ref listItemOperateResults,bConflictExists,strobjError))
            {
                return false;
            }
            else
            {
                if (!OperatorResultsHaveError(listItemOperateResults))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

        }
        public bool GetFileModifiedDate(string path, out System.DateTime date)
        {
            date = System.DateTime.Today;
            SAWItemInfo itemInfo = GetFileInfo(path, -1);
            if (itemInfo != null)
            {
                date = itemInfo.date;
            }
            return true;
        }
        public bool GetDateFromVersion(string path, int version, out System.DateTime date)  //no use
        {
            date = System.DateTime.Today;
            return false;
        }
        public int GetVersionFromDate(string path, System.DateTime date)
        {
            List<SAWItemInfo> history = GetFileHistory(path);
            int version = -1;
            for (int i = 0; i < history.Count; i++)
            {
                SAWItemInfo item = history[i];
                if (item.date <= date)
                    version = version > item.version ? version : item.version;
                else
                    break;
            }
            return version;
 
        }
        public bool FileExists(string strRemotePath, int ver)
        {
            bool bFileExists = false;
            if (null != GetFileInfo(strRemotePath, -1))
            {
                bFileExists = true;
            }
            return bFileExists;
        }
        public int GetLastestVersionNum(string strPath)
        {
            int iLatesetVersion = -1;
            SAWItemInfo itemInfo = GetFileInfo(strPath, -1);
            if (itemInfo != null)
            {
                iLatesetVersion = itemInfo.version;
            }
            return iLatesetVersion;

        }
        public List<SAWItemInfo> EnumItems(string path, int ver)
        {
            if(false==GetFileInfo(path,-1).isdir)
                return null;
            List<SAWItemInfo> result = new List<SAWItemInfo>();
            path = SAWCommon.ConvertPath(path);
             List<string> listSubFileNames=new List<string>();
             StringObject strobjError = new StringObject();
            bool bRecursive=false;
            List<string> listSubFolderFullNames = new List<string>();
            if ((0 != sdkObject.GetSubFolderFromParentFolder(strRepositoryName, path, bRecursive, ref listSubFolderFullNames, strobjError)) ||
                (0 != sdkObject.GetFileListFromParentProject(strRepositoryName, path, ref listSubFileNames, strobjError)))
            {
                return null;
            }
            foreach (string strSubFolder in listSubFolderFullNames)
            {
                result.Add(GetFileInfo(strSubFolder, -1));
            }
            foreach (string strSubFile in listSubFileNames)
            {
                result.Add(GetFileInfo(strSubFile, -1));
            }
            return result;
        }
        public bool GetFile(int version, string strLocalPath, string strRemotePath)
        {
            string strFileServerFullNameToGet = SAWCommon.ConvertPath(strRemotePath);
            long lVersionNumber = version;
            string strLocalFileFullName = strLocalPath;
            bool bMakeWritable = true;
            int iEnumEOL = EnumEOL.enumEOLNative;
            int iEnumSetLocalFileTime = EnumSetLocalFileTime.enumCurrentFileTime;
            int iEnumModifiedFileHandling = EnumModifiedFileHandling.enumReplaceModifiedFile;
            SDKDiffMergeParameter stDiffMergeParameter = new SDKDiffMergeParameter();
            List<SDKItemOperatorResult> listItemOperateResults = new List<SDKItemOperatorResult>();
            StringObject strobjError = new StringObject();
            if (0 != sdkObject.GetOldVersionFile(strRepositoryName, strFileServerFullNameToGet, lVersionNumber, strLocalFileFullName, bMakeWritable, iEnumEOL, iEnumModifiedFileHandling, iEnumSetLocalFileTime, stDiffMergeParameter, ref listItemOperateResults, strobjError))
            {
                return false;
            }
            else
            {
                if (!OperatorResultsHaveError(listItemOperateResults))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

        }
        public SAWItemInfo GetFileInfo(string file, int ver)
        {
            SAWItemInfo ItemInfo = new SAWItemInfo();
            LongObject version = new LongObject();
            DateTime time = new DateTime();
            LongObject size = new LongObject();
            StringObject comment = new StringObject();
            StringObject error = new StringObject();
            file = SAWCommon.ConvertPath(file);
            if (0 != sdkObject.GetFileGeneralInfo(strRepositoryName, file, version, ref time, size, comment, error))
            {
                string errorIfFolder = "File \"" + file + "\" not found.";
                if (errorIfFolder == error.GetValue())
                {
                    IntObject iobjSubFoldersNotDeletedCount = new IntObject();
                    IntObject iobjSubFoldersDeletedCount = new IntObject();
                    IntObject iobjFileNotDeletedCount = new IntObject();
                    IntObject iobjFilesDeletedCount = new IntObject();
                    List<SDKDeletedItemGeneralInfo> listDeletedItemGeneralInfo = new List<SDKDeletedItemGeneralInfo>();
                    IntObject iobjRight = new IntObject();
                    if (0 == sdkObject.GetFolderGeneralInfo(strRepositoryName, file, iobjSubFoldersNotDeletedCount, iobjSubFoldersDeletedCount, iobjFileNotDeletedCount, iobjFilesDeletedCount, version, ref time, comment, ref listDeletedItemGeneralInfo, iobjRight, error))
                    {
                        ItemInfo.comment = comment.GetValue();
                        ItemInfo.name = file;
                        ItemInfo.size = 0;
                        ItemInfo.version = (int)version.GetValue();
                        ItemInfo.date = time;
                        ItemInfo.isdir = true;
                        ItemInfo.type = EnumActionType.Enum_ActionTypeNull;
                        ItemInfo.user = "";
                        return ItemInfo;
                    }
                }
                return null;
            }
            else
            {
                ItemInfo.comment = comment.GetValue();
                ItemInfo.name = file;
                ItemInfo.size = (int)size.GetValue();
                ItemInfo.version = (int)version.GetValue();
                ItemInfo.date = time;
                ItemInfo.isdir = false;
                ItemInfo.type = EnumActionType.Enum_ActionTypeNull;
                ItemInfo.user = "";
                return ItemInfo;
            }
        }
        public bool Lock(string strFile)
        {
            string temp;
            temp = string.Format("{0}{1}", SAWCommon.TempPath, Guid.NewGuid().ToString());
            return CheckOutFiles(temp, strFile);
        }
        public bool Unlock(string strFile) 
        {
            return UndoCheckoutFiles(strFile);
        }
        public bool UndoCheckoutFiles(string strRemoteFile)
        {
            List<string> listFileFullNameToUndoCheckout = new List<string>();
            int enumUndoCheckOutChangedFileHandling = EnumUndoCheckOutChangedFileHandling.enumUndoCheckOutAndDeleteLocalCopy;
            int enumEOL = EnumEOL.enumEOLNative;
            int enumSetFileTime = EnumSetLocalFileTime.enumCurrentFileTime;
            bool bReadOnly = false;
            List<SDKItemOperatorResult> listItemOperateResults = new List<SDKItemOperatorResult>();
            StringObject strobjError = new StringObject();
            strRemoteFile = SAWCommon.ConvertPath(strRemoteFile);
            listFileFullNameToUndoCheckout.Add(strRemoteFile);
            if(0!=sdkObject.UndoCheckoutFiles(strRepositoryName,listFileFullNameToUndoCheckout,enumUndoCheckOutChangedFileHandling,enumEOL,enumSetFileTime,bReadOnly,ref listItemOperateResults,strobjError))
            {
                return false;
            }
            else
            {
                if (!OperatorResultsHaveError(listItemOperateResults))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool OperatorResultsHaveError(List<SDKItemOperatorResult> results)
        {
            bool bHaveError = false;
            foreach (SDKItemOperatorResult result in results)
            {
                if (result.m_ienumErrorCode != 0)
                {
                    bHaveError = true;
                    break;
                }
            }
            return bHaveError;
        }
        private bool IsItemAFolder(int SCMItemType)
        {
            bool bIsFolder = false;
            if (SCMItemType == EnumItemType.enumFolder)
            {
                bIsFolder = true;
            }
            return bIsFolder;
        }
        private EnumActionType ConvertActionType(int SCMActionType)
        {
            switch(SCMActionType)
            {
                case com.dynamsoft.scm.client.sc.common.framework.EnumActionType.enumAdd:
                    return EnumActionType.Enum_ActionTypeAdd;
                case com.dynamsoft.scm.client.sc.common.framework.EnumActionType.enumBranch:
                    return EnumActionType.Enum_ActionTypeBranch;
                case com.dynamsoft.scm.client.sc.common.framework.EnumActionType.enumDelete:
                    return EnumActionType.Enum_ActionTypeDelete;
                case com.dynamsoft.scm.client.sc.common.framework.EnumActionType.enumEdit:
                    return EnumActionType.Enum_ActionTypeCheckin;
                case com.dynamsoft.scm.client.sc.common.framework.EnumActionType.enumLabel:
                    return EnumActionType.Enum_ActionTypeLabel;
                case com.dynamsoft.scm.client.sc.common.framework.EnumActionType.enumMerge:
                    return EnumActionType.Enum_ActionTypeMerge;
                case com.dynamsoft.scm.client.sc.common.framework.EnumActionType.enumNoAction:
                    return EnumActionType.Enum_ActionTypeNull;
                case com.dynamsoft.scm.client.sc.common.framework.EnumActionType.enumPurge:
                    return EnumActionType.Enum_ActionTypePurge;
                case com.dynamsoft.scm.client.sc.common.framework.EnumActionType.enumRecover:
                    return EnumActionType.Enum_ActionTypeRecover;
                case com.dynamsoft.scm.client.sc.common.framework.EnumActionType.enumRename:
                    return EnumActionType.Enum_ActionTypeRename;
                default:
                    return EnumActionType.Enum_ActionTypeNull;
            }
        }

    }
}
