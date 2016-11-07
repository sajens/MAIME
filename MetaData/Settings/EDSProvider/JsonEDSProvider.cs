using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaData.MetaData.MetaStore;
using Newtonsoft.Json;

namespace MetaData.Settings.EDSProvider
{
    public class JsonEDSProvider : IEDSProvider
    {
        private string GetLocation()
        {
            return SettingsStore.Settings.locations.EDSes;
        }

        public List<EDSSettings> GetSettings()
        {
            List<EDSSettings> edsSettings = new List<EDSSettings>();

            DirectoryInfo dir = new DirectoryInfo(GetLocation());

            if(!dir.Exists)
                return edsSettings;

            foreach (FileInfo fileInfo in dir.GetFiles("*.json"))
            {
                edsSettings.Add(LoadSettings(fileInfo.FullName));
            }

            return edsSettings;
        }

        /// <summary>
        /// Save EDSsettings to file
        /// </summary>
        /// <param name="EDSSettings"></param>
        public void SaveSettings(EDSSettings settings)
        {
            string filename = $"{settings.Name}.json";
            var path = Path.Combine(GetLocation(), filename);

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

        public List<EDSSettings> CreateTemplate()
        {
            EDSSettings EDSSettings = new EDSSettings("Template", "Data Source=myServerAddress;Initial Catalog=myDataBase; User Id=myUsername; Password=myPassword");
            SaveSettings(EDSSettings);

            return new List<EDSSettings> {EDSSettings};
        }

        /// <summary>
        /// Load EDS settings from JSON file
        /// </summary>
        /// <param name="filename">Full path to EDS settings</param>
        /// <returns>EDSSettings</returns>
        private EDSSettings LoadSettings(string filename)
        {
            // Open file, read text and deserialize.
            using (var file = File.Open(filename, FileMode.Open, FileAccess.Read))
            using (TextReader reader = new StreamReader(file))
            {
                var jsonString = reader.ReadToEnd();
                var snapshot = JsonConvert.DeserializeObject<EDSSettings>(jsonString);
                return snapshot;
            }
        }
    }
}
