using System;
using System.Windows;

namespace OCOverlay {
    public partial class Options : Window {
        public Options(MainWindow context) {
            InitializeComponent();
            DataContext = context;
        }

        private void Options_ContentRendered(object sender, EventArgs e) {
            this.RegisterWindow();
            ButtonUtils.setBoxesGen(9, reset);
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            if (OwnedWindows.Count == 0) {
                EditBlinks bli = new EditBlinks();
                bli.Owner = this;
                bli.Show();
            } else {
                OwnedWindows[0].Focus();
                OwnedWindows[0].WindowState = WindowState.Normal;
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e) {
            top.IsChecked = false;
            lok.IsChecked = false;
            tran.IsChecked = false;
            x.IsChecked = false;
            y.IsChecked = false;
            cov.IsChecked = false;
            ((MainWindow)DataContext).Scale = 0;
            opa.Value = 100;
            rot.Value = 0;
            scale.Value = 0;
        }
    }
}
