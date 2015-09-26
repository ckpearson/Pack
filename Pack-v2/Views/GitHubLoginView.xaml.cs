﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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
    /// Interaction logic for GitHubLoginView.xaml
    /// </summary>
    public partial class GitHubLoginView : UserControl, IViewFor<GitHubLoginViewModel>
    {
        public GitHubLoginView()
        {
            InitializeComponent();
            this.BindDataContext();
            PasswordBox
                .Events()
                .PasswordChanged
                .Select(_ => PasswordBox.Password)
                .Subscribe(pass => ViewModel.Password = pass);

            PasswordBox.Events()
                .Loaded
                .Subscribe(_ => PasswordBox.Password = ViewModel.Password);
        }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof (GitHubLoginViewModel), typeof (GitHubLoginView), new PropertyMetadata(default(GitHubLoginViewModel)));

        public GitHubLoginViewModel ViewModel
        {
            get { return (GitHubLoginViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as GitHubLoginViewModel; }
        }
    }
}
