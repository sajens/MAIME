using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using MetaData.MetaData.MetaStore.Providers;
using MetaData.Settings;

namespace MetaData.MetaData.MetaStore
{
    public class MetaDataStore
    {
        public static IMetaDataProvider Provider;

        public static void Init(IMetaDataProvider provider)
        {
            Provider = provider;
        }

        /// <summary>
        /// Get a dictionary containing changes form all supplied EDSs
        /// </summary>
        /// <param name="edsList">List of EDSs to include</param>
        /// <returns>Dictionary of EDS connectionstring and DatabaseMetaChange</returns>
        public static Dictionary<string, DatabaseMetaChange> GetLatestChanges(IEnumerable<EDSSettings> edsList)
        {
            Dictionary<string, DatabaseMetaChange> changes = new Dictionary<string, DatabaseMetaChange>();

            foreach (EDSSettings eds in edsList)
            {
                if(eds.Name.Equals("Template"))
                    continue;

                changes[eds.ConnectionString] =
                    new MetaDataSnapshot(eds.ConnectionString).CompareVerions(Provider.GetLatest(eds));
            }

            return changes;
        }
    }
}
