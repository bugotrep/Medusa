using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using NLog;

namespace Medusa
{
	public static class DBManager
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

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
			try
			{
				var sql = string.Format(sqlScript, databaseName, backupPath.EndWith("\\"));
                ExecuteSqlScriptWithGo(connectionString, sql);
			}
			catch(Exception ex)
			{
				logger.Error(ex, "Error executing script.");
				throw;
			}
		}

		public static void ExecuteSqlScriptWithGo(string connectionString, string sql)
		{
			try
			{
				using(SqlConnection connection = new SqlConnection(connectionString))
				{
					var server = new Server(new ServerConnection(connection));
                    server.ConnectionContext.ExecuteNonQuery(sql);
				}
			}
			catch(Exception ex)
			{
				logger.Error(ex, "Error executing script.");
				throw;
			}
		}

		public static IEnumerable<string> GetDatabases(string connectionString)
		{
			SqlDataReader reader = null;
			try
			{
				using(SqlConnection connection = new SqlConnection(connectionString))
				{
					connection.Open();
					var command = new SqlCommand("SELECT [name] FROM [master].[sys].[databases] WHERE [owner_sid] <> 0x01", connection)
					{
						CommandType = System.Data.CommandType.Text,
					};
                    reader = command.ExecuteReader();
				}
			}
			catch(Exception ex)
			{
				logger.Error(ex, "Error executing script.");
				throw;
			}
			if(reader == null)
			{
				yield break;
			}
			while(reader.Read())
			{
				yield return reader.GetString(0);
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