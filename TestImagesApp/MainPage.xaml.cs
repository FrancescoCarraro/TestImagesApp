using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.FileProperties;
using static System.Net.Mime.MediaTypeNames;
using Windows.Storage.Streams;
using System.Threading;
using Windows.Graphics.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestImagesApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private CancellationTokenSource CancellationTokenSource;
        private CancellationToken CancellationToken;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async Task readImageFileAsync()
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".bmp");

            StorageFile imageFile = await openPicker.PickSingleFileAsync();
            if (imageFile == null)
                return;

            await showImageAsync(imageFile);
        }

        private void buttonLoad_Tapped(object sender, TappedRoutedEventArgs e)
        {
            readImageFileAsync();
        }

        private async void buttonFind_Tapped(object sender, TappedRoutedEventArgs e)
        {
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

            IReadOnlyList<StorageFile> files = await new FilesFinder().findFilesAsync();

            foreach (StorageFile file in files)
            {
                if (CancellationToken.IsCancellationRequested)
                    break;

                await showImageAsync(file);
                await Task.Delay(300);
            }
        }

        private async Task showImageAsync(StorageFile imageFile)
        {
            try
            {
                textblockFilename.Text = imageFile.Name;

                var p = await imageFile.GetBasicPropertiesAsync();
                ImageProperties imageProperties = await imageFile.Properties.GetImagePropertiesAsync();

                int requestedHeight = 250;
                double ratio = (double)imageProperties.Width / (double)imageProperties.Height;
                int requestedWidth = (int)(requestedHeight * ratio);

                int requestedSize = Math.Max(requestedWidth, requestedHeight);

                //var thumbnail = await imageFile.GetScaledImageAsThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.PicturesView,
                //    190, ThumbnailOptions.UseCurrentScale);
                var thumbnail = await imageFile.GetThumbnailAsync(ThumbnailMode.SingleItem, 250);
                if (thumbnail == null || thumbnail.Type == ThumbnailType.Icon)
                    thumbnail = await imageFile.GetThumbnailAsync(ThumbnailMode.SingleItem, (uint)requestedSize, ThumbnailOptions.UseCurrentScale);
                if (thumbnail == null || thumbnail.Type == ThumbnailType.Icon)
                    thumbnail = await imageFile.GetThumbnailAsync(ThumbnailMode.SingleItem, (uint)requestedSize, ThumbnailOptions.ResizeThumbnail);
                if (thumbnail == null || thumbnail.Type == ThumbnailType.Icon)
                    thumbnail = await imageFile.GetThumbnailAsync(ThumbnailMode.SingleItem, (uint)requestedSize, ThumbnailOptions.ReturnOnlyIfCached);
                if (thumbnail == null || thumbnail.Type == ThumbnailType.Icon)
                    thumbnail = await imageFile.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem);
                if (thumbnail == null || thumbnail.Type == ThumbnailType.Icon)
                    thumbnail = await imageFile.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem, (uint)requestedSize, ThumbnailOptions.UseCurrentScale);
                if (thumbnail == null || thumbnail.Type == ThumbnailType.Icon)
                    thumbnail = await imageFile.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem, (uint)requestedSize, ThumbnailOptions.ReturnOnlyIfCached);
                if (thumbnail == null || thumbnail.Type == ThumbnailType.Icon)
                    thumbnail = await imageFile.GetScaledImageAsThumbnailAsync(ThumbnailMode.SingleItem, (uint)requestedSize, ThumbnailOptions.ResizeThumbnail);

                if (thumbnail == null)
                    return;

                System.Diagnostics.Debug.WriteLine($"DISPLAYING {imageFile.Name} - " +
                    $"w x h: {requestedWidth} x {requestedHeight}, " +
                    $"IsAvailable: {imageFile.IsAvailable}, " +
                    $"type: {thumbnail.Type}");

                byte[] bytes = new byte[thumbnail.AsStreamForRead().Length];
                await thumbnail.AsStreamForRead().ReadAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                //System.Diagnostics.Debug.WriteLine($"BYTES: {string.Join(",", bytes.Skip(0).Take(2000))}");

                BitmapImage bitmapImage = new BitmapImage()
                {
                    DecodePixelWidth = (int)thumbnail.OriginalWidth,
                    DecodePixelHeight = (int)thumbnail.OriginalHeight,
                };

                image.Width = requestedWidth;
                image.Height = requestedHeight;
                image.Source = bitmapImage;

                using (InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream())
                {
                    await randomAccessStream.WriteAsync(bytes.AsBuffer());
                    randomAccessStream.Seek(0);

                    await bitmapImage.SetSourceAsync(randomAccessStream);
                }
            }
            catch (Exception ex)
            {
                image.Source = null;
                System.Diagnostics.Debug.WriteLine($"{nameof(showImageAsync)} - {imageFile.Name} - ex: {ex.Message}");
            }
        }

        private async Task showImageNewAsync(StorageFile imageFile)
        {
            try
            {
                textblockFilename.Text = imageFile.Name;

                ImageProperties imageProperties = await imageFile.Properties.GetImagePropertiesAsync();

                int requestedHeight = 250;
                double ratio = (double)imageProperties.Width / (double)imageProperties.Height;
                int requestedWidth = (int)(requestedHeight * ratio);

                int requestedSize = Math.Max(requestedWidth, requestedHeight);

                System.Diagnostics.Debug.WriteLine($"DISPLAYING {imageFile.Name} - w x h: {requestedWidth} x {requestedHeight}");

                byte[] bytes = null;
                using (var resizingStream = await imageFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    byte[] originalbytes = new byte[resizingStream.AsStreamForRead().Length];
                    await resizingStream.ReadAsync(originalbytes.AsBuffer(), (uint)originalbytes.Length, InputStreamOptions.None);
                    bytes = await resizeImageAsync(originalbytes, requestedWidth, requestedHeight, quality: 1);
                }

                //var thumbnail = await imageFile.GetScaledImageAsThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem);
                //var thumbnail = await imageFile.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem);
                //byte[] bytes = new byte[thumbnail.AsStreamForRead().Length];
                //await thumbnail.AsStreamForRead().ReadAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                //System.Diagnostics.Debug.WriteLine($"BYTES: {string.Join(",", bytes.Skip(6000).Take(2000))}");

                BitmapImage bitmapImage = new BitmapImage()
                {
                    DecodePixelWidth = requestedWidth,
                    DecodePixelHeight = requestedHeight,
                };

                image.Width = requestedWidth;
                image.Height = requestedHeight;
                image.Source = bitmapImage;

                using (InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream())
                {
                    await randomAccessStream.WriteAsync(bytes.AsBuffer());
                    randomAccessStream.Seek(0);

                    await bitmapImage.SetSourceAsync(randomAccessStream);
                }
            }
            catch (Exception ex)
            {
                image.Source = null;
                System.Diagnostics.Debug.WriteLine($"{nameof(showImageAsync)} - {imageFile.Name} - ex: {ex.Message}");
            }
        }

        public async Task<byte[]> resizeImageAsync(byte[] imageData, int reqWidth, int reqHeight, int quality)
        {

            var memStream = new MemoryStream(imageData);

            IRandomAccessStream imageStream = memStream.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(imageStream);
            if (decoder.PixelHeight > reqHeight || decoder.PixelWidth > reqWidth)
            {
                using (imageStream)
                {
                    using (var resizedStream = new InMemoryRandomAccessStream())
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
                        double widthRatio = (double)reqWidth / decoder.PixelWidth;
                        double heightRatio = (double)reqHeight / decoder.PixelHeight;

                        double scaleRatio = Math.Min(widthRatio, heightRatio);

                        if (reqWidth == 0)
                            scaleRatio = heightRatio;

                        if (reqHeight == 0)
                            scaleRatio = widthRatio;

                        uint aspectHeight = (uint)Math.Floor(decoder.PixelHeight * scaleRatio);
                        uint aspectWidth = (uint)Math.Floor(decoder.PixelWidth * scaleRatio);

                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;

                        encoder.BitmapTransform.ScaledHeight = aspectHeight;
                        encoder.BitmapTransform.ScaledWidth = aspectWidth;

                        await encoder.FlushAsync();
                        resizedStream.Seek(0);
                        var outBuffer = new byte[resizedStream.Size];
                        await resizedStream.ReadAsync(outBuffer.AsBuffer(), (uint)resizedStream.Size, InputStreamOptions.None);
                        return outBuffer;
                    }
                }
            }
            return imageData;
        }

        private void buttonStop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (CancellationTokenSource != null)
                CancellationTokenSource.Cancel();
        }
    }
}
