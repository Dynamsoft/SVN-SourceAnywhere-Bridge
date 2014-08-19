using System.IO;
using System.Text;
using CodePlex.TfsLibrary.RepositoryWebSvc;
using SvnBridge.Exceptions;
using SvnBridge.Interfaces;
using SvnBridge.Net;
using SvnBridge.Protocol;
using SvnBridge.SourceControl;
using SvnBridge.Utility;
using SvnBridge.SourceControl.Dto;
using System;

namespace SvnBridge.RequestHandlers.SAWSHandlers
{
    class SAWSLockHandler : SAWSHandlerBase
    {
        protected override void Handle(IHttpContext context, TFSSourceControlProvider sourceControlProvider)
        {
            IHttpRequest request = context.Request;
            IHttpResponse response = context.Response;

            SetResponseSettings(response, "text/xml; charset=\"utf-8\"", Encoding.UTF8, 200);
                string path = GetPath(request);
                GetSDKObject().Lock(path);
                Guid token = Guid.NewGuid();
           response.AppendHeader("X-SVN-Creation-Date", System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.000000Z"));
           response.AppendHeader("X-SVN-Lock-Owner", UserInfo.strUserName );
           string strToken = "<opaquelocktoken:" + token + ">";
           response.AppendHeader("Lock-Token", strToken);
           
                string responseContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + 
"<D:prop xmlns:D=\"DAV:\">\n" +
"<D:lockdiscovery>\n"+
"<D:activelock>\n" +
"<D:locktype><D:write/></D:locktype>\n" +
"<D:lockscope><D:exclusive/></D:lockscope>\n" +
"<D:depth>0</D:depth>\n" +
"<ns0:owner xmlns:ns0=\"DAV:\"/><D:timeout>Infinite</D:timeout>\n" +
"<D:locktoken>\n" +
"<D:href>opaquelocktoken:" + token + "</D:href>\n" +
"</D:locktoken>\n" +
"</D:activelock>\n" +
"</D:lockdiscovery>\n" +
"</D:prop>";
             
            /*
            string responseContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
"<D:prop xmlns:D=\"DAV:\">" +
"<D:lockdiscovery>" +
"<D:activelock>" +
"<D:locktype><D:write/></D:locktype>" +
"<D:lockscope><D:exclusive/></D:lockscope>" +
"<D:depth>0</D:depth>" +
"<ns0:owner xmlns:ns0=\"DAV:\"/><D:timeout>Infinite</D:timeout>" +
"<D:locktoken>" +
"<D:href>opaquelocktoken:fc8422b9-b191-4340-834e-ef40046aaa32</D:href>" +
"</D:locktoken>" +
"</D:activelock>" +
"</D:lockdiscovery>" +
"</D:prop>4b-bfe3-7bb243e65697</lp2:repository-uuid>" +
"</D:prop>" +
"<D:status>HTTP/1.1 200 OK</D:status>" +
"</D:propstat>" +
"</D:response>" +
"</D:multistatus>" +
"tivity-collection-set></D:options-response>";
             */
                WriteToResponse(response, responseContent);

        }
    }
}
