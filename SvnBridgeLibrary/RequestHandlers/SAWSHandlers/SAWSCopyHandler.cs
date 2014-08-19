﻿using System.Text;
using SvnBridge.Interfaces;
using SvnBridge.Net;
using SvnBridge.Utility;
using SvnBridge.SourceControl;
using SvnBridge.RequestHandlers.SAWSHandlers;

namespace SvnBridge.Handlers
{
    public class SAWSCopyHandler : SAWSHandlerBase
    {
        protected override void Handle(IHttpContext context, TFSSourceControlProvider sourceControlProvider)
        {
            IHttpRequest request = context.Request;
            IHttpResponse response = context.Response;

            SetResponseSettings(response, "text/html", Encoding.UTF8, 201);

            string activityId = PathParser.GetActivityIdFromDestination(request.Headers["Destination"]);

            string path = GetPath(request);
            path = path.Substring(path.IndexOf('/', 9));

            string destination = PathParser.GetPathFromDestination(Helper.DecodeC(request.Headers["Destination"]));
            string targetPath = destination.Substring(destination.IndexOf('/', 12));
            // SAWS not supported.
            // sourceControlProvider.CopyItem(activityId, path, targetPath);

            response.AppendHeader("Location", Helper.DecodeC(request.Headers["Destination"]));

            string responseContent =
                "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\n" +
                "<html><head>\n" +
                "<title>201 Created</title>\n" +
                "</head><body>\n" +
                "<h1>Created</h1>\n" +
                "<p>Destination /" + Helper.EncodeB(destination) + " has been created.</p>\n" +
                "<hr />\n" +
                "<address>Apache at " + request.Url.Host + " Port " +
                request.Url.Port + "</address>\n" +
                "</body></html>\n";

            WriteToResponse(response, responseContent);
        }
    }
}