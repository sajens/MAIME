using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using MetaData;

namespace DatabaseModifier
{
    class Program
    {
        private static string DBName = "DatabaseTesting";
        private static string DWName = "DataWarehouseTesting";
        private static ServerConnection conn = new ServerConnection("172.25.23.78", "D802f16", "D802f16");
        private static Server Server = new Server(conn);
        private static Database DB;
        private static Random Ran = new Random();

        private static List<string> ChangesMade = new List<string>();


        private static Dictionary<SqlDataType, List<SqlDataType>> CompatibleDatatypes = new Dictionary<SqlDataType, List<SqlDataType>>
        {
            [SqlDataType.BigInt] = new List<SqlDataType> { SqlDataType.Money, SqlDataType.Binary, SqlDataType.Decimal, SqlDataType.Numeric, SqlDataType.Float, SqlDataType.Int, SqlDataType.TinyInt },
            [SqlDataType.VarChar] = new List<SqlDataType> { SqlDataType.Char, SqlDataType.NChar, SqlDataType.NVarChar },
            [SqlDataType.NVarChar] = new List<SqlDataType> { SqlDataType.Char, SqlDataType.NChar, SqlDataType.VarChar },
            [SqlDataType.Int] = new List<SqlDataType> { SqlDataType.Money, SqlDataType.Char, SqlDataType.NChar, SqlDataType.VarChar, SqlDataType.NVarChar, SqlDataType.Binary, SqlDataType.Decimal, SqlDataType.Numeric, SqlDataType.Float, SqlDataType.Int, SqlDataType.TinyInt },
            [SqlDataType.Money] = new List<SqlDataType> { SqlDataType.Char, SqlDataType.NChar, SqlDataType.VarChar, SqlDataType.Binary, SqlDataType.Decimal, SqlDataType.Numeric, SqlDataType.Float, SqlDataType.Int, SqlDataType.TinyInt }
        };

        static void Main(string[] args)
        {

            //DropAndCreateDBandDW();
            //DB = Server.Databases[DBName];
            //PrintDB();
            //Console.WriteLine("===================");
            ////RenameRandomColumn();
            ////DeleteRandomColumn();
            ////AddRandomColumn();
            ////ChangeDatatypeRandomColumn();
            ////RenameAndDatatypeChangeRandomColumn();

            ////DeleteRandomTable();
            ////DeleteRandomDB();
            //Console.WriteLine("===================");

            UserDefinedChanges();

            Console.WriteLine("Done");
            Console.ReadKey();
        }

        private static void ReplaceExpression(string input, string oldName, string newName)
        {
            string pattern = @"\b" + oldName + @"\b";

            string output = Regex.Replace(input, pattern, newName);

            Console.WriteLine($"{input} \t {output}");
        }

        private static void UserDefinedChanges(string str = "")
        {
            DB = Server.Databases[DBName];
            PrintDB();
            PrintInstructions();

            bool notDone = true;
            while (notDone)
            {
                string input;
                input = str == "" ? Console.ReadLine() : str;

                string command;
                string target = "";
                if (input.Contains(" "))
                {
                    command = input.Substring(0, input.IndexOf(" "));
                    target = input.Substring(input.IndexOf(" ") + 1, (input.Length - 1) - input.IndexOf(" "));
                }
                else
                {
                    command = input;
                }

                switch (command.ToLower())
                {
                    case "add":
                    case "a":
                        AddColumn(target);
                        break;
                    case "delete":
                    case "del":
                    case "d":
                        DeleteColumn(target);
                        break;
                    case "rename":
                    case "r":
                        RenameColumn(target);
                        break;
                    case "dtc":
                        DatatypeChange(target);
                        PrintDB();
                        break;
                    case "reset":
                        DropAndCreateDBandDW();
                        ChangesMade = new List<string>();
                        Server = new Server(conn);
                        DB = Server.Databases[DBName];
                        break;
                    case "save":
                        MetaData.Program.SaveSnapshot();
                        break;
                    case "done":
                        notDone = false;
                        break;
                    default:
                        Console.Clear();
                        PrintDB();
                        PrintChanges();
                        PrintInstructions();

                        continue;
                }
                PrintChanges();
                PrintDB();
            }
        }



        private static void PrintInstructions()
        {
            Console.WriteLine("Add, Delete, Rename, or Datatypechange (DTC) - Examples:");
            Console.WriteLine("(1) Add Person \t\t Adds a random attribute to a person table");
            Console.WriteLine("(2) Delete Person.Name \t Deletes column Name in person table");
        }

        private static void PrintChanges()
        {
            if (ChangesMade.Count == 0)
                return;
            Console.WriteLine("=====================");
            foreach (var change in ChangesMade)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(change);
            }
            Console.WriteLine("=====================");
            Console.ResetColor();
        }

        private static void AddColumn(string target)
        {
            AddRandomColumn(0, target);
        }

        private static void DeleteColumn(string target)
        {
            string tableName;
            string columnName;
            GetTableNameAndColumn(target, out tableName, out columnName);
            DeleteRandomColumn(0, tableName, columnName);
        }

