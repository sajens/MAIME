using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaData.Settings.EDSProvider;
using MetaData.Settings.SettingsProvider;

namespace MetaData.Settings
{
    /// <summary>
    /// Class to handle program and EDS related settings
    /// </summary>
    public static class SettingsStore
    {
        public static ISettingsProvider SettingsProvider;
        public static IEDSProvider EDSProvider;

        public static Settings Settings;
        public static List<EDSSettings> EDSSettings;

        public static void Init(ISettingsProvider settingsProvider, IEDSProvider edsProvider)
        {
            if (SettingsProvider != null && EDSProvider != null)
                return;

            SettingsProvider = settingsProvider;
            EDSProvider = edsProvider;

            Settings = settingsProvider.Load();
            if(Settings == null)
                Settings = settingsProvider.CreateTemplate();
                
            EDSSettings = edsProvider.GetSettings();
            if(EDSSettings.Count == 0)
                EDSSettings = edsProvider.CreateTemplate();
        }
    }
}
