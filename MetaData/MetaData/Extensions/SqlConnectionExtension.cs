using System.Data;
using System.Data.SqlClient;

namespace MetaData.MetaData.Extensions
{
    public static class SqlConnectionExtension
    {
        //reference link: https://support.microsoft.com/en-us/kb/309681
        // restrictions: {TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE}

        /// <summary>
        /// Get databases for a specific sql connection
        /// </summary>
        /// <param name="conn">Sql Connection</param>
        /// <returns>DataTable containing the result</returns>
        public static DataTable GetDatabase(this SqlConnection conn)
        {
            var sqlCommand = conn.CreateCommand();

            sqlCommand.CommandText = "SELECT * FROM sys.databases " +
                                     $"WHERE name = '{conn.Database}';";

            DataTable data;
            using (var reader = sqlCommand.ExecuteReader())
            {
                data = new DataTable();
                data.Load(reader);
            }

            return data;
        }

        /// <summary>
        /// Get tables for a specific sql connection.
        /// </summary>
        /// <param name="conn">Sql Connection</param>
        /// <returns>DataTable containing the result</returns>
        public static DataTable GetTables(this SqlConnection conn)
        {
            var sqlCommand = conn.CreateCommand();

            // Selects metadata from database.
            // type = 'U': User table, aka. a table created by the user.
            // name: Name of table to pick
            //sqlCommand.CommandText = $"SELECT * FROM sys.sysobjects WHERE type = 'U'";
            sqlCommand.CommandText = $"SELECT '['+SCHEMA_NAME(schema_id)+'].['+name+']' AS full_name, name, object_id AS id FROM sys.tables";

            DataTable data;
            using (var reader = sqlCommand.ExecuteReader())
            {
                data = new DataTable();
                data.Load(reader);
            }

            return data;
        }

        public static DataTable GetPrimaryKeys(this SqlConnection conn)
        {
            string command = "SELECT *, " +
                             "COLUMNPROPERTY(OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'ColumnID') AS COLUMN_ID " +
                             "FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE " +
                             "WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1 ";

            return ExecureSqlCommand(conn, command);
        }

        public static DataTable GetUniqueColumns(this SqlConnection conn)
        {
            string command = "SELECT *, " +
                             "COLUMNPROPERTY(OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'ColumnID') AS COLUMN_ID " +
                             "FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE " +
                             "WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsUniqueCnst') = 1 ";

            return ExecureSqlCommand(conn, command);
        }

        public static DataTable GetPrimaryKeys(this SqlConnection conn, Table table)
        {
            string command = "SELECT *, " +
                             "COLUMNPROPERTY(OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'ColumnID') AS COLUMN_ID " +
                             "FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE " +
                             "WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1 " +
                             $"AND TABLE_NAME = '{table.Name}'";

            return ExecureSqlCommand(conn, command);
        }

        public static DataTable GetUniqueColumns(this SqlConnection conn, Table table)
        {
            string command = "SELECT *, " +
                             "COLUMNPROPERTY(OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'ColumnID') AS COLUMN_ID " +
                             "FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE " +
                             "WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsUniqueCnst') = 1 " +
                             $"AND TABLE_NAME = '{table.Name}'";

            return ExecureSqlCommand(conn, command);
        }

        /// <summary>
        /// Get the columns of a specific table
        /// </summary>
        /// <param name="conn">Sql connection</param>
        /// <param name="table">Table to get columns from</param>
        /// <returns>DataTable containing the results</returns>
        public static DataTable GetColumns(this SqlConnection conn, Table table)
        {
            var sqlCommand = conn.CreateCommand();

            // Get all information about the columns of a table, including the unique ID of the column
            sqlCommand.CommandText = "SELECT " +
                                     "*, " +
                                     "COLUMNPROPERTY(OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'ColumnID') AS COLUMN_ID " +
                                     $"FROM {conn.Database}.INFORMATION_SCHEMA.COLUMNS " +
                                     $"WHERE TABLE_NAME = '{table.Name}'";

            DataTable data;
            using (var reader = sqlCommand.ExecuteReader())
            {
                data = new DataTable();
                data.Load(reader);
            }

            return data;
        }

        private static DataTable ExecureSqlCommand(SqlConnection conn, string command)
        {
            var sqlCommand = conn.CreateCommand();

            // Get all information about the columns of a table, including the unique ID of the column
            sqlCommand.CommandText = command;

            DataTable data;
            using (var reader = sqlCommand.ExecuteReader())
            {
                data = new DataTable();
                data.Load(reader);
            }

            return data;
        }
    }
}