using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using MaimeGui.Annotations;
using MetaData.MetaData;
using SamyazaSSIS.Options;

namespace MaimeGui.ViewModel
{
    public class OptionsViewModel : ViewModelBase
    {
        // Options to set
        private Options _options;

        // Command when view should switch to repairview
        public RelayCommand OnRepairClick { get; private set; }

        public OptionsViewModel()
        {
            _options = new Options();

            AllowDeletionOfVertices = _options.AllowDeletionOfVertices;
            AllowModificationsOfExpressions = _options.AllowModificationOfExpressions;
            UseGlobalBlockingSemantics = _options.UseGlobalBlockingSemantics;

            // Map policytable into wpf bindable datastructure
            foreach (var keyValuePair in _options.PolicyTable)
            {
                OptionsGroup group = new OptionsGroup
                {
                    Change = keyValuePair.Key
                };

                foreach (var valuePair in keyValuePair.Value)
                {
                    @group.Options.Add(new Option
                    {
                        Type = valuePair.Key,
                        Policy = valuePair.Value
                    });
                }

                _groups.Add(group);
            }

            OnRepairClick = new RelayCommand(() => 
            {
                UpdateOptionsValues();
                SwitchToRepair();
            });
        }

        private void SwitchToRepair()
        {
            var msg = new MainViewModel.GoToPageMessage() {PageName = "Repair", Payload = _options};
            Messenger.Default.Send<MainViewModel.GoToPageMessage>(msg);
        }


        ObservableCollection<OptionsGroup> _groups = new ObservableCollection<OptionsGroup>();
        private bool _allowDeletionOfVertices;
        private bool _allowModificationsOfExpressions;
        private bool _useGlobalBlockingSemantics;
        private bool _fixDatabaseNameAndTable;

        public ObservableCollection<OptionsGroup> Groups
        {
            get
            {
                return _groups;
            }
            set
            {
                if (_groups == value)
                    return;

                _groups = value;
                RaisePropertyChanged(nameof(Groups));
            }
        }

        /// <summary>
        /// Updates updates with values in wpf bindable structures
        /// </summary>
        public void UpdateOptionsValues()
        {
            foreach (OptionsGroup optionsGroup in _groups)
            {
                var columnChange = _options.PolicyTable[optionsGroup.Change];

                foreach (Option option in optionsGroup.Options)
                {
                    columnChange[option.Type] = option.Policy;
                }
            }

            _options.AllowDeletionOfVertices = AllowDeletionOfVertices;
            _options.AllowModificationOfExpressions = AllowModificationsOfExpressions;
            _options.UseGlobalBlockingSemantics = UseGlobalBlockingSemantics;
        }

        /// <summary>
        /// Options group, e.g. Addition, containing all transformation types
        /// </summary>
        public class OptionsGroup
        {
            public ColumnChanges Change { get; set; }

            private Policy _policy;

            public Policy Policy
            {
                get { return _policy; }
                set
                {
                    _policy = value;
                    foreach (Option option in Options)
                    {
                        option.Policy = value;
                    }
                }
            }

            public ObservableCollection<Option> Options { get; } = new ObservableCollection<Option>();
        }

        /// <summary>
        /// Options, like addition -> Conditionalsplit = Propagate
        /// </summary>
        public class Option : INotifyPropertyChanged
        {
            public Type Type { get; set; }

            private Policy _policy;
            public Policy Policy
            {
                get { return _policy;}
                set
                {
                    if(_policy == value)
                        return;

                    _policy = value;
                    OnPropertyChanged(nameof(Policy));
                }
            }

            // Allow wpf to bind property change events
            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #region AdvancedOptions

        public bool AllowDeletionOfVertices
        {
            get { return _allowDeletionOfVertices; }
            set
            {
                _allowDeletionOfVertices = value; 
                RaisePropertyChanged(nameof(AllowDeletionOfVertices));
            }
        }

        public bool AllowModificationsOfExpressions
        {
            get { return _allowModificationsOfExpressions; }
            set
            {
                _allowModificationsOfExpressions = value;
                RaisePropertyChanged(nameof(AllowModificationsOfExpressions));
            }
        }

        public bool UseGlobalBlockingSemantics
        {
            get { return _useGlobalBlockingSemantics; }
            set
            {
                _useGlobalBlockingSemantics = value;
                RaisePropertyChanged(nameof(UseGlobalBlockingSemantics));
            }
        }

        public bool FixDatabaseNameAndTable
        {
            get { return _fixDatabaseNameAndTable; }
            set
            {
                _fixDatabaseNameAndTable = value;
                RaisePropertyChanged(nameof(FixDatabaseNameAndTable));
            }
        }

        #endregion
    }
}