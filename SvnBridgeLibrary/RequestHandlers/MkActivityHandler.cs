using System.Text;
using SvnBridge.Interfaces;
using SvnBridge.Net;
using SvnBridge.SourceControl;

namespace SvnBridge.Handlers
{
    public class MkActivityHandler : RequestHandlerBase
    {
        protected override void Handle(IHttpContext context,
                                       TFSSourceControlProvider sourceControlProvider)
        {
            IHttpRequest request = context.Request;
            IHttpResponse response = context.Response;

            string path = GetPath(request);
            string activityId = path.Substring(10);
            sourceControlProvider.MakeActivity(activityId);

            SetResponseSettings(response, "text/html", Encoding.UTF8, 201);

            response.AppendHeader("Cache-Control", "no-cache");
            response.AppendHeader("Location", "http://" + request.Headers["Host"] + path);
            response.AppendHeader("X-Pad", "avoid browser bug");

            string responseContent = "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\n" +
                                     "<html><head>\n" +
                                     "<title>201 Created</title>\n" +
                                     "</head><body>\n" +
                                     "<h1>Created</h1>\n" +
                                     "<p>Activity " + path + " has been created.</p>\n" +
                                     "<hr />\n" +
                                     "<address>Apache at " + request.Url.Host +
                                     " Port " + request.Url.Port + "</address>\n" +
                                     "</body></html>\n";

            WriteToResponse(response, responseContent);
        }
    }
}