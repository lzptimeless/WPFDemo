using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfI18n.Tools
{
    internal class DesignerHelper
    {
        #region fields
        private static bool _isDesignMode;
        #endregion

        #region constructors
        static DesignerHelper()
        {
            DependencyProperty prop = DesignerProperties.IsInDesignModeProperty;
            _isDesignMode = (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;
        }
        #endregion

        #region properties
        /// <summary>
        /// Is in design mode
        /// </summary>
        public static bool IsDesignMode
        {
            get { return _isDesignMode; }
        }
        #endregion
    }
}
