using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using System.Xml;

namespace AutoUpdates.Extensions
{
    public class Utils
    {

        /// <summary>
        /// Returns true/false whether update is requires or not
        /// </summary>
        /// <param name="sManifestfile">Name of the Manifest file</param>
        /// <returns>true/false</returns>
        public static bool UpdatesAvailable(string sManifestfile)
        {
            string TempFolder = getTempDirectory();

            try
            {
                string sAutoUpdateManifest = System.IO.File.ReadAllText(AssemblyDirectory + "\\" + sManifestfile); 
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(sAutoUpdateManifest);

                XmlNodeList Name = doc.GetElementsByTagName("Name");
                XmlNodeList Version = doc.GetElementsByTagName("Version");
                XmlNodeList DownloadUrl = doc.GetElementsByTagName("DownloadUrl");
                string sClientName = string.Empty;
                string sClientVersion = string.Empty;
                string sDownloadUrl = string.Empty;
                for (int i = 0; i < Name.Count; i++)
                {
                    try
                    {
                        sClientName = Name[i].InnerText.ToString();
                        sClientVersion = Version[i].InnerText.ToString();
                        sDownloadUrl = DownloadUrl[i].InnerText.ToString();
                    }
                    catch (Exception ex)
                    {
                        LogFile.LogMessage(ex.Message.ToString());
                    }
                }


                if (DownloadMaifestFile(sDownloadUrl, TempFolder, sManifestfile))
                {
                    sAutoUpdateManifest = System.IO.File.ReadAllText(TempFolder + "\\" + sManifestfile);
                    doc.LoadXml(sAutoUpdateManifest);

                    Name = doc.GetElementsByTagName("Name");
                    Version = doc.GetElementsByTagName("Version");

                    string sServerName = string.Empty;
                    string sServerVersion = string.Empty;

                    for (int i = 0; i < Name.Count; i++)
                    {
                        try
                        {
                            sServerName = Name[i].InnerText.ToString();
                            sServerVersion = Version[i].InnerText.ToString();
                        }
                        catch (Exception ex)
                        {
                            LogFile.LogMessage(ex.Message.ToString());
                            return false;
                        }
                    }

                    LogFile.LogMessage("Client Name: " + sClientName.Trim()
                        + ", Server Name: "  + sServerName.Trim()
                        + ", Server Version: " + sServerVersion.Trim()
                        + ", Client Version: " + sClientVersion.Trim());

                    if (string.Equals(sClientName.Trim(), sServerName.Trim(), StringComparison.OrdinalIgnoreCase) && !string.Equals(sServerVersion.Trim(), sClientVersion.Trim(), StringComparison.OrdinalIgnoreCase))
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (Directory.Exists(TempFolder))
                    Directory.Delete(TempFolder, true);
            }
        }
 
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static void installUpdateRestart(string PublishFile, 
            string PublishUrl, 
            string LocalFolder, 
            string ExecutableName, 
            bool Restart, 
            string CommandArguments)
        {

            string cmdLn = "";

            cmdLn += "/PublishFile:" + PublishFile;
            cmdLn += " /PublishUrl:" + PublishUrl;
            cmdLn += " /LocalFolder:\"" + LocalFolder + "\"";
            cmdLn += " /ExecutableName:" + ExecutableName;
            cmdLn += " /Restart:" + Restart.ToString();
            cmdLn += " /CommandArgs:" + CommandArguments;

            LogFile.LogMessage("Command Line: " + cmdLn);
  


            if (File.Exists(LocalFolder + "\\AutoUpdates.exe"))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = LocalFolder + "\\AutoUpdates.exe";
                startInfo.Arguments = cmdLn;
                Process.Start(startInfo);
            }
            else
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = new DirectoryInfo(LocalFolder).Parent.FullName + "\\" + "AutoUpdates.exe";
                startInfo.Arguments = cmdLn;
                Process.Start(startInfo);

            }

        }

        private static bool DownloadMaifestFile(string sDownloadUrl,
            string sTargetFolder,
            string sManifestfile)
        {
            try
            {
                string sFileName = Path.GetFileNameWithoutExtension(sManifestfile)+".dat", sWebResource = null;
                // Create a new WebClient instance.
                WebClient webClient = new WebClient();
                // Concatenate the domain with the Web resource filename.
                sWebResource = sDownloadUrl + "\\" + sFileName;
                // Download the Web resource and save it into the current filesystem folder.
                webClient.DownloadFile(sWebResource, sTargetFolder + "\\" + sFileName);

                EncryptDecrypt.DecryptFile(sTargetFolder + "\\" + sFileName, Path.ChangeExtension(sTargetFolder + "\\" + sFileName, ".xml"), "ecKey123");
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string getTempDirectory()
        {
            string path = Path.GetRandomFileName();
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), path));
            return Path.GetTempPath() + path + "\\";
        }
    }

    public class LogFile
    {
        private static string GetPath()
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            return System.IO.Path.GetDirectoryName(path);
        }
        /// <summary>
        /// Logs message to the text file
        /// </summary>
        /// <param name="message">String:Log message</param>
        public static void LogMessage(string message)
        {
            Object thisLock = new Object();
            lock (thisLock)
            {
                try
                {
                    if (File.Exists(GetPath() + "\\AutoUpdates.Extensions.log"))
                    {
                        using (System.IO.StreamWriter sw = System.IO.File.AppendText(GetPath() + "\\AutoUpdates.Extensions.log"))
                        {
                            Log(message, sw);
                        }
                    }
                    else
                    {
                        using (System.IO.StreamWriter sw = System.IO.File.CreateText(GetPath() + "\\AutoUpdates.Extensions.log"))
                        {
                            Log(message, sw);
                        }
                    }
                    //Force clean up
                    GC.Collect();
                }
                catch (Exception ex)
                {

                }
            }
        }

        public static void Log(string logMessage, TextWriter w)
        {
            w.Write("\r\nLog Entry : ");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            w.WriteLine("{0}", logMessage);
            w.WriteLine("---------------------------------------------------------------------------------------");
        }
    }
}