        private static void RenameColumn(string target)
        {
            string tableName;
            string columnName;
            GetTableNameAndColumn(target, out tableName, out columnName);
            RenameRandomColumn(0, tableName, columnName);
        }

        private static void DatatypeChange(string target)
        {
            string tableName;
            string columnName;
            GetTableNameAndColumn(target, out tableName, out columnName);
            ChangeDatatypeRandomColumn(0, tableName, columnName);
        }

        public static void ChangeDatatypeRandomColumn(int i = 0, string tableName = "", string columnName = "")
        {
            Table table;
            Column column;
            RetrieveColumn(out table, out column, tableName, columnName);

            // Get random datatype from datatype conversion
            var newDataType = DataTypeConversion.RetrieveRandomCompatibleDataType(column.DataType.SqlDataType);
            column.DataType.SqlDataType = newDataType;
            try
            {
                column.Alter();
                ChangesMade.Add($"Changed Datatype on Column: {DB.Name}.{table.Name}.{column.Name}");
            }
            catch (Microsoft.SqlServer.Management.Smo.FailedOperationException e)
            {
                Console.WriteLine($"Could not convert to: {newDataType}");
                ChangeDatatypeRandomColumn(0,tableName,columnName);
            }

            //if (CompatibleDatatypes.ContainsKey(column.DataType.SqlDataType))
            //{
            //    Console.WriteLine($"Changed Datatype on Column: {DB.Name}.{table.Name}.{column.Name}");
            //    List<SqlDataType> sqlDataTypes = CompatibleDatatypes[column.DataType.SqlDataType];
            //    column.DataType = new DataType(sqlDataTypes[Ran.Next(sqlDataTypes.Count)]);
            //    column.Alter();
            //}
            //else
            //{
            //    Console.WriteLine($"Could not find alternative datatype for: {DB.Name}.{table.Name}.{column.Name}");
            //}

            if (i > 0)
                ChangeDatatypeRandomColumn(i--);
        }

        public static void AddRandomColumn(int i = 0, string tableName = "")
        {
            Table table;
            if (tableName != "")
            {
                if (DB.Tables.Contains(tableName))
                    table = DB.Tables[tableName];
                else
                {
                    Console.WriteLine($"Could not find table {tableName}");
                    return;
                }
            }
            else
            {
                table = DB.Tables[Ran.Next(DB.Tables.Count - 1)];
            }

            Guid g = Guid.NewGuid();

            var columnName = Convert.ToBase64String(g.ToByteArray());
            columnName = Regex.Replace(columnName, @"[\W]", "").Substring(0, 6);
            Column column = new Column(table, columnName, new DataType((SqlDataType)Ran.Next(0,22)));
            column.Create();
            ChangesMade.Add($"Added Column: {DB.Name}.{table.Name}.{column.Name}");

            if(i>0)
                AddRandomColumn(i--);
        }

        public static void DeleteRandomColumn(int i = 0, string tableName = "", string columnName = "")
        {
            Table table;
            Column column;
            RetrieveColumn(out table, out column, tableName, columnName);
            ChangesMade.Add($"Deleted {DB.Name}.{table.Name}.{column.Name}");
            column.Drop();

            if (i > 0)
                DeleteRandomColumn(i--);
        }
        public static void RenameRandomColumn(int i = 0, string tableName = "", string columnName = "")
        {
            Table table;
            Column column;
            RetrieveColumn(out table, out column, tableName, columnName);
            column.Rename("Renamed" + column.Name);
            ChangesMade.Add($"Renamed Column: {DB.Name}.{table.Name}.{column.Name}");

            if (i > 0)
                RenameRandomColumn(i--);
        }

        public static void RetrieveColumn(out Table table, out Column column, string tableName = "", string columnName = "")
        {
            if (isNumeric(tableName))
                table = DB.Tables.ItemById(int.Parse(tableName));
            else
                table = tableName != "" ? DB.Tables[tableName] : DB.Tables[Ran.Next(DB.Tables.Count)];

            if (isNumeric(columnName))
                column = table.Columns.ItemById(int.Parse(columnName));
            else
                column = columnName != "" ? table.Columns[columnName] : table.Columns[Ran.Next(table.Columns.Count)];

        }

        private static bool isNumeric(string str)
        {
            int n;
            return int.TryParse(str, out n);
        }

