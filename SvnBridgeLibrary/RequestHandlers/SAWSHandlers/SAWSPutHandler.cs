using System;
using System.IO;
using System.Text;
using SvnBridge.Interfaces;
using SvnBridge.Net;
using SvnBridge.SourceControl;
using SvnBridge.Utility;
using SvnBridge.SourceControl.Dto;
using SvnBridge.Exceptions;
using SvnBridge.Infrastructure;

namespace SvnBridge.RequestHandlers.SAWSHandlers
{
    class SAWSPutHandler : SAWSHandlerBase
    {
        protected override void Handle(IHttpContext context, TFSSourceControlProvider sourceControlProvider)
        {
            IHttpRequest request = context.Request;
            IHttpResponse response = context.Response;

            try
            {
                string path = GetPath(request);
                bool created = Put(sourceControlProvider, path, request.InputStream, request.Headers["X-SVN-Base-Fulltext-MD5"], request.Headers["X-SVN-Result-Fulltext-MD5"]);

                if (created)
                {
                    SetResponseSettings(response, "text/html", Encoding.UTF8, 201);

                    response.AppendHeader("Location", "http://" + request.Headers["Host"] + "/" + Helper.Decode(path));

                    string responseContent = "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\n" +
                                             "<html><head>\n" +
                                             "<title>201 Created</title>\n" +
                                             "</head><body>\n" +
                                             "<h1>Created</h1>\n" +
                                             "<p>Resource /" + Helper.EncodeB(Helper.Decode(path)) +
                                             " has been created.</p>\n" +
                                             "<hr />\n" +
                                             "<address>Apache at" + request.Url.Host +
                                             " Port " + request.Url.Port + "</address>\n" +
                                             "</body></html>\n";

                    WriteToResponse(response, responseContent);
                }
                else
                {
                    SetResponseSettings(response, "text/plain", Encoding.UTF8, 204);
                }
            }
            catch (ConflictException ex)
            {
                RequestCache.Items["RequestBody"] = null;
                DefaultLogger logger = Container.Resolve<DefaultLogger>();
                logger.ErrorFullDetails(ex, context);

                SetResponseSettings(response, "text/xml; charset=\"utf-8\"", Encoding.UTF8, 409);
                string responseContent =
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                    "<D:error xmlns:D=\"DAV:\" xmlns:m=\"http://apache.org/dav/xmlns\" xmlns:C=\"svn:\">\n" +
                    "<C:error/>\n" +
                    "<m:human-readable errcode=\"160024\">\n" +
                    "The version resource does not correspond to the resource within the transaction.  Either the requested version resource is out of date (needs to be updated), or the requested version resource is newer than the transaction root (restart the commit).\n" +
                    "</m:human-readable>\n" +
                    "</D:error>\n";
                WriteToResponse(response, responseContent);
            }
        }

        private bool Put(TFSSourceControlProvider sourceControlProvider, string path, Stream inputStream, string baseHash, string resultHash)
        {
            if (!path.StartsWith("//"))
            {
                path = "/" + path;
            }

            string activityId = path.Substring(11, path.IndexOf('/', 11) - 11);
            string serverPath = Helper.Decode(path.Substring(11 + activityId.Length));
            byte[] sourceData = ReadFile(serverPath, GetSDKObject().GetLastestVersionNum(serverPath));
            /*
            if (baseHash != null)
            {
                // TODONEXT:
                // ItemMetaData item = sourceControlProvider.GetItemInActivity(activityId, serverPath);
                ItemMetaData item = new ItemMetaData();
                item.Name = serverPath;
                GetData(item);
                sourceData = null;
             //   sourceData = sourceControlProvider.ReadFile(item);
                if (ChecksumMismatch(baseHash, sourceData))
                {
                    throw new Exception("Checksum mismatch with base file");
                }
            }*/

            if (baseHash != null && sourceData != null && ChecksumMismatch(baseHash, sourceData))
            {
                throw new ConflictException();
            //    throw new Exception("Checksum mismatch with base file, please save your work, recheckout the project, apply changes, and try to commit again.");
            }

            byte[] fileData = SvnDiffParser.ApplySvnDiffsFromStream(inputStream, sourceData);
            string data = string.Format("{0}", fileData);
            if (fileData.Length > 0)
            {
				if (resultHash != null && fileData != null && ChecksumMismatch(resultHash, fileData))
                {
                    throw new ConflictException();
                //    throw new Exception("Checksum mismatch with new file, , please save your work, recheckout the project, apply changes, and try to commit again.");
                }
            }


            bool bRet = true;
            ActivityRepository.Use(activityId, delegate(Activity activity)
            {
                activity.MergeList.Add(new ActivityItem(serverPath, CodePlex.TfsLibrary.RepositoryWebSvc.ItemType.File, ActivityItemAction.Updated));
            
                bRet = WriteData(serverPath, fileData, activity.Comment);
            });

            return bRet;
        }

    	private static bool ChecksumMismatch(string hash, byte[] data)
    	{
			// git will not pass the relevant checksum, so we need to ignore 
			// this
			if(hash==null)
				return false;
    		return Helper.GetMd5Checksum(data) != hash;
    	}
    }
}
