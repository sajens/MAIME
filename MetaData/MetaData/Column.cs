using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MetaData.MetaData.Tools;
using Microsoft.SqlServer.Management.Smo;

namespace MetaData.MetaData
{
    public class Column
    {
        public int CharacterMaxLength;
        public string CharacterSetCatalog;
        public string CharacterSetName;
        public string CharacterSetSchema;
        public SqlDataType DataType;
        public short DateTimePrecision;
        public int ID;
        public bool IsNullable;
        public string Name;
        public short Ordinal;
        public byte Precision;
        public short PrecisionRadix;
        public int Scale;

        // Queried in INFORMATION_SCHEMA
        public bool IsPrimaryKey;
        public bool IsUnique;

        public Table Table;

        /// <summary>
        /// Needed default constructor for JSON-deserialization
        /// </summary>
        public Column()
        {
        }

        public Column(DataRow row, Table table)
        {
            ID = (int) row["COLUMN_ID"];
            Name = row["COLUMN_NAME"] as string;
            Ordinal = (short) (int) row["ORDINAL_POSITION"];
            IsNullable = ((string) row["IS_NULLABLE"]).Equals("YES");
            DataType = (SqlDataType) Enum.Parse(typeof (SqlDataType), (string) row["DATA_TYPE"], true);
            CharacterMaxLength = DataTableTools.GetValue<int>(row["CHARACTER_MAXIMUM_LENGTH"]);
            Precision = DataTableTools.GetValue<byte>(row["NUMERIC_PRECISION"]);
            PrecisionRadix = DataTableTools.GetValue<short>(row["NUMERIC_PRECISION_RADIX"]);
            Scale = DataTableTools.GetValue<int>(row["NUMERIC_SCALE"]);
            DateTimePrecision = DataTableTools.GetValue<short>(row["DATETIME_PRECISION"]);
            CharacterSetCatalog = DataTableTools.GetValue<string>(row["CHARACTER_SET_CATALOG"]);
            CharacterSetSchema = DataTableTools.GetValue<string>(row["CHARACTER_SET_SCHEMA"]);
            CharacterSetName = DataTableTools.GetValue<string>(row["CHARACTER_SET_NAME"]);

            Table = table;
        }

        public static ColumnMetaChange CompareVersions(Column oldColumn, Column newColumn)
        {
            return DetectChanges(oldColumn, newColumn);
        }

        /// <summary>
        /// Pretty-print column
        /// </summary>
        /// <returns></returns>
        public string DebugPrint(int depth = 0)
        {
            var str = new StringBuilder();

            var t = GetType();

            string spacing = new string(' ', depth * 2);

            // Create a box of '='-signs with the length of 24 characters
            // The name of the column will be centered inside this box
            str.AppendLine(spacing + new string('=', 24));
            str.AppendLine(spacing + new string(' ', Math.Max(24/2 - Name.Length/2, 1)) + Name);
            str.AppendLine(spacing + new string('=', 24));


            // Loop through every single public field in a column. Exclude name because that is already printed
            foreach (var fieldInfo in t.GetFields().Where(f => f.IsPublic && f.Name != "Name"))
            {
                // Print name of field and value of field
                str.AppendLine(spacing + $"{fieldInfo.Name}: {fieldInfo.GetValue(this)}");
            }


            return str.ToString();
        }

        #region Metadata change

        private static ColumnMetaChange DetectChanges(Column old, Column @new)
        {
            var metaChange = new ColumnMetaChange(old, @new);

            CheckAddition(old, @new, metaChange);
            CheckDeletion(old, @new, metaChange);

            // Check if added or removed. If that is the case, a rename wouldn't make any sense.
            if (metaChange.IsSet(ColumnChanges.Addition | ColumnChanges.Deletion))
                return metaChange;

            CheckRename(old, @new, metaChange);
            CheckDatatype(old, @new, metaChange);
            CheckNullable(old, @new, metaChange);
            CheckPrimaryKey(old, @new, metaChange);
            CheckUnique(old, @new, metaChange);

            return metaChange;
        }

