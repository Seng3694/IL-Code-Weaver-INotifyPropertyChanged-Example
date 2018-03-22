using Engine.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TestApp.Model;

namespace TestApp.ViewModels
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChanged(typeof(TestModelComparer))]
        public TestModel TestModelProperty { get; set; }

        [NotifyPropertyChanged]
        public int TestIntProperty { get; set; }
        
        [NotifyPropertyChanged]
        public string TestStringProperty { get; set; }

        [NotifyPropertyChanged]
        public ObservableCollection<string> Log { get; set; }

        public ViewModel()
        {
            Log = new ObservableCollection<string>();
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Log.Add(e.PropertyName + ": " + this.GetType().GetProperty(e.PropertyName).GetValue(this));
            while (Log.Count > 100)
                Log.RemoveAt(0);
        }
    }
}
