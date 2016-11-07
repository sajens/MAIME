using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using MaimeGui.Annotations;
using MetaData.MetaData.MetaStore;
using MetaData.Settings;
using SamyazaSSIS;
using SamyazaSSIS.Options;

namespace MaimeGui.ViewModel
{
    public class RepairViewModel : ViewModelBase
    {
        private string _status;

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                RaisePropertyChanged(nameof(Status));
            }
        }

        ObservableCollection<GraphInfo> _graphs = new ObservableCollection<GraphInfo>();

        public ObservableCollection<GraphInfo> Graphs
        {
            get { return _graphs; }
        }

        private ObservableCollection<Logger.LogEntry> _logMessages;
        public ObservableCollection<Logger.LogEntry> LogMessages
        {
            get { return _logMessages; }
            set
            {
                _logMessages = value;
                RaisePropertyChanged(nameof(LogMessages));
            }
        }

        public RelayCommand OnJobStart { get; private set; }
        public RelayCommand OnCreateSnapshot { get; private set; }
        public Options _options;

        public RepairViewModel()
        {
            LogMessages = new ObservableCollection<Logger.LogEntry> ();
            Logger.OnNewEntry += NewLogEntry; 

            OnJobStart = new RelayCommand(StartRepair, CanStartRepair);
            OnCreateSnapshot = new RelayCommand(CreateSnapshot);
        }

        public async void LoadMaime()
        {
            await Task.Run(() => Maime.Init(_options)).ConfigureAwait(true);
            ConstructGraphs();
        }

        public void ConstructGraphs()
        {
            Logger.Common("Loading graphs...");
            foreach (string jobName in GetEtlJobs())
            {
                try
                {
                    Graph graph = Maime.CreateGraph(jobName);
                    Graphs.Add(new GraphInfo(graph, jobName));
                }
                catch (NotImplementedException e)
                {
                    Logger.Error($"{jobName} includes a non supported transformation.");
                }
            }
            Logger.Common("Finished loading graphs.");
            _canRepairStart = true;
        }

        private string[] GetEtlJobs()
        {
            DirectoryInfo dir = new DirectoryInfo(SettingsStore.Settings.locations.Packages);


            if (!Directory.Exists(SettingsStore.Settings.locations.Packages))
                Directory.CreateDirectory(SettingsStore.Settings.locations.Packages);


            return dir.GetFiles("*.dtsx").Select(f => Path.GetFileNameWithoutExtension(f.FullName)).ToArray();
        }

        private async void StartRepair()
        {
            _canRepairStart = false;

            foreach (GraphInfo graphInfo in Graphs)
            {
                graphInfo.Status = 50;

                Task<bool> task = Task<bool>.Factory.StartNew(() => Maime.AlterGraph(graphInfo.Graph));
                bool res = await task;

                if (!res)
                    graphInfo.Color = Brushes.DarkRed;

                graphInfo.Status = 100;
            }
            _canRepairStart = true;
        }

        private bool _canRepairStart;

        private bool CanStartRepair()
        {
            return _canRepairStart;
        }

        private void CreateSnapshot()
        {
            Logger.Common("Creating snapshots");
            // Traverse through every single EDS and save a snapshot of it
            foreach (EDSSettings EDS in SettingsStore.EDSSettings.Where(e => e.Name != "Template"))
            {
                Logger.Common($"Creating snapshot for {EDS.Name}");
                MetaDataSnapshot metaDataSnapshot = new MetaDataSnapshot(EDS.ConnectionString);

                Console.WriteLine(metaDataSnapshot);

                Logger.Common($"Saving snapshot for {EDS.Name}");
                MetaDataStore.Provider.SaveSnapshot(EDS, metaDataSnapshot);
                Logger.Common($"Finishing snapshot for {EDS.Name}");
            }
            Logger.Common($"Finished creating ({SettingsStore.EDSSettings.Count(e => e.Name != "Template")}) snapshots");
        }

        private readonly object _logLock = new object();

        private void NewLogEntry(Logger.LogEventArgs e)
        {
            lock (_logLock)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => 
                {
                    LogMessages.Add(e.LogEntry);
                }));
            }
        }

        public class GraphInfo : INotifyPropertyChanged
        {
            public Graph Graph;
            private double _status;
            private Brush _color;

            public Brush Color
            {
                get { return _color; }
                set
                {
                    _color = value;
                    OnPropertyChanged(nameof(Color));
                }
            }

            public double Status
            {
                get { return _status; }
                set
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }

            public string Name { get; set; }

            public GraphInfo(Graph graph, string name)
            {
                Graph = graph;
                Name = name;

                Status = 0.0;
                Color = Brushes.ForestGreen;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}