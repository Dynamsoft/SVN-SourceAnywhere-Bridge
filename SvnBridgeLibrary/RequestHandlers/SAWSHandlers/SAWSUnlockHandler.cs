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
    class SAWSUnlockHandler : SAWSHandlerBase 
    {
         protected override void Handle(IHttpContext context, TFSSourceControlProvider sourceControlProvider)
        {
            IHttpRequest request = context.Request;
            IHttpResponse response = context.Response;

            SetResponseSettings(response, "text/plain", Encoding.UTF8, 204);
                string path = GetPath(request);
                GetSDKObject().Unlock(path);
                Guid token = new Guid();
                string strToken = "<opaquelocktoken:" + token + ">";
                response.AppendHeader("Lock-Token", strToken);
                response.AppendHeader("X-SVN-Creation-Date", System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.000000Z"));
        }
    }
}
