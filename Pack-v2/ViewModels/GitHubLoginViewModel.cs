using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Akavache;
using Octokit;
using ReactiveUI;

namespace Pack_v2.ViewModels
{
    public class GitHubLoginViewModel : ReactiveObject
    {
        private string _username;
        private string _password;
        private readonly ReactiveCommand<object> _signIn;

        public GitHubLoginViewModel(IObserver<Credentials> credentialsObserver)
        {
            _signIn = ReactiveCommand.Create(
                this.WhenAnyValue(v => v.Username, v => v.Password)
                    .Select(
                        (username, pass) => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password)));
            _signIn.Subscribe(_ => credentialsObserver.OnNext(new Credentials(Username, Password)));
        }

        public string Username
        {
            get { return _username; }
            set { this.RaiseAndSetIfChanged(ref _username, value); }
        }

        public string Password
        {
            get { return _password;}
            set { this.RaiseAndSetIfChanged(ref _password, value); }
        }

        public ReactiveCommand<object> SignIn => _signIn;
    }
}