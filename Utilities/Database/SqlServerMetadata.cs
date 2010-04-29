using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using AlienForce.Utilities.Text;

namespace AlienForce.Utilities.Database
{
	public class SqlServerMetadata
	{
		internal static string CleanUp(string tableName)
		{
			string result = tableName;

			//strip blanks
			result = result.Replace(" ", "");

			//put your logic here...

			return result;
		}

		#region Type conversion
		public static string GetSysType(string sqlType) { return GetSysType(sqlType, false); }

		public static string GetSysType(string sqlType, bool isNullable)
		{
			string sysType = "string";
			switch (sqlType)
			{
				case "bigint":
					sysType = isNullable ? "long?" : "long";
					break;
				case "smallint":
					sysType = isNullable ? "short?" : "short";
					break;
				case "int":
					sysType = isNullable ? "int?" : "int";
					break;
				case "uniqueidentifier":
					sysType = isNullable ? "Guid?" : "Guid";
					break;
				case "smalldatetime":
				case "datetime":
				case "date":
					sysType = isNullable ? "DateTime?" : "DateTime";
					break;
				case "float":
					sysType = isNullable ? "double?" : "double";
					break;
				case "real":
				case "numeric":
				case "smallmoney":
				case "decimal":
				case "money":
					sysType = isNullable ? "decimal?" : "decimal";
					break;
				case "tinyint":
					sysType = isNullable ? "byte?" : "byte";
					break;
				case "bit":
					sysType = isNullable ? "bool?" : "bool";
					break;
				case "image":
				case "binary":
				case "varbinary":
					sysType = "byte[]";
					break;
			}
			return sysType;
		}

		public static DbType GetDbType(string sqlType)
		{
			switch (sqlType)
			{
				case "varchar":
					return DbType.AnsiString;
				case "nvarchar":
					return DbType.String;
				case "int":
					return DbType.Int32;
				case "uniqueidentifier":
					return DbType.Guid;
				case "datetime":
					return DbType.DateTime;
				case "bigint":
					return DbType.Int64;
				case "binary":
					return DbType.Binary;
				case "bit":
					return DbType.Boolean;
				case "char":
					return DbType.AnsiStringFixedLength;
				case "decimal":
					return DbType.Decimal;
				case "float":
					return DbType.Double;
				case "image":
					return DbType.Binary;
				case "money":
					return DbType.Currency;
				case "nchar":
					return DbType.String;
				case "ntext":
					return DbType.String;
				case "numeric":
					return DbType.Decimal;
				case "real":
					return DbType.Single;
				case "smalldatetime":
					return DbType.DateTime;
				case "smallint":
					return DbType.Int16;
				case "smallmoney":
					return DbType.Currency;
				case "sql_variant":
					return DbType.String;
				case "sysname":
					return DbType.String;
				case "text":
					return DbType.AnsiString;
				case "timestamp":
					return DbType.Binary;
				case "tinyint":
					return DbType.Byte;
				case "varbinary":
					return DbType.Binary;
				case "xml":
					return DbType.Xml;
				default:
					return DbType.AnsiString;
			}
		}
		#endregion

		string mConnectionString;
		string mDatabase;

		public SqlServerMetadata(string connectionString, string dbName)
		{
			mConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings[connectionString].ConnectionString ?? connectionString;
			mDatabase = dbName;
		}

		#region Stored Procedures
		public class StoredProcedureParameter
		{
			public string Name { get; internal set; }
			public string CleanName { get { return CleanUp(Name); } }

			public string SysType { get; internal set; }
			public string DbType { get; internal set; }

			public bool IsOutput { get; internal set; }
			public bool IsInput { get; internal set; }
			public bool HasDefault { get; internal set; }
			
			public bool ShouldBeNullable
			{
				get
				{
					return IsOutput && (SysType == "int" || SysType == "bool" || SysType == "double" || SysType == "long" || SysType == "short" || SysType == "decimal" || SysType == "byte");
				}
			}
		}

		public class StoredProcedure
		{
			public string Name { get; internal set; }

			public string ClassName { get; internal set; }

			public string CleanName { get { return CleanUp(Name); } }

			public StoredProcedureMetadata Metadata { get; internal set; }

			public IEnumerable<StoredProcedureParameter> Parameters
			{
				get
				{
					foreach (var p in _Parameters)
					{
						yield return p;
					}
				}
			}

