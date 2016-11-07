using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using C5;
using MetaData.MetaData;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime;
using SamyazaSSIS.Options;
using SamyazaSSIS.Transformations;
using AttrTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, SamyazaSSIS.Attribute>>>;

namespace SamyazaSSIS
{
    public class Graph
    {
        // List of vertices contained in graph
        public List<Vertex> Vertices;
        // List of edges contained in graph
        public List<Edge> Edges;
        public Options.Options Options;
        // DatabaseName, TableName, ColumnName -> Attribute
        public AttrTable AttributeTable;

        public readonly Application Application;
        public readonly Package Package;

        // Pipe, of which the graph is generated
        private MainPipe _pipe;

        // Name of file, e.g. EmployeesToDatawareHouse
        public readonly string FileName;

        /// <summary>
        /// Construct a graph
        /// </summary>
        /// <param name="application">Application in which the graph is loaded</param>
        /// <param name="package">Package which this graph corresponds to</param>
        /// <param name="fileName">Filename of graph</param>
        public Graph(Application application, Package package, string fileName, Options.Options options)
        {
            Application = application;
            Package = package;
            FileName = fileName;
            Options = options;

            // TODO: Future implementation should work with multiple executables
            TaskHost th = (TaskHost)package.Executables[0]; //pick specific task

            // MainPipe equals a Data Flow Task
            if (th.InnerObject is MainPipe)
            {
                MainPipe dataFlowTask = (MainPipe)th.InnerObject;

                _pipe = dataFlowTask;
                Vertices = new List<Vertex>();
                Edges = new List<Edge>();
                AttributeTable = new AttrTable();

                //=======================
                //     1st iteration
                //=======================
                // Iteration of vertices/components and edges/paths

                // Instanciate all vertexes
                foreach (IDTSComponentMetaData100 dfComponent in dataFlowTask.ComponentMetaDataCollection)
                {
                    // Vertex is given a transformation type
                    string componentName = application.PipelineComponentInfos[dfComponent.ComponentClassID].Name;
                    Vertex vertex = CreateClass(componentName);

                    // Reference to SSIS component + name
                    vertex.ComponentRef = dfComponent;
                    vertex.Name = dfComponent.Name;

                    // Add vertex to graph
                    Vertices.Add(vertex);
                }

                // Get all the edges
                foreach (IDTSPath100 path in dataFlowTask.PathCollection)
                {
                    Edge edge = new Edge(path)
                    {
                        //Find the source + destination vertex
                        Source = Vertices.Find(v => v.ComponentRef.ID == path.StartPoint.Component.ID),
                        Destination = Vertices.Find(v => v.ComponentRef.ID == path.EndPoint.Component.ID)
                    };

                    // Add edge to graph, which connects two vertices
                    Edges.Add(edge);
                }

                //=======================
                //     2nd iteration
                //=======================
                // Iterate through every single vertex in topological order, and set their dependencies and properties
                Vertices = TopologicalSort(ColumnChanges.None);
                Vertices.ForEach(v => v.MapDependenciesAndProperties());

                //VisualizeGraph();
                //Program.VisualGraph.Draw(fileName);
                //Program.VisualGraph.Open();
            }
        }

        /// <summary>
        /// Sorts the vertices in topological order
        /// </summary>
        /// <returns>List of topological sorted vertices</returns>
        public List<Vertex> TopologicalSort(ColumnChanges columnChanges)
        {
            IPriorityQueue<Vertex> priorityQueue = new IntervalHeap<Vertex>(new VerticesComparer(Options, columnChanges));
            var sortedVertices = new List<Vertex>();

            var incomingEdges = new Dictionary<Vertex, int>();
            foreach (var v in Vertices)
            {
                incomingEdges[v] = v.IngoingEdges().Count;
                if (incomingEdges[v] == 0) priorityQueue.Add(v);
            }

            while (!priorityQueue.IsEmpty)
            {
                Vertex v = priorityQueue.FindMin();
                sortedVertices.Add(v);
                priorityQueue.DeleteMin();

                foreach (var e in v.OutgoingEdges())
                {
                    incomingEdges[e.Destination]--;
                    if (incomingEdges[e.Destination] == 0) priorityQueue.Add(e.Destination);
                }
            }

            return sortedVertices;
        }

        private class VerticesComparer : IComparer<Vertex>
        {
            private readonly Options.Options _options;
            private readonly ColumnChanges _columnChange;

            public VerticesComparer(Options.Options options, ColumnChanges columnChange)
            {
                _options = options;
                _columnChange = columnChange;
            }

