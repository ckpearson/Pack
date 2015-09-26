using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Xml.Linq;
using Akavache;
using Octokit;
using Octokit.Reactive;
using Pack_v2.Security;
using ReactiveUI;

namespace Pack_v2.ViewModels
{
    public class ShellViewModel : ReactiveObject
    {
        private ReactiveObject _currentViewModel;

        public ReactiveObject CurrentViewModel
        {
            get { return _currentViewModel; }
            set { this.RaiseAndSetIfChanged(ref _currentViewModel, value); }
        }

        public void OnLoaded()
        {
            CurrentViewModel = new ProgressViewModel {IsIndeterminate = true, Message = "Starting"};

            BlobCache.Secure.GetLoginAsync("github")
                .Catch<LoginInfo, KeyNotFoundException>(_ => Observable.Return(new LoginInfo("", "")))
                .Select(li => new GitHubLoginViewModel(Observer.Create<Credentials>(
                    creds =>
                    {
                        var vm = CurrentViewModel;
                        CurrentViewModel = new ProgressViewModel {IsIndeterminate = true, Message = "Authenticating"};
                        var client = new ObservableGitHubClient(new ProductHeaderValue("Pack"));
                        client.Connection.Credentials = creds;
                        client.User
                            .Current()
                            .Catch<User, Exception>(_ =>
                            {
                                CurrentViewModel = vm;
                                return Observable.Empty<User>();
                            })
                            .SelectMany(_ => BlobCache.Secure.SaveLogin(creds.Login, creds.Password, "github"))
                            .SelectMany(_ =>
                            {
                                return client.Repository.Get(creds.Login, "pack-app-hub")
                                    .Catch<Repository, NotFoundException>(
                                        __ => client.Repository.Create(new NewRepository("pack-app-hub")));
                            })
                            .SelectMany(repo =>
                            {
                                return client.Repository.Content.GetAllContents(creds.Login, "pack-app-hub",
                                    "pack-key.xml")
                                    .Catch<RepositoryContent, NotFoundException>(
                                        _ =>
                                            client.Repository.Content.CreateFile(creds.Login, "pack-app-hub",
                                                "pack-key.xml",
                                                new CreateFileRequest("Initial Commit", ""))
                                                .Catch<RepositoryContentChangeSet, Exception>(
                                                    ex => Observable.Empty<RepositoryContentChangeSet>())
                                                .SelectMany(
                                                    cs =>
                                                        client.Repository.Content.GetAllContents(creds.Login,
                                                            "pack-app-hub", "pack-key.xml")));
                            })
                            .Subscribe(contents =>
                            {
                                CurrentViewModel = new ProgressViewModel
                                {
                                    IsIndeterminate = true,
                                    Message = "Checking Pack Auth Info"
                                };

                                var packVm = new PackScreenViewModel(async () => new SecuringContext());

                                var hasContent = !string.IsNullOrEmpty(contents.Content);
                                vm = new AuthPasswordEntryViewModel(!hasContent,
                                    Observer.Create<string>(pass =>
                                    {
                                        if (!hasContent)
                                        {
                                            var key = Encryption.DerivePackKey(pass, Guid.NewGuid().ToByteArray());
                                            var xml = PackKeyEncoder.ToXml(key).ToString();
                                            client.Repository.Content.UpdateFile(creds.Login, "pack-app-hub",
                                                "pack-key.xml",
                                                new UpdateFileRequest("Added pack key", xml, contents.Sha))
                                                .Catch<RepositoryContentChangeSet, Exception>(
                                                    ex => Observable.Empty<RepositoryContentChangeSet>())
                                                .Subscribe(_ =>
                                                {
                                                    CurrentViewModel = packVm;
                                                });
                                        }
                                        else
                                        {
                                            var key = PackKeyEncoder.FromXml(XElement.Parse(contents.Content));
                                            var decryptedPrivateKey = Encryption.DecryptPrivateKey(pass, key.Salt,
                                                key.InitialisationVector, key.EncryptedPrivateKey);
                                            var decryptedChallenge = Encryption.RsaDecrypt(decryptedPrivateKey,
                                                key.Challenge);
                                            if (decryptedChallenge.SequenceEqual(key.Salt))
                                            {
                                                CurrentViewModel = packVm;
                                            }
                                            else
                                            {
                                                
                                            }
                                        }
                                    }));
                                CurrentViewModel = vm;
                            });
                    }))
                {Username = li.UserName, Password = li.Password})
                .Subscribe(vm => CurrentViewModel = vm);
        }
    }
}