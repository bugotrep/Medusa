
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Linq;
using System;
using System.Web;

namespace Medusa
{
	public static class GoogleDriveUploader
	{
		public static void UploadFile(string file, string parent)
		{
			// If modifying these scopes, delete your previously saved credentials
			// at ~/.credentials/drive-dotnet-quickstart.json
			string[] Scopes = { DriveService.Scope.Drive };
			string ApplicationName = "Google Drive Uploader";

			UserCredential credential;

			using(var stream =
				new FileStream(Config.Get("Google.JSON"), FileMode.Open, FileAccess.Read))
			{
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					Scopes,
					"user",
					CancellationToken.None).Result;
			}

			// Create Drive API service.
			using(var service = new DriveService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = ApplicationName,
			}))
			{
				// Define parameters of request.
				string fileName = Path.GetFileName(file);
				string mimeType = MimeMapping.GetMimeMapping(file);
				string id = service.FindIdByNameAndMimeType(fileName, mimeType, parent);
				if(id.IsNullOrEmpty())
				{
					Google.Apis.Drive.v3.Data.File body = new Google.Apis.Drive.v3.Data.File
					{
						Name = fileName,
						MimeType = mimeType,
						Description = string.Format("Last backuped at: {0}", DateTime.Now),
						Parents = new List<string>() { parent },
					};
					FilesResource.CreateMediaUpload request = service.Files.Create(body, new FileStream(file, FileMode.Open), mimeType);
					var response = request.Upload();
				}
				else
				{
					Google.Apis.Drive.v3.Data.File body = new Google.Apis.Drive.v3.Data.File();
					FilesResource.UpdateMediaUpload request = service.Files.Update(body, id, new FileStream(file, FileMode.Open), mimeType);
					var response = request.Upload();
				}
			}
		}

		public static string FindIdByNameAndMimeType(this DriveService service, string fileName, string mimeType, string parent = null)
		{
			FilesResource.ListRequest request = service.Files.List();
			request.PageSize = 1000;
			request.Fields = "nextPageToken, files(id, name)";
			if(mimeType != "application/unknown")
			{
				request.Q = "mimeType = '" + mimeType + "' and name = '" + fileName + "' and trashed=false";
			}
			else
			{
				request.Q = "mimeType!='application/vnd.google-apps.folder' and name = '" + fileName + "' and trashed=false";
			}
			if(!parent.IsNullOrWhiteSpace())
			{
				request.Q += " and '" + parent + "' in parents";
			}
			var list = request.Execute();
			Google.Apis.Drive.v3.Data.File file = list.Files.FirstOrDefault(q => q.Name.Equals(fileName));
			string id = file == null ? null : file.Id;
			return id;
		}
	}
}
