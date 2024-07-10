using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WPFFeatures.Admin;
using WPFFeatures.CustomUrl;
using WPFFeatures.SingleInstance;
using WPFFeatures.Systray;
using WpfI18n.Sources;

namespace WPFDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region properties
        public new static App Current => (App)Application.Current;
        internal SingleInstanceFeature? SingleInstanceFeature { get; }
        internal AdminFeature? AdminFeature { get; }
        internal SystrayFeature? SystrayFeature { get; }
        internal CustomUrlFeature? CustomUrlFeature { get; }
        #endregion

        public App()
        {
            //SingleInstanceFeature = new SingleInstanceFeature();
            //AdminFeature = new AdminFeature();
            //SystrayFeature = new SystrayFeature();
            //CustomUrlFeature = new CustomUrlFeature();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (SingleInstanceFeature?.Register() == false)
            {
                SingleInstanceFeature?.Active(e);
                Shutdown();
                return;
            }

            if (AdminFeature?.CheckPrivilege() == false)
            {
                // 没有admin权限，尝试重启
                SingleInstanceFeature?.Dispose();
                AdminFeature?.LaunchNewInstanceWithAdmin(e);
                Shutdown();
                return;
            }

            CustomUrlFeature?.TryRegisterUriScheme();

            LanguageManager.Default.Register("Languages");
            LanguageManager.Default.Load("en", "en");

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SingleInstanceFeature?.Dispose();

            base.OnExit(e);
        }
    }
}
