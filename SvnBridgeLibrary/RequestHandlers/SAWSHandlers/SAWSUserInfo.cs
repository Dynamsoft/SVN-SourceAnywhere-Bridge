using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SvnBridge.RequestHandlers;

namespace SvnBridge.RequestHandlers.SAWSHandlers
{
    public class SAWSUserInfo
    {
        public string strUserName;
        public string strPassword;
        public string strServer;
        public int nPort;
        public string strRepository;
        public SAWAdapter theSDKObject;
        public SAWSUserInfo(string strServerIP, int nPort, string strUserName, string strPassword, string Rep)
        {
            this.strUserName = strUserName;
            this.strPassword = strPassword;
            this.strServer = strServerIP;
            this.nPort = nPort;
            this.strRepository = Rep;

            // If username and password do not match, throw an exception to indicate it.
#if SC_SAWS
            theSDKObject = new SAWSAdapter();
            if (!theSDKObject.Connect(strServer, nPort) || !theSDKObject.Login(strUserName, strPassword, Rep))
                throw new CodePlex.TfsLibrary.NetworkAccessDeniedException();
#endif

#if SC_SAWH
            theSDKObject = new SAWHAdapter();
            if (!theSDKObject.Connect(strServer, nPort) || !theSDKObject.Login(strUserName, strPassword, Rep))
                throw new CodePlex.TfsLibrary.NetworkAccessDeniedException();
#endif

#if SC_SAWV
            theSDKObject = new SAWVAdapter();
            if (!theSDKObject.Connect(strServer, nPort) || !theSDKObject.Login(strUserName, strPassword, Rep))
                throw new CodePlex.TfsLibrary.NetworkAccessDeniedException();
#endif

#if SC_SCMS
            theSDKObject = new SCMSAdapter();
            if (!theSDKObject.Connect(strServer, nPort) || !theSDKObject.Login(strUserName, strPassword, Rep))
                throw new CodePlex.TfsLibrary.NetworkAccessDeniedException();
#endif
        }
    }
}
