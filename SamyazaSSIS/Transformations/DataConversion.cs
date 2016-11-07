using System;
using System.Collections.Generic;
using System.Linq;
using MetaData.MetaData;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Attribute = SamyazaSSIS.Attribute;

namespace SamyazaSSIS.Transformations
{
    // TODO: Only partially implemented, will not work
    class DataConversion : Vertex
    {
        public List<Conversion> Conversions;

        public DataConversion(Graph graph) : base(graph)
        {
            Conversions = new List<Conversion>();
        }

        public class Conversion
        {
            public Attribute A;
            public Attribute Output;

        }

        public void RemoveConversion(Conversion conv)
        {
            Conversions.Remove(conv);
            
            foreach (IDTSOutput100 o in ComponentRef.OutputCollection)
            {
                o.OutputColumnCollection.RemoveObjectByID(conv.Output.Id);
            }
        }

        public override bool IsFullyDependent(Attribute attribute)
        {
            throw new NotImplementedException();
        }

        public override void MapDependenciesAndProperties()
        {
            throw new NotImplementedException();
            //Start off by getting a list of input attributes to start off.
            //List<Attribute> inputAttributes = new List<Attribute>();
            //foreach (IDTSInput100 input in componentRef.InputCollection)
            //{
            //    foreach (IDTSInputColumn100 column in input.InputColumnCollection)
            //    {
            //        inputAttributes.Add(inputAttributes.);
            //    }
            //}

            //Go through output to fill "dependencies" + "conversions"
            foreach (IDTSOutput100 output in ComponentRef.OutputCollection)
            {
                Edge edge = OutgoingEdges().Find(e => e.Path.StartPoint.ID == output.ID);

                //If it's not the error output
                if (!output.IsErrorOut)
                {
                    foreach (IDTSOutputColumn100 column in output.OutputColumnCollection)
                    {

                        Attribute outputAttribute = new Attribute(column.LineageID, column.Name, column.DataType, column);

                        int val = column.CustomPropertyCollection["SourceInputColumnLineageID"].Value;

                        //The input for "dependencies" will always just be a list of one input
                        List<Attribute> dependencyInputAttributeList = IngoingEdges().First().Attributes.FindAll(a => a.Id == val);

                        //Add tuple + fill in depencies
                        Dependencies.Add(outputAttribute, dependencyInputAttributeList);
                        Conversion conversion = new Conversion();
                        conversion.Output = outputAttribute;
                        conversion.A = dependencyInputAttributeList[0];
                        Conversions.Add(conversion);
                       

                        //Add the attribute to the edge
                        //TODO: if there will be an error output, we might not be able to use "find()" anymore.
                        edge?.Attributes.Add(outputAttribute);
                    }
                }   //error output
                else
                {
                    foreach (IDTSOutputColumn100 errorOutputColumn in output.OutputColumnCollection)
                    {
                        Attribute outputAttribute = new Attribute(errorOutputColumn.LineageID, errorOutputColumn.Name,
                            errorOutputColumn.DataType, errorOutputColumn);

                        switch (errorOutputColumn.Name)
                        {
                            case "ErrorCode":
                                Dependencies.Add(outputAttribute, new List<Attribute>());
                                break;
                            case "ErrorColumn":
                                Dependencies.Add(outputAttribute, new List<Attribute>());
                                break;
                        }
                        edge?.Attributes.Add(outputAttribute);
                    }

                }

                //attributes that are just passing through.
                foreach (Attribute outputAttribute in IngoingEdges().First().Attributes)
                {
                    edge = OutgoingEdges().Find(e => e.Path.StartPoint.ID == output.ID);
                    //Output attribute has already been created.
                    if (Dependencies.ContainsKey(outputAttribute))
                    {
                        //Add an additional dependency to existing mapping
                        Dependencies[outputAttribute].Add(outputAttribute);
                    }
                    else    //first time attribute has been encountered.
                    {
                        //Create a new list with just one new element(itself)
                        List<Attribute> outputList = new List<Attribute> { outputAttribute };
                        Dependencies.Add(outputAttribute, outputList);
                    }
                    edge?.Attributes.Add(outputAttribute);
                }

                

                //TODO: Consider how to set the two error things(attributes) on the error output
            }
        }

        public override void AdditionAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            throw new NotImplementedException();
        }

        public override void DeletionAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            throw new NotImplementedException();
        }

        public override void RenameAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            // Could rename the output attribute as well here, but probably best not to

            //foreach (Conversion conv in Conversions.Where(c => c.A == a))
            //{
            //    conv.Output.Name = a.Name;
            //}
        }

        public override void DataTypeAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            throw new NotImplementedException();
        }

        public override void CleanUpRemovedAttribute(Attribute a)
        {
            for (int i = Conversions.Count - 1; i >= 0; i--)
            {
                Conversion c = Conversions[i];
                if (c.A == a)
                {
                    RemoveConversion(c);
                }
            }
        }
    }
}
