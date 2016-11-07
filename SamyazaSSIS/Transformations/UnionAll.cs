using System;
using System.Collections.Generic;
using MetaData.MetaData;

namespace SamyazaSSIS.Transformations
{
    // TODO: Only partially implemented, will not work
    class UnionAll : Vertex
    {
        public List<InputTuple> Inputs;
        public List<UnionTuple> Unions;

        public UnionAll(Graph graph) : base(graph)
        {
            Inputs = new List<InputTuple>();
            Unions = new List<UnionTuple>();
        }

        public class InputTuple
        {
            public List<Attribute> Table;
        }

        public class UnionTuple
        {
            public Attribute OutputAttributes;
            public List<Attribute> InputAttributes;
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
