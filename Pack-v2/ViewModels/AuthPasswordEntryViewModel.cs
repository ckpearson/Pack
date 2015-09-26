using System;
using ReactiveUI;

namespace Pack_v2.ViewModels
{
    public class AuthPasswordEntryViewModel : ReactiveObject
    {
        private readonly bool _isForSetup;
        private string _password;
        private string _confirmation;
        private readonly ReactiveCommand<object> _continue; 

        public AuthPasswordEntryViewModel(bool isForSetup, IObserver<string> passwordObserver)
        {
            _isForSetup = isForSetup;
            _continue = ReactiveCommand.Create(this.WhenAnyValue(v => v.Password, v => v.Confirmation,
                (pass, conf) =>
                    ForSetup
                        ? (!string.IsNullOrEmpty(pass) && !string.IsNullOrEmpty(conf) && pass == conf)
                        : !string.IsNullOrEmpty(pass)));
            _continue.Subscribe(_ => passwordObserver.OnNext(Password));
        }

        public bool ForSetup => _isForSetup;

        public string Password
        {
            get { return _password; }
            set { this.RaiseAndSetIfChanged(ref _password, value); }
        }

        public string Confirmation
        {
            get { return _confirmation; }
            set { this.RaiseAndSetIfChanged(ref _confirmation, value); }
        }

        public ReactiveCommand<object> Continue => _continue;
    }
}