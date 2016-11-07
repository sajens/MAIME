using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using SamyazaSSIS.Options;

namespace MaimeGui.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel; // Current viewmodel shown inside MainView

        // References to available view models
        readonly static OptionsViewModel OptionsViewModel = new OptionsViewModel();
        readonly static RepairViewModel RepairViewModel = new RepairViewModel();

        public ViewModelBase CurrentViewModel
        {
            get
            {
                return _currentViewModel;
            }
            set
            {
                if (_currentViewModel == value)
                    return;

                _currentViewModel = value;
                RaisePropertyChanged(nameof(CurrentViewModel));
            }
        }

        // Command used to switch RepairView
        public RelayCommand RepairViewCommand { get; private set; }

        public MainViewModel()
        {
            CurrentViewModel = OptionsViewModel;

            RepairViewCommand = new RelayCommand(() => CurrentViewModel = RepairViewModel);

            Messenger.Default.Register<GoToPageMessage> (this, ReceiveMessage);
        }


        /// <summary>
        /// Receives message stating which page to switch to
        /// </summary>
        /// <param name="action">Name of page to switch to</param>
        private void ReceiveMessage(GoToPageMessage action)
        {
            switch (action.PageName)
            {
                case "Repair":
                    RepairViewModel._options = (Options) action.Payload;
                    CurrentViewModel = RepairViewModel;
                    RepairViewModel.LoadMaime();
                    break;
            }
        }
            
        public class GoToPageMessage
        {
            public string PageName { get; set; }
            public object Payload { get; set; }
        }
    }
}
