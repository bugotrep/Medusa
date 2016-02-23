using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using NLog;

namespace Medusa
{
	public static class FtpUploader
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public static void UploadFiles(string exportDirectory, string ftpUrl, string ftpUser, string ftpPassword)
		{
			try
			{
				var files = Directory.GetFiles(exportDirectory);
				using(WebClient ftp = new WebClient()
				{
					BaseAddress = ftpUrl,
					Credentials = new NetworkCredential(ftpUser, ftpPassword),
				})
				{
					foreach(var file in files)
					{
						try
						{
							ftp.UploadFile(Path.GetFileName(file), "STOR", file);
							File.Delete(file);
							logger.Info("Uploaded file: {0}, deleting original...", file);
						}
						catch(Exception ex)
						{
							logger.Error(ex, "Error uploading file: {0}", file);
						}
					}
				}
			}
			catch(Exception ex)
			{
				logger.Error(ex, "Error uploading files!");
			}
		}
	}
}