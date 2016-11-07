using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using MetaData.MetaData.Extensions;
using MetaData.MetaData.Tools;

namespace MetaData.MetaData
{
    public class Table
    {
        public Dictionary<int, Column> Columns;
        public string FullName;
        public string Name;
        public int ID;
        public DataBase Database;

        /// <summary>
        /// Needed default constructor for JSON-deserialization
        /// </summary>
        public Table()
        {
        }

        public Table(SqlConnection conn, DataRow row, DataBase dataBase, Dictionary<string, List<int>> primaryKeys, Dictionary<string, List<int>> uniqueColumns)
        {
            FullName = DataTableTools.GetValue<string>($"{row["full_name"]}");
            Name = DataTableTools.GetValue<string>(row["name"]);
            ID = DataTableTools.GetValue<int>(row["id"]);
            GetColumns(conn);

            Database = dataBase;

            if (primaryKeys.ContainsKey(FullName))
            {
                foreach (int primaryKeyId in primaryKeys[FullName])
                {
                    Columns[primaryKeyId].IsPrimaryKey = true;
                }
            }

            if (uniqueColumns.ContainsKey(FullName))
            {
                foreach (int uniqueColumnId in uniqueColumns[FullName])
                {
                    Columns[uniqueColumnId].IsUnique = true;
                }
            }

        }

        private void GetColumns(SqlConnection conn)
        {
            DataTable dataTable = conn.GetColumns(this);

            Columns = new Dictionary<int, Column>(dataTable.Rows.Count);

            foreach (DataRow row in dataTable.Rows)
            {
                Column c = new Column(row, this);

                Columns[c.ID] = c;
            }
        }

        /// <summary>
        /// Compare an old and new version of a table
        /// </summary>
        /// <param name="oldTable">Old version</param>
        /// <param name="newTable">New version</param>
        /// <returns>A object stating changes to the table</returns>
        public static TableMetaChange CompareVersions(Table oldTable, Table newTable)
        {
            var tableMetaChange = DetectChanges(oldTable, newTable);

            if (tableMetaChange.IsSet(TableChanges.Addition | TableChanges.Deletion))
                return tableMetaChange;

            foreach (var pair in DictionaryExtensions.MergeKeys(oldTable.Columns, newTable.Columns))
            {
                tableMetaChange.Columns[pair.Key] = Column.CompareVersions(pair.Value.Old, pair.Value.New);
            }

            return tableMetaChange;
        }

        public string DebugPrint(int depth = 0)
        {
            StringBuilder str = new StringBuilder();

            string spacing = new string(' ', depth * 2);

            str.AppendLine(spacing + new string('=', 32));
            str.AppendLine(spacing + new string(' ', Math.Max(32 / 2 - Name.Length / 2, 1)) + Name);
            str.AppendLine(spacing + new string('=', 32));

            foreach (var column in Columns.Values)
            {
                str.AppendLine(column.DebugPrint(depth + 1));
            }

            return str.ToString();
        }

        #region Metadata change

        private static TableMetaChange DetectChanges(Table old, Table @new)
        {
            var metaChange = new TableMetaChange(old, @new);

            CheckAddition(old, @new, metaChange);
            CheckDeletion(old, @new, metaChange);

            // Check if added or removed. If that is the case, a rename wouldn't make any sense.
            if (!metaChange.IsSet(TableChanges.Addition | TableChanges.Deletion))
                CheckRename(old, @new, metaChange);

            return metaChange;
        }

        private static void CheckAddition(Table old, Table @new, TableMetaChange metaChange)
        {
            if (old == null)
                metaChange.Set(TableChanges.Addition);
        }

        private static void CheckDeletion(Table old, Table @new, TableMetaChange metaChange)
        {
            if (@new == null)
                metaChange.Set(TableChanges.Deletion);
        }

        private static void CheckRename(Table old, Table @new, TableMetaChange metaChange)
        {
            if (!old.Name.Equals(@new.Name))
                metaChange.Set(TableChanges.Rename);
        }

        #endregion
    }
}