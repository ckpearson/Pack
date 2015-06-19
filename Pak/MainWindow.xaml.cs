using System;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;

namespace Pak
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

        

        private const string DropMessage = "Drop Files or Pak'd Image Here";

        private async void OnDrop(object sender, DragEventArgs e)
        {
            var data = e.Data;

            if (data.GetDataPresent(DataFormats.Text))
            {
                var uri = new Uri((string) data.GetData(DataFormats.Text));
                Bitmap bmp;
                using (var client = new WebClient())
                {
                    var imgData = await client.DownloadDataTaskAsync(uri);
                    bmp = new Bitmap(new MemoryStream(imgData));
                }
            }else if (data.GetDataPresent(DataFormats.FileDrop))
            {
                DropPanel.Visibility = Visibility.Collapsed;
                ProgessPanel.Visibility = Visibility.Visible;
                DragDrop.RemoveDropHandler(Border, OnDrop);
                var dat = (string[])data.GetData(DataFormats.FileDrop);
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
                ProgessPanel.Visibility = Visibility.Collapsed;
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
    }
}
