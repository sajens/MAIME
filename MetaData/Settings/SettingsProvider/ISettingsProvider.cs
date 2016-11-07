using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaData.Settings.SettingsProvider
{
    public interface ISettingsProvider
    {
        Settings Load();

        void Save(Settings settings);

        Settings CreateTemplate();
    }
}
