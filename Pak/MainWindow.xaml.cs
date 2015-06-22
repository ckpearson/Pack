using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace Pack
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
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
        }

        

        private const string DropMessage = "Drop Files or Packed Image Here";

        private async void OnDrop(object sender, DragEventArgs e)
        {
            var data = e.Data;

            if (data.GetDataPresent(DataFormats.Text))
            {
                var uri = new Uri((string) data.GetData(DataFormats.Text));
                byte[] imgData;
                var proxy = WebRequest.DefaultWebProxy;
                //var creds = await Settings.GetProxyCredentials();
                proxy.Credentials = CredentialCache.DefaultCredentials;
                using (var client = new WebClient
                {
                    Proxy = proxy
                })
                {
                    imgData = await client.DownloadDataTaskAsync(uri);
                }
                DropPanel.Visibility = Visibility.Collapsed;
                ProgressPanel.Visibility = Visibility.Visible;
                DragDrop.RemoveDropHandler(Border, OnDrop);

                var details = await Task.Run(() => Packer.UnpackImage(imgData));
                if (details == null)
                {
                    MessageBox.Show("Unable to unpack image, is it a correct image created with this program?", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    ProgressPanel.Visibility = Visibility.Collapsed;
                    DropPanel.Visibility = Visibility.Visible;
                    DragDrop.AddDropHandler(Border, OnDrop);
                    return;
                }

                var sfd = new SaveFileDialog
                {
                    FileName = details.Item1,
                    AddExtension = true,
                };
                var res = sfd.ShowDialog();
                if (res == null || !res.Value)
                {
                    ProgressPanel.Visibility = Visibility.Collapsed;
                    DropPanel.Visibility = Visibility.Visible;
                    DragDrop.AddDropHandler(Border, OnDrop);
                    return;
                }
                File.WriteAllBytes(sfd.FileName, details.Item2);
                ProgressPanel.Visibility = Visibility.Collapsed;
                DropPanel.Visibility = Visibility.Visible;
                DragDrop.AddDropHandler(Border, OnDrop);
            }
            else if (data.GetDataPresent(DataFormats.FileDrop))
            {
                DropPanel.Visibility = Visibility.Collapsed;
                ProgressPanel.Visibility = Visibility.Visible;
                DragDrop.RemoveDropHandler(Border, OnDrop);
                var dat = (string[]) data.GetData(DataFormats.FileDrop);
                if (dat.Length > 1)
                {
                    MessageBox.Show(this, "If you want to share multiple files at the same time, please zip / 7-zip them first.", "Multiple Files",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var imgPath = Path.GetTempFileName();
                var newName = Path.Combine(Path.GetDirectoryName(imgPath), Path.GetFileNameWithoutExtension(imgPath) + ".png");
                File.Move(imgPath, newName);
                imgPath = newName;
                await Task.Run(() => Packer.CreateImage(dat[0], imgPath));
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bi.CacheOption = BitmapCacheOption.None;
                bi.UriSource = new Uri(imgPath, UriKind.Absolute);
                bi.EndInit();
                Image.Source = bi;
                Image.Tag = imgPath;
                Message.Text = "Drag Image to Comment Box";
                GoAgainButton.Visibility = Visibility.Visible;
                ProgressPanel.Visibility = Visibility.Collapsed;
                DropPanel.Visibility = Visibility.Visible;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void GoAgainButton_OnClick(object sender, RoutedEventArgs e)
        {
            GoAgainButton.Visibility = Visibility.Collapsed;
            Image.Source = null;
            Message.Text = DropMessage;
            DragDrop.AddDropHandler(Border, OnDrop);
        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            var settingsWin = new SettingsWindow
            {
                Owner = this,
            };
            settingsWin.ShowDialog();
        }
    }
}
