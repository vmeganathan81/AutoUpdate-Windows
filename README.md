# AutoUpdate-Windows
Autoupdate feature for windows application

#	Overview

This document is intended to describe how we can implement auto update feature for existing/new application. We have created generic feature that can be leveraged by other applications. For auto update, the following applications, and extension class were created
-	AutoUpdate.exe: Download manager, installing or replacing files.
-	AutoUpdates.Extensions.dll: Extension class that was created to help applications that intend to implement auto update feature.
-	AutoUpdate.GenerateFiles.exe: Console application required by release management or developers to generate the files that needs to be updated. Creates files that are encrypted.

# Project Exclusions

Project currently excluded the feature when to check for update, an example if you want to check every time at launch of application, this can always be built in to the application that wants to implement auto update.

#	System Constraints
The following are the system constraints associated with this project. 

Technology Constraints: 
•	The system should have .NET Framework 4.5 and above installed.
•	The application that wants to implement the auto update should be compiled at least .Net 2.0 and greater.


# Implementing Auto update

•	Copy these two files to folder where in application needs an update, if the exe already exists in parent folder or application folder, there is no need to copy one.
    o	 Auto update executable file
    o	Auto update extension class library
 
•	Create a manifest file for the application, with following name format NAMEOFAPPLICATIONManifest.xml, example for ApplicationTest, the manifest file was named as ApplicationTestManifest.xml

```bash
<Application>
  <Name>ApplicationTest</Name>
  <Version>2.3.0.1</Version>
  <DownloadUrl>https://download.url.com/Upgrade/test/</DownloadUrl>
  <DownloadFileName>ApplicationTest.dat</DownloadFileName> 
</Application>
```

•	Generate the two files, first would be a zip file which contains the files to be replaced, second file would be your manifest file. To generate file, we have create a console application AutoUpdate.GenerateFiles.exe, which can be leveraged to generate the files.  

Steps to generate the files
1.	Copy the files AutoUpdates.Extensions.dll, and AutoUpdate.GenerateFiles.exe to same folder
2.	Run command line 
AutoUpdate.GenerateFiles.exe /InputFile:"C:\temp\ApplicationTest.zip" /InputManifest: "C:\temp\ApplicationTestManifest.xml"
3.	Once the command line utility completes, we would have two files generated with *.dat extensions.
4.	Upload the *.dat extension file to the server.




#Security

Both the zip file and manifest file on the server are encrypted using Rijndael algorithm.

#Appendix
In print to pdf file, we have used this code snipped to do auto update check, download and install

```bash
if (AutoUpdates.Extensions.Utils.UpdatesAvailable("Manifest file name"))
{
	var th = new Thread(() =>
	{
		XmlDocument doc = new XmlDocument();
		string sAutoUpdateManifest = System.IO.File.ReadAllText(Application.StartupPath + "\\" + "(Manifest file name)");
		doc.LoadXml(sAutoUpdateManifest);
		// Loads the settings from manifest file
		XmlNodeList DownloadUrl = doc.GetElementsByTagName("DownloadUrl");
		XmlNodeList Name = doc.GetElementsByTagName("Name");
		XmlNodeList DownloadFileName = doc.GetElementsByTagName("DownloadFileName");
		for (int i = 0; i < DownloadUrl.Count; i++)
		{
			try
			{
				AutoUpdates.Extensions.Utils.installUpdateRestart("DownloadFileName", "DownloadUrl", "Application.StartupPath", "Name of the application", "Pass true or false based on whether you want to restart", " Command line arguments");
			}
			catch (Exception ex)
			{
				// return false;
			}
		}
	});
	th.SetApartmentState(ApartmentState.STA);
	th.Start();
}
```