        private static void GetTableNameAndColumn(string target, out string tableName, out string columnName)
        {
            if (!target.Contains("."))
            {
                tableName = target;
                columnName = "";
                return;
            }
            tableName = target.Substring(0, target.IndexOf("."));
            columnName = target.Substring(target.IndexOf(".") + 1, target.Length - 1 - target.IndexOf("."));
        }

       
#region Extra stuff
        public static void RenameAndDatatypeChangeRandomColumn(int i = 0)
        {
            Table table;
            Column column;
            RetrieveColumn(out table, out column);
     
            if (CompatibleDatatypes.ContainsKey(column.DataType.SqlDataType))
            {
                column.Rename("Renamed" + column.Name);
                Console.WriteLine($"Renamed Column: {DB.Name}.{table.Name}.{column.Name}");

                Console.WriteLine($"Changed Datatype on Column: {DB.Name}.{table.Name}.{column.Name}");
                List<SqlDataType> sqlDataTypes = CompatibleDatatypes[column.DataType.SqlDataType];
                column.DataType = new DataType(sqlDataTypes[Ran.Next(sqlDataTypes.Count)]);
                column.Alter();
            }
            else
            {
                Console.WriteLine($"Could not find alternative datatype for: {DB.Name}.{table.Name}.{column.Name}");
            }

            if (i > 0)
                RenameAndDatatypeChangeRandomColumn(i--);
        }
#endregion

        private static void PrintDB()
        {
            Server srv = new Server(conn);
            Database db = srv.Databases[DBName];

            foreach (Table table in db.Tables)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"T: {table.Name}");
                foreach (Column col in table.Columns)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\t C: {col.ID}.{col.Name}.{col.DataType.Name}");
                    
                }
            }
            Console.ResetColor();
        }

        public static void DropAndCreateDBandDW()
        {
            // Drop and re-Create DW
            Database database = Server.Databases[DBName];
            database?.Drop();

            Database newDB = new Database(Server, DBName);


            Table personTable = new Table(newDB, "Person");

            Column idColumn = new Column(personTable, "ID", new DataType(SqlDataType.Int));
            idColumn.Nullable = false;
            idColumn.Identity = true;
            idColumn.IdentityIncrement = 1;
            idColumn.IdentitySeed = 1;

            DataType nvarchar = new DataType(SqlDataType.NVarChar);
            nvarchar.MaximumLength = 30;
            Column nameColumn = new Column(personTable, "Name", nvarchar);
            nameColumn.Nullable = true;

            Column ageColumn = new Column(personTable, "Age", new DataType(SqlDataType.Int));
            nameColumn.Nullable = true;

            personTable.Columns.Add(idColumn);
            personTable.Columns.Add(nameColumn);
            personTable.Columns.Add(ageColumn);



            Table saleTable = new Table(newDB, "Sale");

            Column id = new Column(saleTable, "ID", new DataType(SqlDataType.Int));
            id.Nullable = false;
            id.Identity = true;
            id.IdentityIncrement = 1;
            id.IdentitySeed = 1;

            Column customerId = new Column(saleTable, "CustomerID", new DataType(SqlDataType.Int));
            customerId.Nullable = false;

            Column storeID = new Column(saleTable, "StoreID", new DataType(SqlDataType.Int));
            storeID.Nullable = false;

            Column totalAmount = new Column(saleTable, "TotalAmount", new DataType(SqlDataType.Money));
            totalAmount.Nullable = false;
            totalAmount.DataType = new DataType(SqlDataType.Money);

            saleTable.Columns.Add(id);
            saleTable.Columns.Add(customerId);
            saleTable.Columns.Add(storeID);
            saleTable.Columns.Add(totalAmount);



            Table storeTable = new Table(newDB, "Store");

            Column storeTableId = new Column(storeTable, "ID", new DataType(SqlDataType.Int));
            storeTableId.Nullable = false;
            storeTableId.Identity = true;
            storeTableId.IdentityIncrement = 1;
            storeTableId.IdentitySeed = 1;

            DataType dtAddress = new DataType(SqlDataType.NVarChar);
            dtAddress.MaximumLength = 80;
            Column addressColumn = new Column(storeTable, "Address", dtAddress);

            storeTable.Columns.Add(storeTableId);
            storeTable.Columns.Add(addressColumn);

            // TODO Add new tables here in DB, below add tables for DW


            // Drop and re-Create DW
            Database dw = Server.Databases[DWName];
            dw?.Drop();

            Database newDW = new Database(Server, DWName);


            Table lookupTable = new Table(newDW, "Person_Sale_Lookup");
            Column lookupID = new Column(lookupTable, "ID", new DataType(SqlDataType.Int));
            lookupID.Nullable = false;

            Column lookupName = new Column(lookupTable, "Name", nvarchar);
            lookupName.Nullable = true;

            Column lookupAge = new Column(lookupTable, "Age", new DataType(SqlDataType.Int));
            lookupAge.Nullable = true;

            Column lookupAmount = new Column(lookupTable, "TotalAmount", new DataType(SqlDataType.Money));
            lookupAmount.Nullable = false;

            lookupTable.Columns.Add(lookupID);
            lookupTable.Columns.Add(lookupName);
            lookupTable.Columns.Add(lookupAge);
            lookupTable.Columns.Add(lookupAmount);


            //Add & Create tables to DB
            newDB.Tables.Add(personTable);
            newDB.Tables.Add(saleTable);
            newDB.Tables.Add(storeTable);

            newDB.Create();
            personTable.Create();
            saleTable.Create();
            storeTable.Create();


            //Add & Create tables to DW
            newDW.Tables.Add(lookupTable);

            newDW.Create();
            lookupTable.Create();
        }
    }
}
