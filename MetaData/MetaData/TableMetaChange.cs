using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MetaData.MetaData
{
    public class TableMetaChange
    {

        public TableChanges TableChanges { get; private set; }
        public Table OldTable { get; }
        public Table NewTable { get; }
        public Dictionary<int, ColumnMetaChange> Columns { get; }


        public TableMetaChange(Table oldTable, Table newTable)
        {
            OldTable = oldTable;
            NewTable = newTable;
            Columns = new Dictionary<int, ColumnMetaChange>();
        }


        /// <summary>
        /// Set value flag in enum
        /// </summary>
        /// <param name="value">Flag to set</param>
        public void Set(TableChanges value)
        {
            TableChanges |= value;
        }

        /// <summary>
        /// Unset value flag in enum
        /// </summary>
        /// <param name="value">Flag to unset</param>
        public void Unset(TableChanges value)
        {
            TableChanges &= ~value;
        }

        /// <summary>
        /// Check if a specific flag is set
        /// </summary>
        /// <param name="value">Flag to check</param>
        /// <returns>True if set, false if unset</returns>
        public bool IsSet(TableChanges value)
        {
            // If value is None, then the enum must be zero, since no other change could have happened
            if (value == TableChanges.None)
                return value == 0;

            return (TableChanges & value) != 0;
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            str.AppendLine($"{OldTable?.Name ?? "NULL"}/{NewTable?.Name ?? "NULL"}: {TableChanges}");

            foreach (var column in Columns)
            {
                str.AppendLine($"\t{column.Value}");
            }

            return str.ToString();
        }

        public Dictionary<string, ColumnMetaChange> RetrieveColumnMetaChanges()
        {
            return Columns.ToDictionary(x => x.Value.OldColumn.Name, x => x.Value);
        }

        public List<ColumnMetaChange> ColumnMetaChanges()
        {
            return Columns.Values.ToList();
        }
    }

    [Flags]
    public enum TableChanges : short
    {
        None = 0,
        Addition = 1,
        Deletion = 2,
        Rename = 4,
    }
}