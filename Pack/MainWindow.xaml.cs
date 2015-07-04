using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Pack.Packers;
using Squirrel;

namespace Pack
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IInput
    {
        private readonly IPacker[] _packers;

        public MainWindow()
        {
            InitializeComponent();

            Image.Events().MouseLeftButtonDown.Subscribe(_ =>
            {
                DragDrop.DoDragDrop(Image, new DataObject(DataFormats.FileDrop, new[] {(string) Image.Tag}),
                    DragDropEffects.Copy);

            });

            Message.Text = DropMessage;

            DragDrop.AddDropHandler(Border, OnDrop);

            _packers = new IPacker[]
            {
                new V1Packer(),
            };

            Loaded += MainWindow_Loaded;
        }

        async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            using (var mgr = new UpdateManager("http://ckpearson.github.io/Pack/Releases"))
            {
                await mgr.UpdateApp();
            }
#endif
        }

        private const string DropMessage = "Drop Files or Packed Image Here";

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

        private void ShowProgress()
        {
            DropPanel.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;
            GoAgainButton.Visibility = Visibility.Collapsed;
            DragDrop.RemoveDropHandler(Border, OnDrop);
        }

        private void ShowInput()
        {
            DropPanel.Visibility = Visibility.Visible;
            ProgressPanel.Visibility = Visibility.Collapsed;
            DragDrop.AddDropHandler(Border, OnDrop);
            GoAgainButton.Visibility = Visibility.Collapsed;
        }

        private void ShowImage()
        {
            DropPanel.Visibility = Visibility.Visible;
            ProgressPanel.Visibility = Visibility.Collapsed;
            GoAgainButton.Visibility = Visibility.Visible;
            Message.Text = "Drag Image From Here";
        }

        private async Task HandleImage(byte[] imageData)
        {
            var packer = _packers.AsParallel().SingleOrDefault(p => p.DataIsForPacker(imageData));
            if (packer == null)
            {
                MessageBox.Show(this, "Did not understand image");
                return;
            }
            var unpacked = await Task.Run(() => packer.Unpack(imageData, this));
            var sfd = new SaveFileDialog
            {
                Title = "Save File",
                FileName = unpacked.Name,
            };
            var res = sfd.ShowDialog(this);
            if (res == null || !res.Value) return;
            File.WriteAllBytes(sfd.FileName, unpacked.Data);
            ShowInput();
        }

        private async Task HandleFile(string filePath)
        {
            var packer = _packers.AsParallel().OrderBy(p => p.Version).First();
            using (var bmp = await Task.Run(() => packer.CreateImage(File.ReadAllBytes(filePath), Path.GetFileName(filePath), this)))
            {
                var temp = Path.GetTempFileName();
                var nTemp = Path.Combine(Path.GetDirectoryName(temp), Path.GetFileNameWithoutExtension(temp)) + ".png";
                File.Delete(temp);
                temp = nTemp;

                bmp.Save(temp, ImageFormat.Png);
                Image.Source = new BitmapImage(new Uri(temp, UriKind.Absolute));
                Image.Tag = temp;
            }
            ShowImage();
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            var data = e.Data;
            ShowProgress();

            if (data.GetDataPresent(DataFormats.Text))
            {
                await HandleImage(await DownloadData(new Uri((string) data.GetData(DataFormats.Text))));
            }
            else if (data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]) data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && files[0].ToLowerInvariant().EndsWith(".png"))
                {
                    await HandleImage(await Task.Run(() => File.ReadAllBytes(files[0])));
                }
                else if (files.Length == 1)
                {
                    await HandleFile(files[0]);
                }
            }
        }

        private void GoAgainButton_OnClick(object sender, RoutedEventArgs e)
        {
            GoAgainButton.Visibility = Visibility.Collapsed;
            Image.Source = null;
            Message.Text = DropMessage;
            DragDrop.AddDropHandler(Border, OnDrop);
        }
    }
}
