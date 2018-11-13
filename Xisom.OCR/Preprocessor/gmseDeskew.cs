using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using Xisom.OCR.Geometry;

namespace Xisom.OCR.Preprocessor
{
    internal class gmseDeskew
    {
        /// <summary>
        /// Representation of a line in the image
        /// </summary>
        public class HougLine
        {
            //Count of points in line 
            public int Count;
            //Index in matrix
            public int Index;
            //The  line is represented as all x,y that solve y*cos(alpha)-x*sin(alpha)=d
            public double Alpha;
            public double d;
        }

        //The Bitmap
        Bitmap cBmp;
        //The range of angles to search for line
        double cAlphaStart = -20;
        double cAlphaStep = 0.2;
        int cSteps = 40 * 5;
        //PreCalculation of sin and cos
        double[] cSinA;
        double[] cCosA;
        //Range of d
        double cDMin;
        double cDStep = 1;
        int cDCount;
        //Count of points that fit in a line
        int[] cHMatrix;

        //Calculate the skew angle of the image cBmp

        public double GetSkewAngle()
        {
            HougLine[] hl;
            //int i;
            double sum = 0;
            int count = 0;

            //Hough Transformation
            Calc();
            //Top 20 of the dedetected lines in the image
            hl = GetTop(20);
            //Average angle of the lines
            for (int i = 0; i < 19; i++)
            {
                sum += hl[i].Alpha;
                count += 1;

            }
            return sum / count;
        }

        //Calculate the Count lines in the image with most points
        private HougLine[] GetTop(int Count)
        {
            HougLine[] hl;
            int j;
            HougLine tmp;
            int AlphaIndex, dIndex;
            hl = new HougLine[Count];
            for (int i = 0; i < Count; i++)
            {
                hl[i] = new HougLine();

            }

            for (int i = 0; i < cHMatrix.Length-1; i++)
            {
                if (cHMatrix[i]>hl[Count-1].Count)
                {
                    hl[Count - 1].Count = cHMatrix[i];
                    hl[Count - 1].Index = i;
                    j = Count - 1;
                    while (j>0 && hl[j].Count > hl[j-1].Count)
                    {
                        tmp = hl[j];
                        hl[j] = hl[j - 1];
                        hl[j - 1] = tmp;
                        j -= 1;
                    }
                }
            }

            for (int i = 0; i < Count; i++)
            {
                dIndex = hl[i].Index / cSteps;
                AlphaIndex = hl[i].Index - dIndex * cSteps;
                hl[i].Alpha = GetAlpha(AlphaIndex);
                hl[i].d = dIndex + cDMin;
            }
            return hl;


        }

        public gmseDeskew(Bitmap bmp)
        {
            cBmp = bmp;
        }

        //Hough Transformation

        private void Calc()
        {
            int x;
            int y;
            int hMin = cBmp.Height/4;
            int hMax = cBmp.Height * 3 / 4;
            Init();
            for (y = hMin; y<hMax  ; y++)
            {
                for (x = 1; x < cBmp.Width -2; x++)
                {
                    //Only lower edges are considered
                    if (IsBlack(x,y) == true)
                    {
                        if (IsBlack(x,y+1) == false)
                        {
                            Calc(x, y);
                        }
                    }
                }
            }
        }

        //Calculate all lines through the point (x,y)
        private void Calc(int x, int y)
        {
            double d;
            int dIndex;
            int Index;
            for (int alpha =  0; alpha < cSteps-1; alpha++)
            {
                d = y * cCosA[alpha] - x * cSinA[alpha];
                dIndex = (int)CalcDIndex(d);
                Index = dIndex * cSteps + alpha;

                try
                {
                    cHMatrix[Index] += 1;

                }
                catch (Exception e)
                {

                    Debug.WriteLine(e.ToString());
                }

            }
        }

        private double CalcDIndex(double d)
        {
            return Convert.ToInt32(d - cDMin);
        }
        private bool IsBlack(int x,int y)
        {
            Color c;
            double luminance;
            c = cBmp.GetPixel(x, y);
            luminance = (c.R * 0.299) + (c.G * 0.587) + (c.B * 0.114);
            return luminance < 140;
        }

