using System;
using System.Windows;
using MahApps.Metro.Controls;
using Pack_v2.Tools;
using Pack_v2.ViewModels;
using ReactiveUI;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Media.Animation;
using MahApps.Metro.Controls.Dialogs;

namespace Pack_v2.Views
{
    /// <summary>
    /// Interaction logic for ShellWindow.xaml
    /// </summary>
    public partial class ShellWindow : MetroWindow, IViewFor<ShellViewModel>
    {
        public ShellWindow()
        {
            InitializeComponent();
            this.BindDataContext();
            AllowsTransparency = true;
            
            this.Events().Loaded.Subscribe(_ => ViewModel.OnLoaded());
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (sizeInfo.HeightChanged)
            {
                Top += (sizeInfo.PreviousSize.Height - sizeInfo.NewSize.Height)/2;
            }

            if (sizeInfo.WidthChanged)
            {
                Left += (sizeInfo.PreviousSize.Width - sizeInfo.NewSize.Width)/2;
            }
        }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof (ShellViewModel), typeof (ShellWindow), new PropertyMetadata(default(ShellViewModel)));

        public ShellViewModel ViewModel
        {
            get { return (ShellViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as ShellViewModel; }
        }
    }
}