        private static void CheckAddition(Column old, Column @new, ColumnMetaChange metaChange)
        {
            if (old == null)
                metaChange.Set(ColumnChanges.Addition);
        }

        private static void CheckDeletion(Column old, Column @new, ColumnMetaChange metaChange)
        {
            if (@new == null)
                metaChange.Set(ColumnChanges.Deletion);
        }

        private static void CheckRename(Column old, Column @new, ColumnMetaChange metaChange)
        {
            if (!old.Name.Equals(@new.Name))
                metaChange.Set(ColumnChanges.Rename);
        }

        // TODO: Not near all necessary checks are made. We have to decide how much to track and which flags to set on said changes.
        private static void CheckDatatype(Column old, Column @new, ColumnMetaChange metaChange)
        {
            if (old.DataType != @new.DataType)
                metaChange.Set(ColumnChanges.DataType);

            switch (@new.DataType)
            {
                // Integers
                case SqlDataType.TinyInt:
                case SqlDataType.SmallInt:
                case SqlDataType.Int:
                case SqlDataType.BigInt:
                    CheckDatatypeInteger(old, @new, metaChange);
                    break;

                // Strings/chars
                case SqlDataType.Char:
                case SqlDataType.NChar:
                case SqlDataType.NText:
                case SqlDataType.NVarChar:
                case SqlDataType.NVarCharMax:
                case SqlDataType.Text:
                case SqlDataType.VarChar:
                case SqlDataType.VarCharMax:
                    CheckDatatypeString(old, @new, metaChange);
                    break;


                // Date and time
                case SqlDataType.SmallDateTime:
                case SqlDataType.Timestamp:
                case SqlDataType.Date:
                case SqlDataType.Time:
                case SqlDataType.DateTimeOffset:
                case SqlDataType.DateTime2:
                case SqlDataType.DateTime:
                    break;

                // Bit/binary
                case SqlDataType.VarBinaryMax:
                case SqlDataType.VarBinary:
                case SqlDataType.Binary:
                case SqlDataType.Bit:
                    break;

                // Float/decimal
                case SqlDataType.Float:
                case SqlDataType.Decimal:
                    break;

                // Money / precise
                case SqlDataType.Real:
                case SqlDataType.SmallMoney:
                case SqlDataType.Numeric:
                case SqlDataType.Money:
                    break;
            }
        }

        private static void CheckNullable(Column old, Column @new, ColumnMetaChange metaChange)
        {
            // Check if changed to non-nullable
            if (old.IsNullable && !@new.IsNullable)
                metaChange.Set(ColumnChanges.NonNull);
            else if (!old.IsNullable && @new.IsNullable)
                metaChange.Set(ColumnChanges.Nullable);
        }

        private static void CheckPrimaryKey(Column old, Column @new, ColumnMetaChange metaChange)
        {
            if (old.IsPrimaryKey && !@new.IsPrimaryKey)
                metaChange.Set(ColumnChanges.NonPrimary);
            else if (!old.IsPrimaryKey && @new.IsPrimaryKey)
                metaChange.Set(ColumnChanges.PrimaryKey);
        }

        private static void CheckUnique(Column old, Column @new, ColumnMetaChange metaChange)
        {
            if (old.IsUnique && !@new.IsUnique)
                metaChange.Set(ColumnChanges.NonUnique);
            else if (!old.IsUnique && @new.IsUnique)
                metaChange.Set(ColumnChanges.Unique);
        }

        #endregion

        #region Datatype specific checks
        private static void CheckDatatypeInteger(Column old, Column @new, ColumnMetaChange metaChange)
        {
            if (old.Precision != @new.Precision)
                metaChange.Set(ColumnChanges.Length);
        }

        private static void CheckDatatypeString(Column old, Column @new, ColumnMetaChange metaChange)
        {
            if(old.CharacterMaxLength != @new.CharacterMaxLength)
                metaChange.Set(ColumnChanges.Length);
        }

        private static void CheckDatatypeDateTime(Column old, Column @new, ColumnMetaChange metaChange)
        {
        }

        #endregion
    }
}