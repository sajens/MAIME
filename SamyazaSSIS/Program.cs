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
using Microsoft.SqlServer.Dts.Runtime;
using SamyazaSSIS.Visual;
using Application = Microsoft.SqlServer.Dts.Runtime.Application;
using DTSExecResult = Microsoft.SqlServer.Dts.Runtime.DTSExecResult;
using Package = Microsoft.SqlServer.Dts.Runtime.Package;

namespace SamyazaSSIS
{

    /// <summary>
    /// Entry point, used when testing without GUI attached
    /// </summary>
    public class Program
    {
        private static readonly string ProjectPathFolder = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()));
        private static readonly string TestDataFolder = Path.GetFullPath(Path.Combine(ProjectPathFolder, "..\\", "Data"));
        private static readonly string TestOutputFolder = Path.GetFullPath(Path.Combine(ProjectPathFolder, "..\\", "Output"));
        private static readonly string SSISFolder = Path.GetFullPath(Path.Combine(TestDataFolder, "packages"));

        private static DatabaseMetaChange _databaseMetaChange;
        public static VisualizedGraph VisualGraph = new VisualizedGraph();

        static Program()
        {
            SettingsStore.Init(
                new JsonSettingsProvider(TestDataFolder),
                new JsonEDSProvider()
                );

            MetaDataStore.Init(new JSONSnapshotProvider());
        }

        static void Main(string[] args)
        {
            string testDTSX = "LargeExample.dtsx";

            string path = Path.Combine(SSISFolder, testDTSX);
            // Traverse through every single EDS and save a snapshot of it
            //foreach (EDSSettings EDS in SettingsStore.EDSSettings.Where(e => e.Name != "Template"))
            //{
            //    MetaDataSnapshot metaDataSnapshot = new MetaDataSnapshot(EDS.ConnectionString);

            //    Console.WriteLine(metaDataSnapshot);

            //    MetaDataStore.Provider.SaveSnapshot(EDS, metaDataSnapshot);
            //}


            // Key = EDS connection string
            Dictionary<string, DatabaseMetaChange> latestChanges = MetaDataStore.GetLatestChanges(SettingsStore.EDSSettings);

            _databaseMetaChange = latestChanges.Values.First();   //TODO: change this later.

            // Print out changes
            Console.WriteLine(_databaseMetaChange);

            // Create a new application to host packages, and load a package within it
            Application application = new Application();
            Package package = application.LoadPackage(path, null);

            Graph g = new Graph(application, package, testDTSX, new Options.Options())
            {
                Options = new Options.Options()
            };

            // Print attributes, which is used in graph
            List<Dictionary<string, Attribute>> list = g.AttributeTable.Values.SelectMany(d => d.Values).ToList();
            List<Attribute> attributes = list.SelectMany(d => d.Values).ToList();
            string s = string.Join("\n", attributes.Select(a => $"  -{a.Id}/{a.AttributeRef?.ID}: {a.Name}"));
            Console.WriteLine(s);

            // Iterate through all meta changes
            foreach (TableMetaChange tableMetaChange in _databaseMetaChange.Tables.Values)
            {
                foreach (ColumnMetaChange columnMetaChange in tableMetaChange.Columns.Values.Where(c => c.ColumnChanges > 0))
                {
                    foreach (ColumnChanges change in columnMetaChange.ListChanges())
                    {
                        g.Alter(columnMetaChange, change);
                    }
                }
            }

            // Validate package before saving it
            package.Validate(package.Connections, package.Variables, new PackageValidateErrorEvent(), null);

            // Save package
            application.SaveToXml(Path.Combine(TestOutputFolder, "packages", testDTSX), package, null);

            Console.WriteLine("Done");
            Console.ReadKey();
        }

        // Class called, when errors occur while validating a package
        private class PackageValidateErrorEvent : DefaultEvents
        {
            public override bool OnError(DtsObject source, int errorCode, string subComponent, string description, string helpFile, int helpContext,
                string idofInterfaceWithError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Source: {source} \n SubComponent: {subComponent} \n Description {description}");
                Console.ResetColor();
                return false;
            }
        }
    }
}
