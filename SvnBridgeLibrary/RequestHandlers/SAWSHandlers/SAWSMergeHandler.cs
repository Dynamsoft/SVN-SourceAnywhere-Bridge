﻿using System.IO;
using System.Text;
using CodePlex.TfsLibrary.RepositoryWebSvc;
using SvnBridge.Exceptions;
using SvnBridge.Interfaces;
using SvnBridge.Net;
using SvnBridge.Protocol;
using SvnBridge.SourceControl;
using SvnBridge.Utility;
using SvnBridge.SourceControl.Dto;

namespace SvnBridge.RequestHandlers.SAWSHandlers
{
    class SAWSMergeHandler : SAWSHandlerBase
    {
        protected override void Handle(IHttpContext context,
                                   TFSSourceControlProvider sourceControlProvider)
        {
            IHttpRequest request = context.Request;
            IHttpResponse response = context.Response;

            MergeData data = Helper.DeserializeXml<MergeData>(request.InputStream);
            string activityId = PathParser.GetActivityId(data.Source.Href);
            response.AppendHeader("Cache-Control", "no-cache");

            try
            {
                // MergeActivityResponse mergeResponse = sourceControlProvider.MergeActivity(activityId);
                int version = 0;
                System.DateTime date = System.DateTime.Now;
                
                ActivityRepository.Use(activityId, delegate(Activity activity)
                {
                    if (activity.MergeList.Count != 0)
                    {
                        for(int i = 0; i < activity.MergeList.Count; i++)
                        {
                            string path = activity.MergeList[i].Path;
                            if (path.StartsWith("//!svn/wrk"))
                            {
                                path = Helper.Decode(path.Substring(11 + activityId.Length));
                            }
                            int ver = GetSDKObject().GetLastestVersionNum(path);
                            System.DateTime d;
                            version = ver > version ? ver : version;
                            GetSDKObject().GetFileModifiedDate(path, out d);
                            date = d > date ? d : date;
                        }
                    }
                });

                MergeActivityResponse mergeResponse = new MergeActivityResponse(version, date, this.UserInfo.strUserName);
                ActivityRepository.Use(activityId, delegate(Activity activity)
                {
                    foreach(ActivityItem t in activity.MergeList)
                    {
                        string path = t.Path;
                        if (path.StartsWith("//!svn/wrk"))
                        {
                            path = Helper.Decode(path.Substring(11 + activityId.Length));
                        }
                        MergeActivityResponseItem item = new MergeActivityResponseItem(t.FileType, path);
                        mergeResponse.Items.Add(item);
                    }
                });

                SetResponseSettings(response, "text/xml", Encoding.UTF8, 200);
                response.SendChunked = true;
                using (StreamWriter output = new StreamWriter(response.OutputStream))
                {
                    WriteMergeResponse(request, mergeResponse, output);
                }
            }
            catch (ConflictException ex)
            {
                SetResponseSettings(response, "text/xml; charset=\"utf-8\"", Encoding.UTF8, 409);
                string responseContent =
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                    "<D:error xmlns:D=\"DAV:\" xmlns:m=\"http://apache.org/dav/xmlns\" xmlns:C=\"svn:\">\n" +
                    "<C:error/>\n" +
                    "<m:human-readable errcode=\"160024\">\n" +
                    ex.Message + "\n" +
                    "</m:human-readable>\n" +
                    "</D:error>\n";
                WriteToResponse(response, responseContent);
            }
        }

        private void WriteMergeResponse(IHttpRequest request, MergeActivityResponse mergeResponse,
                                        TextWriter output)
        {
            output.Write("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
            output.Write("<D:merge-response xmlns:D=\"DAV:\">\n");
            output.Write("<D:updated-set>\n");
            output.Write("<D:response>\n");
            output.Write("<D:href>" + VccPath + "</D:href>\n");
            output.Write("<D:propstat><D:prop>\n");
            output.Write("<D:resourcetype><D:baseline/></D:resourcetype>\n");
            output.Write("\n");
            output.Write("<D:version-name>" + mergeResponse.Version.ToString() + "</D:version-name>\n");
            output.Write("<D:creationdate>" + Helper.FormatDate(mergeResponse.CreationDate) + "</D:creationdate>\n");
            output.Write("<D:creator-displayname>" + mergeResponse.Creator + "</D:creator-displayname>\n");
            output.Write("</D:prop>\n");
            output.Write("<D:status>HTTP/1.1 200 OK</D:status>\n");
            output.Write("</D:propstat>\n");
            output.Write("</D:response>\n");

            foreach (MergeActivityResponseItem item in mergeResponse.Items)
            {
                output.Write("<D:response>\n");
                output.Write("<D:href>" + PathParser.ToApplicationPath(request, Helper.Encode(item.Path, true)) + "</D:href>\n");
                output.Write("<D:propstat><D:prop>\n");
                if (item.Type == ItemType.Folder)
                {
                    output.Write("<D:resourcetype><D:collection/></D:resourcetype>\n");
                }
                else
                {
                    output.Write("<D:resourcetype/>\n");
                }

                output.Write("<D:checked-in><D:href>");

                output.Write(GetLocalPath("/!svn/ver/" +
                             mergeResponse.Version +
                             Helper.Encode(item.Path, true)));

                output.Write("</D:href></D:checked-in>\n");

                output.Write("</D:prop>\n");
                output.Write("<D:status>HTTP/1.1 200 OK</D:status>\n");
                output.Write("</D:propstat>\n");
                output.Write("</D:response>\n");
            }
            output.Write("</D:updated-set>\n");
            output.Write("</D:merge-response>\n");
        }
    }
}
