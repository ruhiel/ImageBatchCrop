using Reactive.Bindings;
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
        // コマンド
        // =========================
        public ReactiveCommand<MouseButtonEventArgs> CropMouseLeftButtonDownCommand { get; }

        public ReactiveCommand<MouseEventArgs> CropMouseMoveCommand { get; }

        public ReactiveCommand<MouseButtonEventArgs> CropMouseLeftButtonUpCommand { get; }

        public ReactiveCommand<DragDeltaEventArgs> ResizeCropCommand { get; }

        public ReactiveCommand<DragEventArgs> FileDropAreaDragOverCommand { get; }

        public ReactiveCommand<DragEventArgs> FileDropAreaDropCommand { get; }

        public ReactiveCommand ExecuteCropCommand { get; }


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

        private const double DisplayWidth = 800;

        private const double DisplayHeight = 450;

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

            ExecuteCropCommand =
                new ReactiveCommand()
                    .WithSubscribe(ExecuteCrop);


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
        // 一括切り抜き
        // =========================
        // =========================
        // 一括切り抜き
        // =========================

        private void ExecuteCrop()
        {
            if (ImageFiles.Count == 0)
            {
                MessageBox.Show(
                    "対象画像ファイルがありません。",
                    "実行",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            int successCount = 0;
            int errorCount = 0;

            foreach (var file in ImageFiles)
            {
                try
                {
                    CropImage(file);
                    successCount++;
                }
                catch
                {
                    errorCount++;
                }
            }

            // 切り抜き後の画像を再読み込み
            if (!string.IsNullOrEmpty(SelectedImage.Value))
            {
                UpdateCurrentImage(SelectedImage.Value);
            }

            if (errorCount == 0)
            {
                MessageBox.Show(
                    $"{successCount} 個の画像を切り抜きました。\n" +
                    "元画像は backup フォルダに保存されています。",
                    "実行完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    $"{successCount} 個の画像を切り抜きました。\n" +
                    $"{errorCount} 個の画像でエラーが発生しました。\n\n" +
                    "元画像は backup フォルダに保存されています。",
                    "実行完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void CropImage(string file)
        {
            // =========================
            // 元画像をバックアップ
            // =========================
            var directory = Path.GetDirectoryName(file)
                ?? throw new InvalidOperationException(
                    "画像ファイルのフォルダを取得できません。");

            var backupDirectory = Path.Combine(
                directory,
                "backup");

            Directory.CreateDirectory(backupDirectory);

            var backupFile = Path.Combine(
                backupDirectory,
                Path.GetFileName(file));

            // すでにバックアップが存在する場合は上書きしない
            if (!File.Exists(backupFile))
            {
                File.Copy(file, backupFile);
            }


            // =========================
            // 画像を読み込む
            // =========================
            var source = new BitmapImage();

            source.BeginInit();
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.UriSource = new Uri(file, UriKind.Absolute);
            source.EndInit();
            source.Freeze();


            // =========================
            // 表示座標 → 実画像座標
            // =========================
            var scaleX = source.PixelWidth / DisplayWidth;
            var scaleY = source.PixelHeight / DisplayHeight;

            var left = (int)Math.Round(
                CropLeft.Value * scaleX);

            var top = (int)Math.Round(
                CropTop.Value * scaleY);

            var width = (int)Math.Round(
                CropWidth.Value * scaleX);

            var height = (int)Math.Round(
                CropHeight.Value * scaleY);

            // =========================
            // 画像外にはみ出さないよう補正
            // =========================
            left = Math.Max(
                0,
                Math.Min(left, source.PixelWidth - 1));

            top = Math.Max(
                0,
                Math.Min(top, source.PixelHeight - 1));

            width = Math.Min(
                width,
                source.PixelWidth - left);

            height = Math.Min(
                height,
                source.PixelHeight - top);

            if (width <= 0 || height <= 0)
            {
                throw new InvalidOperationException(
                    "切り抜き範囲が画像外です。");
            }

            // =========================
            // 切り抜き
            // =========================
            var cropped = new CroppedBitmap(
                source,
                new Int32Rect(
                    left,
                    top,
                    width,
                    height));

            cropped.Freeze();


            // =========================
            // 一時ファイルへ保存
            // =========================
            var tempFile = file + ".tmp";

            try
            {
                SaveImage(
                    cropped,
                    tempFile,
                    Path.GetExtension(file));

                // =========================
                // 元ファイルを置き換え
                // =========================

                File.Delete(file);
                File.Move(tempFile, file);
            }
            catch
            {
                // 失敗した場合は一時ファイルを削除
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                throw;
            }
        }

        private static void SaveImage(
            BitmapSource image,
            string file,
            string extension)
        {
            BitmapEncoder encoder;

            switch (extension.ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;

                case ".png":
                    encoder = new PngBitmapEncoder();
                    break;

                case ".bmp":
                    encoder = new BmpBitmapEncoder();
                    break;

                case ".gif":
                    encoder = new GifBitmapEncoder();
                    break;

                default:
                    throw new NotSupportedException(
                        $"この画像形式には対応していません: {extension}");
            }

            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var stream = new FileStream(
                file,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                encoder.Save(stream);
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
            {
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var file in files)
            {
                if (!IsImageFile(file))
                {
                    continue;
                }

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
            {
                return;
            }

            if (e.OriginalSource is Thumb)
            {
                return;
            }

            _isDragging = true;

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
            {
                return;
            }

            var current = e.GetPosition(null);

            var dx = current.X - _dragStart.X;
            var dy = current.Y - _dragStart.Y;

            CropLeft.Value = _dragStartLeft + dx;
            CropTop.Value = _dragStartTop + dy;
        }


        private void CropMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

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
            {
                return;
            }

            if (thumb.Tag is not string direction)
            {
                return;
            }

            var left = CropLeft.Value;
            var top = CropTop.Value;
            var width = CropWidth.Value;
            var height = CropHeight.Value;

            var dx = e.HorizontalChange;
            var dy = e.VerticalChange;

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
                {
                    left -= MinCropSize - width;
                }

                width = MinCropSize;
            }

            if (height < MinCropSize)
            {
                if (direction.Contains("N"))
                {
                    top -= MinCropSize - height;
                }

                height = MinCropSize;
            }

            CropLeft.Value = left;
            CropTop.Value = top;
            CropWidth.Value = width;
            CropHeight.Value = height;
        }
    }
}