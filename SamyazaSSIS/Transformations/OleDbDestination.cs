using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using MetaData.MetaData;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

namespace SamyazaSSIS.Transformations
{
    class OLEDBDestination : Vertex
    {
        public DatabaseTuple Database;
        public TableTuple Table;

        public OLEDBDestination(Graph graph) : base(graph)
        {
            Database = new DatabaseTuple();
            Table = new TableTuple();
        }

        public class DatabaseTuple
        {
            public string name; // Name of database
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

            if(Table.Attributes.Count == 0)
                Graph.RemoveVertex(this);
        }

        public string GetTableName()
        {
            foreach (IDTSCustomProperty100 dtsCustomProperty100 in ComponentRef.CustomPropertyCollection)
            {
                if (dtsCustomProperty100.Name.Equals("OpenRowset"))
                    return ((string)dtsCustomProperty100.Value).Replace("[dbo].[", "").Replace("]", "");
            }
            return string.Empty;
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
            return Table.Attributes.Count == 1 && Table.Attributes[0] == a;
        }

        public override void MapDependenciesAndProperties()
        {
            //No dependencies for OLEDB destination as no output edge.

            //Properties
            Table.TableName = GetTableName();
            //Database name
            Database.name = GetDBName();

            Edge inputEdge = IngoingEdges().First();
            Table.Attributes = inputEdge.Attributes.ToList();
            

            foreach (IDTSInput100 o in ComponentRef.InputCollection)
            {
                foreach (IDTSInputColumn100 o1 in o.InputColumnCollection)
                {
                    Console.WriteLine($"{o1.ID}: {o1.LineageID}");
                }
            }

            // Set error output
            foreach (IDTSOutput100 o in ComponentRef.OutputCollection)
            {
                if (!o.IsErrorOut)
                    break;

                foreach (IDTSOutputColumn100 column in o.OutputColumnCollection)
                {
                    Attribute outputAttribute = new Attribute(column.LineageID, column.Name, column.DataType, column, Database.name, Table.TableName);

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
                            break;
                    }
                }
            }
        }

        public override void RenameAttribute(Graph g, Attribute a, ColumnMetaChange c)
        {
            // No renaming required
        }

        public override void AdditionAttribute(Graph g, Attribute a, ColumnMetaChange c)
        {
            throw new NotImplementedException();
        }

        public override void DeletionAttribute(Graph g, Attribute a, ColumnMetaChange c)
        {
            // If no ingoing edges present, then delete vertex
            if (IngoingEdges().Count == 0)
            {
                g.RemoveVertex(this);
                return;
            }

            List<Attribute> attributes = Table.Attributes.Except(InputAttributes()).ToList();
            foreach (Attribute attribute in attributes)
            {
                RemoveAttribute(attribute);
            }
        }

        public override void DataTypeAttribute(Graph g, Attribute a, ColumnMetaChange c)
        {
            throw new NotImplementedException();
        }

        public override void CleanUpRemovedAttribute(Attribute a)
        {
            RemoveAttribute(a);
        }
    }
}
