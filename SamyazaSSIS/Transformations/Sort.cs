using System;
using System.Collections.Generic;
using MetaData.MetaData;

namespace SamyazaSSIS.Transformations
{
    // TODO: Only partially implemented, will not work
    class Sort : Vertex
    {
        public List<Sorting> Sortings;

        public Sort(Graph graph) : base(graph)
        {
            Sortings = new List<Sorting>();
        }

        public class Sorting
        {
            Attribute _input;
            Attribute _output;
            SortType _sortType;
            uint _order;
        }

        public enum SortType
        {
            ASCENDING,
            DESCENDING
        }

        public override bool IsFullyDependent(Attribute attribute)
        {
            throw new NotImplementedException();
        }

        public override void MapDependenciesAndProperties()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override void DataTypeAttribute(Graph g, Attribute a, ColumnMetaChange change)
        {
            throw new NotImplementedException();
        }

        public override void CleanUpRemovedAttribute(Attribute a)
        {
            throw new NotImplementedException();
        }
    }
}
