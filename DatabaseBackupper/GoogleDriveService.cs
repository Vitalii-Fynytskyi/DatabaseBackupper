using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.Util.Store;

namespace DatabaseBackupper
{
    public class GoogleDriveService
    {
        private static DriveService service;
        static string[] Scopes = { DriveService.Scope.Drive };
        static string ApplicationName = "DatabaseBackupper";
        public static void Authenticate()
        {
            UserCredential credential;

            using (var stream = new FileStream("GmailApiCredentials2.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }
            // Added a refresh logic if access token has expired
            
            if (credential.Token.IsStale == true)
            {
                if (!string.IsNullOrEmpty(credential.Token.RefreshToken))
                {
                    credential.RefreshTokenAsync(CancellationToken.None).Wait();
                }
                else
                {
                    throw new InvalidOperationException("Refresh token is missing");
                }
            }

            // Create Drive API service.
            service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }
        public static string UploadFile(string driveFileName, string description, string mimeType, string pathToLocalFile)
        {
            var existingFile = FindFileByName(driveFileName);

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = driveFileName,
                Description = description,
                MimeType = mimeType
            };

            FilesResource.CreateMediaUpload request;

            using (var stream = new System.IO.FileStream(pathToLocalFile, System.IO.FileMode.Open))
            {
                if (existingFile != null)
                {
                    // If file exists, delete the file.
                    var deleteRequest = service.Files.Delete(existingFile.Id);
                    deleteRequest.Execute();
                    Thread.Sleep(60_000);
                }

                // Create a new file.
                request = service.Files.Create(fileMetadata, stream, mimeType);
                request.Fields = "id";
                request.Upload();
            }

            var file = request.ResponseBody;
            return file.Id;
        }

        private static Google.Apis.Drive.v3.Data.File FindFileByName(string name)
        {
            var request = service.Files.List();
            request.Q = $"name='{name}'";
            request.Fields = "files(id, name)";
            var result = request.Execute();

            foreach (var file in result.Files)
            {
                if (file.Name == name)
                {
                    return file;
                }
            }

            return null;
        }
    }
}
