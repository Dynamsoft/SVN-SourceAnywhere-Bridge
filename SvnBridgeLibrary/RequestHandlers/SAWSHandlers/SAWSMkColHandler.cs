using System.Text;
using System.Text.RegularExpressions;
using SvnBridge.Exceptions;
using SvnBridge.Interfaces;
using SvnBridge.Net;
using SvnBridge.Utility;
using SvnBridge.SourceControl;
using SvnBridge.SourceControl.Dto;
using CodePlex.TfsLibrary.RepositoryWebSvc;

namespace SvnBridge.RequestHandlers.SAWSHandlers
{
    public class SAWSMkColHandler : SAWSHandlerBase
    {
        protected override void Handle(IHttpContext context, TFSSourceControlProvider sourceControlProvider)
        {
            IHttpRequest request = context.Request;
            IHttpResponse response = context.Response;

            string path = Helper.Decode(GetPath(request));

            try
            {
                MakeCollection(path, sourceControlProvider);

                SendCreatedResponse(request, response, path, request.Url.Host, request.Url.Port.ToString());
            }
            catch (FolderAlreadyExistsException)
            {
                SendFailureResponse(response, path, request.Url.Host, request.Url.Port.ToString());
            }
        }

        private static void MakeCollection(string path, TFSSourceControlProvider sourceControlProvider)
        {
            if (!path.StartsWith("//"))
            {
                path = "/" + path;
            }

            Match match = Regex.Match(path, @"//!svn/wrk/([a-zA-Z0-9\-]+)/?");
            string folderPath = path.Substring(match.Groups[0].Value.Length - 1);
            string activityId = match.Groups[1].Value;
            ActivityRepository.Use(activityId, delegate(Activity activity)
            {
                activity.MergeList.Add(
                    new ActivityItem(path, ItemType.Folder, ActivityItemAction.New));
                activity.Collections.Add(path);
            });
        }

        private static void SendCreatedResponse(IHttpRequest request, IHttpResponse response, string path, string server, string port)
        {
            SetResponseSettings(response, "text/html", Encoding.UTF8, 201);

            response.AppendHeader("Location", "http://" + request.Headers["Host"] + "/" + path);

            string responseContent = "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\n" +
                                     "<html><head>\n" +
                                     "<title>201 Created</title>\n" +
                                     "</head><body>\n" +
                                     "<h1>Created</h1>\n" +
                                     "<p>Collection /" + Helper.EncodeB(path) + " has been created.</p>\n" +
                                     "<hr />\n" +
                                     "<address>Apache at " + server + " Port " +
                                     port + "</address>\n" +
                                     "</body></html>\n";

            WriteToResponse(response, responseContent);
        }

        private static void SendFailureResponse(IHttpResponse response, string path, string server, string port)
        {
            SetResponseSettings(response, "text/html; charset=iso-8859-1", Encoding.UTF8, 405);

            response.AppendHeader("Allow", "TRACE");

            string responseContent =
                "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\n" +
                "<html><head>\n" +
                "<title>405 Method Not Allowed</title>\n" +
                "</head><body>\n" +
                "<h1>Method Not Allowed</h1>\n" +
                "<p>The requested method MKCOL is not allowed for the URL /" + path + ".</p>\n" +
                "<hr>\n" +
                "<address>Apache at " + server + " Port " + port + "</address>\n" +
                "</body></html>\n";

            WriteToResponse(response, responseContent);
        }
    }
}