			internal void SetParameters(List<StoredProcedureParameter> p)
			{
				_Parameters = p;
			}

			private List<StoredProcedureParameter> _Parameters;

			public string ArgList
			{
				get
				{
					StringBuilder sb = new StringBuilder();
					foreach (var par in Parameters)
					{
						if (sb.Length != 0)
						{
							sb.Append(", ");
						}
						sb.AppendFormat("{3}{0}{1} @{2}", par.SysType, par.ShouldBeNullable ? "?" : "", par.Name,
							par.IsOutput ? "ref " : String.Empty);
					}
					return sb.ToString();
				}
			}

		}

		public class StoredProcedureResultSetType
		{
			public string TypeName;
			public string PropertyName;
			public bool Multiple;
		}

		public class StoredProcedureMetadata
		{
			public string ReturnType;
			public bool IsMultiResult;
			public string Attributes;
			public bool DontMakeClass;

			public List<string> Optional;
			public List<StoredProcedureResultSetType> MultiResults;
		}

		public List<StoredProcedure> GetStoredProcedures()
		{
			var result = new List<StoredProcedure>();
			//pull the SPs

			DataTable sprocs = null;

			using (SqlConnection conn = new SqlConnection(mConnectionString))
			{
				conn.Open();
				sprocs = conn.GetSchema("Procedures");
				conn.Close();
			}

			foreach (DataRow row in sprocs.Rows)
			{
				string spType = row["ROUTINE_TYPE"].ToString();
				var sp = new StoredProcedure();
				sp.Name = row["ROUTINE_NAME"].ToString();

				if (spType == "PROCEDURE" & !sp.Name.StartsWith("sp_"))
				{
					sp.SetParameters(GetSPParams(sp.Name));
					GetSPMetadata(sp);
					result.Add(sp);
				}
			}
			return result;
		}

		static Regex ReturnType = new Regex(@"--\s*@Returns\s+(\S+)\s*(\S+)?");
		static Regex ResultSet = new Regex(@"--\s*@ResultSet\s+(\S+)\s*(\S+)?");
		static Regex Attribute = new Regex(@"--\s*@Attributes\s+(.*)");
		static Regex Optional = new Regex(@"--\s*@Optional\s+(.*)");
		static Regex OutputOnly = new Regex(@"--\s*@OutputOnly\s+(.*)");
		static Regex OmitParameter = new Regex(@"--\s*@Omit\s+(\S+)");

