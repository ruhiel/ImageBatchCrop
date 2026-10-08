using Reactive.Bindings;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ImageBatchCrop.ViewModel
{
    public class MainViewModel
    {
        // =========================
        // 矩形
        // =========================

        public ReactiveProperty<double> CropLeft { get; } = new(100);
        public ReactiveProperty<double> CropTop { get; } = new(80);
        public ReactiveProperty<double> CropWidth { get; } = new(180);
        public ReactiveProperty<double> CropHeight { get; } = new(120);


        // =========================
        // 画像
        // =========================

        public ReactiveCollection<string> ImageFiles { get; } = new();

        public ReactiveProperty<string> SelectedImage { get; set; } = new();

        public ReactiveProperty<BitmapImage?> CurrentImage { get; } = new();


        // =========================
        // マウス操作
        // =========================

        public ReactiveCommand<MouseButtonEventArgs> CropMouseLeftButtonDownCommand { get; }

        public ReactiveCommand<MouseEventArgs> CropMouseMoveCommand { get; }

        public ReactiveCommand<MouseButtonEventArgs> CropMouseLeftButtonUpCommand { get; }

        public ReactiveCommand<DragDeltaEventArgs> ResizeCropCommand { get; }

        public ReactiveCommand<DragEventArgs> FileDropAreaDragOverCommand { get; }

        public ReactiveCommand<DragEventArgs> FileDropAreaDropCommand { get; }


        // =========================
        // ドラッグ状態
        // =========================

        private bool _isDragging;

        private Point _dragStart;

        private double _dragStartLeft;

        private double _dragStartTop;


        // =========================
        // 定数
        // =========================

        private const double MinCropSize = 20;


        public MainViewModel()
        {
            CropMouseLeftButtonDownCommand =
                new ReactiveCommand<MouseButtonEventArgs>()
                    .WithSubscribe(CropMouseLeftButtonDown);

            CropMouseMoveCommand =
                new ReactiveCommand<MouseEventArgs>()
                    .WithSubscribe(CropMouseMove);

            CropMouseLeftButtonUpCommand =
                new ReactiveCommand<MouseButtonEventArgs>()
                    .WithSubscribe(CropMouseLeftButtonUp);

            ResizeCropCommand =
                new ReactiveCommand<DragDeltaEventArgs>()
                    .WithSubscribe(ResizeCrop);

            FileDropAreaDragOverCommand =
                new ReactiveCommand<DragEventArgs>()
                    .WithSubscribe(FileDropAreaDragOver);

            FileDropAreaDropCommand =
                new ReactiveCommand<DragEventArgs>()
                    .WithSubscribe(FileDropAreaDrop);


            // 選択画像が変わったら左側の画像を更新
            SelectedImage
                .Subscribe(path => UpdateCurrentImage(path));
        }


        // =========================
        // 画像
        // =========================

        private void UpdateCurrentImage(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                CurrentImage.Value = null;
                return;
            }

            if (!File.Exists(path))
            {
                CurrentImage.Value = null;
                return;
            }

            try
            {
                var image = new BitmapImage();

                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.EndInit();
                image.Freeze();

                CurrentImage.Value = image;
            }
            catch
            {
                CurrentImage.Value = null;
            }
        }


        // =========================
        // ファイルドロップ
        // =========================

        private void FileDropAreaDragOver(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (var file in files)
                {
                    if (IsImageFile(file))
                    {
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }


        private void FileDropAreaDrop(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var file in files)
            {
                if (!IsImageFile(file))
                    continue;

                if (!ImageFiles.Contains(file))
                {
                    ImageFiles.Add(file);
                }
            }

            // まだ画像が選択されていなければ、
            // 最初の画像を選択する
            if (string.IsNullOrEmpty(SelectedImage.Value)
                && ImageFiles.Count > 0)
            {
                SelectedImage.Value = ImageFiles[0];
            }

            e.Handled = true;
        }


        private static bool IsImageFile(string file)
        {
            var extension = Path.GetExtension(file);

            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
        }


        // =========================
        // 矩形移動
        // =========================

        private void CropMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (e.OriginalSource is Thumb)
                return;

            _isDragging = true;

            // 画面座標を取得
            _dragStart = e.GetPosition(null);

            _dragStartLeft = CropLeft.Value;
            _dragStartTop = CropTop.Value;

            if (e.Source is IInputElement element)
            {
                element.CaptureMouse();
            }

            e.Handled = true;
        }


        private void CropMouseMove(MouseEventArgs e)
        {
            if (!_isDragging)
                return;

            // 開始時と同じ座標系で取得
            Point current = e.GetPosition(null);

            double dx = current.X - _dragStart.X;
            double dy = current.Y - _dragStart.Y;

            CropLeft.Value = _dragStartLeft + dx;
            CropTop.Value = _dragStartTop + dy;
        }


        private void CropMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            _isDragging = false;

            if (e.Source is IInputElement element)
            {
                element.ReleaseMouseCapture();
            }

            e.Handled = true;
        }


        // =========================
        // 矩形リサイズ
        // =========================

        private void ResizeCrop(DragDeltaEventArgs e)
        {
            if (e.OriginalSource is not Thumb thumb)
                return;

            if (thumb.Tag is not string direction)
                return;

            double left = CropLeft.Value;
            double top = CropTop.Value;
            double width = CropWidth.Value;
            double height = CropHeight.Value;

            double dx = e.HorizontalChange;
            double dy = e.VerticalChange;

            switch (direction)
            {
                case "NW":
                    left += dx;
                    top += dy;
                    width -= dx;
                    height -= dy;
                    break;

                case "N":
                    top += dy;
                    height -= dy;
                    break;

                case "NE":
                    top += dy;
                    width += dx;
                    height -= dy;
                    break;

                case "W":
                    left += dx;
                    width -= dx;
                    break;

                case "E":
                    width += dx;
                    break;

                case "SW":
                    left += dx;
                    width -= dx;
                    height += dy;
                    break;

                case "S":
                    height += dy;
                    break;

                case "SE":
                    width += dx;
                    height += dy;
                    break;
            }

            // 最小サイズ
            if (width < MinCropSize)
            {
                if (direction.Contains("W"))
                    left -= MinCropSize - width;

                width = MinCropSize;
            }

            if (height < MinCropSize)
            {
                if (direction.Contains("N"))
                    top -= MinCropSize - height;

                height = MinCropSize;
            }

            CropLeft.Value = left;
            CropTop.Value = top;
            CropWidth.Value = width;
            CropHeight.Value = height;
        }
    }
}