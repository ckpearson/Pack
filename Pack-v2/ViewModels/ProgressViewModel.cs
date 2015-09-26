using ReactiveUI;

namespace Pack_v2.ViewModels
{
    public class ProgressViewModel : ReactiveObject
    {
        private bool _isIdeterminate;
        private double _progress;
        private string _message;

        public bool IsIndeterminate
        {
            get { return _isIdeterminate; }
            set { this.RaiseAndSetIfChanged(ref _isIdeterminate, value); }
        }

        public double Progress
        {
            get { return _progress;}
            set { this.RaiseAndSetIfChanged(ref _progress, value); }
        }

        public string Message
        {
            get { return _message;}
            set { this.RaiseAndSetIfChanged(ref _message, value); }
        }
    }
}