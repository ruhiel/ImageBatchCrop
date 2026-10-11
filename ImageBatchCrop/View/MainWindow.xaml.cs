using MahApps.Metro.Controls;
using System.Windows.Input;

namespace ImageBatchCrop.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ImageBorder_PreviewMouseWheel(
            object sender,
            MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt)
            {
                return;
            }

            ImageScrollViewer.ScrollToHorizontalOffset(
                ImageScrollViewer.HorizontalOffset - e.Delta);

            // ScrollViewer の縦スクロール処理を抑止する
            e.Handled = true;
        }

    }
}