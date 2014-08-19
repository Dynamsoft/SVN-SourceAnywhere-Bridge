#define __USE_Dynamsoft__
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using CodePlex.TfsLibrary;
using SvnBridge.Handlers;
using SvnBridge.Handlers.Renderers;
using SvnBridge.Infrastructure;
using SvnBridge.Infrastructure.Statistics;
using SvnBridge.Interfaces;
using SvnBridge.SourceControl;
using System.Web;
using SvnBridge.RequestHandlers.SAWSHandlers;


namespace SvnBridge.Net
{
    public class HttpContextDispatcher
    {
        protected readonly IPathParser parser;
        protected readonly ActionTrackingViaPerfCounter actionTracking;

        public HttpContextDispatcher(IPathParser parser, ActionTrackingViaPerfCounter actionTracking)
        {
            this.parser = parser;
            this.actionTracking = actionTracking;
        }

        public virtual RequestHandlerBase GetHandler(string httpMethod)
        {
            switch (httpMethod.ToLowerInvariant())
            {
                    
#if __USE_Dynamsoft__
                case "lock":
                    return new SAWSLockHandler();
                case "unlock":
                    return new SAWSUnlockHandler();
#endif
                case "checkout":
#if __USE_Dynamsoft__
                    return new SAWSCheckOutHandler();
#else
                    return new CheckOutHandler();
#endif
                case "copy":
#if __USE_Dynamsoft__
                    return new SAWSCopyHandler();
#else
                    return new CopyHandler();
#endif
                case "delete":
#if __USE_Dynamsoft__
                    return new SAWSDeleteHandler();
#else
                    return new DeleteHandler();
#endif
                case "merge":
#if __USE_Dynamsoft__
                    return new SAWSMergeHandler();
#else
                    return new MergeHandler();
#endif
                case "mkactivity":
#if __USE_Dynamsoft__
                    return new SAWSMkActivityHandler();
#else
                    return new MkActivityHandler();
#endif
                case "mkcol":
#if __USE_Dynamsoft__
                    return new SAWSMkColHandler();
#else
                    return new MkColHandler();
#endif
                case "options":
#if __USE_Dynamsoft__
                    return new SAWSOptionsHandler();
#else
                    return new OptionsHandler();
#endif
                case "propfind":
#if __USE_Dynamsoft__
                    return new SAWSPropFindHandler();
#else
                    return new PropFindHandler();
#endif
                case "proppatch":
#if __USE_Dynamsoft__
                    return new SAWSPropPatchHandler();
#else
                    return new PropPatchHandler();
#endif
                case "put":
#if __USE_Dynamsoft__
                    return new SAWSPutHandler();
#else
                    return new PutHandler();
#endif
                case "report":
#if __USE_Dynamsoft__
                    return new SAWSReportHandler();
#else
                    return new ReportHandler();
#endif
                case "get":
#if __USE_Dynamsoft__
                    return new SAWSGetHandler();
#else
                    return new GetHandler();
#endif
                default:
                    return null;
            }
        }

