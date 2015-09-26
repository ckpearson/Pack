using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
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
        private readonly Func<Task<SecuringContext>> _getSecuringContext;
        private const string DragFileMessage = "Drag File or Packed Image Here";
        private const string DragImageMessage = "Drag Packed Image from Here to GitHub";

        private string _message = DragFileMessage;
        private bool _dropperShowing = true;
        private readonly ObservableAsPropertyHelper<bool> _progressShowing;
        private BitmapImage _currentImage;
        private string _imageTag;
        private readonly ReactiveCommand<object> _goAgain;
        private readonly ObservableAsPropertyHelper<bool> _canGoAgain; 

        public PackScreenViewModel(Func<Task<SecuringContext>> getSecuringContext)
        {
            _getSecuringContext = getSecuringContext;
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

        private async Task HandleFile(string file)
        {
            var ctx = await _getSecuringContext();
            if (ctx.TargetPublicKeys.Count == 0)
            {
                var v1 = new V1Packer();
                using (var bmp = await Task.Run(() => v1.CreateImage(File.ReadAllBytes(file), Path.GetFileName(file))))
                {
                    var temp = Path.GetTempFileName();
                    var nTemp = Path.Combine(Path.GetDirectoryName(temp), Path.GetFileNameWithoutExtension(temp)) + ".png";
                    File.Delete(temp);
                    temp = nTemp;

                    bmp.Save(temp, ImageFormat.Png);
                    ImageTag = temp;
                    CurrentImage = new BitmapImage(new Uri(temp, UriKind.Absolute));
                }
                IsDropperAvailable = true;
                Message = DragImageMessage;
                return;
            }
        }

        private async Task HandleImage(byte[] imageData)
        {
            var v1 = new V1Packer();
            if (v1.DataIsForPacker(imageData))
            {
                var unpacked = await Task.Run(() => v1.Unpack(imageData));
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
}