using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace AutoUpdates
{
    public partial class InstallUpdate : Form
    {
        static Arguments cmdArgs = new Arguments();
        delegate void SetLabelText(string text);

        [STAThread]
        static void Main(string[] args)
        {

            if (args.Length > 0)
            {
                try
                {
                    Regex cmdRegEx = new Regex(@"/(?<name>.+?):(?<val>.+)");
                    foreach (string s in args)
                    {
                        Match m = cmdRegEx.Match(s);
                        if (m.Success)
                        {
                            ProcessCommandArgs(m.Groups[1].Value, m.Groups[2].Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogFile.LogMessage(ex.Message.ToString());
                }
                finally
                {
                }
            }

            Application.EnableVisualStyles();
            Info info = new Info();
            info.Name = cmdArgs.ExecutableName;
            Application.Run(info);

            if (bInstall)
            {
                //Run application
                InstallUpdate frmUpdate = new InstallUpdate();
                Application.Run(frmUpdate);
            }
        }

        public InstallUpdate()
        {
            InitializeComponent();
        }

        private static void ProcessCommandArgs(string key, string value)
        {
            switch (key)
            {
                case "PublishFile":
                    cmdArgs.PublishFile = value;
                    break;
                case "PublishUrl":
                    cmdArgs.PublishUrl = value;
                    break;
                case "LocalFolder":
                    cmdArgs.LocalFolder = value;
                    break;
                case "ExecutableName":
                    cmdArgs.ExecutableName = value;
                    break;
                case "Restart":
                    cmdArgs.Restart = bool.Parse(value);
                    break;
                case "CommandArgs":
                    cmdArgs.CommandArgs = value;
                    break;
            }

        }

        public static bool bInstall { get; set; }

        private void InstallUpdate_Load(object sender, EventArgs e)
        {
            this.Text = cmdArgs.ExecutableName;
            bgWorker.RunWorkerAsync();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string TempFolder = getTempDirectory();

            try
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    if (process.ProcessName == cmdArgs.ExecutableName)
                        process.Kill();
                }
                //Downloading Files
                DownloadManager.bytesDownloaded += Bytesdownloaded;
                if (DownloadManager.downloadFromWeb(cmdArgs.PublishUrl, cmdArgs.PublishFile, TempFolder))
                {
                    //Decrypt the file
                    DecryptFile(TempFolder + "\\" + cmdArgs.PublishFile, Path.ChangeExtension(TempFolder + "\\" + (cmdArgs.PublishFile), ".zip"), "ecKey123");
                    //Decompress file
                    Decompress(Path.ChangeExtension(TempFolder + "\\" + (cmdArgs.PublishFile), ".zip"), TempFolder);

                    // Clean up before moving files
                    if (File.Exists(TempFolder + "\\" + cmdArgs.PublishFile))
                        File.Delete(TempFolder + "\\" + cmdArgs.PublishFile);
                    if (File.Exists(Path.ChangeExtension(TempFolder + "\\" + (cmdArgs.PublishFile), ".zip")))
                        File.Delete(Path.ChangeExtension(TempFolder + "\\" + (cmdArgs.PublishFile), ".zip"));


                    //CopyFiles to destination folder
                    CopyFiles(TempFolder);
                    //Delete Temp folder
                    if (Directory.Exists(TempFolder))
                        Directory.Delete(TempFolder, true);

                    ReplaceManifest();

                    if ((cmdArgs.Restart))
                    {
                        ProcessStartInfo restartApplication = new ProcessStartInfo();
                        restartApplication.FileName = cmdArgs.LocalFolder + "\\" + cmdArgs.ExecutableName+".exe";
                        if (!string.IsNullOrEmpty(cmdArgs.CommandArgs))
                            restartApplication.Arguments = cmdArgs.CommandArgs;
                        Process.Start(restartApplication);
                    }
                }
                else
                    MessageBox.Show("Updates utility stopped working.");
            }
            catch (Exception ex)
            {
                LogFile.LogMessage(ex.Message.ToString());
            }

            finally
            {
                //Delete Temp folder
                if (Directory.Exists(TempFolder))
                    Directory.Delete(TempFolder, true);

            }
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Close();
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pgBar.Value = e.ProgressPercentage;
        }

        public static void DecryptFile(string sInputFilename,
           string sOutputFilename,
           string sPassword)
        {
            try
            {

                UnicodeEncoding UE = new UnicodeEncoding();
                byte[] key = UE.GetBytes(sPassword);

                using (FileStream fsCrypt = new FileStream(sInputFilename, FileMode.Open))
                {

                    RijndaelManaged RMCrypto = new RijndaelManaged();
                    using (CryptoStream cs = new CryptoStream(fsCrypt,
                                            RMCrypto.CreateDecryptor(key, key),
                                            CryptoStreamMode.Read))
                    {
                        using (FileStream fsOut = new FileStream(sOutputFilename, FileMode.Create))
                        {
                            int data;
                            while ((data = cs.ReadByte()) != -1)
                                fsOut.WriteByte((byte)data);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogFile.LogMessage(ex.Message.ToString());
            }
        }

        public void CopyFiles(string TargetFolder)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(TargetFolder);
                FileInfo[] files = di.GetFiles();
                int filesCopied = 0;
                foreach (FileInfo fi in files)
                {
                    if (File.Exists(cmdArgs.LocalFolder + fi.Name))
                        File.Delete(cmdArgs.LocalFolder + fi.Name);
                    File.Copy(TargetFolder + "\\" + fi.Name, cmdArgs.LocalFolder + "\\" + fi.Name,true);
                    filesCopied = filesCopied + 1;
                    float percentage = ((float)filesCopied / (float)files.Length) * 100;
                    bgWorker.ReportProgress((int)(percentage));
                    SetLabel("Installing Updates...");

                }
            }
            catch (Exception ex)
            {
                LogFile.LogMessage(ex.Message.ToString());
            }

        }

        public static bool ReplaceManifest()
        {
            string TempFolder = getTempDirectory();
            try
            {
                string sDownloadUrl = cmdArgs.PublishUrl;

                if (DownloadMaifestFile(sDownloadUrl, TempFolder,cmdArgs.ExecutableName))
                {
                    File.Copy(TempFolder + "\\" + cmdArgs.ExecutableName + "Manifest.xml", cmdArgs.LocalFolder + "\\" + cmdArgs.ExecutableName + "Manifest.xml", true);
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                LogFile.LogMessage(ex.Message.ToString());
                return false;
            }
            finally
            {
                if (Directory.Exists(TempFolder))
                    Directory.Delete(TempFolder, true);
            }
        }

        private static bool DownloadMaifestFile(string sDownloadUrl,
            string sTargetFolder,
            string sExecutableName)
        {
            try
            {
                string sFileName = sExecutableName+"Manifest.dat", sWebResource = null;
                // Create a new WebClient instance.
                WebClient webClient = new WebClient();
                // Concatenate the domain with the Web resource filename.
                sWebResource = sDownloadUrl + "\\" + sFileName;
                // Download the Web resource and save it into the current filesystem folder.
                webClient.DownloadFile(sWebResource, sTargetFolder + "\\" + sFileName);

                DecryptFile(sTargetFolder + "\\" + sFileName, Path.ChangeExtension(sTargetFolder + "\\" + sFileName, ".xml"), "ecKey123");
                return true;
            }
            catch (Exception ex)
            {
                LogFile.LogMessage(ex.Message.ToString());
                return false;
            }
        }

        public static void Decompress(string zipPath, string extractPath)
        {

            ZipFile.ExtractToDirectory(zipPath, extractPath);
        }

        private void Bytesdownloaded(ByteArgs e)
        {
            if (e.downloaded / e.total < 1)
            {
                float percentage = ((float)e.downloaded / (float)e.total) * 100;
                bgWorker.ReportProgress((int)(percentage));
                SetLabel("Downloading Updates...");
            }
            Invalidate();
        }

        public void SetLabel(string text)
        {
            if (lblSetText.InvokeRequired)
            {
                SetLabelText d = new SetLabelText(SetLabel);
                lblSetText.Invoke(d, new object[] { text });
            }
            else
            {
                lblSetText.Text = text;
                lblSetText.Refresh();
                Invalidate();
            }
        }

        public static string getTempDirectory()
        {
            string path = Path.GetRandomFileName();
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), path));
            return Path.GetTempPath() + path + "\\";
        }

    }

    public class Arguments
    {
        #region Private Properties
        private string _publishFile;
        private string _publishUrl;
        private string _localFolder;
        private bool _restart;
        private string _executableName;
        private string _commandArgs;
        #endregion

        #region Constructors
        public Arguments()
        {
            _publishFile = "";
            _publishUrl = "";
            _localFolder = "";
            _executableName = "";
            _restart = false;
            _commandArgs = "";
        }
        #endregion

        #region Property Methods
        public string PublishFile
        {
            get
            {
                return _publishFile;
            }
            set
            {
                _publishFile = value;
            }
        }

        public string PublishUrl
        {
            get
            {
                return _publishUrl;
            }
            set
            {
                _publishUrl = value;
            }
        }

        public string LocalFolder
        {
            get
            {
                return _localFolder;
            }
            set
            {
                _localFolder = value;
            }
        }

        public string ExecutableName
        {
            get
            {
                return _executableName;
            }
            set
            {
                _executableName = value;
            }
        }

        public bool Restart
        {
            get
            {
                return _restart;
            }
            set
            {
                _restart = value;
            }
        }

        public string CommandArgs
        {
            get
            {
                return _commandArgs;
            }
            set
            {
                _commandArgs = value;
            }
        }
        #endregion
    }

    public delegate void BytesDownloadedEventHandler(ByteArgs e);

    public class ByteArgs : EventArgs
    {
        private int _downloaded;
        private int _total;

        public int downloaded
        {
            get
            {
                return _downloaded;
            }
            set
            {
                _downloaded = value;
            }
        }

        public int total
        {
            get
            {
                return _total;
            }
            set
            {
                _total = value;
            }
        }

    }

    public class DownloadManager
    {
        public static event BytesDownloadedEventHandler bytesDownloaded;

        public static bool downloadFromWeb(string URL, string file, string downloadFolder)
        {
            try
            {

                byte[] downloadedData;


                downloadedData = new byte[0];

                //open a data stream from the supplied URL
                WebRequest webReq = WebRequest.Create(URL + file);
                WebResponse webResponse = webReq.GetResponse();


                //Download the data in chuncks
                byte[] dataBuffer = new byte[1024];

                //Get the total size of the download
                int dataLength = (int)webResponse.ContentLength;

                //lets declare our downloaded bytes event args
                ByteArgs byteArgs = new ByteArgs();

                byteArgs.downloaded = 0;
                byteArgs.total = dataLength;

                //we need to test for a null as if an event is not consumed we will get an exception
                if (bytesDownloaded != null) bytesDownloaded(byteArgs);

                using (Stream dataStream = webResponse.GetResponseStream())
                {
                    //Download the data
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        while (true)
                        {
                            //Let's try and read the data
                            int bytesFromStream = dataStream.Read(dataBuffer, 0, dataBuffer.Length);

                            if (bytesFromStream == 0)
                            {

                                byteArgs.downloaded = dataLength;
                                byteArgs.total = dataLength;
                                if (bytesDownloaded != null) bytesDownloaded(byteArgs);

                                //Download complete
                                break;
                            }
                            else
                            {
                                //Write the downloaded data
                                memoryStream.Write(dataBuffer, 0, bytesFromStream);

                                byteArgs.downloaded = int.Parse(memoryStream.Length.ToString());
                                byteArgs.total = dataLength;
                                if (bytesDownloaded != null) bytesDownloaded(byteArgs);

                            }
                        }
                        //Convert the downloaded stream to a byte array
                        downloadedData = memoryStream.ToArray();
                    }
                }

                //Write bytes to the specified file
                using (FileStream newFile = new FileStream(downloadFolder + file, FileMode.Create))
                {
                    newFile.Write(downloadedData, 0, downloadedData.Length);
                }
                return true;

            }

            catch (Exception ex)
            {
                //We may not be connected to the internet
                //Or the URL may be incorrect
                LogFile.LogMessage(ex.Message.ToString());
                return false;
            }

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
                    if (File.Exists(GetPath() + "\\AutoUpdates.log"))
                    {
                        using (System.IO.StreamWriter sw = System.IO.File.AppendText(GetPath() + "\\AutoUpdates.log"))
                        {
                            Log(message, sw);
                        }
                    }
                    else
                    {
                        using (System.IO.StreamWriter sw = System.IO.File.CreateText(GetPath() + "\\AutoUpdates.log"))
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

