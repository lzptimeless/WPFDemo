using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFDemo.ViewModels
{
    internal class MainViewModel : ObservableObject
    {
        private int _hours;
        public int Hours
        {
            get => _hours;
            set => SetProperty(ref _hours, value);
        }
    }
}
