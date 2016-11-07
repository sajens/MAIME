using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MetaData.Settings;
using Newtonsoft.Json;

namespace MetaData.MetaData.MetaStore.Providers
{
    /// <summary>
    /// Load and store MetadataSnapshot as JSON files.
    /// </summary>
    public class JSONSnapshotProvider : IMetaDataProvider
    {
        public string GetLocation(EDSSettings eds)
        {
            return Path.Combine(SettingsStore.Settings.locations.Snapshots, eds.Name); 
        }

        /// <summary>
        /// Get latest snapshot based on filename.
        /// </summary>
        /// <returns></returns>
        public MetaDataSnapshot GetLatest(EDSSettings eds)
        {
            DirectoryInfo dir = new DirectoryInfo(GetLocation(eds));

            if(!dir.Exists)
                throw new InvalidOperationException($"No snapshots for EDS named '{eds.Name}' was found.");

            FileInfo fileInfo = dir.GetFiles().OrderByDescending(p => p.Name).FirstOrDefault();

            return LoadSnapshot(fileInfo.FullName);
        }

        public MetaDataSnapshot GetSnapshot(EDSSettings eds, DateTime dateTime)
        {
            string fileName = Path.Combine(GetLocation(eds), $"snapshot-{dateTime:yyyy-MM-dd_HH-mm}.json");

            if (!File.Exists(fileName))
                throw new InvalidOperationException($"No snapshot named '{fileName}' was found.");

            return LoadSnapshot(fileName);
        }

        /// <summary>
        /// Get all snapshots
        /// </summary>
        /// <returns>List of snapshots, sorted in chronological order</returns>
        public List<MetaDataSnapshot> GetAllSnapshots(EDSSettings eds)
        {
            List<MetaDataSnapshot> snapshots = new List<MetaDataSnapshot>();

            DirectoryInfo dir = new DirectoryInfo(GetLocation(eds));
            foreach (FileInfo fileInfo in dir.GetFiles().OrderBy(p => p.Name))
            {
                snapshots.Add(LoadSnapshot(fileInfo.FullName));
            }

            return snapshots;
        }

        /// <summary>
        /// Get snaphots before datetime
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns>List of snaphots, sorted in chronological order</returns>
        public List<MetaDataSnapshot> GetSnapshotsBefore(EDSSettings eds, DateTime dateTime)
        {
            List<MetaDataSnapshot> snapshots = new List<MetaDataSnapshot>();

            DirectoryInfo dir = new DirectoryInfo(GetLocation(eds));
            foreach (FileInfo fileInfo in dir.GetFiles().OrderBy(p => p.Name))
            {
                // Get datetime part of filename
                string name = fileInfo.Name.Substring(9, 16);

                DateTime time = DateTime.ParseExact(name, "yyyy-MM-dd_HH-mm", CultureInfo.InvariantCulture);

                // If datetime is after specificed datetime, then skip since the rest is older
                if (time > dateTime)
                    break;

                snapshots.Add(LoadSnapshot(fileInfo.FullName));
            }

            return snapshots;
        }

        /// <summary>
        /// Load snapshot from JSON file
        /// </summary>
        /// <param name="filename">Full path to snapshot</param>
        /// <returns>MetaDataSnapshot</returns>
        private MetaDataSnapshot LoadSnapshot(string filename)
        {
            // Open file, read text and deserialize.
            using (var file = File.Open(filename, FileMode.Open, FileAccess.Read))
            using (TextReader reader = new StreamReader(file))
            {
                var jsonString = reader.ReadToEnd();
                var snapshot = JsonConvert.DeserializeObject<MetaDataSnapshot>(jsonString);
                return snapshot;
            }
        }

        /// <summary>
        /// Save snapshot to file
        /// </summary>
        /// <param name="metaDataSnapshot"></param>
        public void SaveSnapshot(EDSSettings eds, MetaDataSnapshot metaDataSnapshot)
        {
            // Eg.: snapshot-2000-01-15_13:52.json
            string filename = $"snapshot-{metaDataSnapshot.creationDate:yyyy-MM-dd_HH-mm}.json";
            var path = Path.Combine(GetLocation(eds), filename);

            // Create directory if it isn't present
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // Create file if not existing, and serialize database to file.
            using (var file = File.Create(path))
            using (TextWriter writer = new StreamWriter(file))
            {
                JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
                jsonSerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                var o = JsonConvert.SerializeObject(metaDataSnapshot, Formatting.Indented, jsonSerializerSettings);
                writer.Write(o);
            }
        }
    }
}
