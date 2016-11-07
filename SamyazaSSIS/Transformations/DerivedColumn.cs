using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MetaData.MetaData;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

namespace SamyazaSSIS.Transformations
{
    class DerivedColumn : Vertex
    {
        // Derivations used in a transformation
        public List<Derivation> Derivations;

        /// <summary>
        /// Remove a derivation
        /// </summary>
        /// <param name="d">Derivation</param>
        public void RemoveDerivation(Derivation d)
        {
            Derivations.Remove(d);
            Logger.Warn($"Removed derivation {d.Expr}");

            if (d.OutputRef == null)
            {
                foreach (IDTSInput100 input in ComponentRef.InputCollection)
                {
                    foreach (IDTSInputColumn100 column in input.InputColumnCollection)
                    {
                        if (column.ID == d.OutputColumnRef.ID)
                        {
                            input.InputColumnCollection.RemoveObjectByID(column.ID);
                            return;
                        }
                    }
                }

                return;
            }

            d.OutputRef.OutputColumnCollection.RemoveObjectByID(d.Output.Id);

            List<Edge> outputEdges = OutgoingEdges();

            // If no output edges exist, then nothing more is necessary
            if (outputEdges.Count == 0)
                return;

            // Remove attribute from output edge
            outputEdges[0].Attributes.Remove(d.Output);

            // Can't do this, since derived column requires exactly 2 outputs
            //if(d.OutputRef.OutputColumnCollection.Count == 0)
            //    ComponentRef.OutputCollection.RemoveObjectByID(d.OutputRef.ID);
        }

        /// <summary>
        /// Modify derivation based on new expression
        /// </summary>
        /// <param name="d">Derivation to modify</param>
        /// <param name="newExpr">New expression</param>
        private void ModifyDerivation(Derivation d, string newExpr)
        {
            d.Expr = newExpr;
            d.ExpressionRef.Value = newExpr;
        }
    

        public DerivedColumn(Graph graph) : base(graph)
        {
            Derivations = new List<Derivation>();
        }

        /// <summary>
        /// Describtion of a derivation
        /// </summary>
        public class Derivation
        {
            public IDTSOutput100 OutputRef; // Referende to output
            public IDTSCustomProperty100 ExpressionRef; // Reference to expression
            public string Expr; // Expression
            public Attribute Output; // Attribute created in derivation

            public IDTSInputColumn100 OutputColumnRef; // Only used, when it is a Replace derivation

            public List<Attribute> Attributes = new List<Attribute>(); // Attributes used for derivation
        }

        public override bool IsFullyDependent(Attribute attribute)
        {
            // Derived Column is fully dependent on an attribute, if it is included in all derivations
            return
                Derivations.TrueForAll(
                        d => Regex.IsMatch(d.Expr, $@"\b{attribute.Name}\b")
                        );
        }

