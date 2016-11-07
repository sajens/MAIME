using System.Collections.Generic;
using MetaData.MetaData;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Management.Smo;
using Column = MetaData.MetaData.Column;
using DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType;

namespace SamyazaSSIS
{
    /// <summary>
    /// Attribute representing an input or output attribute
    /// </summary>
    public class Attribute
    {
        public int Id; // Lineage ID of attribute
        private string _name;
        public DataType DataType;
        public IDTSObject100 AttributeRef; // Reference to SSIS-attribute
        public IDTSExternalMetadataColumn100 ExternalRef; // Reference to external meta data (snapshot of column in EDS)

        public Attribute ErrorAttribute; // First error output associated with this attribute

        // Source database and table, of which the attribute originated
        public string SourceDatabase;
        public string SourceTable;

        // Datatype relevant information
        public int CharacterMaxLength; // String related
        public string CharacterSetCatalog; // String related
        public string CharacterSetName; // String related
        public string CharacterSetSchema; // String related
        public short DateTimePrecision; // DateTime related
        public bool IsNullable; // Is attribute able to be null?
        public byte Precision; // Precision of attribute
        public short PrecisionRadix; // Radix of attribute (e.g. 10 = decimal, 2 = binary)
        public int Scale; // Scale of datatype

        // Queried in INFORMATION_SCHEMA
        public bool IsPrimaryKey;
        public bool IsUnique;

        // Taken from: https://msdn.microsoft.com/en-us/library/ms141036.aspx
        // Things without a mapping is not included in dictionary. If multiple mappings -> we took the first one in the table.
        public static Dictionary<SqlDataType, DataType> SQLtoSSIS = new Dictionary<SqlDataType, DataType>
        {
            [SqlDataType.Bit] = DataType.DT_BOOL,
            [SqlDataType.VarBinary] = DataType.DT_BOOL,
            [SqlDataType.Timestamp] = DataType.DT_BOOL,
            [SqlDataType.Binary] = DataType.DT_BYTES,
            [SqlDataType.SmallMoney] = DataType.DT_CY,
            [SqlDataType.Money] = DataType.DT_CY,
            [SqlDataType.Date] = DataType.DT_DBDATE,
            [SqlDataType.Time] = DataType.DT_DBTIME2,
            [SqlDataType.DateTime] = DataType.DT_DBTIMESTAMP,
            [SqlDataType.SmallDateTime] = DataType.DT_DBTIMESTAMP,
            [SqlDataType.DateTime2] = DataType.DT_DBTIMESTAMP2,
            [SqlDataType.DateTimeOffset] = DataType.DT_DBTIMESTAMPOFFSET,
            [SqlDataType.UniqueIdentifier] = DataType.DT_GUID,
            [SqlDataType.SmallInt] = DataType.DT_I2,
            [SqlDataType.Int] = DataType.DT_I4,
            [SqlDataType.BigInt] = DataType.DT_I8,
            [SqlDataType.Decimal] = DataType.DT_NUMERIC,
            [SqlDataType.Numeric] = DataType.DT_NUMERIC,
            [SqlDataType.Real] = DataType.DT_R4,
            [SqlDataType.Float] = DataType.DT_R8,
            [SqlDataType.Char] = DataType.DT_STR,
            [SqlDataType.VarChar] = DataType.DT_STR,
            [SqlDataType.TinyInt] = DataType.DT_UI1,
            [SqlDataType.NChar] = DataType.DT_WSTR,
            [SqlDataType.NVarChar] = DataType.DT_WSTR,
            [SqlDataType.Xml] = DataType.DT_WSTR,
            [SqlDataType.Variant] = DataType.DT_WSTR,
            [SqlDataType.Image] = DataType.DT_IMAGE,
            [SqlDataType.NText] = DataType.DT_NTEXT,
            [SqlDataType.Text] = DataType.DT_TEXT,
        };

        /// <summary>
        /// When setting property, it automatically propagates the changes into the SSIS package
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (_name == value)
                    return;

                _name = value;


                // Set the name in the package
                if (AttributeRef != null)
                    AttributeRef.Name = value;

                if (ErrorAttribute != null)
                    ErrorAttribute.Name = value;
                if (ExternalRef != null)
                    ExternalRef.Name = value;
            }
        }

        /// <summary>
        /// Perform a change on an attribute, this includes renaming and datatype change
        /// </summary>
        /// <param name="c">Relevant ColumnMetaChange</param>
        public void Change(ColumnMetaChange c)
        {
            Column newColumn = c.NewColumn;
            Name = newColumn.Name;

            SetValues(newColumn);

            ((IDTSOutputColumn100) AttributeRef).SetDataTypeProperties(SQLtoSSIS[newColumn.DataType], CharacterMaxLength, Precision, Scale, 0);

            if (ExternalRef != null)
            {
                // TODO: Maybe problems if the column doesn't have precision/scale/whatever
                ExternalRef.DataType = SQLtoSSIS[newColumn.DataType];
                ExternalRef.Length = newColumn.CharacterMaxLength;
                ExternalRef.Precision = newColumn.Precision;
                ExternalRef.Scale = newColumn.Scale;
            }
        }

        /// <summary>
        /// Set values of attribute, when given a column
        /// </summary>
        /// <param name="newColumn">Column to take values from</param>
        private void SetValues(Column newColumn)
        {
            DataType = SQLtoSSIS[newColumn.DataType];
            CharacterMaxLength = newColumn.CharacterMaxLength;
            Precision = newColumn.Precision;
            Scale = newColumn.Scale;
        }

        public Attribute(int id, string name, DataType dataType, IDTSObject100 attributeRef, string sourceDatabase, string sourceTable)
        {
            Id = id;
            _name = name;
            DataType = dataType;
            AttributeRef = attributeRef;
            SourceDatabase = sourceDatabase;
            SourceTable = sourceTable;
        }

        public Attribute(int id, string name, DataType dataType, IDTSObject100 attributeRef)
        {
            Id = id;
            _name = name;
            DataType = dataType;
            //this.dataType = SSIStoSQL[dataType];
            AttributeRef = attributeRef;           
        }

        public Attribute()
        {
            
        }
    }
}
