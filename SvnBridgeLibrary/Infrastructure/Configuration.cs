﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.IO;
using System.Xml;

namespace SvnBridge.Infrastructure
{
    public static class Configuration
    {
        private enum ConfigSettings
        {
            CacheEnabled,
            CodePlexWorkItemUrl,
            DomainIncludesProjectName,
            LogCancelErrors,
            LogPath,
            PerfCountersAreMandatory,
            ProxyEncryptedPassword,
            ProxyUrl,
            ProxyPort,
            ProxyUseDefaultCredentials,
            ProxyUsername,
            TfsPort,
            TfsUrl,
            TfsProxyUrl,
            TraceEnabled,
            UseCodePlexServers,
            UseProxy,
            ReadAllUserDomain,
            ReadAllUserName,
            ReadAllUserPassword,
            CodePlexAnonUserDomain,
            CodePlexAnonUserName,
            CodePlexAnonUserPassword
        }
        private static readonly string userConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\SvnBridge\3.0");

        private static Dictionary<string, string> userConfig = new Dictionary<string, string>();

        static Configuration()
        {
            ReadUserConfig();
        }

        public static void Save()
        {
            Directory.CreateDirectory(userConfigFolder);

            XmlDocument xml = new XmlDocument();
            xml.AppendChild(xml.CreateElement("configuration"));
            foreach (KeyValuePair<string, string> setting in userConfig)
            {
                if (setting.Value != null)
                {
                    XmlElement element = xml.CreateElement("setting");
                    element.Attributes.Append(xml.CreateAttribute("name"));
                    element.Attributes.Append(xml.CreateAttribute("value"));
                    element.Attributes["name"].Value = setting.Key;
                    element.Attributes["value"].Value = setting.Value.ToString();
                    xml["configuration"].AppendChild(element);
                }
            }

            string config = xml.InnerXml.Replace("><", ">\r\n<");
            File.WriteAllText(Path.Combine(userConfigFolder, "user.config"), config);
        }

        public static bool CacheEnabled
        {
            get { return ReadConfig<bool>(ConfigSettings.CacheEnabled, false); }
        }

        public static string CodePlexWorkItemUrl
        {
            get { return ReadConfig<string>(ConfigSettings.CodePlexWorkItemUrl, null); }
        }

        public static string LogPath
        {
            get { return ReadConfig<string>(ConfigSettings.LogPath, null); }
        }

        public static bool LogCancelErrors
        {
            get { return ReadConfig<bool>(ConfigSettings.LogCancelErrors, false); }
        }

        public static bool PerfCountersMandatory
        {
            get { return ReadConfig<bool>(ConfigSettings.PerfCountersAreMandatory, false); }
        }

        public static int TfsPort
        {
            get { return ReadConfig<int>(ConfigSettings.TfsPort, 8080); }
            set { userConfig[ConfigSettings.TfsPort.ToString()] = value.ToString(); }
        }

        public static string TfsProxyUrl
        {
            get { return ReadConfig<string>(ConfigSettings.TfsProxyUrl, null); }
            set { userConfig[ConfigSettings.TfsProxyUrl.ToString()] = value; }
        }

        public static string TfsUrl
        {
            get { return ReadConfig<string>(ConfigSettings.TfsUrl, null); }
        }

        public static bool TraceEnabled
        {
            get { return ReadConfig<bool>(ConfigSettings.TraceEnabled, false); }
        }

        public static bool DomainIncludesProjectName
        {
            get { return ReadConfig<bool>(ConfigSettings.DomainIncludesProjectName, false); }
        }

        public static bool UseCodePlexServers
        {
            get { return ReadConfig<bool>(ConfigSettings.UseCodePlexServers, false); }
        }

        public static bool UseProxy
        {
            get { return ReadConfig<bool>(ConfigSettings.UseProxy, false); }
            set { userConfig[ConfigSettings.UseProxy.ToString()] = value.ToString(); }
        }

        public static string ProxyUrl
        {
            get { return ReadConfig<string>(ConfigSettings.ProxyUrl, ""); }
            set { userConfig[ConfigSettings.ProxyUrl.ToString()] = value.ToString(); }
        }

        public static int ProxyPort
        {
            get { return ReadConfig<int>(ConfigSettings.ProxyPort, 80); }
            set { userConfig[ConfigSettings.ProxyPort.ToString()] = value.ToString(); }
        }

