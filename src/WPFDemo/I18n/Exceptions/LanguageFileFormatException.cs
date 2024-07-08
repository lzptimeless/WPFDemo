using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFDemo.I18n.Exceptions
{
    public class LanguageFileFormatException:Exception
    {
        public LanguageFileFormatException(string msg)
            :base(msg)
        { }
    }
}
