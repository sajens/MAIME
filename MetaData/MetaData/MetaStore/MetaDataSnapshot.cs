using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using MetaData.MetaData.Extensions;

namespace MetaData.MetaData.MetaStore
{
    public class MetaDataSnapshot
    {
        public DataBase Database;
        public DateTime creationDate;

        public MetaDataSnapshot()
        {
        }

        public MetaDataSnapshot(string connectionString)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                GetDatabase(conn);
            }

            creationDate = DateTime.Now;
        }

        private void GetDatabase(SqlConnection conn)
        {
            DataTable dataTable = conn.GetDatabase();

            Database = new DataBase(conn, dataTable.Rows[0]);
        }

        public DatabaseMetaChange CompareVerions(MetaDataSnapshot old)
        {
            return DataBase.CompareVersions(old.Database, Database);
        }

        public override string ToString()
        {
            return DebugPrint();
        }

        public string DebugPrint(int depth = 0)
        {
            StringBuilder str = new StringBuilder();

            str.AppendLine(Database.DebugPrint());

            return str.ToString();
        }
    }
}
