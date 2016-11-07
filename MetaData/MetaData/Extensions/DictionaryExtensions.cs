using System;
using System.Collections.Generic;
using MetaData.MetaData.Items;

namespace MetaData.MetaData.Extensions
{
    public static class DictionaryExtensions
    {

        /// <summary>
        /// Merges the keys of two dictionaries. The result is a dictionary of every single key, and the value is a VersionItem
        /// containing the two different versions old/first and new/second
        /// </summary>
        /// <typeparam name="TK">Type of the key</typeparam>
        /// <typeparam name="TV">Type of the value</typeparam>
        /// <param name="first">First dictionary to use</param>
        /// <param name="second">Second dictionary to use</param>
        /// <returns>A dictionary of keys and VersionItems containing the two different versions</returns>
        public static IDictionary<TK, VersionItem<TV>> MergeKeys<TK, TV>(IDictionary<TK, TV> first,
            IDictionary<TK, TV> second) where TV : class
        {
            // Create dictionary with the length of the longest input dictionary
            var res = new Dictionary<TK, VersionItem<TV>>(Math.Max(first.Count, second.Count));

            // Populate new dictionary with all the keys of the first dictionary
            foreach (var pair in first)
            {
                res[pair.Key] = new VersionItem<TV>(pair.Value, null);
            }

            // Then populate with the keys of the second dictionary
            foreach (var pair in second)
            {
                // If key already exists, then add value to the new part of the VersionItem
                if (res.ContainsKey(pair.Key))
                    res[pair.Key] = new VersionItem<TV>(res[pair.Key].Old, pair.Value);
                else
                    res[pair.Key] = new VersionItem<TV>(null, pair.Value);
            }

            return res;
        }
    }
}