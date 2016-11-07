using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text.RegularExpressions;
using MetaData.MetaData;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;

namespace SamyazaSSIS.Transformations
{
    class Lookup : Vertex
    {
        public DatabaseTuple Database;
        public TableTuple Table;
        public List<JoinTuple> Joins;
        public List<OutputTuple> Output; // Output 0 = Match, output 1 = no match

        private bool RedirectRowsToNoMatchOutput;

        public Lookup(Graph graph) : base(graph)
        {
            Database = new DatabaseTuple();
            Table = new TableTuple();
            Joins = new List<JoinTuple>();
            Output = new List<OutputTuple>
            {
                new OutputTuple(),
                new OutputTuple()
            };
        }

        public class DatabaseTuple
        {
            public string Name; // Name of database
        }

        /// <summary>
        /// Tuple describing table and sql query performed on it
        /// </summary>
        public class TableTuple
        {
            public string Name; // Name of table
            public List<Attribute> Attributes; // Attributes "created" by lookup
            public List<Attribute> JoinAttributes; // Name of attributes used in the lookup join expression

            public IDTSCustomProperty100 sqlCommandParam; // Sql command used to perform lookup
        }

        /// <summary>
        /// Tuple describing a join condition
        /// </summary>
        public class JoinTuple
        {
            public string JoinCondition; // Join condition
            public IDTSInputColumn100 relatedInputColumnRef; // Input column/attribute used in condition
        }

        /// <summary>
        /// Output created by join
        /// </summary>
        public class OutputTuple
        {
            //TODO: these attributes should be a subseteq of the ones in the LookupTableTuple. Consider if something should be done to ensure this.
            public List<Attribute> Attributes;
        }

        /// <summary>
        /// Look input connection manager to get database name
        /// </summary>
        /// <returns>Name of used database</returns>
        private string GetDBName()
        {
            OleDbConnection oleDbConnection =
                new OleDbConnection(
                    Graph.Package.Connections[ComponentRef.RuntimeConnectionCollection[0].ConnectionManagerID]
                        .ConnectionString);

            return oleDbConnection.Database;
        }

        /// <summary>
        /// Parse sql command to get name of table used
        /// </summary>
        /// <returns></returns>
        private string GetTableName()
        {
            Regex r = new Regex(@"(\[.+\].\[.+\])", RegexOptions.Compiled);

            string value = (string)ComponentRef.CustomPropertyCollection["SqlCommand"].Value;

            Match match = r.Match(value);
            return match.Value;
        }

        /// <summary>
        /// Remove attribute from transformation
        /// </summary>
        /// <param name="attribute"></param>
        private void RemoveAttribute(Attribute attribute)
        {
            foreach (IDTSOutput100 output in ComponentRef.OutputCollection)
            {
                foreach (IDTSOutputColumn100 outputColumn in output.OutputColumnCollection)
                {
                    if(outputColumn.LineageID == attribute.Id)
                        output.OutputColumnCollection.RemoveObjectByID(outputColumn.ID);
                }
            }
        }

        /// <summary>
        /// Get attributes used for lookup, which recide in the table
        /// </summary>
        /// <returns>Attributes used for lookup</returns>
        private List<Attribute> GetLookupAttributes()
        {
            // Basic parse of sql to find the 'where' part
            Table.sqlCommandParam = ComponentRef.CustomPropertyCollection["SqlCommandParam"];
            string command = Table.sqlCommandParam.Value;
            string joinPart = Regex.Replace(command, @".*\swhere\s", "");

            // Get name of attributes which is written between '[ ]'
            MatchCollection matches = Regex.Matches(joinPart, @"\.\[([a-zA-Z0-9-_]*)\]");

            List<Attribute> attributes = new List<Attribute>();

            // Parse names into new pseudo-attributes, which is not used outside this vertex
            foreach (Match match in matches)
            {
                Attribute a;
                string name = match.Groups[1].Value;

                if ((a = Graph.GetAttribute(Database.Name, Table.Name, name)) == null)
                {
                    a = new Attribute(-1, name, DataType.DT_EMPTY, null, Database.Name, Table.Name);
                    Graph.SetAttributes(a);
                }

                attributes.Add(a);
            }

            return attributes;
        }

        /// <summary>
        /// Get attributes used for lookup, which is provided as input
        /// </summary>
        /// <returns>Attributes from input used for lookup</returns>
        private List<Attribute> GetProvidedLookupAttributes()
        {
            Dictionary<int, Attribute> inputAttributes = InputAttributes().ToDictionary(a => a.Id);
            List<Attribute> joinedAttributes = new List<Attribute>();
           
            string parameters = ComponentRef.CustomPropertyCollection["ParameterMap"].Value;

            // Parse list of '#42;' '#52;' ids into attributes
            Regex matchIds = new Regex(@"#(\d+);");
            MatchCollection matchCollection = matchIds.Matches(parameters);

            foreach (Match id in matchCollection)
            {
                joinedAttributes.Add(inputAttributes[int.Parse(id.Groups[1].Value)]);
            }

            return joinedAttributes;
        }

