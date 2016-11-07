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
using Application = Microsoft.SqlServer.Dts.Runtime.Application;
using Package = Microsoft.SqlServer.Dts.Runtime.Package;

namespace SamyazaSSIS
{
    public static class Maime
    {
        /// <summary>
        /// Reference to current SSIS-related application
        /// </summary>
        private static Application _application;

        /// <summary>
        /// Dictionary of latest metadata changes
        /// Key is the connectionstring used for the EDS
        /// </summary>
        private static Dictionary<string, DatabaseMetaChange> _latestChanges;

        //TODO: Using this is a hotfix and the program will therefore only work with one EDS
        private static DatabaseMetaChange _databaseMetaChange;

        private static string _projectPathFolder;
        private static string _dataFolder;

        /// <summary>
        /// Options specifying how Maime shall repair etl jobs
        /// </summary>
        private static Options.Options _options;

        /// <summary>
        /// Initialize Maime with Options
        /// </summary>
        /// <param name="options">Options used for reparation</param>
        public static void Init(Options.Options options)
        {
            Logger.Common("Maime initializing");
            _options = options;

            // If debugger attached, then locate folders in solution folder.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                _projectPathFolder = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()));
                _dataFolder = Path.GetFullPath(Path.Combine(_projectPathFolder, "..\\", "Data"));
            }
            else // Else locate in same folder as .exe file
            {
                _projectPathFolder = AppDomain.CurrentDomain.BaseDirectory;
                _dataFolder = Path.GetFullPath(Path.Combine(_projectPathFolder, "Data"));
            }

            Logger.Common($"Settings stored at {_projectPathFolder}");
            Logger.Common($"Data folder location {_dataFolder}");

            // ======== Settings ======== //
            Logger.Common("Loading settings");
            SettingsStore.Init(
                new JsonSettingsProvider(_dataFolder),
                new JsonEDSProvider()
                );
            Logger.Common("Settings loaded");

            // ======== Metadata store ======== //
            Logger.Common("Initializing metadata store");
            MetaDataStore.Init(new JSONSnapshotProvider());
            Logger.Common("Metadata store initialized");

            _application = new Application();

            // ======== Fetch Changes ======== //
            Logger.Common("Fetching latest metadata changes");
            _latestChanges = MetaDataStore.GetLatestChanges(SettingsStore.EDSSettings);

            if (_latestChanges.Count == 0)
            {
                Logger.Error("No database(s) or change(s) found. Please create a snapshot and rerun the program.");
                _databaseMetaChange = null;
                return;
            }

            _databaseMetaChange = _latestChanges.Values.First();
            Logger.Common("Finished fetching metadata changes");

            Logger.Common("Maime initialized");
        }

        /// <summary>
        /// Constructs a graph given a name
        /// </summary>
        /// <param name="name">Name of package</param>
        /// <returns>Graph</returns>
        public static Graph CreateGraph(string name)
        {
            Logger.Common($"Creating graph {name}");
            Package package = _application.LoadPackage(NameToPath(name), null);
            return new Graph(_application, package, name, _options);
        }

        /// <summary>
        /// Performs alterations on graph
        /// </summary>
        /// <param name="graph">Graph</param>
        /// <returns>True = success, false = failure</returns>
        public static bool AlterGraph(Graph graph)
        {
            Logger.Common($"<-- {graph.FileName} -->");

            // Loop through all column meta changes
            foreach (TableMetaChange tableMetaChange in _databaseMetaChange.Tables.Values)
            {
                foreach (ColumnMetaChange columnMetaChange in tableMetaChange.Columns.Values.Where(c => c.ColumnChanges > 0))
                {
                    Logger.Common($"Graph {graph.FileName} with changes {columnMetaChange.ColumnChanges} on table {tableMetaChange.OldTable?.Name}");

                    try
                    {
                        foreach (ColumnChanges change in columnMetaChange.ListChanges())
                        {
                            graph.Alter(columnMetaChange, change);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"[AlterGraph]{e.Message}\n{e.StackTrace}");
                        return false;
                    }
                }
            }

            // Validate package
            DTSExecResult dtsExecResult = graph.Package.Validate(graph.Package.Connections, graph.Package.Variables, new PackageValidateErrorEvent(), null);

            // Save package
            try
            {
                string path = NameToSavePath(graph.FileName);

                // If output folder doesn't not exist, then create it
                if (!Directory.Exists(Path.GetFullPath(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                _application.SaveToXml(path, graph.Package, null);

                Logger.Common($"{graph.FileName} saved to {path}");
            }
            catch (Exception e)
            {
                Logger.Error($"[SaveXML]{e.Message}");
                return false;
            }

            if (dtsExecResult == DTSExecResult.Failure)
            {
                return false;
            }

            Logger.Common($"</-- {graph.FileName} -->");
            return true;
        }

        /// <summary>
        /// Prepend location of package folder to package name
        /// </summary>
        /// <param name="name">Package name</param>
        /// <returns>Location of package</returns>
        private static string NameToPath(string name)
        {
            return Path.Combine(SettingsStore.Settings.locations.Packages, name)+".dtsx";
        }

        /// <summary>
        /// Prepend location of output package folder to package name
        /// </summary>
        /// <param name="name">Package name</param>
        /// <returns>Location of package</returns>
        private static string NameToSavePath(string name)
        {
            return Path.Combine(SettingsStore.Settings.locations.OutputPackages, name) + ".dtsx";
        }

        /// <summary>
        /// Used when validation, is called when an error occurs
        /// </summary>
        private class PackageValidateErrorEvent : DefaultEvents
        {
            public override bool OnError(DtsObject source, int errorCode, string subComponent, string description, string helpFile, int helpContext,
                string idofInterfaceWithError)
            {
                Logger.Error($"[Validate] Source: {source}\nSubComponent: {subComponent}\nDescription {description}");
                return false;
            }
        }
    }
}