		void GetSPMetadata(StoredProcedure proc)
		{
			var meta = proc.Metadata = new StoredProcedureMetadata();
			string cmt;
			using (SqlConnection conn = new SqlConnection(mConnectionString))
			{
				conn.Open();
				SqlCommand sc = new SqlCommand("select text from sysobjects s, syscomments c where type = N'P' and category = 0 and name = @P1 and s.id = c.id", conn);
				sc.Parameters.Add("@P1", SqlDbType.VarChar, proc.Name.Length);
				sc.Parameters[0].Value = proc.Name;
				cmt = (string)sc.ExecuteScalar();

				sc = new SqlCommand("select ROUTINE_DEFINITION from INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE='PROCEDURE' AND ROUTINE_NAME = @P1", conn);
				sc.Parameters.Add("@P1", SqlDbType.VarChar, proc.Name.Length);
				sc.Parameters[0].Value = proc.Name;
				using (IDataReader ir = sc.ExecuteReader())
				{
					ir.Read();
					string text = (string)ir[0];
					Regex rex = new Regex(@"CREATE\s+PROCEDURE\s+(?:\[?dbo\]?\.)*\[?" + proc.Name + "\\]?(.*?)\\s+AS", RegexOptions.IgnoreCase | RegexOptions.Singleline);
					Match m = rex.Match(text);
					text = m.Groups[1].Value;
					foreach (var param in proc.Parameters)
					{
						rex = new Regex("@" + param.Name + @"[^,]*=[^,]+[,)]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
						param.HasDefault = rex.IsMatch(text);
					}
				}
				conn.Close();
			}
			string[] args = cmt.Replace("\r", "").Trim().Split('\n');
			int i = 0;
			while (i < args.Length)
			{
				string trimmed = args[i].Trim();

				Match m;
				if ((m = ReturnType.Match(trimmed)).Success)
				{
					meta.ReturnType = m.Groups[1].Value;
					if (m.Groups[2].Success && "AlreadyDefined".Equals(m.Groups[2].Value, StringComparison.OrdinalIgnoreCase))
					{
						meta.DontMakeClass = true;
					}
					else if (m.Groups[2].Success && m.Groups[2].Value[0] == '@')
					{
						foreach (var exParm in proc.Parameters)
						{
							if (exParm.Name.TrimStart('@').Equals(m.Groups[2].Value.TrimStart('@'), StringComparison.OrdinalIgnoreCase))
							{
								exParm.IsInput = false;
								exParm.IsOutput = false;
								meta.Attributes = (meta.Attributes ?? "") + String.Format("[ScalarSource(BLToolkit.Data.ScalarSourceType.OutputParameter, \"{0}\")]", m.Groups[2].Value.TrimStart('@'));
								break;
							}
						}
					}
				}
				else if ((m = ResultSet.Match(trimmed)).Success)
				{
					if (meta.MultiResults == null) { meta.MultiResults = new List<StoredProcedureResultSetType>(); }
					meta.IsMultiResult = true;
					var ri = new StoredProcedureResultSetType();
					ri.TypeName = m.Groups[1].Value.TrimEnd('*');
					if (m.Groups[2].Success && !String.IsNullOrEmpty(m.Groups[2].Value))
					{
						ri.PropertyName = m.Groups[2].Value;
					}
					else
					{
						ri.PropertyName = ri.TypeName;
					}
					if (m.Groups[1].Value.EndsWith("*"))
					{
						ri.Multiple = true;
					}
					meta.MultiResults.Add(ri);
				}
				else if ((m = OutputOnly.Match(trimmed)).Success)
				{
					List<string> strs = StringTokenizer.ParseCollection(m.Groups[1].Value);
					foreach (var parm in strs)
					{
						foreach (var exParm in proc.Parameters)
						{
							if (exParm.Name.TrimStart('@').Equals(parm.TrimStart('@'), StringComparison.OrdinalIgnoreCase))
							{
								exParm.IsInput = false;
								break;
							}
						}
					}
				}
				else if ((m = OmitParameter.Match(trimmed)).Success)
				{
					List<string> strs = StringTokenizer.ParseCollection(m.Groups[1].Value);
					foreach (var parm in strs)
					{
						foreach (var exParm in proc.Parameters)
						{
							if (exParm.Name.TrimStart('@').Equals(parm.TrimStart('@'), StringComparison.OrdinalIgnoreCase))
							{
								exParm.IsInput = false;
								exParm.IsOutput = false;
								break;
							}
						}
					}
				}
				else if ((m = Attribute.Match(trimmed)).Success)
				{
						meta.Attributes = (meta.Attributes ?? String.Empty) + m.Groups[1].Value.Trim();
				}
				i++;
			}
		}

		List<StoredProcedureParameter> GetSPParams(string spName)
		{
			var result = new List<StoredProcedureParameter>();
			string[] restrictions = new string[4] { mDatabase, null, spName, null };
			using (SqlConnection conn = new SqlConnection(mConnectionString))
			{
				conn.Open();
				var sprocs = conn.GetSchema("ProcedureParameters", restrictions);
				conn.Close();
				foreach (DataRow row in sprocs.Select("", "ORDINAL_POSITION"))
				{
					StoredProcedureParameter p = new StoredProcedureParameter();
					p.SysType = GetSysType(row["DATA_TYPE"].ToString());
					p.DbType = GetDbType(row["DATA_TYPE"].ToString()).ToString();
					p.Name = row["PARAMETER_NAME"].ToString().Replace("@", "");
					p.IsOutput = (row["PARAMETER_MODE"].ToString() == "INOUT") || (row["PARAMETER_MODE"].ToString() == "OUT");
					p.IsInput = (row["PARAMETER_MODE"].ToString() == "INOUT") || (row["PARAMETER_MODE"].ToString() == "IN");
					result.Add(p);
				}
			}
			return result;
		}
		#endregion

		#region Tables
		const string TABLE_SQL = @"SELECT *
	    FROM  INFORMATION_SCHEMA.TABLES
  	  WHERE TABLE_TYPE='BASE TABLE'";

		const string PK_SQL = @"SELECT KCU.COLUMN_NAME 
        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU
        JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC
        ON KCU.CONSTRAINT_NAME=TC.CONSTRAINT_NAME
        WHERE TC.CONSTRAINT_TYPE='PRIMARY KEY'
		AND KCU.TABLE_NAME=@tableName";

		const string COLUMN_SQL = @"SELECT 
        TABLE_CATALOG AS [Database],
        TABLE_SCHEMA AS Owner, 
        TABLE_NAME AS TableName, 
        COLUMN_NAME AS ColumnName, 
        ORDINAL_POSITION AS OrdinalPosition, 
        COLUMN_DEFAULT AS DefaultSetting, 
        IS_NULLABLE AS IsNullable, DATA_TYPE AS DataType, 
        CHARACTER_MAXIMUM_LENGTH AS MaxLength, 
        DATETIME_PRECISION AS DatePrecision,
        COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') AS IsIdentity,
        COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsComputed') as IsComputed
    FROM  INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME=@tableName
    ORDER BY OrdinalPosition ASC";

		IDataReader GetReader(string sql)
		{
			SqlConnection conn = new SqlConnection(mConnectionString);
			SqlCommand cmd = new SqlCommand(sql, conn);
			conn.Open();
			return cmd.ExecuteReader(CommandBehavior.CloseConnection);
		}

		SqlCommand GetCommand(string sql)
		{
			SqlConnection conn = new SqlConnection(mConnectionString);
			SqlCommand cmd = new SqlCommand(sql, conn);
			conn.Open();
			return cmd;
		}

		public class TableColumn
		{
			public string Name;
			public string CleanName { get { return CleanUp(Name); } }
			public string DataType { get; internal set; }
			public string SysType { get; internal set; }
			public DbType DbType { get; internal set; }
			public bool AutoIncrement { get; internal set; }
			public bool IsPrimaryKey { get; internal set; }
			public int MaxLength { get; internal set; }
			public bool IsNullable { get; internal set; }
		}

		public class Table
		{
			public string Name;
			public string CleanName { get { return CleanUp(Name); } }
			public List<TableColumn> Columns;
		}

		public List<Table> GetTables()
		{
			var result = new List<Table>();

			//pull the tables in a reader
			using (IDataReader rdr = GetReader(TABLE_SQL))
			{
				while (rdr.Read())
				{
					Table tbl = new Table();
					tbl.Name = rdr["TABLE_NAME"].ToString();
					//tbl.Schema = rdr["TABLE_SCHEMA"].ToString();
					tbl.Columns = LoadColumns(tbl);
					//tbl.PrimaryKey = GetPK(tbl.Name);

					//set the PK for the columns
					/*
					var pkColumn = tbl.Columns.SingleOrDefault(x => x.Name.ToLower().Trim() == tbl.PrimaryKey.ToLower().Trim());
					if (pkColumn != null)
						pkColumn.IsPK = true;

					tbl.FKTables = LoadFKTables(tbl.Name);
					*/
					result.Add(tbl);
				}
			}

			/*
			foreach (Table tbl in result)
			{
				//loop the FK tables and see if there's a match for our FK columns
				foreach (Column col in tbl.Columns)
				{
					col.IsForeignKey = tbl.FKTables.Any(
							x => x.ThisColumn.Equals(col.Name, StringComparison.InvariantCultureIgnoreCase)
					);
				}
			}
			 * */
			return result;
		}

		List<TableColumn> LoadColumns(Table tbl)
		{
			var result = new List<TableColumn>();

			var cmd = GetCommand(PK_SQL);
			cmd.Parameters.AddWithValue("@tableName", tbl.Name);
			var pkCol = cmd.ExecuteScalar();
			var pkColName = pkCol as string;
			
			cmd = GetCommand(COLUMN_SQL);
			cmd.Parameters.AddWithValue("@tableName", tbl.Name);

			using (IDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
			{
				while (rdr.Read())
				{
					TableColumn col = new TableColumn();
					col.Name = rdr["ColumnName"].ToString();
					if (pkColName == col.Name)
					{
						col.IsPrimaryKey = true;
					}
					col.DataType = rdr["DataType"].ToString();
					col.IsNullable = rdr["IsNullable"].ToString() == "YES";
					col.SysType = GetSysType(col.DataType, col.IsNullable);
					col.DbType = GetDbType(col.DataType);
					col.AutoIncrement = rdr["IsIdentity"].ToString() == "1";
					int colMaxLength;
					if (int.TryParse(rdr["MaxLength"].ToString(), out colMaxLength))
					{
						col.MaxLength = colMaxLength;
					}

					result.Add(col);
				}

			}

			return result;
		}

		#endregion
	}
}
