using System;

namespace MetaData.MetaData.Tools
{
    public static class DataTableTools
    {
        /// <summary>
        /// When fetching metadata from the database, all rows without a value is of the type DBNull
        /// To ease the reading of these values, this function checks if it is DBNull and if so
        /// create a new Object/Value from the default. So string would be value = new string(), int would be int = 0
        /// It also ensures that the value is properly cast from object to T
        /// This handles both reference- and value-types
        /// </summary>
        /// <typeparam name="T">Type of object/value to expect</typeparam>
        /// <param name="value">Value to retrieve and cast</param>
        /// <returns>Casted value</returns>
        public static T GetValue<T>(object value)
        {
            return (T) (value.GetType() != typeof (DBNull) ? value : default(T));
        }
    }
}