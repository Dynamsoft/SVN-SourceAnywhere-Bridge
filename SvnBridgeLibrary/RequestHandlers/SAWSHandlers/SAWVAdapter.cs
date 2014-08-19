using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SvnBridge.RequestHandlers
{
    public class SAWVAdapter: SAWAdapter
    {
        #region SAWAdapter Members

        public bool Connect(string strServerIP, int nServerPort)
        {
            throw new NotImplementedException();
        }

        public bool Login(string strName, string strPassword, string rep)
        {
            throw new NotImplementedException();
        }

        public string GetCurRepository()
        {
            throw new NotImplementedException();
        }

        public void SetRepository(string rep)
        {
            throw new NotImplementedException();
        }

        public bool DeleteFile(string strRemotePath)
        {
            throw new NotImplementedException();
        }

        public bool AddFile(string strLocalPath, string strRemotePath, string Comment)
        {
            throw new NotImplementedException();
        }

        public string GetFileComment(string path)
        {
            throw new NotImplementedException();
        }

        public List<SAWItemInfo> GetFileHistory(string path)
        {
            throw new NotImplementedException();
        }

        public long GetFileSize(string path)
        {
            throw new NotImplementedException();
        }

        public bool CheckInProject(string strLocalPath, string strRemotePath, string Comment)
        {
            throw new NotImplementedException();
        }

        public bool CheckOutFiles(string strLocalPath, string strRemotePath)
        {
            throw new NotImplementedException();
        }

        public bool CheckOutProject(string strLocalPath, string strRemotePath, string Comment)
        {
            throw new NotImplementedException();
        }

        public bool CheckInFiles(string strLocalPath, string strRemotePath, string Comment)
        {
            throw new NotImplementedException();
        }

        public bool GetFileModifiedDate(string path, out DateTime date)
        {
            throw new NotImplementedException();
        }

        public SAWItemInfo GetFileInfo(string file, int ver)
        {
            throw new NotImplementedException();
        }

        public bool GetDateFromVersion(string path, int version, out DateTime date)
        {
            throw new NotImplementedException();
        }

        public int GetVersionFromDate(string path, DateTime date)
        {
            throw new NotImplementedException();
        }

        public bool FileExists(string strRemotePath, int ver)
        {
            throw new NotImplementedException();
        }

        public int GetLastestVersionNum(string strPath)
        {
            throw new NotImplementedException();
        }

        public List<SAWItemInfo> EnumItems(string strFolder, int ver)
        {
            throw new NotImplementedException();
        }

        public bool GetFile(int version, string strLocalPath, string strRemotePath)
        {
            throw new NotImplementedException();
        }

        public bool Lock(string strFile)
        {
            throw new NotImplementedException();
        }

        public bool Unlock(string strFile)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