            int IComparer<Vertex>.Compare(Vertex a, Vertex b)
            {
                if (_columnChange == ColumnChanges.None) return 0;

                bool aBlocked = _options.PolicyTable[_columnChange][a.GetType()] == Policy.BLOCK;
                bool bBlocked = _options.PolicyTable[_columnChange][b.GetType()] == Policy.BLOCK;

                if (aBlocked && !bBlocked)
                    return 1;
                if (!aBlocked && bBlocked)
                    return -1;
                else
                    return 0;
            }
        }

        public void VisualizeGraph()
        {
            foreach (var vertex in Vertices)
            {
                vertex.CreateVisualNode();
            }
            foreach (var edge in Edges)
            {
                edge.CreateVisualLinks();
            }
        }

        /// <summary>
        /// Get attribute related to a ColumnMetaChange, which is the database-, table-, and columnname
        /// </summary>
        /// <param name="change">ColumnMetaChange to reference</param>
        /// <returns>Attribute</returns>
        public Attribute GetAttribute(ColumnMetaChange change)
        {
            return GetAttribute(change.OldColumn.Table.Database.Name, change.OldColumn.Table.FullName,
                change.OldColumn.Name);

            //return AttributeTable[change.OldColumn.Table.Database.Name][change.OldColumn.Table.Name][change.OldColumn.Name];
        }

        public Attribute GetAttribute(string database, string table, string column)
        {
            Dictionary<string, Dictionary<string, Attribute>> databaseLevel;
            if (!AttributeTable.TryGetValue(database, out databaseLevel))
                return null;

            Dictionary<string, Attribute> tableLevel;
            if (!databaseLevel.TryGetValue(table, out tableLevel))
                return null;

            Attribute attribute;
            if (!tableLevel.TryGetValue(column, out attribute))
                return null;

            return attribute;
        }

        public void SetAttributes(Attribute attr)
        {
            if (!AttributeTable.ContainsKey(attr.SourceDatabase))
                AttributeTable[attr.SourceDatabase] = new Dictionary<string, Dictionary<string, Attribute>>();

            if (!AttributeTable[attr.SourceDatabase].ContainsKey(attr.SourceTable))
                AttributeTable[attr.SourceDatabase][attr.SourceTable] = new Dictionary<string, Attribute>();

            if (!AttributeTable[attr.SourceDatabase][attr.SourceTable].ContainsKey(attr.Name))
                AttributeTable[attr.SourceDatabase][attr.SourceTable][attr.Name] = attr;
        }

        /// <summary>
        /// Removes a given edge
        /// </summary>
        /// <param name="e"></param>
        public void RemoveEdge(Edge e)
        {
            Edges.Remove(e);

            // Remove corresponding SSIS construct (Path)
            _pipe.PathCollection.RemoveObjectByID(e.Path.ID);
        }

        /// <summary>
        /// Rename attribute
        /// </summary>
        /// <param name="a">Attribute to rename</param>
        /// <param name="newName">New name of attribute</param>
        public void RenameAttribute(Attribute a, string newName)
        {
            // Renames attribute, and using properties also updates name in SSIS package
            a.Name = newName;
        }

        /// <summary>
        /// Remove vertex
        /// </summary>
        /// <param name="v">Vertex to remove</param>
        public void RemoveVertex(Vertex v)
        {
            // Do reverse iteration because we delete while iterating
            for (int i = Edges.Count - 1; i >= 0; i--)
            {
                Edge e = Edges[i];
                if (e.Source == v || e.Destination == v)
                {
                    RemoveEdge(e);
                }
            }

            Logger.Warn($"Removed vertex {v.Name}");
            // Remove the vertex
            Vertices.Remove(v);
            _pipe.ComponentMetaDataCollection.RemoveObjectByID(v.ComponentRef.ID);
        }

        // Returns a list of vertices that currently (before the change) make use of the changed attribute.
        private List<Vertex> GetAffectedVertices(ColumnMetaChange change)
        {
            List<Vertex> affected = new List<Vertex>();
            Attribute attribute = GetAttribute(change) ?? new Attribute
            {
                Name = change?.OldColumn.Name ?? change?.NewColumn.Name,
                SourceTable = change?.OldColumn.Table.Name ?? change?.NewColumn.Table.Name,
                SourceDatabase = change?.OldColumn.Table.Database.Name ?? change?.NewColumn.Table.Database.Name
            };


            foreach (Vertex v in Vertices)
            {
                if (v.UsesAttribute(attribute))
                    affected.Add(v);
            }
            return affected;
        }

