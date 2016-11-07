using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseModifier
{
    public static class DataTypeConversion
    {
        private static int[,] PossibleConversion = new int[,]
        {
            {9, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1}, //0 binary
            {1, 9, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1}, //1 varbinary
            {2, 2, 9, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1}, //2 char
            {2, 2, 1, 9, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1}, //3 varchar
            {2, 2, 1, 1, 9, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 0, 1, 1, 1, 1, 1, 1}, //4 nchar
            {2, 2, 1, 1, 1, 9, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 0, 1, 1, 1, 1, 1, 1}, //5 nvarchar - done
            {2, 2, 1, 1, 1, 1, 9, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 1, 0, 0, 0}, //6 datetime
            {2, 2, 1, 1, 1, 1, 1, 9, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 1, 0, 0, 0}, //7 smalldatetime
            {2, 2, 1, 1, 1, 1, 1, 1, 9, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0}, //8 date
            {2, 2, 1, 1, 1, 1, 1, 1, 0, 9, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0}, //9 time
            {2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 9, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0}, //10 datetimeoffset
            {2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0}, //11 datetime2
            {2, 2, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0}, //12 decimal
            {1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0}, //13 numeric
            {1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 9, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0}, //14 float
            {1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 9, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0}, //15 real
            {1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 9, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0}, //16 bigint
            {1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 9, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0}, //17 int(int4)
            {1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 9, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0}, //18 smallint(int2)
            {1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 9, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0}, //19 tinyint(int1)
            {1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 9, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0}, //20 money
            {1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 9, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0}, //21 smallmoney
            {1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 9, 1, 0, 0, 0, 0, 1, 0, 0, 0}, //22 bit
            {1, 1, 1, 1, 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 9, 0, 1, 0, 0, 0, 0, 0, 0}, //23 timestamp
            {1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 1, 0, 0, 0}, //24 uniqueidentifier
            {1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 9, 0, 0, 0, 0, 0, 0}, //25 image
            {0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 1, 0, 1, 0, 0}, //26 ntext
            {0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 9, 0, 1, 0, 0}, //27 text
            {2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 2, 0, 0, 0, 9, 0, 0, 0}, //28 sql_variant
            {2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 1, 0}, //29 xml
            {2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 9, 0}, //30 CLR UDT
            {2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9}  //31 hierarchyid
        };

        private static int[] Mapping = new[]
        {
            99, 16, 0, 22, 2, 99, 6, 12, 14, 25, 17, 20, 4, 26, 5, 5, 15, 7, 18, 21, 27, 23, 19, 24, 99, 30, 99, 99, 1,
            1, 3, 3, 28, 29, 99, 13, 8, 9, 10, 11, 99, 31, 99, 99
        };


        public static void Test()
        {
            for (int i = 0; i < 43; i++)
            {
                SqlDataType type = (SqlDataType) i;
                var name = type.ToString();
                Console.WriteLine($"Name: {name}:{Mapping[i]}");
            }

            int[] dataTypeRow = GetRow(PossibleConversion, Mapping[(int) SqlDataType.NVarChar]);

            List<int> convertableTypesByIndex = new List<int>();
            var list = dataTypeRow.ToList();

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == 1)
                    convertableTypesByIndex.Add(i);
            }

            List<SqlDataType> possibleDataTypes = new List<SqlDataType>();
            var listMapping = Mapping.ToList();

            foreach (var indexInt in convertableTypesByIndex)
            {
                foreach (var source in listMapping)
                {
                    if (source == indexInt)
                    {
                        possibleDataTypes.Add((SqlDataType) listMapping.IndexOf(source));
                    }
                }

            }

            possibleDataTypes = possibleDataTypes.Distinct().ToList();

            Console.WriteLine();
        }

        public static SqlDataType RetrieveRandomCompatibleDataType(SqlDataType dataType)
        {
            int[] dataTypeRow = GetRow(PossibleConversion, Mapping[(int)dataType]);

            List<int> convertableTypesByIndex = new List<int>();
            var list = dataTypeRow.ToList();

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == 1)
                    convertableTypesByIndex.Add(i);
            }

            List<SqlDataType> possibleDataTypes = new List<SqlDataType>();
            var listMapping = Mapping.ToList();

            foreach (var indexInt in convertableTypesByIndex)
            {
                foreach (var source in listMapping)
                {
                    if (source == indexInt)
                    {
                        possibleDataTypes.Add((SqlDataType)listMapping.IndexOf(source));
                    }
                }

            }

            // TODO: Could not find suitable change
            if (possibleDataTypes.Count == 0)
                return dataType;

            possibleDataTypes = possibleDataTypes.Distinct().ToList();

            //foreach (var possibleDataType in possibleDataTypes)
            //{
            //    Console.WriteLine(possibleDataType.ToString());
            //}
            Random ran = new Random();
            return possibleDataTypes[ran.Next(possibleDataTypes.Count)];
        }

        private static bool IsDatatypeConvertable(SqlDataType from, SqlDataType to)
        {
            int i = Mapping[(int) from];
            int j = Mapping[(int) to];

            if (PossibleConversion[i, j] == 1 || PossibleConversion[i, j] == 2)
                return true;
            return false;
        }

        private static T[] GetRow<T>(T[,] matrix, int row)
        {
            var columns = matrix.GetLength(1);
            var array = new T[columns];
            for (int i = 0; i < columns; ++i)
                array[i] = matrix[row, i];
            return array;
        }


    }
}
