/*
 * Created by Rajan Tawate.
 * User: Owner
 * Date: 9/3/2006
 * Time: 8:00 PM
 * Edited and updated by quanap5 
 */

using System;
using System.Drawing;
using System.IO;
using System.Collections;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;

/// <summary>
/// Description of ImageConverter.
/// </summary>
public static class ImageConverter
{
    /// <summary>
    /// This method is used to convert image to byte array
    /// </summary>
    /// <param name="imageIn">This is the input image</param>
    /// <returns>Return n array of byte </returns>
    public static byte[] imageToByteArray(System.Drawing.Image imageIn)
    {
        MemoryStream ms = new MemoryStream();
        imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
        return ms.ToArray();
    }
    /// <summary>
    /// This method is used to convert byte array to Image
    /// </summary>
    /// <param name="byteArrayIn">This is the input byte array</param>
    /// <returns>Return image that converted from byte array</returns>
    public static Image byteArrayToImage(byte[] byteArrayIn)
    {
        MemoryStream ms = new MemoryStream(byteArrayIn);
        Image returnImage = Image.FromStream(ms);
        return returnImage;

    }

    /// <summary>
    /// This method is used to convert Bitmap to Bitmap Image
    /// </summary>
    /// <param name="bitmap">This is Bitmap inpput image</param>
    /// <returns>Return the BitmapImage as output</returns>
    public static BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
    {
        using (var memory = new MemoryStream())
        {
            bitmap.Save(memory, ImageFormat.Png);
            memory.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }
    }
    /// <summary>
    /// This method is used to convert Image to BitmapImage
    /// </summary>
    /// <param name="img">This is Image input image</param>
    /// <returns>Return the bitmapImage as output</returns>
    public static BitmapImage Image2BitmapImage(Image img)
    {
        using (var memory = new MemoryStream())
        {
            img.Save(memory, ImageFormat.Png);
            memory.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            return bitmapImage;
        }
    }

    /// <summary>
    /// This method is used to convert BitmapImage to Bitmap
    /// </summary>
    /// <param name="bitmapImage">This is the bitmapImage input</param>
    /// <returns>Return the bitmap as output</returns>
    public static Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
    {
        // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

        using (MemoryStream outStream = new MemoryStream())
        {
            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bitmapImage));
            enc.Save(outStream);
            Bitmap bitmap = new Bitmap(outStream);

            return new Bitmap(bitmap);
        }

    }
}

