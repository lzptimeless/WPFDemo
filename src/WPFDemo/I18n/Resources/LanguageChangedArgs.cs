using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFDemo.I18n.Resources
{
    public class LanguageChangedArgs : EventArgs
    {
        public LanguageChangedArgs(string ietfTag)
        {
            CurrentIetfTag = ietfTag;
        }

        public string CurrentIetfTag { get; set; }
    }
}
