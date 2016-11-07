using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SamyazaSSIS;

namespace MaimeGui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An unhandled exception just occured: {e.Exception.Message}\n{e.Exception.StackTrace}",
                "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            Logger.Error($"{e.Exception.Message}\n{e.Exception.StackTrace}");
        }
    }
}
