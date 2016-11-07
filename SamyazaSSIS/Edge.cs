using System.Collections.Generic;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using MVG = Microsoft.VisualStudio.GraphModel;

namespace SamyazaSSIS
{
    /// <summary>
    /// Edge connecting vertices
    /// </summary>
    public class Edge
    {
        public Vertex Source;
        public Vertex Destination;
        public List<Attribute> Attributes;

        // SSIS path, which this edge represents
        public IDTSPath100 Path;

        public Edge(IDTSPath100 path)
        {
            Path = path;
            Attributes = new List<Attribute>();
        }

        /// <summary>
        /// Create a link between two vertices, when visualizing the graph
        /// </summary>
        public void CreateVisualLinks()
        {
            MVG.GraphLink graphLink = Program.VisualGraph.Graph.Links.GetOrCreate(
                Program.VisualGraph.LinkNodes[Source.ComponentRef.ID], Program.VisualGraph.LinkNodes[Destination.ComponentRef.ID]);

            graphLink.Label = Path.Name;

            // TODO: Improve to add type of property 
            if (Attributes != null) 
            {
                foreach (Attribute attribute in Attributes)
                {
                    MVG.GraphProperty property = Program.VisualGraph.GetOrAddAttribute(
                        "Attribute", typeof (string));

                    graphLink[property] = attribute.Name;
                }
            }
        }
    }
}
