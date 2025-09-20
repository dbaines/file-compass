using CommunityToolkit.Mvvm.ComponentModel;

namespace HDDIndexer.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        private bool _isBusy;
        private string? _title;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string? Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public virtual void Initialize() { }
        public virtual void Cleanup() { }
    }
}