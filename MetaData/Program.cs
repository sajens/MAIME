using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetaData.MetaData;
using MetaData.MetaData.MetaStore;
using MetaData.MetaData.MetaStore.Providers;
using MetaData.Settings;
using MetaData.Settings.EDSProvider;
using MetaData.Settings.SettingsProvider;

namespace MetaData
{
    public static class Program
    {
        public static readonly string ProjectPathFolder;
        public static readonly string TestDataFolder;
        public static readonly string TestOutputFolder;

        /// <summary>
        /// Initiate static objects
        /// </summary>
        static Program()
        {
            // If debugger attached, then locate folders in solution folder.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                ProjectPathFolder = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()));
                TestDataFolder = Path.GetFullPath(Path.Combine(ProjectPathFolder, "..\\", "Data"));
                TestOutputFolder = Path.GetFullPath(Path.Combine(ProjectPathFolder, "..\\", "Output"));
            }
            else // Else locate in same folder as .exe file
            {
                ProjectPathFolder = AppDomain.CurrentDomain.BaseDirectory;
                TestDataFolder = Path.GetFullPath(Path.Combine(ProjectPathFolder, "Data"));
                TestOutputFolder = Path.GetFullPath(Path.Combine(ProjectPathFolder, "Output"));
            }

            SettingsStore.Init(
                new JsonSettingsProvider(TestDataFolder), 
                new JsonEDSProvider()
                );
            
            MetaDataStore.Init(new JSONSnapshotProvider());
        }

        private static void Main(string[] args)
        {
            // Traverse through every single EDS and save a snapshot of it
            //SaveSnapshot();

            // Create dictionary of latest snapshot, argument is a list of relevant EDSs to include
            Dictionary<string, DatabaseMetaChange> databaseMetaChanges = MetaDataStore.GetLatestChanges(SettingsStore.EDSSettings);

            foreach (var databaseMetaChange in databaseMetaChanges.Values)
            {
                Console.WriteLine(databaseMetaChange);
            }

            Console.WriteLine("Done...");
            Console.Read();
        }

        public static void SaveSnapshot()
        {
            foreach (EDSSettings EDS in SettingsStore.EDSSettings.Where(e => e.Name != "Template"))
            {
                MetaDataSnapshot metaDataSnapshot = new MetaDataSnapshot(EDS.ConnectionString);

                Console.WriteLine(metaDataSnapshot);

                MetaDataStore.Provider.SaveSnapshot(EDS, metaDataSnapshot);
            }
        }
    }
}