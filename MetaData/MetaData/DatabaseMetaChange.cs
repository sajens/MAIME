using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MetaData.MetaData
{
    public class DatabaseMetaChange
    {
        public DatabaseChanges DatabaseChanges { get; private set; }
        public DataBase OldDatabase { get; }
        public DataBase NewDatabase { get; }
        public Dictionary<int, TableMetaChange> Tables { get; }

        public DatabaseMetaChange(DataBase oldDatabase, DataBase newDatabase)
        {
            OldDatabase = oldDatabase;
            NewDatabase = newDatabase;

            Tables = new Dictionary<int, TableMetaChange>();
            DatabaseChanges = DatabaseChanges.None;
        }

        /// <summary>
        /// Set value flag in enum
        /// </summary>
        /// <param name="value">Flag to set</param>
        public void Set(DatabaseChanges value)
        {
            DatabaseChanges |= value;
        }

        /// <summary>
        /// Unset value flag in enum
        /// </summary>
        /// <param name="value">Flag to unset</param>
        public void Unset(DatabaseChanges value)
        {
            DatabaseChanges &= ~value;
        }

        /// <summary>
        /// Check if a specific flag is set
        /// </summary>
        /// <param name="value">Flag to check</param>
        /// <returns>True if set, false if unset</returns>
        public bool IsSet(DatabaseChanges value)
        {
            // If value is None, then the enum must be zero, since no other change could have happened
            if (value == DatabaseChanges.None)
                return value == 0;

            return (DatabaseChanges & value) != 0;
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            str.AppendLine($"{OldDatabase?.Name ?? "NULL"}/{NewDatabase?.Name ?? "NULL"}: {DatabaseChanges}");

            foreach (var table in Tables)
            {
                str.AppendLine($"\t{table.Value}");
            }

            return str.ToString();
        }

        public List<TableMetaChange> TableMetaChanges()
        {
            return Tables.Values.ToList();
        }

    }

    [Flags]
    public enum DatabaseChanges : short
    {
        None = 0,
        Addition = 1,
        Deletion = 2,
        Rename = 4
    }
}