        /// <summary>
        /// Map dependencies and properties of a derived column transformation
        /// </summary>
        public override void MapDependenciesAndProperties()
        {
            Dictionary<int, Attribute> attributes = new Dictionary<int, Attribute>();

            InputAttributes().ForEach(a => 
            {
                attributes[a.Id] = a;
                Dependencies[a] = new List<Attribute> {a};
            });

            // Used attributes is defined as a list of ids (#42, #51, etc.), this regex capture these
            Regex r = new Regex(@"#(\d+)+", RegexOptions.Compiled);

            // Iterate through all inputs
            foreach (IDTSInput100 input in ComponentRef.InputCollection)
            {
                foreach (IDTSInputColumn100 inputColumn in input.InputColumnCollection)
                {
                    // If no custom properties exist, then it is not a replacement derivation
                    if(inputColumn.CustomPropertyCollection.Count == 0)
                        continue;

                    // Get expression and friendly expression of input
                    IDTSCustomProperty100 expression = inputColumn.CustomPropertyCollection["Expression"];
                    IDTSCustomProperty100 friendlyExpression = inputColumn.CustomPropertyCollection["FriendlyExpression"];

                    // It is an input attribute, so this is already created and placed in the attributes map
                    Attribute attribute = attributes[inputColumn.LineageID];

                    // Create a new derivation based on currently known information
                    Derivation d = new Derivation
                    {
                        Output = attribute,
                        Expr = friendlyExpression.Value,
                        ExpressionRef = friendlyExpression,
                        OutputColumnRef = inputColumn
                    };

                    Derivations.Add(d);

                    List<Attribute> dependentAttributes = new List<Attribute>();

                    // The dependencies of a derivation is all attribute ids, which is stored in the expression
                    Dependencies[attribute] = dependentAttributes;

                    // Get all ids stored in expression and map to attribute
                    foreach (Match match in r.Matches(expression.Value))
                    {
                        Attribute usedAttribute = attributes[int.Parse(match.Groups[1].Value)];
                        d.Attributes.Add(usedAttribute);

                        dependentAttributes.Add(usedAttribute);
                    }
                }
            }

            // Iteration through all outputs and perform nearly exactly the same operations as with input
            foreach (IDTSOutput100 output in ComponentRef.OutputCollection)
            {
                if (output.IsErrorOut)
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

                        Dependencies[outputAttribute] = new List<Attribute>(attributes.Values);
                    }
                    continue;
                }
                foreach (IDTSOutputColumn100 outputColumn in output.OutputColumnCollection)
                {
                    // If no custom properties exist, then it is not a replacement
                    if (outputColumn.CustomPropertyCollection.Count == 0)
                        continue;

                    IDTSCustomProperty100 expression = outputColumn.CustomPropertyCollection["Expression"];
                    IDTSCustomProperty100 friendlyExpression = outputColumn.CustomPropertyCollection["FriendlyExpression"];

                    Attribute attribute = new Attribute(outputColumn.LineageID, outputColumn.Name, outputColumn.DataType, outputColumn);

                    Derivation d = new Derivation
                    {
                        Output = attribute,
                        Expr = friendlyExpression.Value,
                        ExpressionRef = friendlyExpression,
                        OutputRef = output
                    };

                    Derivations.Add(d);

                    List<Attribute> dependentAttributes = new List<Attribute>();
                    Dependencies[attribute] = dependentAttributes;

                    foreach (Match match in r.Matches(expression.Value))
                    {
                        Attribute usedAttribute = attributes[int.Parse(match.Groups[1].Value)];
                        d.Attributes.Add(usedAttribute);

                        dependentAttributes.Add(usedAttribute);
                    }
                }
            }

            // Assign values to outgoing edge
            Edge outgoingEdge = OutgoingEdges().First();

            outgoingEdge.Attributes = new List<Attribute>(Dependencies.Keys);
            outgoingEdge.Attributes.AddRange(ErrorOutput.Attributes);
            outgoingEdge.Attributes.Add(ErrorOutput?.ErrorCode);
            outgoingEdge.Attributes.Add(ErrorOutput?.ErrorColumn);
        }

        public override void AdditionAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            throw new NotImplementedException();
        }

        public override void DeletionAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            // Delete all derivations, which uses this attribute
            for (int i = Derivations.Count - 1; i >= 0; i--)
            {
                Derivation d = Derivations[i];
                if (Regex.IsMatch(d.Expr, $@"\b{a.Name}\b"))
                {
                    RemoveDerivation(d);
                }
            }
        }

        public override void RenameAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            // Rename all derivations, which contains this attribute
            foreach (Derivation d in Derivations)
            {
                ModifyDerivation(d, Regex.Replace(d.Expr, $"{change.OldColumn.Name}", change.NewColumn.Name));
            }
        }

        public override void DataTypeAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            throw new NotImplementedException();
        }

        public override void CleanUpRemovedAttribute(Attribute a)
        {
            // Delete all derivations, which uses this attribute
            for (int i = Derivations.Count - 1; i >= 0; i--)
            {
                Derivation d = Derivations[i];
                if (Regex.IsMatch(d.Expr, $@"\b{a.Name}\b"))
                {
                    RemoveDerivation(d);
                }
            }
        }
    }
}
