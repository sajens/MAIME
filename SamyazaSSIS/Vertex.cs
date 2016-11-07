using System.Collections.Generic;
using System.Linq;
using MetaData.MetaData;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using MVG = Microsoft.VisualStudio.GraphModel;

namespace SamyazaSSIS
{
    /// <summary>
    /// Represents a vertex, which is equivalent to an instance of a transformation
    /// </summary>
    public abstract class Vertex
    {
        public string Name; // Name of transformation
        public Dictionary<Attribute, List<Attribute>> Dependencies; // Output attributes, which depend on input attributes

        public ErrorTuple ErrorOutput;

        public IDTSComponentMetaData100 ComponentRef; // Reference to the SSIS component which this vertex represents

        protected readonly Graph Graph; // Graph, which this vertex belongs to

        public Vertex(Graph graph)
        {
            Dependencies = new Dictionary<Attribute, List<Attribute>>();
            ErrorOutput = new ErrorTuple();

            Graph = graph;
        }

        /// <summary>
        /// Error tuple describing the error output of a transformation
        /// </summary>
        public class ErrorTuple
        {
            public List<Attribute> Attributes = new List<Attribute>();
            public Attribute ErrorCode;
            public Attribute ErrorColumn;
        }

        /// <summary>
        /// Get a list of all edges going into a vertex
        /// </summary>
        /// <param name="g">Graph</param>
        /// <returns>List of all ingoing edges</returns>
        public List<Edge> IngoingEdges()
        {
            return Graph.Edges.Where(e => e.Destination == this).ToList();
        }

        /// <summary>
        /// Get a list of all edges going out of a vertex
        /// </summary>
        /// <param name="g">Graph</param>
        /// <returns>List of all outgoing edges</returns>
        public List<Edge> OutgoingEdges()
        {
            return Graph.Edges.Where(e => e.Source == this).ToList();
        }

        /// <summary>
        /// Get component ID, which refers to the component ID
        /// </summary>
        public int ID
        {
            get { return ComponentRef.ID; }
        }

        /// <summary>
        /// Removes the specified attribute from all outgoing edges and also from dependencies
        /// </summary>
        public void RemoveOutputAttribute(Attribute a)
        {
            List<Edge> outgoingEdges = OutgoingEdges();

            // Iterate over edges in reverse order because we want to delete while iterating
            for (int i = outgoingEdges.Count - 1; i >= 0 ; i--)
            {
                outgoingEdges[i].Attributes.Remove(a);

                // If no attributes exist on the outgoing edge, then delete that edge
                if (outgoingEdges[i].Attributes.Count == 0)
                {
                    Graph.RemoveEdge(outgoingEdges[i]);
                }
            }
            Dependencies.Remove(a);
        }

        /// <summary>
        /// Remove dependencies on attributes that are no longer present in the ingoing edge
        /// </summary>
        public void CleanUpDependencies()
        {
            List<Attribute> inputAttributes = InputAttributes();

            foreach (Attribute key in Dependencies.Keys.ToList())
            {
                Dependencies[key] = Dependencies[key].Intersect(inputAttributes).ToList();
            }
        }

        public abstract void CleanUpRemovedAttribute(Attribute a);

        /// <summary>
        /// A vertex is fully dependent on an attribute if the removal of this attribute means that the vertex should be deleted
        /// </summary>
        /// <param name="attribute">Attribute to check against</param>
        /// <returns>True = Fully dependent, False = not</returns>
        public abstract bool IsFullyDependent(Attribute attribute);

        /// <summary>
        /// Get the output attributes of the vertex
        /// </summary>
        /// <param name="g">Graph</param>
        /// <returns>List of output attributes</returns>
        public List<Attribute> OutputAttributes()
        {
            IEnumerable<Attribute> output = new List<Attribute>();
            foreach (Edge e in OutgoingEdges())
            {
                output = output.Concat(e.Attributes);
            }
            return output.Distinct().ToList();
        }

        /// <summary>
        /// Removes invalid input columns in SSIS
        /// </summary>
        public void CleanUpInputCollection()
        {
            ComponentRef.RemoveInvalidInputColumns();
        }

        /// <summary>
        /// By default, assume attribute is used if it's in an ingoing edge
        /// </summary>
        /// <param name="a">Attribute</param>
        /// <returns>True = Uses, False = Don't</returns>
        public virtual bool UsesAttribute(Attribute a)
        {
            return InputAttributes().Contains(a);
        }

        /// <summary>
        /// Get a list of all input attributes, which is used in ingoing edges
        /// </summary>
        /// <returns>List of all input attributes</returns>
        public List<Attribute> InputAttributes()
        {
            IEnumerable<Attribute> input = new List<Attribute>();
            foreach (Edge e in IngoingEdges())
            {
                input = input.Concat(e.Attributes);
            }
            return input.Distinct().ToList();
        }

        /// <summary>
        /// Maps the informations within a SSIS package to our graph representation
        /// </summary>
        public abstract void MapDependenciesAndProperties();

        /// <summary>
        /// Visualization on vertex
        /// </summary>
        public void CreateVisualNode()
        {
            MVG.GraphNode nodeA = Program.VisualGraph.Graph.Nodes.GetOrCreate(ComponentRef.Name);

            Program.VisualGraph.LinkNodes.Add(ComponentRef.ID, nodeA);
            nodeA.Label = ComponentRef.Name;

            MVG.GraphProperty property = Program.VisualGraph.GetOrAddAttribute(
                "ComponentClass", typeof(string));
            
            nodeA[property] = Graph.Application.PipelineComponentInfos[ComponentRef.ComponentClassID].Name;
        }

        /// <summary>
        /// Called when a new attribute should be added to a vertex
        /// </summary>
        /// <param name="g">Graph to affect</param>
        /// <param name="a">Attribute to add</param>
        /// <param name="change">Column meta change</param>
        public abstract void AdditionAttribute(Graph g, Attribute a, ColumnMetaChange change);

        /// <summary>
        /// Called when an attribute should be deleted from a vertex
        /// </summary>
        /// <param name="g">Graph to affect</param>
        /// <param name="a">Attribute to delete</param>
        /// <param name="change">Column meta change</param>
        public abstract void DeletionAttribute(Graph g, Attribute a, ColumnMetaChange change);

        /// <summary>
        /// Called when an attribute should be renamed
        /// </summary>
        /// <param name="g">Graph to affect</param>
        /// <param name="a">Attribute to rename</param>
        /// <param name="change">Column meta change</param>
        public abstract void RenameAttribute(Graph g, Attribute a, ColumnMetaChange change);

        /// <summary>
        /// Called when an attribute changes datatype
        /// </summary>
        /// <param name="g">Graph to affect</param>
        /// <param name="a">Attribute to change</param>
        /// <param name="change">Column meta change</param>
        public abstract void DataTypeAttribute(Graph g, Attribute a, ColumnMetaChange change);

    }
}
