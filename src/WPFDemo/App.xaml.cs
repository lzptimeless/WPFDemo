using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WPFFeatures.Admin;
using WPFFeatures.SingleInstance;
using WPFFeatures.Systray;

namespace WPFDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region properties
        internal SingleInstanceFeature SingleInstanceFeature { get; }
        internal AdminFeature AdminFeature { get; }
        internal SystrayFeature SystrayFeature { get; }
        #endregion

        public App()
        {
            SingleInstanceFeature = new SingleInstanceFeature();
            AdminFeature = new AdminFeature();
            SystrayFeature = new SystrayFeature();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!this.GetSingleInstanceFeature().OnStartup())
            {
                Shutdown();
                return;
            }

            if (!this.GetAdminFeature().OnStartup(e, () => this.GetSingleInstanceFeature().Dispose()))
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            this.GetSingleInstanceFeature().OnExit();

            base.OnExit(e);
        }
    }

    public static class ApplicationExtension
    {
        internal static SingleInstanceFeature GetSingleInstanceFeature(this Application app)
        {
            return ((App)app).SingleInstanceFeature;
        }

        internal static AdminFeature GetAdminFeature(this Application app)
        {
            return ((App)app).AdminFeature;
        }

        internal static SystrayFeature GetSystrayFeature(this Application app)
        {
            return ((App)app).SystrayFeature;
        }
    }
}
