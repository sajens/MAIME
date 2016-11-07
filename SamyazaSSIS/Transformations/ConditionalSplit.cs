using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MetaData.MetaData;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

namespace SamyazaSSIS.Transformations
{
    public class ConditionalSplit : Vertex
    {
        public List<Condition> Conditions = new List<Condition>();

        public class Condition
        {
            public IDTSOutput100 OutputRef; // Reference to condition output
            public IDTSCustomProperty100 ExpressionRef; // Reference to condtion (Friendly) expression
            public IDTSCustomProperty100 EvaluationOrderRef; // Reference to the evaluation order
            public string Expression; // FriendlyExpression
            public uint P; // Priority
            public Vertex Dest; // Destination
        }

        public ConditionalSplit(Graph graph) : base(graph)
        {
        }

        /// <summary>
        /// Removes condition from a conditionalsplit
        /// </summary>
        /// <param name="t"></param>
        public void RemoveCondition(Condition t)
        {
            Logger.Warn($"Removed expression {t.Expression}");
            Conditions.Remove(t);

            // Decrement the priority of all priorities above the deleted condition
            foreach (Condition condition in Conditions.Where(c => c.P > t.P))
            {
                condition.P -= 1;
                condition.EvaluationOrderRef.Value -= 1;
            }

            // Remove output column from output collection
            ComponentRef.OutputCollection.RemoveObjectByID(t.OutputRef.ID);
            // Find edge, which should the be deleted
            Edge edge = Graph.Edges.Find(e => e.Destination == t.Dest && e.Source == this);
            Graph.RemoveEdge(edge);
        }

        // TODO: Consider making this a setter/getter for Expression instead
        /// <summary>
        /// Modify the condition of an existing condition
        /// </summary>
        /// <param name="c">Condition</param>
        /// <param name="newExpr">New expression</param>
        public void ModifyCondition(Condition c, string newExpr)
        {
            if (c.ExpressionRef == null)
                return;

            c.Expression = newExpr;
            c.ExpressionRef.Value = newExpr;
        }

        /// <summary>
        /// If the vertex is fully dependent on a given attribute
        /// </summary>
        /// <param name="attribute">Attribute to check</param>
        /// <returns>True = IsFullyDependent, False = Not</returns>
        public override bool IsFullyDependent(Attribute attribute)
        {
            // Is fully dependent, if attribute is part of every condition
            return
                Conditions.TrueForAll(
                        c => Regex.IsMatch(c.Expression, $@"\b{attribute.Name}\b")
                        );
        }

        /// <summary>
        /// Maps the dependencies and properties in a SSIS graph into our graph model
        /// </summary>
        public override void MapDependenciesAndProperties()
        {
            // Get all input attributes
            List<Attribute> inputAttributes = IngoingEdges().First().Attributes.ToList();

            // Non-error outputs don't contain outputcolumns
            foreach (IDTSOutput100 output in ComponentRef.OutputCollection)
            {
                // Obtain destination
                Edge edge = OutgoingEdges().Find(e => e.Path.StartPoint.ID == output.ID);

                // Handle non-error outputs
                if (!output.IsErrorOut)
                {
                    Condition c = new Condition();

                    if (output.CustomPropertyCollection.Count == 1 && output.CustomPropertyCollection["IsDefaultOut"].Value)
                    {
                      //  change.ExpressionRef = null;
                        c.Expression = "IMTHEDEFAULTOUTPUT";
                        c.P = uint.MaxValue;
                    }
                    else
                    {
                        // Obtain FriendlyExpression
                        c.ExpressionRef = output.CustomPropertyCollection["FriendlyExpression"];
                        c.Expression = output.CustomPropertyCollection["FriendlyExpression"].Value;

                        // Obtain EvaluationOrder
                        c.EvaluationOrderRef = output.CustomPropertyCollection["EvaluationOrder"];
                        c.P = Convert.ToUInt32(c.EvaluationOrderRef.Value);
                    }

                    // If an output is connected to an edge, then set the obtained values
                    if (edge != null)
                    {
                        c.Dest = edge.Destination;
                        edge.Attributes = inputAttributes;
                    }

                    // If expressionref is not null (is not a default case) then add it to conditions
                    if(c.ExpressionRef != null)
                        Conditions.Add(c);

                    c.OutputRef = output;
                }
                // Handle error output
                else
                {
                    foreach (IDTSOutputColumn100 errorOutputColumn in output.OutputColumnCollection)
                    {
                        Attribute outputAttribute = new Attribute(errorOutputColumn.LineageID, errorOutputColumn.Name, 
                            errorOutputColumn.DataType, errorOutputColumn);

                        switch (errorOutputColumn.Name)
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

                    // Set error output on edge
                    edge?.Attributes.AddRange(ErrorOutput.Attributes);
                    edge?.Attributes.Add(ErrorOutput?.ErrorCode);
                    edge?.Attributes.Add(ErrorOutput?.ErrorColumn);
                }
            }

            // Map dependencies
            foreach (Edge outputEdge in OutgoingEdges())
            {
                foreach (Attribute attribute in outputEdge.Attributes)
                {
                    // If dependency already is defined from another output, we skip it.
                    if (!Dependencies.ContainsKey(attribute))
                        // Simply just depend the attribute on it self, since it is the same reference that is used as an input
                        Dependencies[attribute] = new List<Attribute> { attribute };
                }
            }

            // ErrorCode and ErrorColumn do depend on every single input attribute, since they are only deletable when
            // all other attributes are deleted
            Dependencies[ErrorOutput.ErrorCode] = inputAttributes;
            Dependencies[ErrorOutput.ErrorColumn] = inputAttributes;
        }

        /// <summary>
        /// Rename attribute
        /// </summary>
        /// <param name="g">Graph</param>
        /// <param name="a">Attribute</param>
        /// <param name="change">Column Meta Change</param>
        public override void RenameAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            // Iterate through all conditions and change name of attribute inside it
            foreach (Condition cond in Conditions)
            {
                ModifyCondition(cond, Regex.Replace(cond.Expression, $"{change.OldColumn.Name}", change.NewColumn.Name));
            }
        }

        public override void AdditionAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete attribute
        /// </summary>
        /// <param name="g">Graph</param>
        /// <param name="a">Attribute</param>
        /// <param name="change">Column Meta Change</param>
        public override void DeletionAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            CleanUpRemovedAttribute(a);
        }

        public override void DataTypeAttribute(Graph g, Attribute a, ColumnMetaChange c)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clean up when removing an attribute
        /// </summary>
        /// <param name="a">Attribute to clean up after</param>
        public override void CleanUpRemovedAttribute(Attribute a)
        {
            for (int i=Conditions.Count-1; i >= 0; i--)
            {
                Condition c = Conditions[i];
                // If condition contains attribute, then delete it
                if (Regex.IsMatch(c.Expression, $@"\b{a.Name}\b"))
                {
                    RemoveCondition(c);
                }
            }
        }
    }
}
