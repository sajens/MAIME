using System;
using System.Collections.Generic;
using System.Linq;
using MetaData.MetaData;
using SamyazaSSIS.Transformations;

namespace SamyazaSSIS.Options
{
    /// <summary>
    /// Options, which is assignable and useable when repairing SSIS job
    /// </summary>
    public class Options
    {
        // Default Policy set on initialization
        const Policy DEFAULT_POLICY = Policy.PROPAGATE;

        public Dictionary<ColumnChanges, Dictionary<Type, Policy>> PolicyTable;

        public bool AllowDeletionOfVertices; // Should we be allowed the deletion of vertices in the graph
        public bool AllowModificationOfExpressions; // May we change expressions
        public bool UseGlobalBlockingSemantics; // May we perform changes, and stop for example half way through


        public Options()
        {
            PolicyTable = new Dictionary<ColumnChanges, Dictionary<Type, Policy>>(4);

            List<Type> transformationTypes = new List<Type>
            {
                typeof (Aggregate),
                typeof (ConditionalSplit),
                typeof (DataConversion),
                typeof (DerivedColumn),
                typeof (Lookup),
                typeof (OLEDBDestination),
                typeof (OLEDBSource),
                typeof (Sort)
            };

            PolicyTable[ColumnChanges.Addition] = transformationTypes.ToDictionary(x => x, x => DEFAULT_POLICY);
            PolicyTable[ColumnChanges.Deletion] = transformationTypes.ToDictionary(x => x, x => DEFAULT_POLICY);
            PolicyTable[ColumnChanges.Rename] = transformationTypes.ToDictionary(x => x, x => DEFAULT_POLICY);
            PolicyTable[ColumnChanges.DataType] = transformationTypes.ToDictionary(x => x, x => DEFAULT_POLICY);
        }
    }
}
