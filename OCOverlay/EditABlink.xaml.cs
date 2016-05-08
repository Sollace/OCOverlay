using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace OCOverlay {
    public partial class EditABlink : Window {
        private BlinkFrame editinItem;

        private int draggedIndex = -1;
        private Point startPosition;

        public EditABlink(BlinkFrame item) {
            InitializeComponent();
            DataContext = editinItem = item;
            loadFrames();
            ContentRendered += EditABlink_ContentRendered;
        }

        void EditABlink_ContentRendered(object sender, EventArgs e) {
            this.RegisterWindow();
        }

        private void loadFrames() {
            foreach (int i in editinItem.ImageKeys) {
                addFrameToList(i);
            }
        }

        private void Rectangle_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                string[] data = (string[])e.Data.GetData(DataFormats.FileDrop, true);
                int key = ImageRegistry.registerImage(data[0]);
                if (key >= 0) {
                    editinItem.AddImage(key);
                    addFrameToList(key);
                }
            }

            e.Handled = true;
        }

        private void addFrameToList(int image) {
            GroupBox box = new GroupBox() {
                Content = new Image() {
                    Source = ImageRegistry.getImage(image),
                    Width = 90,
                    Margin = new Thickness(5)
                }
            };
            box.MouseDown += dragStart;
            box.MouseUp += dragEnd;
            box.MouseDoubleClick += delete;

            FramesList.Children.Add(box);
        }

        private void dragStart(object sender, MouseButtonEventArgs e) {
            draggedIndex = FramesList.Children.IndexOf((GroupBox)sender);
            startPosition = e.GetPosition((GroupBox)sender);
            ((GroupBox)sender).MouseMove += drag;
        }

        private void dragEnd(object sender, MouseButtonEventArgs e) {
            ((GroupBox)sender).MouseMove -= drag;
            draggedIndex = -1;
        }

        private void drag(object sender, MouseEventArgs e) {
            if (draggedIndex >= 0) {
                double x = Point.Subtract(e.GetPosition((GroupBox)sender), startPosition).X;

                if (x < 0 && draggedIndex > 1) {
                    int image = editinItem.imageKeys[--draggedIndex];
                    editinItem.imageKeys.RemoveAt(draggedIndex);
                    editinItem.imageKeys.Insert(draggedIndex - 1, image);
                } else if ((x > 0) && draggedIndex < FramesList.Children.Count - 1) {
                    int image = editinItem.imageKeys[--draggedIndex];
                    editinItem.imageKeys.RemoveAt(draggedIndex);
                    editinItem.imageKeys.Insert(draggedIndex + 1, image);
                }

                draggedIndex = -1;
                FramesList.Children.RemoveRange(1, FramesList.Children.Count - 1);
                loadFrames();
            }
        }

        private void delete(object sender, MouseButtonEventArgs e) {
            editinItem.RemoveImageAt(FramesList.Children.IndexOf((GroupBox)sender) - 1);
            FramesList.Children.RemoveRange(1, FramesList.Children.Count - 1);
            loadFrames();
        }

        private void BlinkWindow_Activated(object sender, EventArgs e) {
            BlinkManager.Pause();
        }

        private void BlinkWindow_Closed(object sender, EventArgs e) {
            BlinkManager.Continue();
        }

        private void export_Click(object sender, RoutedEventArgs e) {
            Utils.SaveBlinkItem(this, editinItem);
        }
    }
}
