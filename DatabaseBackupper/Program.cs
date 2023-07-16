using Microsoft.Data.SqlClient;
namespace DatabaseBackupper
{
    class Program
    {
        static void Main()
        {
            ///read ini file
            string serverName = IniService.GetPrivateString("Options", "ServerName");
            string databaseName = IniService.GetPrivateString("Options", "DatabaseName");
            string databaseBackupPath = IniService.GetPrivateString("Options", "DatabaseBackupPath");
            
            string connectionString = $"Data Source={serverName};Initial Catalog={databaseName};Integrated Security=true;Encrypt=false;TrustServerCertificate=true";
            ///perform backup
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand sqlCommand= connection.CreateCommand();
                sqlCommand.CommandText = $"BACKUP DATABASE {connection.Database} TO DISK='{databaseBackupPath}' WITH INIT";
                sqlCommand.CommandTimeout = 3600; // This will set the timeout to 1 hour
                sqlCommand.ExecuteNonQuery();
            }
            ///upload to google drive
            GoogleDriveService.Authenticate();
            string fileID = GoogleDriveService.UploadFile($"{databaseName}.bak", "Database backup", "application/octet-stream", databaseBackupPath);
        }
    }
}