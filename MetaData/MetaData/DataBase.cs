using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MetaData.MetaData.Extensions;
using MetaData.MetaData.Tools;
using Newtonsoft.Json;

namespace MetaData.MetaData
{
    public class DataBase
    {
        public int ID;
        public string Name;
        public Dictionary<int, Table> Tables;

        //references: 
        // https://msdn.microsoft.com/library/ms254969.aspx
        // https://msdn.microsoft.com/en-us/library/cc668764(v=vs.110).aspx

        /// <summary>
        /// Needed default constructor for JSON-deserialization
        /// </summary>
        public DataBase()
        {
        }

        public DataBase(SqlConnection conn, DataRow dataRow)
        {
            Name = DataTableTools.GetValue<string>(dataRow["name"]);
            ID = DataTableTools.GetValue<int>(dataRow["database_id"]);

            GetTables(conn);
        }

        private void GetTables(SqlConnection conn)
        {
            Dictionary<string, List<int>> primaryKeys;
            Dictionary<string, List<int>> uniqueColumns;

            primaryKeys = conn.GetPrimaryKeys().AsEnumerable()
                                                .GroupBy(r => $"[{r["TABLE_SCHEMA"]}].[{r["TABLE_NAME"]}]")
                                                .ToDictionary(g => g.Key,
                                                              g => g.Select(r => (int)r["COLUMN_ID"]).ToList());

            uniqueColumns = conn.GetUniqueColumns().AsEnumerable()
                                                .GroupBy(r => $"[{r["TABLE_SCHEMA"]}].[{r["TABLE_NAME"]}]")
                                                .ToDictionary(g => g.Key,
                                                              g => g.Select(r => (int)r["COLUMN_ID"]).ToList());


            var tables = conn.GetTables();
            Tables = new Dictionary<int, Table>(tables.Rows.Count);

            foreach (DataRow row in tables.Rows)
            {
                Table t = new Table(conn, row, this, primaryKeys, uniqueColumns);

                Tables[t.ID] = t;
            }

        }

        /// <summary>
        /// Compare an old and new version of a database
        /// </summary>
        /// <param name="oldDataBase">Old version</param>
        /// <param name="newDataBase">New version</param>
        /// <returns>A object stating changes to the database</returns>
        public static DatabaseMetaChange CompareVersions(DataBase oldDataBase, DataBase newDataBase)
        {
            // Get changes happened to the database
            var databaseMetaChange = DetectChanges(oldDataBase, newDataBase);

            // If the database is added or deleted, we don't care about the remaining tables and columns.
            if (databaseMetaChange.IsSet(DatabaseChanges.Addition | DatabaseChanges.Deletion))
                return databaseMetaChange;

            // Proceed down towards the tables and record these.
            foreach (var pair in DictionaryExtensions.MergeKeys(oldDataBase.Tables, newDataBase.Tables).Values)
            {
                // Check if the new version is not null, then use that ID for the dictionary, if it is then use the old ID
                if(pair.New != null)
                    databaseMetaChange.Tables[pair.New.ID] = Table.CompareVersions(pair.Old, pair.New);
                else
                    databaseMetaChange.Tables[pair.Old.ID] = Table.CompareVersions(pair.Old, pair.New);
            }
            
            return databaseMetaChange;
        }

        public string DebugPrint(int depth = 0)
        {
            StringBuilder str = new StringBuilder();

            string spacing = new string(' ', depth * 2);

            str.AppendLine(spacing + new string('=', 42));
            str.AppendLine(spacing + new string(' ', Math.Max(42 / 2 - Name.Length / 2, 1)) + Name);
            str.AppendLine(spacing + new string('=', 42));


            foreach (Table table in Tables.Values)
            {
                str.AppendLine(table.DebugPrint(depth + 1));
            }


            return str.ToString();
        }

        #region Metadata change
        /// <summary>
        /// Check for metadata changes
        /// </summary>
        /// <param name="old">Old version</param>
        /// <param name="new">New version</param>
        /// <returns>DatabaseMetaChange object</returns>
        private static DatabaseMetaChange DetectChanges(DataBase old, DataBase @new)
        {
            var metaChange = new DatabaseMetaChange(old, @new);

            CheckAddition(old, @new, metaChange);
            CheckDeletion(old, @new, metaChange);

            // Check if added or removed. If that is the case, a rename wouldn't make any sense.
            if (!metaChange.IsSet(DatabaseChanges.Addition | DatabaseChanges.Deletion))
                CheckRename(old, @new, metaChange);

            return metaChange;
        }

        private static void CheckAddition(DataBase old, DataBase @new, DatabaseMetaChange metaChange)
        {
            if (old == null)
                metaChange.Set(DatabaseChanges.Addition);
        }

        private static void CheckDeletion(DataBase old, DataBase @new, DatabaseMetaChange metaChange)
        {
            if (@new == null)
                metaChange.Set(DatabaseChanges.Deletion);
        }

        private static void CheckRename(DataBase old, DataBase @new, DatabaseMetaChange metaChange)
        {
            if (!old.Name.Equals(@new.Name))
                metaChange.Set(DatabaseChanges.Rename);
        }

        #endregion

        #region Serialization
        /// <summary>
        /// Load Database from JSON file
        /// </summary>
        /// <returns>Database</returns>
        public static DataBase LoadJSON()
        {
            var path = Environment.CurrentDirectory + @"/OldMetaData.json";

            // Open file, read text and deserialize.
            using (var file = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (TextReader reader = new StreamReader(file))
            {
                var jsonString = reader.ReadToEnd();
                var dataBase = JsonConvert.DeserializeObject<DataBase>(jsonString);
                return dataBase;
            }
        }

        /// <summary>
        /// Save Database to JSON
        /// </summary>
        public void SaveJSON()
        {
            var path = Environment.CurrentDirectory + @"/OldMetaData.json";

            // Create file if not existing, and serialize database to file.
            using (var file = File.Create(path))
            using (TextWriter writer = new StreamWriter(file))
            {
                var o = JsonConvert.SerializeObject(this, Formatting.Indented);
                writer.Write(o);
            }
        }

        #endregion
    }
}