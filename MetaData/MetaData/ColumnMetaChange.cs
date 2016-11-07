using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MetaData.MetaData
{
    [DebuggerDisplay("{OldColumn.Name}/{NewColumn.Name}: {ColumnChanges}")]
    public class ColumnMetaChange
    {
        public ColumnMetaChange(Column oldColumn, Column newColumn)
        {
            OldColumn = oldColumn;
            NewColumn = newColumn;
        }


        public ColumnChanges ColumnChanges { get; private set; }
        public Column OldColumn { get; }
        public Column NewColumn { get; }


        /// <summary>
        /// Set value flag in enum
        /// </summary>
        /// <param name="value">Flag to set</param>
        public void Set(ColumnChanges value)
        {
            ColumnChanges |= value;
        }

        /// <summary>
        /// Unset value flag in enum
        /// </summary>
        /// <param name="value">Flag to unset</param>
        public void Unset(ColumnChanges value)
        {
            ColumnChanges &= ~value;
        }

        /// <summary>
        /// Check if a specific flag is set
        /// </summary>
        /// <param name="value">Flag to check</param>
        /// <returns>True if set, false if unset</returns>
        public bool IsSet(ColumnChanges value)
        {
            // If value is None, then the enum must be zero, since no other change could have happened
            if (value == ColumnChanges.None)
                return value == 0;

            return (ColumnChanges & value) != 0;
        }

        public override string ToString()
        {
            return $"\t{OldColumn?.Name ?? "NULL"}/{NewColumn?.Name ?? "NULL"}: {ColumnChanges}";
        }

        const int COLUMN_CHANGES_MAX_VALUE = 1024;
        // Decomposes the multi-valued ColumnChanges into a list of single-valued ColumnChanges and returns it.
        public List<ColumnChanges> ListChanges()
        {
            List<ColumnChanges> list = new List<ColumnChanges>();
            for (int i = 1; i < COLUMN_CHANGES_MAX_VALUE; i = i*2)
            {
                if (IsSet( (ColumnChanges) i ))
                {
                    list.Add( (ColumnChanges) i);
                }
            }
            return list;
        }

    }

    [Flags]
    public enum ColumnChanges : short
    {
        None = 0,
        Addition = 1,
        Deletion = 2,
        Rename = 4,
        DataType = 8,
        Length = 16,
        Nullable = 32,
        NonNull = 64,
        Unique = 128,
        NonUnique = 256,
        PrimaryKey = 512,
        NonPrimary = 1024
    }
}