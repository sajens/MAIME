using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaData.Settings.EDSProvider
{
    public interface IEDSProvider
    {
        /// <summary>
        /// Get all stored settings
        /// </summary>
        /// <returns></returns>
        List<EDSSettings> GetSettings();

        /// <summary>
        /// Save provided setting
        /// </summary>
        /// <param name="settings">Reference to setting</param>
        /// <returns>True if success, false if error</returns>
        void SaveSettings(EDSSettings settings);

        /// <summary>
        /// Create template of an EDSSetting
        /// </summary>
        List<EDSSettings> CreateTemplate();
    }
}