        /// <summary>
        /// Main algorithm used to perform alterations of graph given a ColumnMetaChange
        /// </summary>
        /// <param name="change">Change to perform in graph</param>
        public void Alter(ColumnMetaChange change, ColumnChanges changeType)
        {
            // if partial fix is not enabled quit
            if (Options.UseGlobalBlockingSemantics &&
                Vertices.Any(v => Options.PolicyTable[changeType][v.GetType()] == Policy.BLOCK)) return;

            List<Vertex> sortedVertices = TopologicalSort(changeType);

            
            Logger.Debug("Order of fixing transformations:");
            sortedVertices.ForEach(v => Logger.Debug(v.Name));

            List<Vertex> affectedVertices = GetAffectedVertices(change);

            foreach (Vertex v in sortedVertices)
            {
                // Assumes partial fix is enabled.
                if (Options.PolicyTable[changeType][v.GetType()] == Policy.BLOCK)
                {
                    Logger.Debug($"Blocked the propagation at {v.Name}");
                    break;
                }
                v.CleanUpDependencies();
                // TODO: Insert Data type change fix/check here


                // Check for attributes that need to be deleted (and delete vertex if no attributes are left)
                if (!(v is OLEDBSource))
                {
                    foreach (Attribute a in v.OutputAttributes())
                    {
                        // If no dependencies are found for a given attribute, then it is safe to delete

                        if (v.Dependencies[a].Count == 0)
                        {
                            if (v.IsFullyDependent(a))
                            {
                                RemoveVertex(v);
                                break;
                            }
                            // If not, then just remove attribute
                            else
                            {
                                v.RemoveOutputAttribute(a);
                                v.CleanUpRemovedAttribute(a);
                            }

                        }
                    }
                }
                
                // Since a destination in many scenarioes don't have any output edges, then this specific check is necessary
                if(v is OLEDBDestination && v.IngoingEdges().Count == 0)
                    RemoveVertex(v);

                // If the vertex has not been removed, we continue
                if (Vertices.Contains(v))
                {
                    v.CleanUpInputCollection();
                    InvokeMethod(v, change, changeType, affectedVertices);   
                }
            } 
            
        }

        /// <summary>
        /// Invoke "fixing" function based on provided change
        /// </summary>
        /// <param name="g">Graph</param>
        /// <param name="v">Vertex to invoke on</param>
        /// <param name="c">Reference to change</param>
        /// <param name="change">Action to perform</param>
        /// <param name="affectedVertices">Vertices affected by an EDS change</param>
        public void InvokeMethod(Vertex v, ColumnMetaChange c, ColumnChanges change, List<Vertex> affectedVertices )
        {
            Attribute a = new Attribute();
            if (change != ColumnChanges.Addition)
            {
                // If attribute isn't available, then it is safe to assume it isn't a part of the job, and won't affect any transformation
                if ((a = GetAttribute(c)) == null)
                    return;
                // If the vertex is not affected by the change, we stop.
                if (!affectedVertices.Contains(v))
                    return;
            }
            else
                a.Change(c);

            // Invoke method on vertex based on the provided change.
            // We've already split the ColumnChanges enum into its components, so it is safe to switch on
            switch (change)
            {
                case ColumnChanges.None:
                    break;
                case ColumnChanges.Addition:
                    if(v.UsesAttribute(a))
                        v.AdditionAttribute(this, a, c);
                    break;
                case ColumnChanges.Deletion:
                    v.DeletionAttribute(this, a, c);
                    break;
                case ColumnChanges.Rename:
                    v.RenameAttribute(this, a, c);
                    break;
                case ColumnChanges.DataType:
                    v.DataTypeAttribute(this, a, c);
                    break;
                case ColumnChanges.Length:
                    break;
                case ColumnChanges.Nullable:
                    break;
                case ColumnChanges.NonNull:
                    break;
                case ColumnChanges.Unique:
                    break;
                case ColumnChanges.NonUnique:
                    break;
                case ColumnChanges.PrimaryKey:
                    break;
                case ColumnChanges.NonPrimary:
                    break;
            }

            
        }

        private Type GetComponentType(string componentName)
        {
            Assembly ass = typeof(Aggregate).Assembly;

            return ass.GetType("SamyazaSSIS.Transformations." + componentName.Replace(" ", string.Empty));
        }

        // TODO: Change to a less hardcoded format
        private Vertex CreateClass(string componentName)
        {
            return (Vertex)Activator.CreateInstance(GetComponentType(componentName), this);
        }

        // TODO: Check if better methods exists
        //private static Vertex CreateClass(string componentName)
        //{
        //    Assembly ass = typeof(Aggregate).Assembly;
        //    Type type = ass.GetTypes().First(t => t.Name == componentName.Replace(" ", string.Empty));

        //    return (Vertex)Activator.CreateInstance(type, componentMetaData);
        //}
    }
}
        
