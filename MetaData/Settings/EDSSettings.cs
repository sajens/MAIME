using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MetaData.Settings
{
    public class EDSSettings
    {
        public string Name;
        public string ConnectionString;
        private SqlConnectionStringBuilder sqlConnectionStringBuilder;

        public EDSSettings(string name, string connectionString)
        {
            Name = name;
            ConnectionString = connectionString;

            sqlConnectionStringBuilder = new SqlConnectionStringBuilder(ConnectionString);
        }

        [JsonIgnore]
        public string InitialCatalog
        {
            get
            {
                return sqlConnectionStringBuilder.InitialCatalog;
            }
        }

        [JsonIgnore]
        public string DataSource
        {
            get
            {
                return sqlConnectionStringBuilder.DataSource;
            }
        }
    }
}