        private void Init()
        {
            double angle;
            //Precalculation of sin and cos
            cSinA = new double[cSteps - 1];
            cCosA = new double[cSteps - 1];
            for (int i = 0; i < cSteps-1; i++)
            {
                angle = GetAlpha(i) *Math.PI / 180.0;
                cSinA[i] = Math.Sin(angle);
                cCosA[i] = Math.Cos(angle);

            }
            //Range of d
            cDMin = -cBmp.Width;
            cDCount = (int) (2*(cBmp.Width +cBmp.Height)/cDStep);
            cHMatrix = new int[cDCount * cSteps];

        }
        public double GetAlpha(int Index)
        {
            return cAlphaStart + Index * cAlphaStep;
        }
        public static Bitmap RotateImage(Bitmap bmp, double angle)
        {
            Graphics g;
            Bitmap tmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
            tmp.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);
            g = Graphics.FromImage(tmp);
            try
            {
                g.FillRectangle(Brushes.White, 0, 0, bmp.Width, bmp.Height);
                g.RotateTransform((float)angle);
                g.DrawImage(bmp, 0, 0);

            }
            finally
            {
                g.Dispose();
            }
            return tmp;
        }

        public static Bitmap RotateImage2(Bitmap bmp, double angle)
        {
            Graphics g;
            Bitmap tmp = new Bitmap(bmp.Width, bmp.Height);
            tmp.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);
            g = Graphics.FromImage(tmp);
            try
            {
                //g.FillRectangle(Brushes.White, 0, 0, bmp.Width, bmp.Height);
                //move rotation point to center of original image
                g.TranslateTransform((float)bmp.Width / 2, (float)bmp.Height / 2);
                //rotation
                g.RotateTransform((float)angle);
                //move image back
                g.TranslateTransform(-(float)bmp.Width / 2, -(float)bmp.Height / 2);
                //darw paseed in image onto graphics object
                g.DrawImage(bmp, 0, 0);

            }
            finally
            {
                g.Dispose();
            }
            return tmp;
        }
        /// <summary>
        /// Save and read Image Deskew
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        //public static string SaveAndRead(string filename)
        //{
        //    const double MINIMUM_DESKEW_THRESHOLD = 0.05d;
        //    //string bmstr = @"C:\Users\admin\source\repos\RGB2Gray\japan3.png";
        //    var bmstr = filename;
        //    var temFile = bmstr.Substring(bmstr.LastIndexOf('\\') + 1);
        //    var temFile2 = temFile.Substring(0, temFile.LastIndexOf('.'));
        //    string graystr = bmstr.Substring(0, bmstr.LastIndexOf('\\') + 1) + temFile2 + "Deskew2.png";
        //    Console.WriteLine(graystr);

        //    Bitmap sourcebm = null;
        //    Bitmap graybm = null;

        //    if (!File.Exists(graystr))
        //    {
        //        try
        //        {
        //            Stopwatch stpWatch = Stopwatch.StartNew();
        //            sourcebm = new Bitmap(bmstr);
        //            //Using deskew algorithm here
        //            gmseDeskew skew= new gmseDeskew(sourcebm);
        //            double imageSkewAngle = skew.GetSkewAngle();
        //            if (imageSkewAngle > MINIMUM_DESKEW_THRESHOLD || imageSkewAngle <-(MINIMUM_DESKEW_THRESHOLD))
        //            {
        //                Console.WriteLine("imageSkewAngle: " + imageSkewAngle);
        //                graybm = RotateImage2((Bitmap)skew.cBmp, -imageSkewAngle);


        //                graybm.Save(graystr);
        //                stpWatch.Stop();
        //                var t = stpWatch.Elapsed.TotalMilliseconds.ToString();
        //                Console.WriteLine("Convert RGB to Gray sucessfully within {0} ms", t);
        //                return graystr;

        //            }
        //            return null;
                    
        //        }
        //        catch (Exception e)
        //        {

        //            Console.WriteLine("Error message" + e.Message);
        //            return null;
        //        }
        //        finally
        //        {
        //            if (sourcebm != null)
        //            {
        //                sourcebm.Dispose();
        //            }
        //            if (graybm != null)
        //            {
        //                graybm.Dispose();
        //            }

        //        }


        //    }
        //    return graystr;



        //}

        // Return deskewedImage
        public static Bitmap DeskewImage(Image currentImg)
        {
            const double MINIMUM_DESKEW_THRESHOLD = 0.05d;
            //string bmstr = @"C:\Users\admin\source\repos\RGB2Gray\japan3.png";
            //var bmstr = filename;
            //var temFile = bmstr.Substring(bmstr.LastIndexOf('\\') + 1);
            //var temFile2 = temFile.Substring(0, temFile.LastIndexOf('.'));
            //string graystr = bmstr.Substring(0, bmstr.LastIndexOf('\\') + 1) + temFile2 + "Deskew2.png";
            //Console.WriteLine(graystr);

            Bitmap sourcebm = null;
            Bitmap graybm = null;

            sourcebm = new Bitmap(currentImg);
            //Using deskew algorithm here
            gmseDeskew skew = new gmseDeskew(sourcebm);
            double imageSkewAngle = skew.GetSkewAngle();
            if (imageSkewAngle > MINIMUM_DESKEW_THRESHOLD || imageSkewAngle < -(MINIMUM_DESKEW_THRESHOLD))
            {
                Console.WriteLine("imageSkewAngle: " + imageSkewAngle);
                graybm = RotateImage2((Bitmap)skew.cBmp, -imageSkewAngle);
                //graybm = gmseDeskew.CropRotationRect((Bitmap)skew.cBmp, new Rectangle(26,146, 676, 109), (float)-imageSkewAngle, true);
                return graybm;

            }
            else
                return sourcebm;


        }

        //perform deskew without deskew

        public static Bitmap CropRotationRect(Bitmap source, Rectangle rect, float angle, bool HighQuality)
        {
            //gmseDeskew skew = new gmseDeskew(source);
            //double imageSkewAngle = skew.GetSkewAngle();

            Bitmap result = new Bitmap(rect.Width, rect.Height);
            using(Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = HighQuality ? InterpolationMode.HighQualityBicubic : InterpolationMode.Default;
                using (Matrix mat = new Matrix())
                {
                    mat.Translate(-rect.X , -rect.Y);
                    Debug.WriteLine("tinh tien: " + (rect.X + (int)rect.Width / 2).ToString() + (rect.Y + (int)rect.Height / 2).ToString());
                    mat.RotateAt(angle, new Point(rect.X + (int)rect.Width/2, rect.Y + (int)rect.Height / 2));
                    //mat.Translate((int)rect.Width/2, (int)rect.Height/2);
                    Debug.WriteLine("tinh tien ve: " + ((int)rect.Width / 2).ToString() + ((int)rect.Height/2).ToString());
                    g.Transform = mat;
                    g.DrawImage(source, new Point(0, 0));
                }
            }
            return result;

        }


        /// <summary>
        /// This method is used for (rotation +crop) throuht RotateCoordinate and Rect bound and Angle
        /// </summary>
        /// <param name="source">Input image</param>
        /// <param name="rect">Reference rectangle</param>
        /// <param name="angle">Angle rotation</param>
        /// <param name="RotateCoor">Rotation coordinate</param>
        /// <param name="HighQuality"></param>
        /// <returns></returns>
        public static Bitmap CropRotationRect(Bitmap source, Rectangle rect, float angle, Point RotateCoor, bool HighQuality)
        {
            //gmseDeskew skew = new gmseDeskew(source);
            //double imageSkewAngle = skew.GetSkewAngle();

            Bitmap result = new Bitmap(rect.Width, rect.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = HighQuality ? InterpolationMode.HighQualityBicubic : InterpolationMode.Default;
                using (Matrix mat = new Matrix())
                {
                    //move to rotateCoor
                    mat.Translate(-RotateCoor.X, -RotateCoor.Y);
                    //mat.Translate(-rect.Location.X, -rect.Location.Y);
                    mat.RotateAt(angle, new Point (RotateCoor.X, RotateCoor.Y));
                    mat.Translate((int)rect.Width/2,(int)rect.Height/2);
                    g.Transform = mat;
                    g.DrawImage(source, new Point(0, 0));
                }
            }
            return result;

        }

        //perform use skew angle from Deskew algorithm
        /// <summary>
        /// This method is used to find skewAngle with specificed time and skew
        /// </summary>
        /// <param name="source"></param>
        /// <param name="rect"></param>
        /// <param name="HighQuality"></param>
        /// <returns></returns>
        public static Bitmap CropRotationRect(Bitmap source, Rectangle rect)
        {
            //Crop function first
            Image img = CropImage.Crop(source, rect);

            //Get skewAngle first time
            gmseDeskew skew = new gmseDeskew(ImageConverter.BitmapImage2Bitmap(ImageConverter.Image2BitmapImage(img)));
            double imageSkewAngle = skew.GetSkewAngle();

            //Accumulate skewAngle second time
            Bitmap img2 = gmseDeskew.DeskewImage(ImageConverter.BitmapImage2Bitmap(ImageConverter.Image2BitmapImage(img)));
            skew = new gmseDeskew(img2);
            imageSkewAngle = imageSkewAngle + skew.GetSkewAngle();

            //Accumulate skewAngle third time
            //Bitmap img3 = gmseDeskew.DeskewImage(img2);
            //gmseDeskew skew3 = new gmseDeskew(img3);
            //imageSkewAngle = imageSkewAngle + skew3.GetSkewAngle();


            Debug.WriteLine("Goc skew sau 2 lan tim: " + imageSkewAngle.ToString());


            Bitmap result = new Bitmap(rect.Width, rect.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                //g.InterpolationMode = HighQuality ? InterpolationMode.HighQualityBicubic : InterpolationMode.Default;
                g.InterpolationMode = true ? InterpolationMode.HighQualityBicubic : InterpolationMode.Default;
                using (Matrix mat = new Matrix())
                {
                    mat.Translate(-rect.X, -rect.Y);
                    mat.RotateAt(-(float)imageSkewAngle, new Point(rect.X + (int)rect.Width / 2, rect.Y + (int)rect.Height / 2));
                    g.Transform = mat;
                    g.DrawImage(source, new Point(0, 0));
                }
            }
            return result;

        }
        /// <summary>
        /// This is used to automatically find skewAngle and skew
        /// </summary>
        /// <param name="source"></param>
        /// <param name="rect"></param>
        /// <param name="HighQuality"></param>
        /// <returns></returns>
        public static Bitmap CropRotationRect2(Bitmap source, Rectangle rect)
        {
            var oriRect = rect;
            int addition = (int)Math.Sqrt(rect.Width * rect.Width + rect.Height * rect.Height) - rect.Width;
            rect = new Rectangle(rect.X - (int)addition / 2, rect.Y, (int)Math.Sqrt(rect.Width * rect.Width + rect.Height * rect.Height), rect.Height);
            //Crop function first
            Image img = CropImage.Crop(source, rect);

            //Get skewAngle first time
            var skew = new gmseDeskew((Bitmap)img);
            double imageSkewAngle_temp = skew.GetSkewAngle();
            //double imageSkewAngle_temp = 100;   //initial skew angle
            double imageSkewAngle = 0;          //not skew
            int iter = 0;                       //iteration
            while (Math.Abs(imageSkewAngle_temp) > 0.5 && iter < 5)
            {
                //var skew = new gmseDeskew(ImageConverter.BitmapImage2Bitmap(ImageConverter.Image2BitmapImage(img)));
                skew = new gmseDeskew((Bitmap)img);
                imageSkewAngle_temp = skew.GetSkewAngle();
                imageSkewAngle = imageSkewAngle + imageSkewAngle_temp;
                img = gmseDeskew.DeskewImage(img);
                iter=iter+1;

            }
            
            Debug.WriteLine("Goc skew tich luy sau loop: " + imageSkewAngle.ToString());
            //Keep original rectangle form as skewAngle is not large enough
            if (Math.Abs(imageSkewAngle) <= 0.5)
            {
                rect = oriRect;
                Debug.WriteLine("Goc skew qua nho ");
            }

            Bitmap result = new Bitmap(rect.Width, rect.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                //g.InterpolationMode = HighQuality ? InterpolationMode.HighQualityBicubic : InterpolationMode.Default;
                g.InterpolationMode = true ? InterpolationMode.HighQualityBicubic : InterpolationMode.Default;
                using (Matrix mat = new Matrix())
                {
                    mat.Translate(-rect.X, -rect.Y);
                    mat.RotateAt(-(float)imageSkewAngle, new Point(rect.X + (int)rect.Width / 2, rect.Y + (int)rect.Height / 2));
                    g.Transform = mat;
                    g.DrawImage(source, new Point(0, 0));
                }
            }
            return result;

        }

        public static Bitmap CropRotationPoly(Bitmap source, Polygon2d box)
        {
            Vector2d[] hull4Points = box.Points.ToArray();
            var minAngleRef = double.MaxValue;
            //min max points

            var top = double.MinValue;
            var bottom = double.MaxValue;
            var left = double.MaxValue;
            var right = double.MinValue;
            for (int i = 0; i < hull4Points.Length; i++)
            {
                var nextIndex = i + 1;
                var current = hull4Points[i];
                var next = hull4Points[nextIndex % hull4Points.Length];
                var segmentRef = new Segment2d(current, next);


                //get the angle of considered segment and x axis
                var angleRef = AngleToXAxis(segmentRef);

                if (Math.Abs(minAngleRef) > Math.Abs(angleRef))
                {
                    top = double.MinValue;
                    bottom = double.MaxValue;
                    left = double.MaxValue;
                    right = double.MinValue;
                    minAngleRef = angleRef;
                    foreach (var p in hull4Points)
                    {
                        var rotatedPoint = RotateToXAxis(p, angleRef);

                        top = Math.Max(top, rotatedPoint.Y);
                        bottom = Math.Min(bottom, rotatedPoint.Y);

                        left = Math.Min(left, rotatedPoint.X);
                        right = Math.Max(right, rotatedPoint.X);
                    }
                }

            }




            GraphicsPath gp = new GraphicsPath();
            Point[] polyPoints = new Point[4];
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;
            foreach (var item in box.Points)
            {
                minX = Math.Min(minX, item.X);
                minY = Math.Min(minY, item.Y);
                maxX = Math.Max(maxX, item.X);
                maxY = Math.Max(maxY, item.Y);
            }


            gp = box.GraphicsPath;

            var w = (int)(10 + right - left);
            var h = (int)(10 + top-bottom);
            int centerX = (int)(box.Points[0].X + box.Points[2].X) / 2;
            int centerY = (int)(box.Points[0].Y + box.Points[2].Y) / 2;

            var rect = new Rectangle((int)minX-10, (int)minY-10, (int)(maxX - minX)+20, (int)(maxY - minY) +20);
            //Crop function first
            Image img = CropImage.Crop(source, rect);

            //Get skewAngle first time
            var skew = new gmseDeskew((Bitmap)img);
            double imageSkewAngle_temp = skew.GetSkewAngle();
            //double imageSkewAngle_temp = 100;   //initial skew angle
            double imageSkewAngle = 0;          //not skew
            int iter = 0;                       //iteration
            while (Math.Abs(imageSkewAngle_temp) > 0.5 && iter < 5)//Math.Abs(imageSkewAngle_temp) > 3
            {
                //var skew = new gmseDeskew(ImageConverter.BitmapImage2Bitmap(ImageConverter.Image2BitmapImage(img)));
                skew = new gmseDeskew((Bitmap)img);
                imageSkewAngle_temp = skew.GetSkewAngle();
                imageSkewAngle = imageSkewAngle + imageSkewAngle_temp;
                img = gmseDeskew.DeskewImage(img);
                iter = iter + 1;

            }

            if (Math.Abs(imageSkewAngle) <= 0.5)
            {
                //angle = 0;
                imageSkewAngle = 0;
                Debug.WriteLine("Goc skew qua nho ");
            }

            var segment = new Segment2d(box.Points[0], box.Points[1]);
            //get the angle of considered segment and x axis
            
            var delta = segment.A - segment.B;
            var angle = Math.Atan(delta.Y / delta.X)*180.0/Math.PI;
            //var angle = 0d;

            rect = new Rectangle(centerX - w / 2, centerY - h / 2, w, h);
            Bitmap result = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(result))
            {
                //g.InterpolationMode = HighQuality ? InterpolationMode.HighQualityBicubic : InterpolationMode.Default;
                g.InterpolationMode = true ? InterpolationMode.HighQualityBicubic : InterpolationMode.Default;
                //g.TranslateTransform(centerX, centerY);
                //g.RotateTransform(-(float)imageSkewAngle);
                //g.TranslateTransform(-centerX, -centerY);
                using (Matrix mat = new Matrix())
                {

                    //mat.Translate(-(centerX - 10 + w), -(centerY +h));
                    ////mat.RotateAt((float)(minAngleRef * 180.0 / Math.PI), new Point(centerX, centerY));
                    //mat.RotateAt(-(float)imageSkewAngle, new Point(centerX-w/2 , centerY-h/2));
                    //g.Transform = mat;
                    ////g.Clip = new Region(gp);
                    //g.DrawImage(source, new Point(0, 0));

                    mat.Translate(-(rect.X), -(rect.Y));
                    mat.RotateAt(-(float)imageSkewAngle, new Point(rect.X + (int)rect.Width / 2, rect.Y + (int)rect.Height / 2));
                    g.Transform = mat;
                    g.DrawImage(source, new Point(0, 0));

                }

            }
            //result.Save("Quan4.png");
            return result;
        }

        /// <summary>
        /// This method is used to calculate angle to the X axis
        /// </summary>
        /// <param name="s">The segment is considering</param>
        /// <returns>Return angle the considered segment and X axis</returns>
        static double AngleToXAxis(Segment2d s)
        {
            var delta = s.A - s.B;
            return -Math.Atan(delta.Y / delta.X);
        }
        /// <summary>
        /// This method is used to rotate vector by an angle to the x axis
        /// </summary>
        /// <param name="v">Vector to rotate</param>
        /// <param name="angle">Input angle for rotating</param>
        /// <returns>Rotated vector</returns>
        static Vector2d RotateToXAxis(Vector2d v, double angle)
        {
            var newX = v.X * Math.Cos(angle) - v.Y * Math.Sin(angle);
            var newY = v.X * Math.Sin(angle) + v.Y * Math.Cos(angle);

            return new Vector2d(newX, newY);
        }
    }
}
