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
    /// Interaction logic for ProgressView.xaml
    /// </summary>
    public partial class ProgressView : UserControl, IViewFor<ProgressViewModel>
    {
        public ProgressView()
        {
            InitializeComponent();
            this.BindDataContext();
        }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof (ProgressViewModel), typeof (ProgressView),
            new PropertyMetadata(default(ProgressViewModel)));

        public ProgressViewModel ViewModel
        {
            get { return (ProgressViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as ProgressViewModel; }
        }
    }
}
