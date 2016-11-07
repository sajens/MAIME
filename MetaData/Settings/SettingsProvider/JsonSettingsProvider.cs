using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaData.MetaData.MetaStore;
using Newtonsoft.Json;

namespace MetaData.Settings.SettingsProvider
{
    public class JsonSettingsProvider : ISettingsProvider
    {
        public string SettingsLocation;

        public JsonSettingsProvider(string settingsLocation)
        {
            SettingsLocation = settingsLocation;
        }

        /// <summary>
        /// Load settings from JSON file
        /// </summary>
        /// <returns>Settings</returns>
        public Settings Load()
        {
            string path = Path.Combine(SettingsLocation, "settings.json");

            if (!File.Exists(path))
                return null;

            // Open file, read text and deserialize.
            using (var file = File.Open(path, FileMode.Open, FileAccess.Read))
            using (TextReader reader = new StreamReader(file))
            {
                var jsonString = reader.ReadToEnd();
                var settings = JsonConvert.DeserializeObject<Settings>(jsonString);
                return settings;
            }
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        /// <param name="settings">Settings to save</param>
        public void Save(Settings settings)
        {
            string path = Path.Combine(SettingsLocation, "settings.json");

            // Create directory if it isn't present
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // Create file if not existing, and serialize database to file.
            using (var file = File.Create(path))
            using (TextWriter writer = new StreamWriter(file))
            {
                var o = JsonConvert.SerializeObject(settings, Formatting.Indented);
                writer.Write(o);
            }
        }

        public Settings CreateTemplate()
        {
            Settings settings = new Settings();
            settings.CreateTemplate();
            Save(settings);

            return settings;
        }
    }
}
