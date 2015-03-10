using System;
using System.Collections.Generic;
using System.Text;


using ZXing;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Storage;
using Windows.Storage.Pickers;

namespace the3DSLoadForWin
{
    class QRCode
    { 
        public static WriteableBitmap GenerateQR(int width, int height, string text)
        {
            var bw = new ZXing.BarcodeWriter();
            var encOptions = new ZXing.Common.EncodingOptions() { Width = width, Height = height, Margin = 0 };
            bw.Options = encOptions;
            bw.Format = ZXing.BarcodeFormat.QR_CODE;
            var result = bw.Write(text);
            Windows.UI.Xaml.Media.Imaging.WriteableBitmap image = new Windows.UI.Xaml.Media.Imaging.WriteableBitmap(result.Width, result.Heigth);
            using (var stream = image.PixelBuffer.AsStream())
            {
                stream.Write(result.Pixel,0,result.Pixel.Length);
            }
            return image;
        }
    }
}
