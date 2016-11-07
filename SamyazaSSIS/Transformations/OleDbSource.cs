using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Runtime.InteropServices;
using MetaData.MetaData;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

namespace SamyazaSSIS.Transformations
{
    class OLEDBSource : Vertex
    {
        public DatabaseTuple Database;
        public TableTuple Table;

        public OLEDBSource(Graph graph) : base(graph)
        {
            Database = new DatabaseTuple();
            Table = new TableTuple();
        }

        public class DatabaseTuple
        {
            public string DatabaseName;
        }

        public class TableTuple
        {
            public string TableName; // Name of table
            public List<Attribute> Attributes = new List<Attribute>();
        }

        public void RemoveAttribute(Attribute a)
        {
            Logger.Warn($"Removed attribute {a.Name}");
            Table.Attributes.Remove(a);

            // Remove output from output collection
            foreach (IDTSOutput100 output in ComponentRef.OutputCollection)
            {
                try
                {
                    // If error output, then delete associated output column
                    if (output.IsErrorOut && a.ErrorAttribute != null)
                    {
                        output.OutputColumnCollection.RemoveObjectByID(a.ErrorAttribute.Id);
                        continue;
                    }

                    // If not, only remove output attribute and externalmetadata attribute
                    output.OutputColumnCollection.RemoveObjectByID(a.Id);
                    output.ExternalMetadataColumnCollection.RemoveObjectByID(a.ExternalRef.ID);
                }
                catch (COMException e)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"OLEDBSource: {a.Name} COMException\tIsError: {output.IsErrorOut}");
                    Console.ResetColor();
                }
            }
        }


        public void AddAttribute(Attribute a)
        {
            Table.Attributes.Add(a);

            foreach (IDTSOutput100 dtsOutput100 in ComponentRef.OutputCollection)
            {
                var dtsOutputColumn100 = dtsOutput100.OutputColumnCollection.New();
                dtsOutputColumn100.Name = a.Name;
                dtsOutputColumn100.SetDataTypeProperties(a.DataType, a.CharacterMaxLength, a.Precision, a.Scale, 0);

                if (dtsOutput100.ExternalMetadataColumnCollection != null)
                {
                    var dtsExternalMetadataColumn100 = dtsOutput100.ExternalMetadataColumnCollection.New();
                    dtsExternalMetadataColumn100.Name = a.Name;
                    dtsExternalMetadataColumn100.DataType = a.DataType;

                    dtsOutputColumn100.ExternalMetadataColumnID = dtsExternalMetadataColumn100.ID;
                    a.AttributeRef = dtsOutputColumn100;
                    a.ExternalRef = dtsExternalMetadataColumn100;
                }
                if (dtsOutput100.IsErrorOut)
                {
                    Attribute errorAttribute = new Attribute(dtsOutputColumn100.ID, dtsOutputColumn100.Name, dtsOutputColumn100.DataType, dtsOutputColumn100);
                    a.ErrorAttribute = errorAttribute;
                }
            }
        }

        public string GetTableName()
        {
            return (string) ComponentRef.CustomPropertyCollection["OpenRowset"].Value;
        }

        public string GetDBName()
        {
            OleDbConnection oleDbConnection =
                new OleDbConnection(
                    Graph.Package.Connections[ComponentRef.RuntimeConnectionCollection[0].ConnectionManagerID]
                        .ConnectionString);

            return oleDbConnection.Database;
        }

        public override bool IsFullyDependent(Attribute a)
        {
            // If there only exists one attribute, then OLEDBSource is fully dependent on it
            return Table.Attributes.Count == 1 && Table.Attributes[0] == a;
        }

        public override void MapDependenciesAndProperties()
        {
            //Database name
            Database.DatabaseName = GetDBName();
            Table.TableName = GetTableName();

            //Get list of attributes for "table" + dependencies
            foreach (IDTSOutput100 output in ComponentRef.OutputCollection)
            {
                foreach (IDTSOutputColumn100 column in output.OutputColumnCollection)
                {
                    Attribute outputAttribute = new Attribute(column.LineageID, column.Name, column.DataType, column, Database.DatabaseName, Table.TableName);

                    //Attribute outputAttribute = new Attribute(column.LineageID, column.Name, column.DataType, column);

                    // If attributes isn't in error column, then add them to the output edge
                    if (!output.IsErrorOut)
                    {
                        outputAttribute.ExternalRef = output.ExternalMetadataColumnCollection.FindObjectByID(column.ExternalMetadataColumnID);
                        // Dependencies
                        //Dependencies.Add(outputAttribute, new List<Attribute> { outputAttribute});
                        // For table property
                        Table.Attributes.Add(outputAttribute);
                    }
                    else
                    {
                        // If not add to ErrorTuple in vertex
                        switch (column.Name)
                        {
                            case "ErrorCode":
                                ErrorOutput.ErrorCode = outputAttribute;
                                break;
                            case "ErrorColumn":
                                ErrorOutput.ErrorColumn = outputAttribute;
                                break;
                            default:
                                ErrorOutput.Attributes.Add(outputAttribute);
                                Table.Attributes.Find(x => x.Name == column.Name).ErrorAttribute = outputAttribute;
                                break;
                        }
                    }
                }
            }
            
            //For OLEDB Source, the attributes on outgoing edge is just the attributes it retrieves
            foreach (Edge outputEdge in OutgoingEdges())
            {
                outputEdge.Attributes = Table.Attributes;
            }

            foreach (Attribute attr in Table.Attributes)
            {
                if (!Graph.AttributeTable.ContainsKey(attr.SourceDatabase))
                    Graph.AttributeTable[attr.SourceDatabase] = new Dictionary<string, Dictionary<string, Attribute>>();

                if(!Graph.AttributeTable[attr.SourceDatabase].ContainsKey(attr.SourceTable))
                    Graph.AttributeTable[attr.SourceDatabase][attr.SourceTable] = new Dictionary<string, Attribute>();

                if (!Graph.AttributeTable[attr.SourceDatabase][attr.SourceTable].ContainsKey(attr.Name))
                    Graph.AttributeTable[attr.SourceDatabase][attr.SourceTable][attr.Name] = attr;
            }
        }

        public override void RenameAttribute(Graph g, Attribute a, ColumnMetaChange c)
        {
            a.Name = c.NewColumn.Name;
        }

        public override void AdditionAttribute(Graph g, Attribute a, ColumnMetaChange c)
        {
            AddAttribute(a);
        }

        public override void DeletionAttribute(Graph g, Attribute a, ColumnMetaChange c)
        {
            if (IsFullyDependent(a))
                g.RemoveVertex(this);
            else
                RemoveAttribute(a);
            
        }

        public override bool UsesAttribute(Attribute a)
        {
            return Table.TableName == a.SourceTable && Database.DatabaseName == a.SourceDatabase;
        }

        public override void DataTypeAttribute(Graph g, Attribute a, ColumnMetaChange c)
        {
            throw new NotImplementedException();
        }

        public override void CleanUpRemovedAttribute(Attribute a)
        {
           // Redundant to do anything here.
        }
    }
}