        public static bool ProxyUseDefaultCredentials
        {
            get { return ReadConfig<bool>(ConfigSettings.ProxyUseDefaultCredentials, false); }
            set { userConfig[ConfigSettings.ProxyUseDefaultCredentials.ToString()] = value.ToString(); }
        }

        public static string ProxyUsername
        {
            get { return ReadConfig<string>(ConfigSettings.ProxyUsername, ""); }
            set { userConfig[ConfigSettings.ProxyUsername.ToString()] = value.ToString(); }
        }

        public static byte[] ProxyEncryptedPassword
        {
            get {
                string proxyEncryptedPassword = ReadConfig<string>(ConfigSettings.ProxyEncryptedPassword, null);
                if (proxyEncryptedPassword != null)
                    return Convert.FromBase64String(ReadConfig<string>(ConfigSettings.ProxyEncryptedPassword, null));
                else
                    return null;
            }
            set {
                if (value == null)
                    userConfig.Remove(ConfigSettings.ProxyEncryptedPassword.ToString());
                else
                    userConfig[ConfigSettings.ProxyEncryptedPassword.ToString()] = Convert.ToBase64String(value);
            }
        }

        public static string ReadAllUserDomain
        {
            get { return ReadConfig(ConfigSettings.ReadAllUserDomain, ""); }
            set { userConfig[ConfigSettings.ReadAllUserDomain.ToString()] = value; }
        }

        public static string ReadAllUserName
        {
            get { return ReadConfig(ConfigSettings.ReadAllUserName, ""); }
            set { userConfig[ConfigSettings.ReadAllUserName.ToString()] = value; }
        }

        public static string ReadAllUserPassword
        {
            get { return ReadConfig(ConfigSettings.ReadAllUserPassword, ""); }
            set { userConfig[ConfigSettings.ReadAllUserPassword.ToString()] = value; }
        }

        public static string CodePlexAnonUserDomain
        {
            get { return ReadConfig(ConfigSettings.CodePlexAnonUserDomain, ""); }
            set { userConfig[ConfigSettings.CodePlexAnonUserDomain.ToString()] = value; }
        }

        public static string CodePlexAnonUserName
        {
            get { return ReadConfig(ConfigSettings.CodePlexAnonUserName, ""); }
            set { userConfig[ConfigSettings.CodePlexAnonUserName.ToString()] = value; }
        }

        public static string CodePlexAnonUserPassword
        {
            get { return ReadConfig(ConfigSettings.CodePlexAnonUserPassword, ""); }
            set { userConfig[ConfigSettings.CodePlexAnonUserPassword.ToString()] = value; } 
        }

        public static object AppSettings(string name)
        {
            name = name.ToLower();
            if (name == ConfigSettings.CacheEnabled.ToString().ToLower()) return CacheEnabled;
            if (name == ConfigSettings.LogPath.ToString().ToLower()) return LogPath;
            if (name == ConfigSettings.PerfCountersAreMandatory.ToString().ToLower()) return PerfCountersMandatory;
            if (name == ConfigSettings.TfsUrl.ToString().ToLower()) return TfsUrl;
            if (name == ConfigSettings.DomainIncludesProjectName.ToString().ToLower()) return DomainIncludesProjectName;
            if (name == ConfigSettings.UseCodePlexServers.ToString().ToLower()) return UseCodePlexServers;
            return null;
        }

        private static void ReadUserConfig()
        {
            string configFile = Path.Combine(userConfigFolder, "user.config");
            if (File.Exists(configFile))
            {
                XmlDocument xml = new XmlDocument();
                xml.InnerXml = File.ReadAllText(configFile);
                foreach (XmlElement node in xml.SelectNodes("//setting"))
                {
                    userConfig[node.Attributes["name"].Value] = node.Attributes["value"].Value;
                }
            }
        }

        private static T ReadConfig<T>(ConfigSettings setting, T defaultValue)
        {
            string name = setting.ToString();
            if (userConfig.ContainsKey(name.ToString()))
                return (T)Convert.ChangeType(userConfig[name], typeof(T));

            if (ConfigurationManager.AppSettings[name] != null)
                return (T)Convert.ChangeType(ConfigurationManager.AppSettings[name], typeof(T));

            return defaultValue;
        }
    }
}