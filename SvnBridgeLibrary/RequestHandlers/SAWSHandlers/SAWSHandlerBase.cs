using System;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using SvnBridge.Interfaces;
using SvnBridge.Net;
using SvnBridge.SourceControl;
using SvnBridge.Utility;
using SvnBridge.Infrastructure;
using System.Collections;
using SvnBridge.Cache;
using System.Collections.Generic;

namespace SvnBridge.RequestHandlers.SAWSHandlers
{
    using SvnBridge.Handlers;
    public abstract class SAWSHandlerBase : RequestHandlerBase
    {
        /// <summary>
        /// Here is some variables to record licenses.
        /// </summary>
        private static List<SAWSUserInfo> verifiedLicense = new List<SAWSUserInfo>();
        private static ReaderWriterLockSlim userInfoLock = new ReaderWriterLockSlim();
        public SAWSUserInfo UserInfo;

        /// <summary>
        /// In this function, we check if username and password are correct.
        /// </summary>
        /// <param name="context"> The HTTP Context</param>
        /// <param name="pathParser">Path Parser</param>
        /// <param name="credentials">Net Credential</param>
        public override void Handle(IHttpContext context, IPathParser pathParser, NetworkCredential credentials)
        {
            // We establish a list to record usernames and passwords. If the new credential is in this list,
            // we can returns true because it has been verified.
            // Otherwise we call server to check it.

            this.credentials = credentials;
            Initialize(context, pathParser);
            /*
            string strPath = context.Request.LocalPath;
            string[] strs = strPath.Split('/', ':', '/');
            string strServerIP = strs[1];
            int nPort = int.Parse(strs[2]);
             */

            if (credentials == null) throw new CodePlex.TfsLibrary.NetworkAccessDeniedException();
            bool bFind = false;

            string rep = credentials.Domain;
            if (rep.Length == 0)
                rep = SAWCommon.RepositoryName;
            
            userInfoLock.EnterReadLock();
            try
            {
                foreach (SAWSUserInfo info in verifiedLicense)
                {
                    if (info.strUserName == credentials.UserName && info.strPassword == credentials.Password &&
                        string.Compare(info.strRepository, rep, true) == 0)
                    {
                        bFind = true;
                        UserInfo = info;
                        break;
                    }
                }
            }
            finally
            {
                userInfoLock.ExitReadLock();
            }

            if (!bFind)
            {// user must contain repository info, if not include, default as default
                // Not found, create a new object.
                UserInfo = new SAWSUserInfo(SAWCommon.SourceControlServer, SAWCommon.SourceControlPort, credentials.UserName, credentials.Password, rep);
                userInfoLock.EnterWriteLock();
                try
                {
                    verifiedLicense.Add(UserInfo);
                }
                finally
                {
                    userInfoLock.ExitWriteLock();
                }
            }

            Handle(context, null);
        }

        public string ParsePath(string str, out string rep)
        {
            rep = null;
            if (str.StartsWith("/!svn/")) return str;
            return str;/*
            string[] res = str.Split('/');
            string repository;
            if (str.StartsWith("/"))
            {
                if (res.Length >= 2)
                {
                    repository = res[1];
                }
                else
                {
                    repository = "";
                }
                //str = str.Substring(1 + repository.Length);
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
                //str = str.Substring(repository.Length);
            }

            rep = repository;
            return str;*/
        }
        
        protected override string GetPath(IHttpRequest request)
        {
            string repository;
            string str = pathParser.GetLocalPath(request);
            str = ParsePath(str, out repository);

            //GetSDKObject().SetRepository(repository);
            return str;
        }
        
        /// <summary>
        /// This Function returns a reference to SDKAdapter;
        /// </summary>
        /// <returns>The Adapter reference.</returns>
        public SAWAdapter GetSDKObject()
        {
            return UserInfo.theSDKObject;
        }

        public byte[] ReadFile(string name, int nVersion)
        {
            string temp;
            temp = string.Format("{0}{1}", SAWCommon.TempPath, Guid.NewGuid().ToString());
            if (File.Exists(temp))
            {
                File.SetAttributes(temp, FileAttributes.Normal);
                File.Delete(temp);
            }
            byte[] ret = null;
            if (GetSDKObject().GetFile(nVersion, temp, name))
            {
                File.SetAttributes(temp, FileAttributes.Normal);
                using (FileStream fs = new FileStream(temp, FileMode.Open))
                {
                    byte[] buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, (int)fs.Length);
                    ret = buffer;
                }
                File.SetAttributes(temp, FileAttributes.Normal);
                File.Delete(temp);
            }
            return ret;
        }

        protected bool GetData(ItemMetaData metadata)
        {
            if (metadata is FolderMetaData)
            {
                bool bRet = true;
                foreach (var x in ((FolderMetaData)metadata).Items)
                {
                    bRet = bRet && GetData(x);
                }
                return bRet;
            }
            else
            {
                byte[] buffer = ReadFile(metadata.Name, metadata.ItemRevision);
                if (buffer == null) return false;
                metadata.Base64DiffData = SvnDiffParser.GetBase64SvnDiffData(buffer);
                metadata.Md5Hash = Helper.GetMd5Checksum(buffer);
                metadata.DataLoaded = true;
                return true;
            }
        }

        protected ItemMetaData QueryItems(string path, int version, Recursion recursion)
        {
            version = version == -1 ? GetSDKObject().GetLastestVersionNum(path) : version;
            int max = GetSDKObject().GetLastestVersionNum(path);
            if (version > max) version = max;

            FolderMetaData folder = new FolderMetaData();
            folder.Name = path;
            folder.ItemRevision = version;
            folder.LastModifiedDate = System.DateTime.Now;
            
            var result = GetSDKObject().EnumItems(path, version);
            
            if(result == null) // may be a file?
            {
                ItemMetaData item = new ItemMetaData();
                item.Name = path;
                item.ItemRevision = version;
                item.LastModifiedDate = System.DateTime.Now;
                return item;
            }
	        else 
            {
                for (int i = 0; i < result.Count; i++)
                {
                    string name = result[i].name.Remove(0, 1);
                    //if (name != null && name[0] == '$') name = name.Substring(1, name.Length - 1);
                    //name = "/" + GetSDKObject().GetCurRepository() + name;
                    ItemMetaData item;
                    if (result[i].isdir && (recursion != Recursion.None))
                    {
                        if (recursion == Recursion.OneLevel)
                            item = QueryItems(name, result[i].version, Recursion.None);
                        else
                            item = QueryItems(name, result[i].version, recursion);
                    }
                    else
                    {
                        item = new ItemMetaData();
                    }
                    item.Name = name;
                    item.ItemRevision = result[i].version;
                    item.LastModifiedDate = result[i].date;
                    folder.Items.Add(item);
                }
            }

            return folder;
        }

        protected bool WriteData(string strServerPath, byte[] data, string Comment)
        {
            string temp;
            temp = string.Format("{0}{1}", SAWCommon.TempPath, Guid.NewGuid().ToString());
            if (File.Exists(temp))
            {
                File.SetAttributes(temp, FileAttributes.Normal);
                File.Delete(temp);
            }
            using(FileStream fs = new FileStream(temp, FileMode.CreateNew))
            {
                fs.Write(data, 0, data.Length);
            }
            bool bRet = GetSDKObject().AddFile(temp, strServerPath, Comment);
            File.SetAttributes(temp, FileAttributes.Normal);
            File.Delete(temp);

            return bRet;
        }

    }
}
