using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Medusa
{
	public static class DBManager
	{
		public static void Backup(string connectionString, string database, string backupPath)
		{
			ExecuteSqlScriptWithGo(connectionString, Resources.Backup, database, backupPath);
		}

		public static void Restore(string connectionString, string database, string backupPath)
		{
			ExecuteSqlScriptWithGo(connectionString, Resources.Restore, database, backupPath);
		}

		public static void ExecuteSqlScriptWithGo(string connectionString, string sqlScript, string databaseName, string backupPath)
		{
			string sql = string.Format(sqlScript, databaseName, backupPath.EndWith("\\"));
			ExecuteSqlScriptWithGo(connectionString, sql);
		}

		public static void ExecuteSqlScriptWithGo(string connectionString, string sql)
		{
			using(SqlConnection connection = new SqlConnection(connectionString))
			{
				Server server = new Server(new ServerConnection(connection));
				server.ConnectionContext.ExecuteNonQuery(sql);
			}
		}

		public static IEnumerable<string> GetDatabases(string connectionString)
		{
			using(SqlConnection connection = new SqlConnection(connectionString))
			{
				connection.Open();
				SqlCommand command = new SqlCommand("SELECT [name] FROM [master].[sys].[databases] WHERE [owner_sid] <> 0x01", connection)
				{
					CommandType = System.Data.CommandType.Text,
				};
				var reader = command.ExecuteReader();
				while(reader.Read())
				{
					yield return reader.GetString(0);
				}
			}
		}
		public static void BackupAllDatabases(string connectionString, string backupPath)
		{
			foreach(string database in GetDatabases(connectionString))
			{
				Backup(connectionString, database, backupPath);
			}
		}
	}
}