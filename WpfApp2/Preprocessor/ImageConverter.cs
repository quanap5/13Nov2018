﻿/*
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
    /// <param name="imageIn"></param>
    /// <returns></returns>
    public static byte[] imageToByteArray(System.Drawing.Image imageIn)
    {
        MemoryStream ms = new MemoryStream();
        imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
        return ms.ToArray();
    }
    /// <summary>
    /// This method is used to convert byte array to Image
    /// </summary>
    /// <param name="byteArrayIn"></param>
    /// <returns></returns>
    public static Image byteArrayToImage(byte[] byteArrayIn)
    {
        MemoryStream ms = new MemoryStream(byteArrayIn);
        Image returnImage = Image.FromStream(ms);
        return returnImage;

    }

    /// <summary>
    /// This method is used to convert Bitmap to Bitmap Image
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns></returns>
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
    /// <param name="img"></param>
    /// <returns></returns>
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
    /// <param name="bitmapImage"></param>
    /// <returns></returns>
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

