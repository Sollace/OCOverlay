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
using System.Windows.Shapes;

namespace OCOverlay {
    public partial class EditBlinks : Window {
        public EditBlinks() {
            InitializeComponent();
            BlinksListing.ItemsSource = BlinkManager.Frames;
        }

        protected override void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);
            this.RegisterWindow();
            ButtonUtils.setBoxesGen(4, add);
            ButtonUtils.setBoxesGen(8, 0, save);
        }

        private void buttonGotFocus(object sender, MouseEventArgs e) {
            ButtonUtils.buttonGotFocus((Button)sender);
        }

        private void buttonLostFocus(object sender, MouseEventArgs e) {
            ButtonUtils.buttonLostFocus((Button)sender);
        }

        private void Remove_Click(object sender, RoutedEventArgs e) {
            BlinkManager.Frames.Remove((BlinkFrame)((Button)sender).DataContext);
        }

        private void Edit_Click(object sender, RoutedEventArgs e) {
            EditABlink win = new EditABlink((BlinkFrame)((Button)sender).DataContext);
            win.Owner = this;
            win.Show();
        }

        private void Add_Click(object sender, RoutedEventArgs e) {
            BlinkFrame newFrame = new BlinkFrame(4, 12, 1);
            BlinkManager.Frames.Add(newFrame);
            EditABlink win = new EditABlink(newFrame);
            win.Owner = this;
            win.Show();
        }

        private void Save(object sender, RoutedEventArgs e) {
            Utils.SaveBlinkItems(this, BlinkManager.Frames.ToArray());
        }
    }
}
