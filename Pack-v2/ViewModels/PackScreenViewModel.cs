using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Pack_v2.Packers;
using Pack_v2.Security;
using ReactiveUI;

namespace Pack_v2.ViewModels
{
    public class PackScreenViewModel : ReactiveObject
    {
        private readonly PackKey _originatingKey;
        private readonly Func<IReadOnlyList<Recipient>> _getRecipientsFunc;
        private readonly string _pass;
        private readonly string _originatorName;
        private readonly Func<string, byte[]> _publicKeyRetrievalFunc;
        private const string DragFileMessage = "Drag File or Packed Image Here";
        private const string DragImageMessage = "Drag Packed Image from Here to GitHub";

        private string _message = DragFileMessage;
        private bool _dropperShowing = true;
        private readonly ObservableAsPropertyHelper<bool> _progressShowing;
        private BitmapImage _currentImage;
        private string _imageTag;
        private readonly ReactiveCommand<object> _goAgain;
        private readonly ObservableAsPropertyHelper<bool> _canGoAgain;

        public PackScreenViewModel(PackKey originatingKey, Func<IReadOnlyList<Recipient>> getRecipientsFunc, string pass,
            string originatorName,
            Func<string,byte[]> publicKeyRetrievalFunc)
        {
            _originatingKey = originatingKey;
            _getRecipientsFunc = getRecipientsFunc;
            _pass = pass;
            _originatorName = originatorName;
            _publicKeyRetrievalFunc = publicKeyRetrievalFunc;
            _progressShowing = this.WhenAnyValue(v => v.IsDropperAvailable).Select(b => !b).ToProperty(this,
                v => v.IsProgressShowing, out _progressShowing);

            _goAgain = ReactiveCommand.Create(this.WhenAnyValue(v => v.IsDropperAvailable));
            _goAgain.Subscribe(_ =>
            {
                CurrentImage = null;
                ImageTag = null;
                IsDropperAvailable = true;
                Message = DragFileMessage;
            });

            _canGoAgain = this.WhenAnyValue(v => v.IsDropperAvailable, v => v.CurrentImage)
                .Select(t => t.Item1 && t.Item2 != null)
                .ToProperty(this, v => v.CanGoAgain, out _canGoAgain);

        }

        public bool CanGoAgain => _canGoAgain.Value;

        public ReactiveCommand<object> GoAgain => _goAgain; 

        public bool IsDropperAvailable
        {
            get { return _dropperShowing; }
            private set { this.RaiseAndSetIfChanged(ref _dropperShowing, value); }
        }

        public BitmapImage CurrentImage
        {
            get { return _currentImage; }
            private set { this.RaiseAndSetIfChanged(ref _currentImage, value); }
        }

        public string ImageTag
        {
            get { return _imageTag;}
            private set { this.RaiseAndSetIfChanged(ref _imageTag, value); }
        }

        public bool IsProgressShowing => _progressShowing.Value;

        public string Message
        {
            get { return _message;}
            private set { this.RaiseAndSetIfChanged(ref _message, value); }
        }

        public async void OnDrop(DragEventArgs e)
        {
            IsDropperAvailable = false;
            try
            {
                var data = e.Data;
                if (data.GetDataPresent(DataFormats.Text))
                {
                    await HandleImage(await DownloadData(new Uri((string) data.GetData(DataFormats.Text))));
                }
                else if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[]) data.GetData(DataFormats.FileDrop);
                    if (files == null || files.Length == 0)
                    {
                        return;
                    }else if (files.Length == 1 && files[0].ToLowerInvariant().EndsWith(".png"))
                    {
                        await HandleImage(await Task.Run(() => File.ReadAllBytes(files[0])));
                    }else if (files.Length == 1)
                    {
                        await HandleFile(files[0]);
                    }
                }
            }
            finally
            {
                IsDropperAvailable = true;
            }
        }

        private Task<byte[]> DownloadData(Uri uri)
        {
            var proxy = WebRequest.DefaultWebProxy;
            proxy.Credentials = CredentialCache.DefaultCredentials;
            using (var client = new WebClient
            {
                Proxy = proxy
            })
            {
                return client.DownloadDataTaskAsync(uri);
            }
        }

        [Serializable]
        public struct SecuredDataDto
        {
            public byte[] EncryptedData { get; set; }
            public byte[] Signature { get; set; }
            public string OriginatorName { get; set; }
        }

        private async Task HandleFile(string file)
        {
            var v1 = new V1Packer();
            var recipients = _getRecipientsFunc();
            var fileData = await Task.Run(() => File.ReadAllBytes(file));
            Bitmap bmp;
            var fileName = Path.GetFileName(file);
            if (recipients.Count == 0)
            {
                bmp = await Task.Run(() => v1.CreateImage(fileData, fileName, false));
            }
            else
            {
                var secured = Encryption.SecureData(fileData, _pass, _originatingKey);
                var securedData = new SecuredDataDto
                {
                    EncryptedData = secured.EncryptedData,
                    Signature = secured.Signature,
                    OriginatorName = _originatorName,
                };
                var bform = new BinaryFormatter();
                using (var ms = new MemoryStream())
                {
                    bform.Serialize(ms, securedData);
                    bmp = await Task.Run(() => v1.CreateImage(ms.ToArray(),
                        fileName, true));
                }
            }

            using (bmp)
            {
                var temp = Path.GetTempFileName();
                var nTemp = Path.Combine(Path.GetDirectoryName(temp), Path.GetFileNameWithoutExtension(temp)) + ".png";
                File.Delete(temp);
                temp = nTemp;
                bmp.Save(temp, ImageFormat.Png);
                CurrentImage = new BitmapImage(new Uri(temp, UriKind.Absolute));
                ImageTag = temp;
            }
            IsDropperAvailable = true;
            Message = DragImageMessage;
        }

        private async Task HandleImage(byte[] imageData)
        {
            var v1 = new V1Packer();
            if (v1.DataIsForPacker(imageData))
            {
                var unpacked = await Task.Run(() => v1.Unpack(imageData));
                if (unpacked.Secured)
                {
                    using (var ms = new MemoryStream(unpacked.Data))
                    {
                        var bform = new BinaryFormatter();
                        var secDto = (SecuredDataDto) bform.Deserialize(ms);
                        var pubKey = _publicKeyRetrievalFunc(secDto.OriginatorName);
                    }
                }
                var sfd = new SaveFileDialog
                {
                    Title = "Save File",
                    FileName = unpacked.Name,
                };
                var res = sfd.ShowDialog(Application.Current.MainWindow);
                if (res.Value)
                {
                    using (var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        using (var ms = new MemoryStream(unpacked.Data))
                        {
                            await ms.CopyToAsync(fs);
                        }
                        await fs.FlushAsync();
                    }
                }
                IsDropperAvailable = true;
                Message = DragFileMessage;
                return;
            }
        }
    }

    public class Recipient
    {
        public int AccountId { get; }
        public byte[] PackPublicKey { get; }

        public Recipient(int accountId, byte[] packPublicKey)
        {
            AccountId = accountId;
            PackPublicKey = packPublicKey;
        }
    }
}