        public void Dispatch(IHttpContext connection)
        {
            RequestHandlerBase handler = null;
            try
            {
                IHttpRequest request = connection.Request;
                if ("/!stats/request".Equals(request.LocalPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    new StatsRenderer(Container.Resolve<ActionTrackingViaPerfCounter>()).Render(connection);
                    return;
                }

                NetworkCredential credential = GetCredential(connection);
                string tfsUrl = parser.GetServerUrl(request, credential);
                if (string.IsNullOrEmpty(tfsUrl))
                {
                    SendFileNotFoundResponse(connection);
                    return;
                }

                if (credential != null && (tfsUrl.ToLowerInvariant().EndsWith("codeplex.com") || tfsUrl.ToLowerInvariant().Contains("tfs.codeplex.com")))
                {
                    string username = credential.UserName;
                    string domain = credential.Domain;
                    if (!username.ToLowerInvariant().EndsWith("_cp"))
                    {
                        username += "_cp";
                    }
                    if (domain == "")
                    {
                        domain = "snd";
                    }
                    credential = new NetworkCredential(username, credential.Password, domain);
                }
                RequestCache.Items["serverUrl"] = tfsUrl;
                RequestCache.Items["projectName"] = parser.GetProjectName(request);
                RequestCache.Items["credentials"] = credential;

                handler = GetHandler(connection.Request.HttpMethod);
                if (handler == null)
                {
                    actionTracking.Error();
                    SendUnsupportedMethodResponse(connection);
                    return;
                }

                try
                {
                    actionTracking.Request(handler);
                    handler.Handle(connection, parser, credential);
                }
                catch (TargetInvocationException e)
                {
                    ExceptionHelper.PreserveStackTrace(e.InnerException);
                    throw e.InnerException;
                }
            }
            catch (WebException ex)
            {
                actionTracking.Error();

                HttpWebResponse response = ex.Response as HttpWebResponse;

                if (response != null && response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    SendUnauthorizedResponse(connection);
                }
                else
                {
                    throw;
                }
            }
            catch (NetworkAccessDeniedException)
            {
                SendUnauthorizedResponse(connection);
            }
            catch (IOException)
            {
                // Error caused by client cancelling operation under IIS 6
                if (Configuration.LogCancelErrors)
                    throw;
            }
            catch (HttpException ex)
            {
                // Check if error caused by client cancelling operation under IIS 7
                if (!ex.Message.StartsWith("An error occurred while communicating with the remote host.") &&
                    !ex.Message.StartsWith("The remote host closed the connection."))
                    throw;

                if (Configuration.LogCancelErrors)
                    throw;
            }
            finally
            {
                if (handler != null)
                    handler.Cancel();
            }
        }

        private static NetworkCredential GetCredential(IHttpContext context)
        {
            string authorizationHeader = context.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                if (authorizationHeader.StartsWith("Digest"))
                {
                    return (NetworkCredential)CredentialCache.DefaultCredentials;
                }
                else if (authorizationHeader.StartsWith("Basic"))
                {
                    string encodedCredential = authorizationHeader.Substring(authorizationHeader.IndexOf(' ') + 1);
                    string credential = UTF8Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredential));
                    string[] credentialParts = credential.Split(':');

                    string username = credentialParts[0];
                    string password = credentialParts[1];

                    if (username.IndexOf('\\') >= 0)
                    {
                        string domain = username.Substring(0, username.IndexOf('\\'));
                        username = username.Substring(username.IndexOf('\\') + 1);
                        return new NetworkCredential(username, password, domain);
                    }
                    return new NetworkCredential(username, password);
                }
                else
                {
                    throw new Exception("Unrecognized authorization header: " + authorizationHeader.StartsWith("Basic"));
                }
            }
            return CredentialsHelper.NullCredentials;
        }


        private static void SendUnauthorizedResponse(IHttpContext connection)
        {
            IHttpRequest request = connection.Request;
            IHttpResponse response = connection.Response;

            response.ClearHeaders();

            response.StatusCode = (int)HttpStatusCode.Unauthorized;
            response.ContentType = "text/html; charset=iso-8859-1";

            response.AppendHeader("WWW-Authenticate", "Basic realm=\"Subversion Repository\"");

            string content = "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\n" +
                             "<html><head>\n" +
                             "<title>401 Authorization Required</title>\n" +
                             "</head><body>\n" +
                             "<h1>Authorization Required</h1>\n" +
                             "<p>This server could not verify that you\n" +
                             "are authorized to access the document\n" +
                             "requested.  Either you supplied the wrong\n" +
                             "credentials (e.g., bad password), or your\n" +
                             "browser doesn't understand how to supply\n" +
                             "the credentials required.</p>\n" +
                             "<hr>\n" +
                             "<address>Apache" + request.Url.Host + " Port " +
                             request.Url.Port +
                             "</address>\n" +
                             "</body></html>\n";

            byte[] buffer = Encoding.UTF8.GetBytes(content);

            response.OutputStream.Write(buffer, 0, buffer.Length);
        }

        private static void SendUnsupportedMethodResponse(IHttpContext connection)
        {
            IHttpResponse response = connection.Response;
            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            response.ContentType = "text/html";

            response.AppendHeader("Allow", "PROPFIND, REPORT, OPTIONS, MKACTIVITY, CHECKOUT, PROPPATCH, PUT, MERGE, DELETE, MKCOL");

            string content = @"
                <html>
                    <head>
                        <title>405 Method Not Allowed</title>
                    </head>
                    <body>
                        <h1>The requested method is not supported.</h1>
                    </body>
                </html>";

            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }

        protected void SendFileNotFoundResponse(IHttpContext connection)
        {
            IHttpResponse response = connection.Response;
            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.ContentType = "text/html; charset=iso-8859-1";

            string content =
                "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\n" +
                "<html><head>\n" +
                "<title>404 Not Found</title>\n" +
                "</head><body>\n" +
                "<h1>Not Found</h1>\n" +
                "<hr>\n" +
                "</body></html>\n";

            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
    }
}
