using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NLog;

namespace YHSQLExport
{
	public static class SQLExporter
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public static void ExportFiles(string sqlDirectory, string connectionString, string xmlRules, string separator, string exportDirectory)
		{
			try
			{
				logger.Info("Exporting files...");
				var files = Directory.GetFiles(sqlDirectory, "*.sql");
				using(SqlConnection connection = new SqlConnection(connectionString))
				{
					connection.Open();
					foreach(var file in files)
					{
						try
						{
							ExportFile(file, connection, exportDirectory, separator);
						}
						catch(Exception ex)
						{
							logger.Error(ex, "Error processing file: {0}", file);
						}
					}
				}
			}
			catch(Exception ex)
			{
				logger.Error(ex, "Error exporting sql data!");
			}
		}

		private static void ExportFile(string file, SqlConnection connection, string exportDirectory, string separator)
		{
			logger.Info("Executing file: {0}", file);
			var sql = File.ReadAllText(file);
			logger.Info("Executing sql command: {0}", sql);
			var fileName = Path.GetFileNameWithoutExtension(file);
            var transforms = GetTransforms(fileName);
			var csvName = Path.Combine(exportDirectory, fileName + ".csv");
			using(SqlCommand command = new SqlCommand(sql, connection)
			{
				CommandTimeout = 0,
				CommandType = System.Data.CommandType.Text,
			})
			{
				using(var csv = File.CreateText(csvName))
				{
					using(var reader = command.ExecuteReader())
					{
						int lines = 0;
						while(reader.Read())
						{
							var row = new object[reader.FieldCount];
                            reader.GetValues(row);
							try
							{
								TransformData(reader, row, transforms);
							}
							catch(Exception ex)
							{
								logger.Error(ex, "Error transforming line: {0}", lines + 1);
							}
							csv.WriteLine(string.Join(separator, row));
							lines++;
						}
						logger.Info("Exported {0} lines to {1}", lines, csvName);
					}
				}
			}
		}

		private static IEnumerable<XElement> GetTransforms(string query)
		{
			var xml = XDocument.Load("Transforms.xml");
            var result = xml.Descendants("query")
				.FirstOrDefault(q => q.Attribute("name").Value == query);
			return result == null ? new List<XElement>() : result.Elements("field");
		}

		private static void TransformData(SqlDataReader reader, object[] row, IEnumerable<XElement> fields)
		{
			foreach(var field in fields)
			{
				foreach(var transform in field.Descendants("transform"))
				{
					int index = reader.GetOrdinal(field.Attribute("name").Value);
					var result = row[index].ToString();
                    switch (transform.Attribute("type").Value)
					{
					case "regex":
						var pattern = transform.Attribute("pattern").Value;
                            result = new Regex(pattern).Replace(row[index].ToString(), transform.Value);
						break;

					case "format":
						result = string.Format(transform.Value, row[index]);
						break;
					}
					//if(!result.Equals(row[index]))
					//{
					//	logger.Info("Data transformations applied: \"{0}\" => \"{1}\"", row[index], result);
					//}
					row[index] = result;
				}
			}
		}
	}
}