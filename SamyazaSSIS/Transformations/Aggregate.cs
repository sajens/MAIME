using System;
using System.Collections.Generic;
using System.Linq;
using MetaData.MetaData;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

namespace SamyazaSSIS.Transformations
{
    class Aggregate : Vertex
    {
        public List<Aggregation> Aggregations = new List<Aggregation>();

        public Aggregate(Graph graph) : base(graph)
        {
        }

        /// <summary>
        /// Remove an aggregation
        /// </summary>
        /// <param name="agg">Aggregation to delete</param>
        public void RemoveAggretation(Aggregation agg)
        {
            Logger.Warn($"Removed aggregate {agg}");
            Aggregations.Remove(agg);

            // Get the ID of an attribute from lineageid and delete the output column
            int outputId = agg.outputRef.OutputColumnCollection.GetOutputColumnByLineageID(agg.Output.Id).ID;
            agg.outputRef.OutputColumnCollection.RemoveObjectByID(outputId);

            // If no aggregations are avaliable, then delete output
            if(agg.outputRef.OutputColumnCollection.Count == 0)
                ComponentRef.OutputCollection.RemoveObjectByID(agg.outputRef.ID);

            // Delete input, which is used by aggregation
            //foreach (IDTSInput100 o in ComponentRef.InputCollection)
            //{
            //    int id = o.InputColumnCollection.GetInputColumnByLineageID(agg.Input.Id).ID;
            //    o.InputColumnCollection.RemoveObjectByID(id);
            //}

            List<Edge> outputEdges = OutgoingEdges();

            // If no output edges exist, then nothing more is necessary
            if (outputEdges.Count == 0)
                return;

            // Remove attribute from output edge
            outputEdges[0].Attributes.Remove(agg.Output);
        }


        public class Aggregation
        {
            public AggregationFunction Function; // Aggregation function used
            public IDTSOutputColumn100 columnRef; // Output column created by aggregation
            public IDTSOutput100 outputRef; // Output collection used by aggregation
            public Attribute Input; // Input attribute
            public Attribute Output; // Output attribute
            public Vertex Dest; // Aggregation destination

            public override string ToString()
            {
                return $"{Function}({Input.Name})";
            }
        }

        /// <summary>
        /// Aggregations possibly to use in aggeration transformation
        /// </summary>
        public enum AggregationFunction
        {
            GROUP_BY = 0,
            COUNT = 1,
            COUNT_ALL = 2,
            COUNT_DISTINCT = 3,
            SUM = 4,
            AVG = 5,
            MIN = 6,
            MAX = 7
        }

        public override bool IsFullyDependent(Attribute attribute)
        {
            // If there is only one aggregation, and the output of it is attribute,
            // then the vertex is fully dependent on that attribute.
            return Aggregations.Count == 1 && Aggregations[0].Output == attribute;
        }

        public override void MapDependenciesAndProperties()
        {
            //Note: there should always be the same number of inputs and outputs in aggregate in SSIS

            List<Attribute> inputAttributes = InputAttributes();

            // Run through all outputs and create attributes, which is generated in this transformation.
            foreach (IDTSOutput100 output in ComponentRef.OutputCollection)
            {
                Edge outgoingEdge = Graph.Edges.Find(e => e.Path.StartPoint.ID == output.ID);

                foreach (IDTSOutputColumn100 column in output.OutputColumnCollection)
                {
                    Aggregation aggregation = new Aggregation();

                    aggregation.outputRef = output;

                    // Find the aggregation type
                    int aggregateColumnId = -1;

                    // Iterate through properties
                    foreach (IDTSCustomProperty100 property in column.CustomPropertyCollection)
                    {
                        // If current property is AggregationType, then set according to Enum
                        if (property.Name == "AggregationType")
                        {
                            aggregation.Function = (AggregationFunction) property.Value;
                        }

                        // If current property is AggregationColumnId, then set the column id
                        if (property.Name == "AggregationColumnId")
                        {
                            aggregateColumnId = property.Value;
                        }
                    }

                    // Set output of an aggregation
                    aggregation.Output = new Attribute(column.LineageID, column.Name, column.DataType, column);

                    // Set input of an aggregation
                    aggregation.Input = inputAttributes.First(a => a.Id == aggregateColumnId);

                    Dependencies[aggregation.Output] = new List<Attribute>{aggregation.Input};

                    outgoingEdge.Attributes.Add(aggregation.Output);

                    aggregation.Dest = outgoingEdge.Destination;
                    Aggregations.Add(aggregation);

                    aggregation.columnRef = column;
                }
            }
        }

        public override void AdditionAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            throw new NotImplementedException();
        }

        public override void DeletionAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            CleanUpRemovedAttribute(a);
        }

        public override void RenameAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            // No need to do anything
        }

        public override void DataTypeAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            throw new NotImplementedException();
        }

        public override void CleanUpRemovedAttribute(Attribute a)
        {
            for (int i = Aggregations.Count-1; i >= 0; i--)
            {
                Aggregation agg = Aggregations[i];
                if (agg.Input == a || agg.Output == a)
                {
                    RemoveAggretation(agg);
                }
            }
        }
    }

}
