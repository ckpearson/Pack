using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Pack_v2.Tools;
using Pack_v2.ViewModels;
using ReactiveUI;

namespace Pack_v2.Views
{
    /// <summary>
    /// Interaction logic for PackScreenView.xaml
    /// </summary>
    public partial class PackScreenView : UserControl, IViewFor<PackScreenViewModel>
    {
        public PackScreenView()
        {
            InitializeComponent();
            this.BindDataContext();

            this.Events().Loaded
                .Subscribe(_ =>
                {
                    Border.Events().Drop.Subscribe(ViewModel.OnDrop);
                    Image.Events().MouseLeftButtonDown.Subscribe(__ =>
                    {
                        DragDrop.DoDragDrop(Image, new DataObject(DataFormats.FileDrop, new[] {(string) Image.Tag}),
                            DragDropEffects.Copy);
                    });
                });
        }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof (PackScreenViewModel), typeof (PackScreenView), new PropertyMetadata(default(PackScreenViewModel)));

        public PackScreenViewModel ViewModel
        {
            get { return (PackScreenViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as PackScreenViewModel; }
        }
    }
}