        public override bool IsFullyDependent(Attribute attribute)
        {
            // Lookup is fully dependent on an attributes, if it is used in any join condition
            return Joins.Any(j => j.JoinCondition.Contains(attribute.Name));
        }

        // NB:
        // Lookup doesn't currently replacing an input attribute with a looked up value, so only creates new attributes
        public override void MapDependenciesAndProperties()
        {
            RedirectRowsToNoMatchOutput = ComponentRef.CustomPropertyCollection["NoMatchBehavior"].Value == 1;

            Database.Name = GetDBName();
            Table.Name = GetTableName();
            Table.Attributes = new List<Attribute>();

            List<Attribute> attributes = InputAttributes();
            List<Edge> outputEdges = OutgoingEdges();
           
            foreach (Attribute attribute in attributes)
            {
                Dependencies[attribute] = new List<Attribute> {attribute};
            }

            List<Attribute> providedLookupAttributes = GetProvidedLookupAttributes();

            // Create new attributes for each output attribute
            foreach (IDTSOutput100 output in ComponentRef.OutputCollection)
            {
                foreach (IDTSOutputColumn100 column in output.OutputColumnCollection)
                {
                    Attribute outputAttribute = new Attribute(column.LineageID, column.Name, column.DataType, column,
                        Database.Name, Table.Name);

                    Dependencies[outputAttribute] = providedLookupAttributes;

                    if (!output.IsErrorOut)
                    {
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

            List<Attribute> outputAttributes = Table.Attributes.Concat(attributes).ToList();
            outputEdges.ForEach(e => e.Attributes = outputAttributes);

            // Insert attributse into attributes map
            foreach (Attribute attr in Table.Attributes)
            {
                Graph.SetAttributes(attr);
            }

            // Create join conditions
            List<Attribute> lookupAttributes = GetLookupAttributes();
            for (int i = 0; i < lookupAttributes.Count; i++)
            {
                JoinTuple joinTuple = new JoinTuple();
                joinTuple.JoinCondition = $"{Table.Name}.[{lookupAttributes[i].Name}] = {providedLookupAttributes[i].SourceTable}.[{providedLookupAttributes[i].Name}]";
                Joins.Add(joinTuple);
            }

            foreach (IDTSInput100 input in ComponentRef.InputCollection)
            {
                foreach (IDTSInputColumn100 inputColumn in input.InputColumnCollection)
                {
                    foreach (JoinTuple joinTuple in Joins.Where(j => j.JoinCondition.Contains(inputColumn.Name)))
                    {
                        joinTuple.relatedInputColumnRef = inputColumn;
                    }
                }
            }

            Table.JoinAttributes = lookupAttributes;

            foreach (OutputTuple outputTuple in Output)
            {
                outputTuple.Attributes = Table.Attributes;
            }
        }

        public override void AdditionAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            throw new NotImplementedException();
        }

        public override void DeletionAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            // If vertex is fully dependent on attribute, then delete vertex
            if (IsFullyDependent(a))
            {
                Graph.RemoveVertex(this);
                return;
            }

            // If attribute is part of the lookup'ed attributes, then just delete it
            if (Table.Attributes.Contains(a))
            {
                CleanUpRemovedAttribute(a);
                return;
            }
            
            // If the attribute isn't any of the above, then it is a passthrough and delete from the output edges.
            OutgoingEdges().ForEach(e => e.Attributes.Remove(a));

        }

        public override bool UsesAttribute(Attribute a)
        {
            return InputAttributes().Contains(a) ||
                   Table.Name == a.SourceTable && Database.Name == a.SourceDatabase;
        }

        /// <summary>
        /// Change join expression
        /// </summary>
        /// <param name="a">Attribute</param>
        /// <param name="change">Change</param>
        private void ChangeJoinExpression(Attribute a, ColumnMetaChange change)
        {
            // Change affected join tuples
            foreach (JoinTuple joinTuple in Joins)
            {
                if (joinTuple.JoinCondition.Contains(change.OldColumn.Name))
                {
                    joinTuple.JoinCondition = joinTuple.JoinCondition.Replace($@"{change.OldColumn.Name}", a.Name);
                    joinTuple.relatedInputColumnRef.CustomPropertyCollection["JoinToReferenceColumn"].Value = a.Name;
                }
            }

            // Change sql command to reflect change
            string value = Table.sqlCommandParam.Value;
            Table.sqlCommandParam.Value = value.Replace($@"{change.OldColumn.Name}", a.Name);
        }

        public override void RenameAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            a.Name = change.NewColumn.Name;
            if (Table.JoinAttributes.Contains(a))
            {
                ChangeJoinExpression(a, change);
                return;
            }

            if (Output[0].Attributes.Contains(a))
            {
                ((IDTSOutputColumn100) a.AttributeRef).CustomPropertyCollection["CopyFromReferenceColumn"].Value =
                    a.Name;
                return;
            }
        }

        public override void DataTypeAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            throw new NotImplementedException();
        }

        public override void CleanUpRemovedAttribute(Attribute a)
        {
            RemoveAttribute(a);
        }
    }
}
