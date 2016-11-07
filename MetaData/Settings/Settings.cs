using System.IO;

namespace MetaData.Settings
{
    public class Settings
    {
        public Locations locations;

        public class Locations
        {
            public string Settings;
            public string Packages;
            public string OutputPackages;
            public string Snapshots;
            public string EDSes;

            public Locations()
            {
            }

            public Locations(string settings, string packages, string ouputPackages, string snapshots, string EDSes)
            {
                Settings = settings;
                Packages = packages;
                OutputPackages = ouputPackages;
                Snapshots = snapshots;
                this.EDSes = EDSes;
            }
        }

        public void CreateTemplate()
        {
            locations = new Locations(
                Path.Combine(Program.TestDataFolder, "settings"),
                Path.Combine(Program.TestDataFolder, "packages"),
                Path.Combine(Program.TestDataFolder, "results"),
                Path.Combine(Program.TestDataFolder, "snapshots"),
                Path.Combine(Program.TestDataFolder, "EDS")
                );
        }
    }
}
