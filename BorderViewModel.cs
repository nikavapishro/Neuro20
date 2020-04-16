using System.ComponentModel;
using System.Windows;

namespace SciChartExamlpeOne
{
    public class BorderViewModel : INotifyPropertyChanged
    {
        private Visibility _settingVisible = Visibility.Hidden;
        public Visibility SettingVisible
        {
            get
            {
                return _settingVisible;
            }

            set
            {
                _settingVisible = value;
                NotifyPropertyChanged("SettingVisible");
            }
        }

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
