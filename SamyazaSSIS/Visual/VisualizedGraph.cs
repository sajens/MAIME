using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MetaData.Settings;
using MVG = Microsoft.VisualStudio.GraphModel;

namespace SamyazaSSIS.Visual
{
    /// <summary>
    /// Classed used when create a DGML file of a given graph
    /// </summary>
    public class VisualizedGraph
    {
        public Dictionary<string, Dictionary<Type, MVG.GraphProperty>> PropertiesMap = new Dictionary<string, Dictionary<Type, MVG.GraphProperty>>();
        // For example: [Attribute][typeof(String)] = GraphProperty
        public MVG.Graph Graph = new MVG.Graph();
        public MVG.GraphPropertyCollection Properties;
        public Dictionary<int, MVG.GraphNode> LinkNodes = new Dictionary<int, MVG.GraphNode>();

        public VisualizedGraph()
        {
            Properties = Graph.DocumentSchema.Properties;
        }

        public MVG.GraphProperty GetOrAddAttribute(string attribute, Type dataType)
        {
            if (!PropertiesMap.ContainsKey(attribute))
                return CreateNewGraphProperty(attribute, dataType);

            if (!PropertiesMap[attribute].ContainsKey(dataType))
                return CreateNewGraphProperty(attribute, dataType);

            return PropertiesMap[attribute][dataType];
        }

        private MVG.GraphProperty CreateNewGraphProperty(string attribute, Type dataType)
        {
            MVG.GraphProperty genericName = Properties.AddNewProperty(attribute, dataType);
            PropertiesMap[attribute] = new Dictionary<Type, MVG.GraphProperty> { [dataType] = genericName };
            return genericName;
        }

        public void Draw(string filename)
        {
            if (!Directory.Exists(SettingsStore.Settings.locations.OutputPackages))
                Directory.CreateDirectory(SettingsStore.Settings.locations.OutputPackages);

            Graph.Save(Path.Combine(SettingsStore.Settings.locations.OutputPackages, $"{filename}.dgml"));
        }

        public void Open()
        {
            Process proc = new Process();
            proc.StartInfo = new ProcessStartInfo(Path.Combine(SettingsStore.Settings.locations.OutputPackages, "TestGraph.dgml"));
            proc.Start();
        }
    }
}
