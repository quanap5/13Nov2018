using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Accord.MachineLearning;
using Accord.Math.Distances;
using Accord.Math;
using Accord.Math.Geometry;
using Accord.Statistics.Models.Regression.Linear;
using Accord.Statistics.Filters;
using System.Data;
using Xisom.OCR.Geometry;
using System.Drawing;

namespace Xisom.OCR.Preprocessor

{
    //Point2d
    public class Point2d
    {
        public int x;
        public int y;
        public float SWT;
        public override string ToString()
        {
            return "x:" + x + " y:" + y + " SWT:" + SWT;
        }
    }
    //Point2dFloat
    public struct Point2dFloat
    {
        public float x;
        public float y;
    }
    //Point3dFloat
    public struct Point3dFloat
    {
        public float x;
        public float y;
        public float z;
    }
    //Ray type
    public class Ray
    {
        public Point2d p;
        public Point2d q;
        public List<Point2d> points;
        public bool valid;
    }
    // Rays class
    public class Rays
    {
        public float median;
        public float variance;
        public float stdev;
        public float average;
        public List<double> rayLengths;
        public List<Ray> rays;

        public Rays(List<Ray> rays)
        {
            this.rays = new List<Ray>(rays);
            rayLengths = new List<double>();

            foreach (Ray ray in rays)
            {
                if (ray.p.x == ray.q.x && ray.p.y == ray.q.y)
                    rayLengths.Add(0);
                else
                    rayLengths.Add(Math.Sqrt(Math.Pow(ray.p.x - ray.q.x, 2) + Math.Pow(ray.p.y - ray.q.y, 2)));
            }

            this.GetStats();
        }

        public void SetValid(bool valid)
        {
            for (int i = 0; i < this.rays.Count(); i++)
            {
                this.rays[i].valid = valid;
            }

        }
        /// <summary>
        /// This method is used to get statistical information of Rays
        /// </summary>
        public void GetStats()
        {

            average = (float)rayLengths.Average();
            variance = (float)GetVariance(rayLengths);
            stdev = (float)Math.Sqrt(variance);
            List<double> rayLengthsCopy = new List<double>(rayLengths);
            rayLengthsCopy.Sort();
            median = (float)rayLengthsCopy[rayLengthsCopy.Count() / 2];


        }
        /// <summary>
        /// This method is used to calculate variance
        /// </summary>
        /// <param name="rayLengths"></param>
        /// <returns></returns>
        public static double GetVariance(List<double> rayLengths)
        {
            if (rayLengths.Count() <= 1)
                return 0;
            else
            {
                double average = rayLengths.Average();
                double sum = 0;
                foreach (double r in rayLengths)
                {
                    sum += Math.Pow(r - average, 2);
                }

                return sum / (rayLengths.Count() - 1);
            }
        }
    }



    // Recursive
    public struct Observation
    {
        public double[] observation;
        public int index;
    }

    public struct KmeanObservation
    {
        public List<Observation> observationsList;
        public bool continueKmean;
    }
    //Chain
    public struct Chain
    {
        public int p;
        public int q;
        public float dist;
        public bool merged;
        public Point2dFloat direction;
        public List<int> components;
    }

    //Kmean based region detection including original rect and minimal bouding rect
    public struct OCRRegion
    {
        
        public  Tuple<CvPoint, CvPoint> rect;
        public Polygon2d mininalbox;
        public Rectangle rect2;

        public OCRRegion(Tuple<CvPoint, CvPoint> rect, Polygon2d mininalbox) : this()
        {
            this.rect = rect;
            this.mininalbox = mininalbox;
            this.rect2 = new Rectangle(rect.Item1.X,rect.Item1.Y, rect.Item2.X - rect.Item1.X, rect.Item2.Y - rect.Item1.Y);
        }

        public OCRRegion(Tuple<CvPoint, CvPoint> rect, Polygon2d mininalbox, Rectangle rect2) : this()
        {
            this.rect = rect;
            this.mininalbox = mininalbox;
            this.rect2 = rect2;
        }
    }
    

    public class Graph : Dictionary<int, GraphNode>
    {
        public Graph()
        {

        }

        public void Add(int key, int value)
        {
            if (this.ContainsKey(key))
            {
                this[key].adjacency.Add(value);

            }
            else
            {
                GraphNode gn = new GraphNode(key);
                gn.adjacency.Add(value);
                base.Add(key, gn);
            }
        }

        public void Add(int key)
        {
            if (this.ContainsKey(key))
            {
                // do nothing
            }
            else
            {
                GraphNode gn = new GraphNode(key);
                base.Add(key, gn);
            }
        }

        public void ResetVisited()
        {
            foreach (KeyValuePair<int, GraphNode> graphNode in this)
            {
                graphNode.Value.visited = false;
            }
        }

        public GraphNode NextUnvisited()
        {
            foreach (KeyValuePair<int, GraphNode> graphNode in this)
            {
                if (graphNode.Value.visited == false)
                    return graphNode.Value;

            }

            return null;
        }

    }

    public class GraphNode
    {
        public int vertexValue;
        public List<int> adjacency;
        public bool visited = false;

        public GraphNode()
        {
        }

        public GraphNode(int key)
        {
            vertexValue = key;
            adjacency = new List<int>();
            visited = false;
        }
    }

    /// <summary>
    /// This method is used to compare the two input points in term of SWT  
    /// </summary>
    public class Point2dComparer : IComparer<Point2d>
    {
        public int Compare(Point2d lhs, Point2d rhs)
        {
            if (lhs.SWT == rhs.SWT)
            {
                return 0;
            }
            else if (lhs.SWT > rhs.SWT)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }
    }


    public class ChainSortDistComparer : IComparer<Chain>
    {
        public int Compare(Chain lhs, Chain rhs)
        {
            if (lhs.dist == rhs.dist)
            {
                return 0;
            }
            else if (lhs.dist > rhs.dist)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }
    }

    public class ChainSortLengthComparer : IComparer<Chain>
    {
        public int Compare(Chain lhs, Chain rhs)
        {
            if (lhs.dist == rhs.dist)
            {
                return 0;
            }
            else if (lhs.dist > rhs.dist)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }
    }

    /// <summary>
    /// This class is used to calculate Stroke Width Transform 
    /// </summary>
     class SWT
    {
        public static List<Tuple<CvPoint, CvPoint>> FindBoundingBoxes(
            List<List<Point2d>> components,
            List<Chain> chains,
            List<Tuple<Point2d, Point2d>> compBB,
            IplImage output)
        {

            List<Tuple<CvPoint, CvPoint>> bb = new List<Tuple<CvPoint, CvPoint>>();

            bb.Capacity = chains.Count();
            foreach (Chain chain in chains)
            {
                int minx = output.Width;
                int miny = output.Height;
                int maxx = 0;
                int maxy = 0;

                foreach (int cit in chain.components)
                {
                    miny = Math.Min(miny, compBB[cit].Item1.y);
                    minx = Math.Min(minx, compBB[cit].Item1.x);
                    maxy = Math.Max(maxy, compBB[cit].Item2.y);
                    maxx = Math.Max(maxx, compBB[cit].Item2.x);
                }

                CvPoint p0 = new CvPoint(minx, miny);
                CvPoint p1 = new CvPoint(maxx, maxy);
                bb.Add(new Tuple<CvPoint, CvPoint>(p0, p1));

            }

            return bb;
        }

        public static List<Tuple<CvPoint, CvPoint>> FindBoundingBoxes(
            List<List<Point2d>> components,
            IplImage output)
        {

            List<Tuple<CvPoint, CvPoint>> bb = new List<Tuple<CvPoint, CvPoint>>();

            bb.Capacity = components.Count();

            foreach (List<Point2d> chain in components)
            {
                int minx = output.Width;
                int miny = output.Height;
                int maxx = 0;
                int maxy = 0;

                foreach (Point2d cit in chain)
                {
                    miny = Math.Min(miny, cit.y);
                    minx = Math.Min(minx, cit.x);
                    maxy = Math.Max(maxy, cit.y);
                    maxx = Math.Max(maxx, cit.x);
                }

                CvPoint p0 = new CvPoint(minx, miny);
                CvPoint p1 = new CvPoint(maxx, maxy);
                bb.Add(new Tuple<CvPoint, CvPoint>(p0, p1));

            }
            return bb;
        }

        public static void NormalizeImage(IplImage input, IplImage output)
        {
            double maxVal;
            double minVal;

            Cv.MinMaxLoc(input, out minVal, out maxVal);
            Cv.Normalize(input, output, maxVal, minVal);

        }

        public static void ConvertColorHue(IplImage origImage, IplImage convertedImage)
        {
            IplImage LABimage = Cv.CreateImage(origImage.GetSize(), BitDepth.U8, 3);
            Cv.CvtColor(origImage, LABimage, ColorConversion.BgrToLab);
            origImage.SetROI(new CvRect(195, 121, 6, 7));
            IplImage whiteBlock = Cv.CreateImage(origImage.GetSize(), BitDepth.U8, 3);
            Cv.Copy(origImage, whiteBlock);
            IplImage whiteLAB = Cv.CreateImage(whiteBlock.GetSize(), BitDepth.U8, 3);
            Cv.CvtColor(whiteBlock, whiteLAB, ColorConversion.BgrToLab);
            origImage.ResetROI();

            unsafe
            {
                byte* whitePtr = (byte*)whiteLAB.ImageData.ToPointer();
                int whiteWidthStep = whiteLAB.WidthStep;
                int count = whiteLAB.Height * whiteLAB.Width;
                double L = 0, A = 0, B = 0;
                for (int i = 0; i < whiteLAB.Height; i++)
                {
                    for (int j = 0; j < whiteLAB.Width; j++)
                    {
                        L += whitePtr[i * whiteWidthStep + 3 * j + 0];
                        A += whitePtr[i * whiteWidthStep + 3 * j + 1];
                        B += whitePtr[i * whiteWidthStep + 3 * j + 2];
                    }
                }
                L = L / count;
                A = A / count;
                B = B / count;


                byte* convertedPtr = (byte*)convertedImage.ImageData.ToPointer();
                int convertedWidthStep = convertedImage.WidthStep;

                byte* LABPtr = (byte*)LABimage.ImageData.ToPointer();
                int LABWidthStep = LABimage.WidthStep;

                double Luminance = 0, Alpha = 0, Beta = 0;
                for (int i = 0; i < convertedImage.Height; i++)
                {
                    for (int j = 0; j < convertedImage.Width; j++)
                    {
                        Luminance = LABPtr[i * LABWidthStep + 3 * j] - L;
                        Alpha = LABPtr[i * LABWidthStep + 3 * j + 1] - A;
                        Beta = LABPtr[i * LABWidthStep + 3 * j + 2] - B;
                        convertedPtr[i * convertedWidthStep + j] = (byte)(Math.Sqrt(Luminance * Luminance + Alpha * Alpha + Beta * Beta) / Math.Sqrt(3));
                    }
                }

            }


        }
        /// <summary>
        /// This method is used to detect the text region from input image
        /// </summary>
        /// <param name="input">This is the input image</param>
        /// <param name="darkOnLight">This is information whether text is darker or lighter than background </param>
        /// <returns>Currently, we expect the bounding rectangle of the text using SWT based method</returns>
        public static List<OCRRegion> textDetection(KmeanOption kmeanOption, IplImage input, bool darkOnLight)
        {
            //List<Tuple<CvPoint, CvPoint>>
            // convert to grayscale, could do better with color subtraction if we know what white will look like
            IplImage grayImg = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            Cv.CvtColor(input, grayImg, ColorConversion.BgrToGray);
            Cv.SaveImage("aGray.png", grayImg);

            //ConvertColorHue(input, grayImg);
            //Cv.SaveImage("hue.png", grayImg);

            // create canny --> hard to automatically find parameters...
            double threshLow = 10;//5
            double threshHigh = 200;//50
            IplImage edgeImg = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            Cv.Canny(grayImg, edgeImg, threshLow, threshHigh, ApertureSize.Size3);
            Cv.SaveImage("1Canny.png", edgeImg);

            // create gradient x, gradient y
            IplImage gaussianImg = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            Cv.ConvertScale(grayImg, gaussianImg, 1.0 / 255.0, 0);
            //Gaussian smoothing is commonly used with eadge detection
            Cv.Smooth(gaussianImg, gaussianImg, SmoothType.Gaussian, 5, 5); //Gaussian filter 5x5
            IplImage gradientX = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            IplImage gradientY = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            Cv.Sobel(gaussianImg, gradientX, 1, 0, ApertureSize.Scharr);
            Cv.Sobel(gaussianImg, gradientY, 0, 1, ApertureSize.Scharr);
            Cv.Smooth(gradientX, gradientX, SmoothType.Blur, 3, 3);
            Cv.Smooth(gradientY, gradientY, SmoothType.Blur, 3, 3);
            //Cv.SaveImage("GradientX.png", gradientX);
            //Cv.SaveImage("GradientY.png", gradientY);


            // Calculate SWT and return ray vectors
            List<Ray> rays = new List<Ray>();
            IplImage SWTImage = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            SWTImage.Set(-1);

            // Stroke width transform from edge image
            StrokeWidthTransform(edgeImg, gradientX, gradientY, darkOnLight, SWTImage, rays);
            IplImage saveSWT = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            Cv.ConvertScale(SWTImage, saveSWT, 255, 0);
            Cv.SaveImage("2SWT.png", saveSWT);

            // Stroke Width Transform using Median filter
            SWTMedianFilter(SWTImage, rays);
            Cv.ConvertScale(SWTImage, saveSWT, 255, 0);
            Cv.SaveImage("3SWTMedianFilter.png", saveSWT);

            
            //Cv.SaveImage("3InverseSWTMedianFilter.png", saveSWT);

            // Check the ray if it are deviating from meadian value -> remove it (clean)
            ////IplImage cleanSWTImage = Cv.CreateImage( input.GetSize(), BitDepth.F32, 1 );
            IplImage cleanSWTImage = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            cleanSWTImage.Set(-1);
            FilterRays(SWTImage, rays, cleanSWTImage);
            Cv.ConvertScale(cleanSWTImage, saveSWT, 255, 0);
            Cv.SaveImage("4CleanSWTImage.png", saveSWT);

            //// normalize
            IplImage output2 = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            NormalizeImage(SWTImage, output2);

            //// binarize and close with rectangle to fill gaps from cleaning
            ////cleanSWTImage = SWTImage;
            IplImage binSWTImage = Cv.CreateImage(cleanSWTImage.GetSize(), BitDepth.U8, 1);
            IplImage tempImg = Cv.CreateImage(cleanSWTImage.GetSize(), BitDepth.U8, 1);
            Cv.Threshold(SWTImage, binSWTImage, 0 , 255, ThresholdType.Binary);
            Cv.ConvertScale(binSWTImage, saveSWT, 255, 0);
            Cv.SaveImage("5newNhiphan.png", saveSWT);

            IplImage binSWTImage2 = Cv.CreateImage(binSWTImage.GetSize(), BitDepth.U8, 1);
            //binSWTImage2 = Cv.Invert
            //Cv.SaveImage("5newNhiphan.png", saveSWT);


            //Cv.Dilate(saveSWT, saveSWT);
            //Cv.Erode(saveSWT, saveSWT);
            //Cv.SaveImage("5Dilation_SWTmedian.png", saveSWT);
            //Cv.MorphologyEx(binSWTImage, binSWTImage, tempImg, new IplConvKernel(5, 17, 2, 8, ElementShape.Rect), MorphologyOperation.Gradient);
            //Cv.MorphologyEx(binSWTImage, binSWTImage, tempImg, new IplConvKernel(21, 3, 10, 2, ElementShape.Rect), MorphologyOperation.TopHat);

            //Cv.ConvertScale(binSWTImage, saveSWT, 255, 0);
            //Cv.SaveImage("5SWT_Morphology.png", saveSWT);


            //Cv.Threshold(SWTImage, binSWTImage, (int)10 * Cv.Avg(SWTImage).Val0, 255, ThresholdType.Binary);
            //Cv.ConvertScale(binSWTImage, saveSWT, 255, 0);
            ////kernel = Cv.CreateStructuringElementEx(Cv.MorphologyEx, (3, 3))
            //// to manipulate the orientation of dilution, large x means horizonatally dilating  more, large y means vertically dilating more
            //var S1 = Cv.CreateStructuringElementEx(3, 1, 1, 0, ElementShape.Rect, null);
            ////Cv.Dilate(SWTImage, SWTImage, S1);
            //Cv.Dilate(SWTImage, SWTImage);
            //Cv.Erode(SWTImage, SWTImage);
            //Cv.ConvertScale(SWTImage, saveSWT, 255, 0);
            //Cv.SaveImage("5Nhiphan.png", saveSWT);

            //IplImage saveSWT = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            //Cv.ConvertScale(output2, saveSWT, 255, 0);
            //Cv.SaveImage("SWT.png", saveSWT);


            //// Calculate legally connect components from SWT and gradient image.
            //// return type is a vector of vectors, where each outer vector is a component and
            //// the inner vector contains the (y,x) of each pixel in that component.
            //List<List<Point2d>> components = findLegallyConnectedComponents( SWTImage, rays );

            //Cung hay
            //cleanSWTImage = output2;
            cleanSWTImage = SWTImage; //quan
            //cleanSWTImage = binSWTImage;

            //Components analysis corresponding to character: ORIGINAL
            //List<List<Point2d>> components = FindLegallyConnectedComponents(cleanSWTImage, rays);

            //Components analysis corresponding to character: OTHER
            IplImage binFloatImg = Cv.CreateImage(binSWTImage.GetSize(), BitDepth.F32, 1);
            //cleanSWTImage = binFloatImg;
            Cv.Convert(binSWTImage, binFloatImg);
            List<List<Point2d>> components = FindLegallyConnectedComponents(binFloatImg, rays);


            // Filter the components
            List<List<Point2d>> validComponents = new List<List<Point2d>>();
            List<Point2dFloat> compCenters = new List<Point2dFloat>();
            List<float> compMedians = new List<float>();
            List<Point2d> compDimensions = new List<Point2d>();
            List<Tuple<Point2d, Point2d>> compBB = new List<Tuple<Point2d, Point2d>>();

            FilterComponents(cleanSWTImage, components, ref validComponents, ref compCenters, ref compMedians, ref compDimensions, ref compBB);
            //validComponents = components;
            IplImage outComponent = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            RenderComponentsWithBoxes(cleanSWTImage, validComponents, compBB, outComponent);
            Cv.SaveImage("6SWT_Components.png", outComponent);
            //List<Tuple<CvPoint, CvPoint>> quanRect = new List<Tuple<CvPoint, CvPoint>>();
            //quanRect = FindBoundingBoxesAll2(compCenters, compBB, cleanSWTImage);//quan them
            //quanRect = FindBoundingBoxesAll(compBB, cleanSWTImage);//quan them lan 1

            //IplImage output3 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3); //save
            //RenderComponentsWithBoxes2(cleanSWTImage, compCenters, components, compBB, output3);
            //Cv.SaveImage("componentsWithK-mean.png", output3);
            //cvReleaseImage ( &output3 );

            //// Make chains of components
            List<Chain> chains;
            chains = makeChains(input, validComponents, compCenters, compMedians, compDimensions, compBB);
            //List<Chain> chains;
            //chains = makeMorphologicalChains(input, chains1, validComponents, compCenters, compMedians, compDimensions, compBB);


            IplImage outChain = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            renderChainsWithBoxes(SWTImage, validComponents, compCenters, chains, compBB, outChain);
            Cv.SaveImage("7SWT_Chains.png", outChain);


            List<Tuple<CvPoint, CvPoint>> kmeanResult = new List<Tuple<CvPoint, CvPoint>>();
            List<OCRRegion> kmeanResult2 = new List<OCRRegion>();
            //kmeanResult = makeKmeanComponents(kmeanOption, input, chains, validComponents, compCenters, compMedians, compDimensions, compBB);// original
            kmeanResult2 = makeKmeanChains(kmeanOption, input, chains, validComponents, compCenters, compMedians, compDimensions, compBB);

            //kmeanResult = makeMorphologicalChains(input, chains, validComponents, compCenters, compMedians, compDimensions, compBB);
            //options for kmean
            //kmeanResult = makeKmeanOptionComponents(true, false, false, false, input, chains, validComponents, compCenters, compMedians, compDimensions, compBB);

            IplImage outputRender = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            RenderKmeanWithBoxesonImage(cleanSWTImage, components, kmeanResult, outputRender);
            Cv.SaveImage("8AccordKmean.png", outputRender);

            IplImage output4 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            renderChains(SWTImage, validComponents, chains, output4);
            Cv.SaveImage("9text.png", output4);

            ////IplImage output5 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            ////Cv.CvtColor(output4, output5, ColorConversion.GrayToRgb);
            ////Cv.SaveImage("text2.png", output5);

            //IplImage output6 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            ////List<Tuple<CvPoint, CvPoint>> quanRect2 = new List<Tuple<CvPoint, CvPoint>>();
            //var quanRect2 = renderChainsWithBoxes(SWTImage, validComponents, compCenters, chains, compBB, output6);
            //Cv.SaveImage("text3.png", output6);

            ////k-mean2
            //List<Tuple<CvPoint, CvPoint>> quanRectKmean = new List<Tuple<CvPoint, CvPoint>>();
            ////quanRectKmean = FindBoundingBoxesAll3(compCenters, quanRect2, cleanSWTImage);//quan them
            //quanRectKmean = FindBoundingBoxesAll2(compCenters, quanRect2, cleanSWTImage);//quan them
            ////quanRect = FindBoundingBoxesAll(compBB, cleanSWTImage);//quan them lan 1

            //IplImage output7 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3); //save
            //RenderComponentsWithBoxes3(cleanSWTImage, compCenters, components, quanRectKmean, output7);
            //Cv.SaveImage("componentsWithK-mean2.png", output7);

            return kmeanResult2;
        }

        public static List<OCRRegion> textPolyDetection(KmeanOption kmeanOption, IplImage input, bool darkOnLight)
        {
            //List<Tuple<CvPoint, CvPoint>>
            // convert to grayscale, could do better with color subtraction if we know what white will look like
            IplImage grayImg = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            Cv.CvtColor(input, grayImg, ColorConversion.BgrToGray);
            Cv.SaveImage("aGray.png", grayImg);

            //ConvertColorHue(input, grayImg);
            //Cv.SaveImage("hue.png", grayImg);

            // create canny --> hard to automatically find parameters...
            double threshLow = 10;//5
            double threshHigh = 200;//50
            IplImage edgeImg = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            Cv.Canny(grayImg, edgeImg, threshLow, threshHigh, ApertureSize.Size3);
            Cv.SaveImage("1Canny.png", edgeImg);

            // create gradient x, gradient y
            IplImage gaussianImg = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            Cv.ConvertScale(grayImg, gaussianImg, 1.0 / 255.0, 0);
            //Gaussian smoothing is commonly used with eadge detection
            Cv.Smooth(gaussianImg, gaussianImg, SmoothType.Gaussian, 5, 5); //Gaussian filter 5x5
            IplImage gradientX = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            IplImage gradientY = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            Cv.Sobel(gaussianImg, gradientX, 1, 0, ApertureSize.Scharr);
            Cv.Sobel(gaussianImg, gradientY, 0, 1, ApertureSize.Scharr);
            Cv.Smooth(gradientX, gradientX, SmoothType.Blur, 3, 3);
            Cv.Smooth(gradientY, gradientY, SmoothType.Blur, 3, 3);
            //Cv.SaveImage("GradientX.png", gradientX);
            //Cv.SaveImage("GradientY.png", gradientY);


            // Calculate SWT and return ray vectors
            List<Ray> rays = new List<Ray>();
            IplImage SWTImage = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            SWTImage.Set(-1);

            // Stroke width transform from edge image
            StrokeWidthTransform(edgeImg, gradientX, gradientY, darkOnLight, SWTImage, rays);
            IplImage saveSWT = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            Cv.ConvertScale(SWTImage, saveSWT, 255, 0);
            Cv.SaveImage("2SWT.png", saveSWT);

            // Stroke Width Transform using Median filter
            SWTMedianFilter(SWTImage, rays);
            Cv.ConvertScale(SWTImage, saveSWT, 255, 0);
            Cv.SaveImage("3SWTMedianFilter.png", saveSWT);


            //Cv.SaveImage("3InverseSWTMedianFilter.png", saveSWT);

            // Check the ray if it are deviating from meadian value -> remove it (clean)
            ////IplImage cleanSWTImage = Cv.CreateImage( input.GetSize(), BitDepth.F32, 1 );
            IplImage cleanSWTImage = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            cleanSWTImage.Set(-1);
            FilterRays(SWTImage, rays, cleanSWTImage);
            Cv.ConvertScale(cleanSWTImage, saveSWT, 255, 0);
            Cv.SaveImage("4CleanSWTImage.png", saveSWT);

            //// normalize
            IplImage output2 = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            NormalizeImage(SWTImage, output2);

            //// binarize and close with rectangle to fill gaps from cleaning
            ////cleanSWTImage = SWTImage;
            IplImage binSWTImage = Cv.CreateImage(cleanSWTImage.GetSize(), BitDepth.U8, 1);
            IplImage tempImg = Cv.CreateImage(cleanSWTImage.GetSize(), BitDepth.U8, 1);
            Cv.Threshold(SWTImage, binSWTImage, 0, 255, ThresholdType.Binary);
            Cv.ConvertScale(binSWTImage, saveSWT, 255, 0);
            Cv.SaveImage("5newNhiphan.png", saveSWT);

            IplImage binSWTImage2 = Cv.CreateImage(binSWTImage.GetSize(), BitDepth.U8, 1);
            //binSWTImage2 = Cv.Invert
            //Cv.SaveImage("5newNhiphan.png", saveSWT);


            //Cv.Dilate(saveSWT, saveSWT);
            //Cv.Erode(saveSWT, saveSWT);
            //Cv.SaveImage("5Dilation_SWTmedian.png", saveSWT);
            //Cv.MorphologyEx(binSWTImage, binSWTImage, tempImg, new IplConvKernel(5, 17, 2, 8, ElementShape.Rect), MorphologyOperation.Gradient);
            //Cv.MorphologyEx(binSWTImage, binSWTImage, tempImg, new IplConvKernel(21, 3, 10, 2, ElementShape.Rect), MorphologyOperation.TopHat);

            //Cv.ConvertScale(binSWTImage, saveSWT, 255, 0);
            //Cv.SaveImage("5SWT_Morphology.png", saveSWT);


            //Cv.Threshold(SWTImage, binSWTImage, (int)10 * Cv.Avg(SWTImage).Val0, 255, ThresholdType.Binary);
            //Cv.ConvertScale(binSWTImage, saveSWT, 255, 0);
            ////kernel = Cv.CreateStructuringElementEx(Cv.MorphologyEx, (3, 3))
            //// to manipulate the orientation of dilution, large x means horizonatally dilating  more, large y means vertically dilating more
            //var S1 = Cv.CreateStructuringElementEx(3, 1, 1, 0, ElementShape.Rect, null);
            ////Cv.Dilate(SWTImage, SWTImage, S1);
            //Cv.Dilate(SWTImage, SWTImage);
            //Cv.Erode(SWTImage, SWTImage);
            //Cv.ConvertScale(SWTImage, saveSWT, 255, 0);
            //Cv.SaveImage("5Nhiphan.png", saveSWT);

            //IplImage saveSWT = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            //Cv.ConvertScale(output2, saveSWT, 255, 0);
            //Cv.SaveImage("SWT.png", saveSWT);


            //// Calculate legally connect components from SWT and gradient image.
            //// return type is a vector of vectors, where each outer vector is a component and
            //// the inner vector contains the (y,x) of each pixel in that component.
            //List<List<Point2d>> components = findLegallyConnectedComponents( SWTImage, rays );

            //Cung hay
            //cleanSWTImage = output2;
            cleanSWTImage = SWTImage; //quan
            //cleanSWTImage = binSWTImage;

            //Components analysis corresponding to character: ORIGINAL
            //List<List<Point2d>> components = FindLegallyConnectedComponents(cleanSWTImage, rays);

            //Components analysis corresponding to character: OTHER
            IplImage binFloatImg = Cv.CreateImage(binSWTImage.GetSize(), BitDepth.F32, 1);
            //cleanSWTImage = binFloatImg;
            Cv.Convert(binSWTImage, binFloatImg);
            List<List<Point2d>> components = FindLegallyConnectedComponents(binFloatImg, rays);


            // Filter the components
            List<List<Point2d>> validComponents = new List<List<Point2d>>();
            List<Point2dFloat> compCenters = new List<Point2dFloat>();
            List<float> compMedians = new List<float>();
            List<Point2d> compDimensions = new List<Point2d>();
            List<Tuple<Point2d, Point2d>> compBB = new List<Tuple<Point2d, Point2d>>();

            FilterComponents(cleanSWTImage, components, ref validComponents, ref compCenters, ref compMedians, ref compDimensions, ref compBB);
            //validComponents = components;
            IplImage outComponent = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            RenderComponentsWithBoxes(cleanSWTImage, validComponents, compBB, outComponent);
            Cv.SaveImage("6SWT_Components.png", outComponent);
            //List<Tuple<CvPoint, CvPoint>> quanRect = new List<Tuple<CvPoint, CvPoint>>();
            //quanRect = FindBoundingBoxesAll2(compCenters, compBB, cleanSWTImage);//quan them
            //quanRect = FindBoundingBoxesAll(compBB, cleanSWTImage);//quan them lan 1

            //IplImage output3 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3); //save
            //RenderComponentsWithBoxes2(cleanSWTImage, compCenters, components, compBB, output3);
            //Cv.SaveImage("componentsWithK-mean.png", output3);
            //cvReleaseImage ( &output3 );

            //// Make chains of components
            List<Chain> chains;
            chains = makeChains(input, validComponents, compCenters, compMedians, compDimensions, compBB);
            //List<Chain> chains;
            //chains = makeMorphologicalChains(input, chains1, validComponents, compCenters, compMedians, compDimensions, compBB);


            IplImage outChain = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            renderChainsWithBoxes(SWTImage, validComponents, compCenters, chains, compBB, outChain);
            Cv.SaveImage("7SWT_Chains.png", outChain);


            List<Tuple<CvPoint, CvPoint>> kmeanResult = new List<Tuple<CvPoint, CvPoint>>();
            List<OCRRegion> kmeanResult2 = new List<OCRRegion>();
            //kmeanResult = makeKmeanComponents(kmeanOption, input, chains, validComponents, compCenters, compMedians, compDimensions, compBB);// original
            kmeanResult2 = makeKmeanChainsPoly(kmeanOption, input, chains, validComponents, compCenters, compMedians, compDimensions, compBB);

            //kmeanResult = makeMorphologicalChains(input, chains, validComponents, compCenters, compMedians, compDimensions, compBB);
            //options for kmean
            //kmeanResult = makeKmeanOptionComponents(true, false, false, false, input, chains, validComponents, compCenters, compMedians, compDimensions, compBB);

            IplImage outputRender = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            RenderKmeanWithBoxesonImage(cleanSWTImage, components, kmeanResult, outputRender);
            Cv.SaveImage("8AccordKmean.png", outputRender);

            IplImage output4 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            renderChains(SWTImage, validComponents, chains, output4);
            Cv.SaveImage("9text.png", output4);

            ////IplImage output5 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            ////Cv.CvtColor(output4, output5, ColorConversion.GrayToRgb);
            ////Cv.SaveImage("text2.png", output5);

            //IplImage output6 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            ////List<Tuple<CvPoint, CvPoint>> quanRect2 = new List<Tuple<CvPoint, CvPoint>>();
            //var quanRect2 = renderChainsWithBoxes(SWTImage, validComponents, compCenters, chains, compBB, output6);
            //Cv.SaveImage("text3.png", output6);

            ////k-mean2
            //List<Tuple<CvPoint, CvPoint>> quanRectKmean = new List<Tuple<CvPoint, CvPoint>>();
            ////quanRectKmean = FindBoundingBoxesAll3(compCenters, quanRect2, cleanSWTImage);//quan them
            //quanRectKmean = FindBoundingBoxesAll2(compCenters, quanRect2, cleanSWTImage);//quan them
            ////quanRect = FindBoundingBoxesAll(compBB, cleanSWTImage);//quan them lan 1

            //IplImage output7 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3); //save
            //RenderComponentsWithBoxes3(cleanSWTImage, compCenters, components, quanRectKmean, output7);
            //Cv.SaveImage("componentsWithK-mean2.png", output7);

            return kmeanResult2;
        }

        /// <summary>
        /// This method is used to render k-mean box on image
        /// </summary>
        /// <param name="input">This is input image</param>
        /// <param name="kmeanResult">Kmean box</param>
        /// <param name="outputRender">output image</param>
        private static void RenderKmeanWithBoxesonImage(
            IplImage input,
            List<List<Point2d>> components,
            List<Tuple<CvPoint, CvPoint>> kmeanResult, 
            IplImage outputRender)
        {
            IplImage outTemp = Cv.CreateImage(outputRender.GetSize(), BitDepth.F32, 1);
            RenderComponents(input, components, outTemp);
            
            IplImage outImg = Cv.CreateImage(outputRender.GetSize(), BitDepth.U8, 1);

            Cv.Convert(outTemp, outImg);
            Cv.CvtColor(outImg, outputRender, ColorConversion.GrayToBgr);

            int count = 0;
            foreach (Tuple<CvPoint, CvPoint> it in kmeanResult)
            {
                CvScalar c;
                if (count % 3 == 0) c = new CvScalar(255, 0, 0);
                else if (count % 3 == 1) c = new CvScalar(0, 255, 0);
                else c = new CvScalar(0, 0, 255);
                count++;
                Cv.Rectangle(outputRender, it.Item1, it.Item2, c, 1);
            }

        }


        /// <summary>
        /// This method is used for cluster for detecting text region
        /// </summary>
        /// <param name="input"></param>
        /// <param name="chains"></param>
        /// <param name="validComponents"></param>
        /// <param name="compCenters"></param>
        /// <param name="compMedians"></param>
        /// <param name="compDimensions"></param>
        /// <param name="compBB"></param>
        /// <returns></returns>
        private static List<Tuple<CvPoint, CvPoint>> makeKmeanComponents(
            //bool posVer, bool posHor, bool pos2D, //options
            KmeanOption kmeanOption,
            IplImage input, 
            List<Chain> chains,
            List<List<Point2d>> validComponents,
            List<Point2dFloat> compCenters,
            List<float> compMedians, 
            List<Point2d> compDimensions,
            List<Tuple<Point2d, Point2d>> compBB)
        {
            //Accord library
            Accord.Math.Random.Generator.Seed = 1234;
            double[][] observations = new double[compCenters.Count()][];

            #region for chain calculate
            ////chainBB
            //List<Tuple<Point2d, Point2d>> chainBB = new List<Tuple<Point2d, Point2d>>(chains.Count());
            //foreach (var cit in chains)
            //{
            //    double minx = input.Width;
            //    double miny = input.Height;
            //    double maxx = 0;
            //    double maxy = 0;

            //    foreach (var item in cit.components)
            //    {
            //        miny = Math.Min(miny, compBB[item].Item1.y);
            //        minx = Math.Min(minx, compBB[item].Item1.x);
            //        maxy = Math.Max(maxy, compBB[item].Item2.y);
            //        maxx = Math.Max(maxx, compBB[item].Item2.x);

            //    }
            //    Point2d p0 = new Point2d();
            //    p0.x = (int)minx;
            //    p0.y = (int)miny;
            //    Point2d p1 = new Point2d();
            //    p1.x = (int)maxx;
            //    p1.y = (int)maxy;
            //    chainBB.Add(new Tuple<Point2d, Point2d>(p0,p1));
            //}

            ////chainCenter
            //List<Point2dFloat> chainCenters = new List<Point2dFloat>(chains.Count());
            //foreach (var cit in chains)
            //{
            //    double minx = input.Width;
            //    double miny = input.Height;
            //    double maxx = 0;
            //    double maxy = 0;

            //    foreach (var item in cit.components)
            //    {
            //        miny = Math.Min(miny, compBB[item].Item1.y);
            //        minx = Math.Min(minx, compBB[item].Item1.x);
            //        maxy = Math.Max(maxy, compBB[item].Item2.y);
            //        maxx = Math.Max(maxx, compBB[item].Item2.x);

            //    }
            //    Point2dFloat p0 = new Point2dFloat();
            //    p0.x = (int)(minx + maxx) / 2;
            //    p0.y = (int)(miny + maxy) / 2;
            //    chainCenters.Add(p0);
            //}
            #endregion

            int index = 0;
            if (kmeanOption.pos2Dimesions)
            {
                foreach (var item in compCenters)
                {
                    observations[index] = new double[] { item.x, item.y };
                    //item.x, item.y, compDimensions[index].x, compDimensions[index].y, compMedians[index]
                    index++;
                }
                index = 0;

            }
            if (kmeanOption.posHorizontal)
            {
                foreach (var item in compCenters)
                {
                    observations[index] = new double[] { item.x };
                    //item.x, item.y, compDimensions[index].x, compDimensions[index].y, compMedians[index]
                    index++;
                }
                index = 0;

            }
            if (kmeanOption.posVertical)
            {
                foreach (var item in compCenters)
                {
                    observations[index] = new double[] { item.y };
                    //item.x, item.y, compDimensions[index].x, compDimensions[index].y, compMedians[index]
                    index++;
                }
                index = 0;
            }
            if (kmeanOption.strokeWidth)
            {
                foreach (var item in compCenters)
                {
                    observations[index] = new double[] { compMedians[index] };
                    //item.x, item.y, compDimensions[index].x, compDimensions[index].y, compMedians[index]
                    index++;
                }
                index = 0;
            }
            if (kmeanOption.dimHorizontal)
            {
                foreach (var item in compCenters)
                {
                    observations[index] = new double[] { compDimensions[index].x };
                    //item.x, item.y, compDimensions[index].x, compDimensions[index].y, compMedians[index]
                    index++;
                }
                index = 0;
            }
            if (kmeanOption.dimVertical)
            {
                foreach (var item in compCenters)
                {
                    observations[index] = new double[] { compDimensions[index].y };
                    //item.x, item.y, compDimensions[index].x, compDimensions[index].y, compMedians[index]
                    index++;
                }
                index = 0;
            }
            if (kmeanOption.dim2Dimensions)
            {
                foreach (var item in compCenters)
                {
                    observations[index] = new double[] { compDimensions[index].y, compDimensions[index].y };
                    //item.x, item.y, compDimensions[index].x, compDimensions[index].y, compMedians[index]
                    index++;
                }
                index = 0;
            }
            #region
            //if (pos2D)
            //{
            //    foreach (var item in compCenters)
            //    {
            //        observations[index] = new double[] { item.x, item.y };
            //        //item.x, item.y, compDimensions[index].x, compDimensions[index].y, compMedians[index]
            //        index++;
            //    }

            //}
            //else if (posHor)
            //{
            //    foreach (var item in compCenters)
            //    {
            //        observations[index] = new double[] { item.x };
            //        //item.x, item.y, compDimensions[index].x, compDimensions[index].y, compMedians[index]
            //        index++;
            //    }

            //}
            //else
            //{
            //    foreach (var item in compCenters)
            //    {
            //        observations[index] = new double[] { item.y };
            //        //item.x, item.y, compDimensions[index].x, compDimensions[index].y, compMedians[index]
            //        index++;
            //    }
            //}
            #endregion

            // Elbow  algorithm to get the optimized number cluster
            var rangeOfcluster = 1;
            int MIN_CLUSTER = 10;
            if (observations.Count() == 0)
            {
                return null;
            }
            if (observations.Count()<= MIN_CLUSTER)
            {
                rangeOfcluster = observations.Count();
            }
            else
            {
                rangeOfcluster = MIN_CLUSTER;
            }

            //Check it out for 1 cluster
            double angleStricness = Math.PI / 12;
            int checkDirection = 1;
            for (int i = 0; i < chains.Count(); i++)
            {
                for (int j = i + 1; j < chains.Count(); j++)
                {
                    //Math.Acos(chains[i].direction.x * -chains[j].direction.x + chains[i].direction.y * -chains[j].direction.y) < strictness
                    Debug.WriteLine(chains[i].direction.x + "," + chains[i].direction.y);
                    Debug.WriteLine(chains[j].direction.x + "," + chains[j].direction.y);
                    Debug.WriteLine("GOC: " + Math.Acos(chains[i].direction.x * -chains[j].direction.x + chains[i].direction.y * -chains[j].direction.y));
                    var goc = Math.Acos(chains[i].direction.x * -chains[j].direction.x + chains[i].direction.y * -chains[j].direction.y);
                    if (goc > angleStricness && goc < (Math.PI - angleStricness))
                    {
                        checkDirection++;
                    }
                }
            }

            //if (checkDirection == 1)
            //{
            //    rangeOfcluster = 1;
            //}


            double[] totalWithinSoS = new double[rangeOfcluster];
            List<List<Tuple<CvPoint, CvPoint >>> bbAll = new List<List<Tuple<CvPoint, CvPoint>>> ();

            for (int cl = 0; cl < rangeOfcluster; cl++)
            {
                var initCluster = cl + 1;
                KMeans kmeans = new KMeans(k: initCluster)
                {
                    //consider the importance of the each column feature
                    //Distance = new WeightedSquareEuclidean(new double[] {0.1, 0.1, 0.9 })
                };

                // Compute and retrieve the data centroids
                var clusters = kmeans.Learn(observations);

                // Use the centroids to parition all the data
                int[] labels = clusters.Decide(observations);
                
                List<Tuple<CvPoint, CvPoint>> _bbAll = new List<Tuple<CvPoint, CvPoint>>();

                for (int i = 0; i < initCluster; i++)
                {
                    double minx = input.Width;
                    double miny = input.Height;
                    double maxx = 0;
                    double maxy = 0;

                    int count = 0;
                    int ind = 0;
                    foreach (var item in labels)
                    {

                        if (item == i)
                        {
                            //Debug.WriteLine("Cluster centroid: " + clusters.Centroids[i][0] + " " + clusters.Centroids[i][1]);
                            //Debug.WriteLine("      data point: " + observations[ind][0] + " " + observations[ind][1]);
                            //totalWithinSoS[cl] = totalWithinSoS[cl] + Distance.SquareEuclidean(observations[ind][0], observations[ind][1], clusters.Centroids[i][0], clusters.Centroids[i][1]);
                            totalWithinSoS[cl] = totalWithinSoS[cl] + Distance.SquareEuclidean(observations[ind], clusters.Centroids[i]);

                            miny = Math.Min(miny, compBB[ind].Item1.y);
                            minx = Math.Min(minx, compBB[ind].Item1.x);
                            maxy = Math.Max(maxy, compBB[ind].Item2.y);
                            maxx = Math.Max(maxx, compBB[ind].Item2.x);
                            count++;
                        }
                        ind++;
                    }

                    if (count != 0)
                    {
                        //Check outside image
                        miny = Math.Max(miny-10, 0);
                        minx = Math.Max(minx-10, 0);
                        maxy = Math.Min(maxy+10, input.Height);
                        maxx = Math.Min(maxx+10, input.Width);

                        CvPoint p0 = new CvPoint((int)minx, (int)miny);//chu y ko + - here
                        CvPoint p1 = new CvPoint((int)maxx, (int)maxy);
                        _bbAll.Add(new Tuple<CvPoint, CvPoint>(p0, p1));
                    }
                }
                bbAll.Add(_bbAll);
            }

            // Based on the distance to refer to the optimized number of cluster
            int optimizedcluster = 0;
            if (rangeOfcluster ==1)
            {
                optimizedcluster = 1;
            }
            else
            {
                int idx = 1;
                Line ImagaryLine;
                ImagaryLine = Line.FromPoints(new Accord.Point((float)totalWithinSoS[0], 1), new Accord.Point((float)totalWithinSoS[rangeOfcluster - 1], rangeOfcluster));
                float maxDistance = 0;
                
                foreach (var item in totalWithinSoS)
                {
                    Debug.WriteLine("totalWithinSoS of " + idx + "cluster: " + item);
                    Debug.WriteLine("   " + ImagaryLine.DistanceToPoint(new Accord.Point((float)item, idx)));
                    var tempDis = ImagaryLine.DistanceToPoint(new Accord.Point((float)item, idx));
                    if (tempDis > maxDistance)
                    {
                        optimizedcluster = idx;
                        maxDistance = tempDis;
                    }
                    ///This is case the number observation = number cluster (ideal)
                    if (item == 0)
                    {
                        optimizedcluster = idx;
                    }
                    idx++;
                }

                Debug.WriteLine("OptimizedCluster using Elbowmethod is: " + optimizedcluster.ToString());

            }
           

            return bbAll[optimizedcluster - 1];
        }

        /// <summary>
        /// This method is used to run Kmean on options setting using input parameters
        /// Parametters includes position, stroke width(SWT), dimension, and direction of components
        /// </summary>
        /// <param name="kmeanOnPosition"></param>
        /// <param name="kmeanOnSWT"></param>
        /// <param name="kmeanOnDimension"></param>
        /// <param name="kmeanOnDirection"></param>
        /// <param name="input"></param>
        /// <param name="chains"></param>
        /// <param name="validComponents"></param>
        /// <param name="compCenters"></param>
        /// <param name="compMedians"></param>
        /// <param name="compDimensions"></param>
        /// <param name="compBB"></param>
        /// <returns></returns>
        private static List<Tuple<CvPoint, CvPoint>> makeKmeanOptionComponents(
           bool kmeanOnPosition, bool kmeanOnSWT, bool kmeanOnDimension, bool kmeanOnDirection,//Option for automode
           IplImage input,
           List<Chain> chains,
           List<List<Point2d>> validComponents,
           List<Point2dFloat> compCenters,
           List<float> compMedians,
           List<Point2d> compDimensions,
           List<Tuple<Point2d, Point2d>> compBB)
        {
            //Accord library
            Accord.Math.Random.Generator.Seed = 1234;
            double[][] observations = new double[compCenters.Count()][];

            #region for chain calculate
            ////chainBB
            //List<Tuple<Point2d, Point2d>> chainBB = new List<Tuple<Point2d, Point2d>>(chains.Count());
            //foreach (var cit in chains)
            //{
            //    double minx = input.Width;
            //    double miny = input.Height;
            //    double maxx = 0;
            //    double maxy = 0;

            //    foreach (var item in cit.components)
            //    {
            //        miny = Math.Min(miny, compBB[item].Item1.y);
            //        minx = Math.Min(minx, compBB[item].Item1.x);
            //        maxy = Math.Max(maxy, compBB[item].Item2.y);
            //        maxx = Math.Max(maxx, compBB[item].Item2.x);

            //    }
            //    Point2d p0 = new Point2d();
            //    p0.x = (int)minx;
            //    p0.y = (int)miny;
            //    Point2d p1 = new Point2d();
            //    p1.x = (int)maxx;
            //    p1.y = (int)maxy;
            //    chainBB.Add(new Tuple<Point2d, Point2d>(p0,p1));
            //}

            ////chainCenter
            //List<Point2dFloat> chainCenters = new List<Point2dFloat>(chains.Count());
            //foreach (var cit in chains)
            //{
            //    double minx = input.Width;
            //    double miny = input.Height;
            //    double maxx = 0;
            //    double maxy = 0;

            //    foreach (var item in cit.components)
            //    {
            //        miny = Math.Min(miny, compBB[item].Item1.y);
            //        minx = Math.Min(minx, compBB[item].Item1.x);
            //        maxy = Math.Max(maxy, compBB[item].Item2.y);
            //        maxx = Math.Max(maxx, compBB[item].Item2.x);

            //    }
            //    Point2dFloat p0 = new Point2dFloat();
            //    p0.x = (int)(minx + maxx) / 2;
            //    p0.y = (int)(miny + maxy) / 2;
            //    chainCenters.Add(p0);
            //}
            #endregion

            //POSITION CENTER CONSIDERATION
            //initialize mean, variance, median, mim, max values
            List<float> meanC = new List<float>(); List<float> varianceC = new List<float>(); List<float> medianC = new List<float>();
            //get the statistical property of connected components
            componentStatsOnPosition(compCenters, ref meanC, ref varianceC, ref medianC);
            Debug.WriteLine("========================");
            Debug.WriteLine("Mean component's Position: " + meanC[0] + " " + meanC[1]);
            Debug.WriteLine("Variance component's Position: " + Math.Sqrt(varianceC[0]) + " " + Math.Sqrt(varianceC[1]));
            Debug.WriteLine("Median component's Position: " + medianC[0] + " " + medianC[1]);

            if (kmeanOnPosition == true && Math.Sqrt(varianceC[0]) >= 0.5 * meanC[0] && Math.Sqrt(varianceC[1]) >= 0.5 * meanC[1])
            {
                kmeanOnPosition = false;
                //kmeanOnSWT = false;
                //kmeanOnDimension = false;
            }

            //STROKE WIDTH CONSIDERATION
            //initialize mean, variance, median, mim, max values
            float mean = 0, variance = 0, median = 0;
            //get the statistical property of connected components
            componentStatsOnSWT(compMedians, ref mean, ref variance, ref median);
            Debug.WriteLine("========================");
            Debug.WriteLine("Mean component's SWTs: " + mean);
            Debug.WriteLine("Variance component's SWTs: " + Math.Sqrt(variance));
            Debug.WriteLine("Median component's SWTs: " + median);

            if (kmeanOnSWT==true && Math.Sqrt(variance) <= 0.5 * mean)
            {
                //kmeanOnPosition = false;
                kmeanOnSWT = false;
                //kmeanOnDimension = false;
            }

            //DIMENSION CONDISERATION
            //initialize mean, variance, median, mim, max values
            List<float> meanD = new List<float>(); List<float> varianceD = new List<float>();  List<float> medianD= new List<float>();
            //get the statistical property of connected components
            componentStatsOnDimension(compDimensions, ref meanD, ref varianceD, ref medianD);
            Debug.WriteLine("========================");
            Debug.WriteLine("Mean component's Dimensions: " + meanD[0] +" " + meanD[1]);
            Debug.WriteLine("Variance component's Dimensions: " + Math.Sqrt(varianceD[0]) + " " + Math.Sqrt(varianceD[1]));
            Debug.WriteLine("Median component's Demensions: " + medianD[0] +" "+ medianD[1]);

            if (kmeanOnDimension ==true && Math.Sqrt(varianceD[0]) <= 0.5 * meanD[0] || Math.Sqrt(varianceD[1]) <= 0.5 * meanD[1])
            {
                //kmeanOnPosition = false;
                //kmeanOnSWT = false;
                kmeanOnDimension = false;
            }

           


            int index = 0;
            if (kmeanOnPosition)
            {
                foreach (var item in compCenters)
                {
                    //compDimensions[index].y
                    observations[index] = new double[] { item.x, item.y};
                    index++;

                }
                index = 0;
            }

            if (kmeanOnSWT)
            {
                foreach (var item in compCenters)
                {
                    observations[index] = new double[] { compMedians[index] };
                    index++;

                }
                index = 0;
            }

            if (kmeanOnDimension)
            {
                foreach (var item in compCenters)
                {
                    observations[index] = new double[] { compDimensions[index].x, compDimensions[index].y };
                    index++;

                }
                index = 0;
            }


            // Elbow  algorithm to get the optimized number cluster
            int MIN_CLUSTER = 5;
            var rangeOfcluster = 1;
           
            if (observations.Count() <= MIN_CLUSTER)
            {
                rangeOfcluster = observations.Count();
            }
            else
            {
                rangeOfcluster = MIN_CLUSTER;
            }

            if (observations[0] == null)
            {
                rangeOfcluster = 1;
                foreach (var item in compCenters)
                {
                    observations[index] = new double[] { item.x, item.y };
                    index++;
                }
            }

            //Check it out for 1 cluster
            double angleStricness = Math.PI / 12;
            int checkDirection = 1;
            List<float> differenceAngle = new List<float>();
            for (int i = 0; i < chains.Count(); i++)
            {
                for (int j = i + 1; j < chains.Count(); j++)
                {
                    //Math.Acos(chains[i].direction.x * -chains[j].direction.x + chains[i].direction.y * -chains[j].direction.y) < strictness
                    Debug.WriteLine(chains[i].direction.x + "," + chains[i].direction.y);
                    Debug.WriteLine(chains[j].direction.x + "," + chains[j].direction.y);
                    Debug.WriteLine("GOC: " + Math.Acos(chains[i].direction.x * -chains[j].direction.x + chains[i].direction.y * -chains[j].direction.y));
                    var goc = Math.Acos(chains[i].direction.x * -chains[j].direction.x + chains[i].direction.y * -chains[j].direction.y);
                    differenceAngle.Add((float)Math.Abs(chains[i].direction.x * -chains[j].direction.x + chains[i].direction.y * -chains[j].direction.y));
                    if (goc > angleStricness && goc < (Math.PI - angleStricness))
                    {
                        checkDirection++;
                    }
                }
            }

            float meanA = 0, varianceA = 0, medianA = 0;
            //get the statistical property of connected components
            if (differenceAngle.Count>=1)
            {
                componentStatsOnSWT(differenceAngle, ref meanA, ref varianceA, ref medianA);
                Debug.WriteLine("========================");
                Debug.WriteLine("Mean component's Angles: " + meanA);
                Debug.WriteLine("Variance component's Angles: " + varianceA);
                Debug.WriteLine("Median component's Angles: " + medianA);
                if (Math.Sqrt(varianceA) > 0.01 * meanA)
                {
                    //kmeanOnPosition = false;
                    //kmeanOnSWT = true;
                    //kmeanOnDimension = false;
                    Debug.WriteLine("NHIEU CHIEU");

                }
                else
                {
                    //rangeOfcluster = 1;
                }
            }
            else
            {
                  //rangeOfcluster = 1;
            }
            
            //if (checkDirection == 1)
            //{
            //    rangeOfcluster = 1;
            //}


            double[] totalWithinSoS = new double[rangeOfcluster];
            List<List<Tuple<CvPoint, CvPoint>>> bbAll = new List<List<Tuple<CvPoint, CvPoint>>>();

            for (int cl = 0; cl < rangeOfcluster; cl++)
            {
                var initCluster = cl + 1;
                KMeans kmeans = new KMeans(k: initCluster)
                {
                    //consider the importance of the each column feature
                    //Distance = new WeightedSquareEuclidean(new double[] {0.1, 0.1, 0.9 })
                };

                // Compute and retrieve the data centroids
                var clusters = kmeans.Learn(observations);

                // Use the centroids to parition all the data
                int[] labels = clusters.Decide(observations);

                List<Tuple<CvPoint, CvPoint>> _bbAll = new List<Tuple<CvPoint, CvPoint>>();

                for (int i = 0; i < initCluster; i++)
                {
                    double minx = input.Width;
                    double miny = input.Height;
                    double maxx = 0;
                    double maxy = 0;

                    int count = 0;
                    int ind = 0;
                    foreach (var item in labels)
                    {

                        if (item == i)
                        {
                            //Debug.WriteLine("Cluster centroid: " + clusters.Centroids[i][0] + " " + clusters.Centroids[i][1]);
                            //Debug.WriteLine("      data point: " + observations[ind][0] + " " + observations[ind][1]);
                            //totalWithinSoS[cl] = totalWithinSoS[cl] + Distance.SquareEuclidean(observations[ind][0], observations[ind][1], clusters.Centroids[i][0], clusters.Centroids[i][1]);
                            totalWithinSoS[cl] = totalWithinSoS[cl] + Distance.SquareEuclidean(observations[ind], clusters.Centroids[i]);

                            miny = Math.Min(miny, compBB[ind].Item1.y);
                            minx = Math.Min(minx, compBB[ind].Item1.x);
                            maxy = Math.Max(maxy, compBB[ind].Item2.y);
                            maxx = Math.Max(maxx, compBB[ind].Item2.x);
                            count++;
                        }
                        ind++;
                    }

                    if (count != 0)
                    {
                        CvPoint p0 = new CvPoint((int)minx - 5, (int)miny - 5);//chu y ko + - here
                        CvPoint p1 = new CvPoint((int)maxx + 5, (int)maxy + 5);
                        _bbAll.Add(new Tuple<CvPoint, CvPoint>(p0, p1));
                    }
                }
                bbAll.Add(_bbAll);
            }

            // Based on the distance to refer to the optimized number of cluster
            int optimizedcluster = 0;
            if (rangeOfcluster == 1)
            {
                optimizedcluster = 1;
            }
            else
            {
                int idx = 1;
                Line ImagaryLine;
                ImagaryLine = Line.FromPoints(new Accord.Point((float)totalWithinSoS[0], 1), new Accord.Point((float)totalWithinSoS[rangeOfcluster - 1], rangeOfcluster));
                float maxDistance = 0;

                foreach (var item in totalWithinSoS)
                {
                    Debug.WriteLine("totalWithinSoS of " + idx + "cluster: " + item);
                    Debug.WriteLine("   " + ImagaryLine.DistanceToPoint(new Accord.Point((float)item, idx)));
                    var tempDis = ImagaryLine.DistanceToPoint(new Accord.Point((float)item, idx));
                    if (tempDis > maxDistance)
                    {
                        optimizedcluster = idx;
                        maxDistance = tempDis;
                    }
                    ///This is case the number observation = number cluster (ideal)
                    if (item == 0)
                    {
                        optimizedcluster = idx;
                    }
                    idx++;
                }

                Debug.WriteLine("OptimizedCluster using Elbowmethod is: " + optimizedcluster.ToString());

            }

            //optimizedcluster = 4;
            return bbAll[optimizedcluster - 1];
        }
        /// <summary>
        /// This method is used to detect text using k-mean cluster based on the Chain feature
        /// Using k-mean recursive
        /// </summary>
        /// <param name="input">The input image for height and weight</param>
        /// <param name="chains">The chains from chain analysis</param>
        /// <param name="validComponents">Components</param>
        /// <param name="compCenters">Component center</param>
        /// <param name="compMedians">Component median</param>
        /// <param name="compDimensions">Component size</param>
        /// <param name="compBB">Bounding components</param>
        /// <returns></returns>
        private static List<OCRRegion> makeKmeanChains(
            //List<Tuple<CvPoint, CvPoint>> 
            //bool posVer, bool posHor, bool pos2D, //options
            KmeanOption kmeanOption,
            IplImage input,
            List<Chain> chains,
            List<List<Point2d>> validComponents,
            List<Point2dFloat> compCenters,
            List<float> compMedians,
            List<Point2d> compDimensions,
            List<Tuple<Point2d, Point2d>> compBB)
        {
            //Accord library
            Accord.Math.Random.Generator.Seed = 1234;
            var countComp = 0;
            foreach (var item in chains)
            {
                countComp = countComp + item.components.Count();
            }
            double[][] observations = new double[countComp][];


            //List<KmeanObservation> recursiveObservation = new List<KmeanObservation>();



            //chainBB
            List<Tuple<Point2d, Point2d>> chainBB = new List<Tuple<Point2d, Point2d>>(chains.Count());
            foreach (var cit in chains)
            {
                double minx = input.Width;
                double miny = input.Height;
                double maxx = 0;
                double maxy = 0;

                foreach (var item in cit.components)
                {
                    miny = Math.Min(miny, compBB[item].Item1.y);
                    minx = Math.Min(minx, compBB[item].Item1.x);
                    maxy = Math.Max(maxy, compBB[item].Item2.y);
                    maxx = Math.Max(maxx, compBB[item].Item2.x);

                }
                Point2d p0 = new Point2d();
                p0.x = (int)minx;
                p0.y = (int)miny;
                Point2d p1 = new Point2d();
                p1.x = (int)maxx;
                p1.y = (int)maxy;
                chainBB.Add(new Tuple<Point2d, Point2d>(p0, p1));
            }

            #region variables for feature (properties of chain)
            //chainCenter
            List<Point2dFloat> chainCenters = new List<Point2dFloat>(chains.Count()); 
            //chainDimension
            List<Point2d> chainDimensions = new List<Point2d>(chains.Count());
            //chainMedian
            List<float> chainMedians = new List<float>(chains.Count());
            //chainDirection
            List<Point2dFloat> chainDirection = new List<Point2dFloat>();
            List<double> chainDirectionRegression = new List<double>();

            int GRID = 50;// Grid 
            int ANGLE_STEP =3;
            int POINT_UNIT = 2;
            foreach (var cit in chains)
            {
                double minx = input.Width;
                double miny = input.Height;
                double maxx = 0;
                double maxy = 0;

                float chainMed = 0;
                float chainDimMedX = 0;
                float chainDimMedY = 0;

                double minCenterX = input.Width;
                double maxCenterX = 0;
                int firstComp = 0;
                int finalComp = 0;

                // for regession direction
                double[] xReg = new double[cit.components.Count()];
                double[] yReg = new double[cit.components.Count()];
                int regIndex = 0;

                foreach (var item in cit.components)
                {
                    miny = Math.Min(miny, compBB[item].Item1.y);
                    minx = Math.Min(minx, compBB[item].Item1.x);
                    maxy = Math.Max(maxy, compBB[item].Item2.y);
                    maxx = Math.Max(maxx, compBB[item].Item2.x);

                    chainMed = chainMed + compMedians[item];

                    chainDimMedX = chainDimMedX + compDimensions[item].x;
                    chainDimMedY = chainDimMedY + compDimensions[item].y;

                    xReg[regIndex] = compCenters[item].x;
                    yReg[regIndex] = compCenters[item].y;
                    regIndex++;

                    if (compCenters[item].x < minCenterX)
                    {
                        minCenterX = compCenters[item].x;
                        firstComp = item;
                    }

                    if (compCenters[item].x > maxCenterX)
                    {
                        maxCenterX = compCenters[item].x;
                        finalComp = item;
                    }
                }

                // median calculate
                chainMed = chainMed / cit.components.Count();
                chainMedians.Add(chainMed);

                // center calculate
                Point2dFloat p0 = new Point2dFloat();
                p0.x = (int)(minx + maxx) / 2/GRID;
                p0.y = (int)(miny + maxy) / 2/GRID;
                chainCenters.Add(p0);

                // dimension calculate
                var w = maxx - minx +1;
                var h = maxy - miny +1;
                Point2d d0 = new Point2d();
                d0.x = (int)w;
                d0.y = (int)h;
                //chainDimensions.Add(d0);
                chainDimMedX = chainDimMedX / cit.components.Count();
                chainDimMedY = chainDimMedY / cit.components.Count();
                d0.x = (int)chainDimMedX;
                d0.y = (int)chainDimMedY;
                chainDimensions.Add(d0);

                // direction calculate using first component and last component
                float d_x = (compCenters[finalComp].x - compCenters[firstComp].x);
                float d_y = (compCenters[finalComp].y - compCenters[firstComp].y);

                float mag = (float)Math.Sqrt(d_x * d_x + d_y * d_y);
                d_x = d_x / mag;
                d_y = d_y / mag;
                Point2dFloat dir;
                dir.x = d_x;
                dir.y = d_y;
                chainDirection.Add(dir);

                #region Regession Calucaltion for Direction
                // Use Ordinary Least Squares to learn the regression
                OrdinaryLeastSquares ols = new OrdinaryLeastSquares();
                ols.UseIntercept = true;
                ols.IsRobust = true;

                // Use OLS to learn the simple linear regression
                double s;
                if (xReg.Length > 1)
                {
                    SimpleLinearRegression regression = ols.Learn(xReg, yReg);

                    // Compute the output for a given input:
                    // double y = regression.Transform(85); 

                    // We can also extract the slope and the intercept term
                    // for the line. Those will be -0.26 and 50.5, respectively.
                    var sl = regression.Slope;
                    var c = regression.Intercept;

                    Debug.WriteLine("SLope Regression: " + sl.ToString());
                    Debug.WriteLine("Intercept Regression: " + c.ToString());
                    s = Math.Atan(sl) * 180 / Math.PI;

                }
                else
                {
                    s = 0;
                }
                s = (int)s / ANGLE_STEP; // consider each 10 degree is one label

                Debug.WriteLine("SLope Regression => label: " + s.ToString());

                chainDirectionRegression.Add(s);
                #endregion
            }
            #endregion

            #region verification properties of chains
            //position chain
            //initialize mean, variance, median, mim, max values
            List<float> meanChP = new List<float>(); List<float> varianceChP = new List<float>(); List<float> medianChP = new List<float>();
            //get the statistical property of connected components
            componentStatsOnPosition(chainCenters, ref meanChP, ref varianceChP, ref medianChP);
            Debug.WriteLine("========================");
            Debug.WriteLine("Mean chain's Position: " + meanChP[0] + " " + meanChP[1]);
            Debug.WriteLine("Variance chain's Position: " + Math.Sqrt(varianceChP[0]) + " " + Math.Sqrt(varianceChP[1]));
            Debug.WriteLine("Median chain's Position: " + medianChP[0] + " " + medianChP[1]);


            //stroke width chain
            //initialize mean, variance, median, mim, max values
            float meanChS = 0, varianceChS = 0, medianChS = 0;
            //get the statistical property of connected components
            componentStatsOnSWT(chainMedians, ref meanChS, ref varianceChS, ref medianChS);
            Debug.WriteLine("========================");
            Debug.WriteLine("Mean chain's SWTs: " + meanChS);
            Debug.WriteLine("Variance chain's SWTs: " + Math.Sqrt(varianceChS));
            Debug.WriteLine("Median chain's SWTs: " + medianChS);

            
            //dimension chain
            //initialize mean, variance, median, mim, max values
            List<float> meanChD = new List<float>(); List<float> varianceChD = new List<float>(); List<float> medianChD = new List<float>();
            //get the statistical property of connected components
            componentStatsOnDimension(chainDimensions, ref meanChD, ref varianceChD, ref medianChD);
            Debug.WriteLine("========================");
            Debug.WriteLine("Mean chain's Dimensions: " + meanChD[0] + " " + meanChD[1]);
            Debug.WriteLine("Variance chain's Dimensions: " + Math.Sqrt(varianceChD[0]) + " " + Math.Sqrt(varianceChD[1]));
            Debug.WriteLine("Median chain's Demensions: " + medianChD[0] + " " + medianChD[1]);


            #endregion

            #region check option to run respective feature based kmean (gan observation)
            int index = 0;
            int index_chain = 0;
            List<Tuple<Point2d, Point2d>> ClonechainBB = new List<Tuple<Point2d, Point2d>>();

            if (kmeanOption.pos2Dimesions)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, item.y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, item.y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                   
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posHorizontal)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x};
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}

                    observations[index_chain] = new double[] { item.x };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posVertical)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;

                }
                index = 0;
                index_chain = 0;
            }

            if (kmeanOption.strokeWidth)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { chainMedians[index_chain] };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { chainMedians[index_chain] };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;

                }
                index = 0;
                index_chain = 0;
            }

            if (kmeanOption.dimHorizontal)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { chainDimensions[index_chain].x };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { chainDimensions[index_chain].x };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;

                }
                index = 0;
                index_chain = 0;
            }

            if (kmeanOption.dimVertical)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;

                }
                index = 0;
                index_chain = 0;
            }

            if (kmeanOption.dim2Dimensions)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;

                }
                index = 0;
                index_chain = 0;
            }

            if (kmeanOption.direction)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    //observations[index] = new double[] { chains[index_chain].direction.x, chains[index_chain].direction.y };
                    //    //ClonechainBB.Add(chainBB[index_chain]);

                    //    //observations[index] = new double[] { chainDirection[index_chain].x, chainDirection[index_chain].y };
                    //    //ClonechainBB.Add(chainBB[index_chain]);

                    //    //observations[index] = new double[] { chainDirectionRegression[index_chain] };
                    //    //ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    //observations[index_chain] = new double[] { chainDirection[index_chain].x, chainDirection[index_chain].y };
                    observations[index_chain] = new double[] { chainDirectionRegression[index_chain] };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;

                }
                index = 0;
                index_chain = 0;

            }

            if(kmeanOption.pos2Dimesions && kmeanOption.direction)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, item.y, chainDirection[index_chain].x , chainDirection[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, item.y, chainDirection[index_chain].x, chainDirection[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posHorizontal && kmeanOption.direction)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, chainDirection[index_chain].x, chainDirection[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    //observations[index_chain] = new double[] { item.x, chainDirection[index_chain].x, chainDirection[index_chain].y };
                    observations[index_chain] = new double[] { item.x, chainDirectionRegression[index_chain] };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posVertical && kmeanOption.direction)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.y, chainDirection[index_chain].x, chainDirection[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    //observations[index_chain] = new double[] { item.y, chainDirection[index_chain].x, chainDirection[index_chain].y };
                    observations[index_chain] = new double[] { item.y, chainDirectionRegression[index_chain] };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posVertical && kmeanOption.dimVertical)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.y, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.y, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posVertical && kmeanOption.dimHorizontal)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.y, chainDimensions[index_chain].x };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.y, chainDimensions[index_chain].x };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posVertical && kmeanOption.dim2Dimensions)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.y, chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.y, chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posHorizontal && kmeanOption.dimVertical)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posHorizontal && kmeanOption.dimHorizontal)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, chainDimensions[index_chain].x };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, chainDimensions[index_chain].x };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posHorizontal && kmeanOption.dim2Dimensions)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.pos2Dimesions && kmeanOption.dimVertical)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x,item.y, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, item.y, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }
            
            if (kmeanOption.pos2Dimesions && kmeanOption.dimHorizontal)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, item.y, chainDimensions[index_chain].x };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, item.y, chainDimensions[index_chain].x };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.pos2Dimesions && kmeanOption.dim2Dimensions)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, item.y, chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, item.y, chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            #endregion

            #region normalize

            List<Point2dFloat> interList = new List<Point2dFloat>();
            for (int i = 0; i < observations[0].Count(); i++)
            {
                List<double> columnList = new List<double>();
                foreach (var item in observations)
                {
                    Debug.WriteLine("Column" + i + " : " + item[i].ToString());
                    columnList.Add(item[i]);
                }

                var min = columnList.Min();
                var max = columnList.Max();
                Point2dFloat minmax = new Point2dFloat();
                minmax.x = (float)min;
                minmax.y = (float)max;
                interList.Add(minmax);

            }

            int obIndex = 0;
            foreach (var item in observations)
            {
                int feature = 0;
                List<double> allFeature = new List<double>();
                foreach (var item2 in item)
                {
                    var normalizedvalue = (item2 - interList[feature].x) / (interList[feature].y - interList[feature].x);
                    allFeature.Add(normalizedvalue);
                    feature++;

                }
                observations[obIndex] = allFeature.ToArray();
                obIndex++;
            }

            #endregion

            #region elbow algorithm to find best cluster
            // Elbow  algorithm to get the optimized number cluster
            //int MIN_CLUSTER = 10;
            //var rangeOfcluster = 1;
            //if (observations.Count() <= MIN_CLUSTER)
            //{
            //    rangeOfcluster = observations.Count();
            //}
            //else
            //{
            //    rangeOfcluster = MIN_CLUSTER;
            //}

            //#region check for 1 cluster
            ////Check it out for 1 cluster
            ////double angleStricness = Math.PI / 12;
            ////int checkDirection = 1;
            ////for (int i = 0; i < chains.Count(); i++)
            ////    {
            ////        for (int j = i + 1; j < chains.Count(); j++)
            ////        {
            ////            //Math.Acos(chains[i].direction.x * -chains[j].direction.x + chains[i].direction.y * -chains[j].direction.y) < strictness
            ////            Debug.WriteLine(chains[i].direction.x + "," + chains[i].direction.y);
            ////            Debug.WriteLine(chains[j].direction.x + "," + chains[j].direction.y);
            ////            Debug.WriteLine("GOC: " + Math.Acos(chains[i].direction.x * -chains[j].direction.x + chains[i].direction.y * -chains[j].direction.y));
            ////            var goc = Math.Acos(chains[i].direction.x * -chains[j].direction.x + chains[i].direction.y * -chains[j].direction.y);
            ////            if (goc > angleStricness && goc < (Math.PI - angleStricness))
            ////            {
            ////                checkDirection++;
            ////            }
            ////        }
            ////    }

            ////if (checkDirection ==1)
            ////{
            ////    rangeOfcluster = 1;
            ////}
            //#endregion


            //double[] totalWithinSoS = new double[rangeOfcluster];
            //List<List<Tuple<CvPoint, CvPoint>>> bbAll = new List<List<Tuple<CvPoint, CvPoint>>>();
            //List<List<List<Vector2d>>> listVectorAll = new List<List<List<Vector2d>>>();

            //for (int cl = 0; cl < rangeOfcluster; cl++)
            //{
            //    var initCluster = cl + 1;
            //    KMeans kmeans = new KMeans(k: initCluster)
            //    {
            //      // Distance = new WeightedSquareEuclidean(new double[] { 0.01, 0.9})
            //    };

            //    // Compute and retrieve the data centroids

            //    var clusters = kmeans.Learn(observations);

            //    // Use the centroids to parition all the data
            //    int[] labels = clusters.Decide(observations);
            //    List<Tuple<CvPoint, CvPoint>> __bbAll = new List<Tuple<CvPoint, CvPoint>>();
            //    List<List<Vector2d>> _listVector = new List<List<Vector2d>>();


            //    for (int i = 0; i < initCluster; i++)
            //    {
            //        double minx = input.Width;
            //        double miny = input.Height;
            //        double maxx = 0;
            //        double maxy = 0;

            //        int count = 0;
            //        int ind = 0;

            //        List<Vector2d> __listVector = new List<Vector2d>();

            //        foreach (var item in labels)
            //        {

            //            if (item == i)
            //            {
            //                //Debug.WriteLine("Cluster centroid: " + clusters.Centroids[i][0] + " " + clusters.Centroids[i][1]);
            //                //Debug.WriteLine("      data point: " + observations[ind][0] + " " + observations[ind][1]);
            //                //totalWithinSoS[cl] = totalWithinSoS[cl] + Distance.SquareEuclidean(observations[ind][0], observations[ind][1], clusters.Centroids[i][0], clusters.Centroids[i][1]);
            //                totalWithinSoS[cl] = totalWithinSoS[cl] + Distance.SquareEuclidean(observations[ind], clusters.Centroids[i]);

            //                miny = Math.Min(miny, ClonechainBB[ind].Item1.y);
            //                minx = Math.Min(minx, ClonechainBB[ind].Item1.x);
            //                maxy = Math.Max(maxy, ClonechainBB[ind].Item2.y);
            //                maxx = Math.Max(maxx, ClonechainBB[ind].Item2.x);

            //                // add component for minimal Bouding box
            //                foreach (var cit in chains[ind].components)
            //                {

            //                    __listVector.Add(new Vector2d(compBB[cit].Item1.x, compBB[cit].Item1.y));
            //                    __listVector.Add(new Vector2d(compBB[cit].Item1.x, compBB[cit].Item2.y));
            //                    __listVector.Add(new Vector2d(compBB[cit].Item2.x, compBB[cit].Item2.y));
            //                    __listVector.Add(new Vector2d(compBB[cit].Item2.x, compBB[cit].Item1.y));

            //                }

            //                count++;
            //            }
            //            ind++;
            //        }

            //        if (count != 0)
            //        {
            //            //Check outside image
            //            miny = Math.Max(miny - 10, 0);
            //            minx = Math.Max(minx - 10, 0);
            //            maxy = Math.Min(maxy + 10, input.Height);
            //            maxx = Math.Min(maxx + 10, input.Width);

            //            CvPoint p0 = new CvPoint((int)minx, (int)miny);//chu y ko + - here
            //            CvPoint p1 = new CvPoint((int)maxx, (int)maxy);
            //            __bbAll.Add(new Tuple<CvPoint, CvPoint>(p0, p1));
            //            _listVector.Add(__listVector);
            //        }


            //    }
            //    bbAll.Add(__bbAll);
            //    listVectorAll.Add(_listVector);
            //}

            ////normalize soS
            ////double minSOS = totalWithinSoS.Min();
            ////double maxSOS = totalWithinSoS.Max();
            ////int SoSindex = 0;
            ////foreach (var item in totalWithinSoS)
            ////{
            ////    var temp = (item - minSOS) / (maxSOS - minSOS);
            ////    totalWithinSoS[SoSindex]= temp;
            ////    SoSindex++;
            ////}

            //// Based on the distance to refer to the optimized number of cluster
            //int optimizedcluster = 1;
            //if (rangeOfcluster == 1)
            //{
            //    optimizedcluster = 1;
            //}
            //else
            //{
            //    int idx = 1;
            //    Line ImagaryLine;
            //    //ImagaryLine = Line.FromPoints(new Accord.Point((float)totalWithinSoS[0], 1), new Accord.Point((float)totalWithinSoS[rangeOfcluster - 1], rangeOfcluster));
            //    ImagaryLine = Line.FromPoints(new Accord.Point(1,(float)totalWithinSoS[0]), new Accord.Point(MIN_CLUSTER, 0));
            //    //ImagaryLine = Line.FromPoints( new Accord.Point(1, (float)totalWithinSoS[0]), new Accord.Point(2 * MIN_CLUSTER, 0));
            //    float maxDistance = 0;

            //    foreach (var item in totalWithinSoS)
            //    {
            //        Debug.WriteLine("totalWithinSoS of " + idx + "cluster: " + item);
            //        Debug.WriteLine("   " + ImagaryLine.DistanceToPoint(new Accord.Point(idx, (float)item)));
            //        var tempDis = ImagaryLine.DistanceToPoint(new Accord.Point(idx,(float)item));
            //        if (tempDis > maxDistance)
            //        {
            //            optimizedcluster = idx;
            //            maxDistance = tempDis;
            //        }
            //        ///This is case the number observation = number cluster (ideal)
            //        //if (item == 0 && rangeOfcluster < MIN_CLUSTER )
            //        //{
            //        //    optimizedcluster = idx;
            //        //}

            //        idx++;
            //    }

            //    Debug.WriteLine("OptimizedCluster using Elbowmethod is: " + optimizedcluster.ToString());

            //}
            #endregion

            #region hoi quy kmean recursive kmean
            int indexObs = 0;

            List<Observation> inputList = new List<Observation>();
            foreach (var item in observations)
            {
                var ob = new Observation();
                ob.observation = item;
                ob.index = indexObs;
                inputList.Add(ob);
                indexObs++;
            }

            KmeanObservation kmeanOb = new KmeanObservation();
            kmeanOb.observationsList = inputList;
            kmeanOb.continueKmean = true;

            List<KmeanObservation> input_ = new List<KmeanObservation>();
            input_.Add(kmeanOb);
            var hoiquy = new List<KmeanObservation>();
            hoiquy = RecursiveKmean(input_);

            // return the coordinate of rect
            List<Tuple<CvPoint, CvPoint>> _bbAll = new List<Tuple<CvPoint, CvPoint>>();
            List<List<Vector2d>> _listVector2 = new List<List<Vector2d>>();
            
            foreach (var kit in hoiquy)
            {
                double minx = input.Width;
                double miny = input.Height;
                double maxx = 0;
                double maxy = 0;

                List<Vector2d> __listVector2 = new List<Vector2d>();

                foreach (var obit in kit.observationsList)
                {
                    miny = Math.Min(miny, ClonechainBB[obit.index].Item1.y);
                    minx = Math.Min(minx, ClonechainBB[obit.index].Item1.x);
                    maxy = Math.Max(maxy, ClonechainBB[obit.index].Item2.y);
                    maxx = Math.Max(maxx, ClonechainBB[obit.index].Item2.x);

                    // add component for minimal Bouding box
                    foreach (var cit in chains[obit.index].components)
                    {

                        __listVector2.Add(new Vector2d(compBB[cit].Item1.x, compBB[cit].Item1.y));
                        __listVector2.Add(new Vector2d(compBB[cit].Item1.x, compBB[cit].Item2.y));
                        __listVector2.Add(new Vector2d(compBB[cit].Item2.x, compBB[cit].Item2.y));
                        __listVector2.Add(new Vector2d(compBB[cit].Item2.x, compBB[cit].Item1.y));

                    }
                }
                //Check outside image
                miny = Math.Max(miny - 10, 0);
                minx = Math.Max(minx - 10, 0);
                maxy = Math.Min(maxy + 10, input.Height);
                maxx = Math.Min(maxx + 10, input.Width);

                CvPoint p0 = new CvPoint((int)minx, (int)miny);//chu y ko + - here
                CvPoint p1 = new CvPoint((int)maxx, (int)maxy);
                _bbAll.Add(new Tuple<CvPoint, CvPoint>(p0, p1));
                _listVector2.Add(__listVector2);

            }
            // combine with minimal rect

            int reInd = 0;
            List<OCRRegion> listRe = new List<OCRRegion>();
            foreach (var vit in _listVector2)
            {

                var mininalbox = MinimalBoundingBox.Calculate(vit.ToArray());
                var rect = _bbAll[reInd];
                var _region = new OCRRegion(rect, mininalbox);
                listRe.Add(_region);

                reInd++;
            }


            //this is for Kmean Recursive
            //return _bbAll;
            return listRe;
            #endregion

            #region minimal bouding box
            ////CvMemStorage storage = new CvMemStorage();
            //////CvSeq<CvPoint> result = contoursRaw;
            ////CvSeq seq = Cv.CreateSeq(SeqType.EltypeF32C2, CvSeq.SizeOf, CvPoint.SizeOf, storage);

            ////List<Vector2d> listVector = new List<Vector2d>();
            //List<Tuple<CvPoint, CvPoint>> _bbAll = new List<Tuple<CvPoint, CvPoint>>();
            //foreach (var item in listVectorAll[optimizedcluster - 1])
            //{
            //    //CvPoint pt1 = new CvPoint(item.Item1.X, item.Item1.Y);
            //    //CvPoint pt2 = new CvPoint(item.Item2.X, item.Item2.Y);
            //    //Cv.SeqPush(seq, pt1);
            //    //Cv.SeqPush(seq, pt2);
            //    //listVector.Add(new Vector2d(item.Item1.X, item.Item1.Y));
            //    //listVector.Add(new Vector2d(item.Item1.X, item.Item2.Y));
            //    //listVector.Add(new Vector2d(item.Item2.X, item.Item2.Y));
            //    //listVector.Add(new Vector2d(item.Item2.X, item.Item1.Y));
            //    var box = MinimalBoundingBox.Calculate(item.ToArray());
            //    foreach (var pit in box.Points)
            //    {
            //        CvPoint p0 = new CvPoint((int)pit.X, (int)pit.Y);
            //        CvPoint p1 = new CvPoint((int)pit.X + 4, (int)pit.Y + 4);
            //        _bbAll.Add(new Tuple<CvPoint, CvPoint>(p0, p1));

            //    }
            //}

            #endregion

            //return _bbAll;

            //optimizedcluster = 3;
            //return bbAll[optimizedcluster - 1];
        }

        /// <summary>
        /// This method is used to detect text using k-mean cluster based on chain feature
        /// using original kmean
        /// Output is mininal rectangle
        /// </summary>
        /// <param name="kmeanOption"></param>
        /// <param name="input"></param>
        /// <param name="chains"></param>
        /// <param name="validComponents"></param>
        /// <param name="compCenters"></param>
        /// <param name="compMedians"></param>
        /// <param name="compDimensions"></param>
        /// <param name="compBB"></param>
        /// <returns>Return the minimal rectangle</returns>
        private static List<OCRRegion> makeKmeanChainsPoly(
            //bool posVer, bool posHor, bool pos2D, //options
            KmeanOption kmeanOption,
            IplImage input,
            List<Chain> chains,
            List<List<Point2d>> validComponents,
            List<Point2dFloat> compCenters,
            List<float> compMedians,
            List<Point2d> compDimensions,
            List<Tuple<Point2d, Point2d>> compBB)
        {
            //Accord library
            Accord.Math.Random.Generator.Seed = 1234;
            var countComp = 0;
            foreach (var item in chains)
            {
                countComp = countComp + item.components.Count();
            }
            double[][] observations = new double[countComp][];


            //List<KmeanObservation> recursiveObservation = new List<KmeanObservation>();



            //chainBB
            List<Tuple<Point2d, Point2d>> chainBB = new List<Tuple<Point2d, Point2d>>(chains.Count());
            foreach (var cit in chains)
            {
                double minx = input.Width;
                double miny = input.Height;
                double maxx = 0;
                double maxy = 0;

                foreach (var item in cit.components)
                {
                    miny = Math.Min(miny, compBB[item].Item1.y);
                    minx = Math.Min(minx, compBB[item].Item1.x);
                    maxy = Math.Max(maxy, compBB[item].Item2.y);
                    maxx = Math.Max(maxx, compBB[item].Item2.x);

                }
                Point2d p0 = new Point2d();
                p0.x = (int)minx;
                p0.y = (int)miny;
                Point2d p1 = new Point2d();
                p1.x = (int)maxx;
                p1.y = (int)maxy;
                chainBB.Add(new Tuple<Point2d, Point2d>(p0, p1));
            }

            //chainCenter
            List<Point2dFloat> chainCenters = new List<Point2dFloat>(chains.Count());
            //chainDimension
            List<Point2d> chainDimensions = new List<Point2d>(chains.Count());
            //chainMedian
            List<float> chainMedians = new List<float>(chains.Count());
            //chainDirection
            List<Point2dFloat> chainDirection = new List<Point2dFloat>();
            List<double> chainDirectionRegression = new List<double>();
            foreach (var cit in chains)
            {
                double minx = input.Width;
                double miny = input.Height;
                double maxx = 0;
                double maxy = 0;

                float chainMed = 0;
                float chainDimMedX = 0;
                float chainDimMedY = 0;

                double minCenterX = input.Width;
                double maxCenterX = 0;
                int firstComp = 0;
                int finalComp = 0;

                // for regession direction
                //double[] xReg = new double[cit.components.Count()];
                //double[] yReg = new double[cit.components.Count()];
                //int regIndex=0;

                foreach (var item in cit.components)
                {
                    miny = Math.Min(miny, compBB[item].Item1.y);
                    minx = Math.Min(minx, compBB[item].Item1.x);
                    maxy = Math.Max(maxy, compBB[item].Item2.y);
                    maxx = Math.Max(maxx, compBB[item].Item2.x);

                    chainMed = chainMed + compMedians[item];

                    chainDimMedX = chainDimMedX + compDimensions[item].x;
                    chainDimMedY = chainDimMedY + compDimensions[item].y;

                    //xReg[regIndex] = compCenters[item].x;
                    //yReg[regIndex] = compCenters[item].y;
                    //regIndex++;

                    if (compCenters[item].x < minCenterX)
                    {
                        minCenterX = compCenters[item].x;
                        firstComp = item;
                    }

                    if (compCenters[item].x > maxCenterX)
                    {
                        maxCenterX = compCenters[item].x;
                        finalComp = item;
                    }
                }

                // median calculate
                chainMed = chainMed / cit.components.Count();
                chainMedians.Add(chainMed);

                // center calculate
                Point2dFloat p0 = new Point2dFloat();
                p0.x = (int)(minx + maxx) / 2;
                p0.y = (int)(miny + maxy) / 2;
                chainCenters.Add(p0);

                // dimension calculate
                var w = maxx - minx + 1;
                var h = maxy - miny + 1;
                Point2d d0 = new Point2d();
                d0.x = (int)w;
                d0.y = (int)h;
                //chainDimensions.Add(d0);
                chainDimMedX = chainDimMedX / cit.components.Count();
                chainDimMedY = chainDimMedY / cit.components.Count();
                d0.x = (int)chainDimMedX;
                d0.y = (int)chainDimMedY;
                chainDimensions.Add(d0);

                // direction calculate
                float d_x = (compCenters[finalComp].x - compCenters[firstComp].x);
                float d_y = (compCenters[finalComp].y - compCenters[firstComp].y);

                float mag = (float)Math.Sqrt(d_x * d_x + d_y * d_y);
                d_x = d_x / mag;
                d_y = d_y / mag;
                Point2dFloat dir;
                dir.x = d_x;
                dir.y = d_y;
                chainDirection.Add(dir);

                //#region Regession Calucaltion for Direction
                //// Use Ordinary Least Squares to learn the regression
                //OrdinaryLeastSquares ols = new OrdinaryLeastSquares();

                //// Use OLS to learn the simple linear regression
                //SimpleLinearRegression regression = ols.Learn(xReg, yReg);

                //// Compute the output for a given input:
                //// double y = regression.Transform(85); 

                //// We can also extract the slope and the intercept term
                //// for the line. Those will be -0.26 and 50.5, respectively.
                //double s = regression.Slope;    
                //double c = regression.Intercept;

                //Debug.WriteLine("SLope Regression: " + s.ToString());
                //Debug.WriteLine("Intercept Regression: " + c.ToString());

                //chainDirectionRegression.Add(s);
                //#endregion
            }



            int index = 0;
            int index_chain = 0;
            List<Tuple<Point2d, Point2d>> ClonechainBB = new List<Tuple<Point2d, Point2d>>();

            if (kmeanOption.pos2Dimesions)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, item.y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, item.y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;

                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posHorizontal)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x};
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}

                    observations[index_chain] = new double[] { item.x };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posVertical)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;

                }
                index = 0;
                index_chain = 0;
            }

            if (kmeanOption.strokeWidth)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { chainMedians[index_chain] };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { chainMedians[index_chain] };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;

                }
                index = 0;
                index_chain = 0;
            }

            if (kmeanOption.dimHorizontal)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { chainDimensions[index_chain].x };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { chainDimensions[index_chain].x };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;

                }
                index = 0;
                index_chain = 0;
            }

            if (kmeanOption.dimVertical)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;

                }
                index = 0;
                index_chain = 0;
            }

            if (kmeanOption.dim2Dimensions)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;

                }
                index = 0;
                index_chain = 0;
            }

            if (kmeanOption.direction)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    //observations[index] = new double[] { chains[index_chain].direction.x, chains[index_chain].direction.y };
                    //    //ClonechainBB.Add(chainBB[index_chain]);

                    //    //observations[index] = new double[] { chainDirection[index_chain].x, chainDirection[index_chain].y };
                    //    //ClonechainBB.Add(chainBB[index_chain]);

                    //    //observations[index] = new double[] { chainDirectionRegression[index_chain] };
                    //    //ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { chainDirection[index_chain].x, chainDirection[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;

                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.pos2Dimesions && kmeanOption.direction)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, item.y, chainDirection[index_chain].x , chainDirection[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, item.y, chainDirection[index_chain].x, chainDirection[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posHorizontal && kmeanOption.direction)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, chainDirection[index_chain].x, chainDirection[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, chainDirection[index_chain].x, chainDirection[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posVertical && kmeanOption.direction)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.y, chainDirection[index_chain].x, chainDirection[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.y, chainDirection[index_chain].x, chainDirection[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posVertical && kmeanOption.dimVertical)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.y, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.y, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posVertical && kmeanOption.dimHorizontal)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.y, chainDimensions[index_chain].x };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.y, chainDimensions[index_chain].x };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posVertical && kmeanOption.dim2Dimensions)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.y, chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.y, chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posHorizontal && kmeanOption.dimVertical)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posHorizontal && kmeanOption.dimHorizontal)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, chainDimensions[index_chain].x };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, chainDimensions[index_chain].x };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.posHorizontal && kmeanOption.dim2Dimensions)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.pos2Dimesions && kmeanOption.dimVertical)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x,item.y, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, item.y, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.pos2Dimesions && kmeanOption.dimHorizontal)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, item.y, chainDimensions[index_chain].x };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, item.y, chainDimensions[index_chain].x };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            if (kmeanOption.pos2Dimesions && kmeanOption.dim2Dimensions)
            {
                observations = new double[chainCenters.Count()][];
                ClonechainBB = new List<Tuple<Point2d, Point2d>>();
                foreach (var item in chainCenters)
                {
                    //foreach (var cit in chains[index_chain].components)
                    //{
                    //    observations[index] = new double[] { item.x, item.y, chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    //    ClonechainBB.Add(chainBB[index_chain]);
                    //    index++;
                    //}
                    observations[index_chain] = new double[] { item.x, item.y, chainDimensions[index_chain].x, chainDimensions[index_chain].y };
                    ClonechainBB.Add(chainBB[index_chain]);
                    index_chain++;
                }
                index = 0;
                index_chain = 0;

            }

            #region normalize
            Normalization norm = new Normalization();
            DataTable table = new DataTable("sample");
            table.Columns.Add("PosX");
            table.Columns.Add("PosY");
            List<Point2dFloat> interList = new List<Point2dFloat>();
            for (int i = 0; i < observations[0].Count(); i++)
            {
                List<double> columnList = new List<double>();
                foreach (var item in observations)
                {
                    Debug.WriteLine("Column" + i + " : " + item[i].ToString());
                    columnList.Add(item[i]);
                }

                var min = columnList.Min();
                var max = columnList.Max();
                Point2dFloat minmax = new Point2dFloat();
                minmax.x = (float)min;
                minmax.y = (float)max;
                interList.Add(minmax);

            }

            int obIndex = 0;
            foreach (var item in observations)
            {
                int feature = 0;
                List<double> allFeature = new List<double>();
                foreach (var item2 in item)
                {
                    var normalizedvalue = (item2 - interList[feature].x) / (interList[feature].y - interList[feature].x);
                    allFeature.Add(normalizedvalue);
                    feature++;

                }
                observations[obIndex] = allFeature.ToArray();
                obIndex++;
            }

            #endregion



            // Elbow  algorithm to get the optimized number cluster
            int MIN_CLUSTER = 10;
            var rangeOfcluster = 1;
            if (observations.Count() <= MIN_CLUSTER)
            {
                rangeOfcluster = observations.Count();
            }
            else
            {
                rangeOfcluster = MIN_CLUSTER;
            }


            double[] totalWithinSoS = new double[rangeOfcluster];
            List<List<Tuple<CvPoint, CvPoint>>> bbAll = new List<List<Tuple<CvPoint, CvPoint>>>();
            List<List<List<Vector2d>>> listVectorAll = new List<List<List<Vector2d>>>();

            for (int cl = 0; cl < rangeOfcluster; cl++)
            {
                var initCluster = cl + 1;
                KMeans kmeans = new KMeans(k: initCluster)
                {
                    // Distance = new WeightedSquareEuclidean(new double[] { 0.01, 0.9})
                };

                // Compute and retrieve the data centroids

                var clusters = kmeans.Learn(observations);

                // Use the centroids to parition all the data
                int[] labels = clusters.Decide(observations);
                List<Tuple<CvPoint, CvPoint>> __bbAll = new List<Tuple<CvPoint, CvPoint>>();
                List<List<Vector2d>> _listVector = new List<List<Vector2d>>();


                for (int i = 0; i < initCluster; i++)
                {
                    double minx = input.Width;
                    double miny = input.Height;
                    double maxx = 0;
                    double maxy = 0;

                    int count = 0;
                    int ind = 0;

                    List<Vector2d> __listVector = new List<Vector2d>();

                    foreach (var item in labels)
                    {

                        if (item == i)
                        {
                            //Debug.WriteLine("Cluster centroid: " + clusters.Centroids[i][0] + " " + clusters.Centroids[i][1]);
                            //Debug.WriteLine("      data point: " + observations[ind][0] + " " + observations[ind][1]);
                            //totalWithinSoS[cl] = totalWithinSoS[cl] + Distance.SquareEuclidean(observations[ind][0], observations[ind][1], clusters.Centroids[i][0], clusters.Centroids[i][1]);
                            totalWithinSoS[cl] = totalWithinSoS[cl] + Distance.SquareEuclidean(observations[ind], clusters.Centroids[i]);

                            miny = Math.Min(miny, ClonechainBB[ind].Item1.y);
                            minx = Math.Min(minx, ClonechainBB[ind].Item1.x);
                            maxy = Math.Max(maxy, ClonechainBB[ind].Item2.y);
                            maxx = Math.Max(maxx, ClonechainBB[ind].Item2.x);

                            // add component for minimal Bouding box
                            foreach (var cit in chains[ind].components)
                            {

                                __listVector.Add(new Vector2d(compBB[cit].Item1.x, compBB[cit].Item1.y));
                                __listVector.Add(new Vector2d(compBB[cit].Item1.x, compBB[cit].Item2.y));
                                __listVector.Add(new Vector2d(compBB[cit].Item2.x, compBB[cit].Item2.y));
                                __listVector.Add(new Vector2d(compBB[cit].Item2.x, compBB[cit].Item1.y));

                            }

                            count++;
                        }
                        ind++;
                    }

                    if (count != 0)
                    {
                        //Check outside image
                        miny = Math.Max(miny - 10, 0);
                        minx = Math.Max(minx - 10, 0);
                        maxy = Math.Min(maxy + 10, input.Height);
                        maxx = Math.Min(maxx + 10, input.Width);

                        CvPoint p0 = new CvPoint((int)minx, (int)miny);//chu y ko + - here
                        CvPoint p1 = new CvPoint((int)maxx, (int)maxy);
                        __bbAll.Add(new Tuple<CvPoint, CvPoint>(p0, p1));
                        _listVector.Add(__listVector);
                    }

                    
                }
                bbAll.Add(__bbAll);
                listVectorAll.Add(_listVector);
            }

            //normalize soS
            //double minSOS = totalWithinSoS.Min();
            //double maxSOS = totalWithinSoS.Max();
            //int SoSindex = 0;
            //foreach (var item in totalWithinSoS)
            //{
            //    var temp = (item - minSOS) / (maxSOS - minSOS);
            //    totalWithinSoS[SoSindex]= temp;
            //    SoSindex++;
            //}

            // Based on the distance to refer to the optimized number of cluster
            int optimizedcluster = 1;
            if (rangeOfcluster == 1)
            {
                optimizedcluster = 1;
            }
            else
            {
                int idx = 1;
                Line ImagaryLine;
                //ImagaryLine = Line.FromPoints(new Accord.Point((float)totalWithinSoS[0], 1), new Accord.Point((float)totalWithinSoS[rangeOfcluster - 1], rangeOfcluster));
                ImagaryLine = Line.FromPoints(new Accord.Point(1, (float)totalWithinSoS[0]), new Accord.Point(MIN_CLUSTER, 0));
                //ImagaryLine = Line.FromPoints( new Accord.Point(1, (float)totalWithinSoS[0]), new Accord.Point(2 * MIN_CLUSTER, 0));
                float maxDistance = 0;

                foreach (var item in totalWithinSoS)
                {
                    Debug.WriteLine("totalWithinSoS of " + idx + "cluster: " + item);
                    Debug.WriteLine("   " + ImagaryLine.DistanceToPoint(new Accord.Point(idx, (float)item)));
                    var tempDis = ImagaryLine.DistanceToPoint(new Accord.Point(idx, (float)item));
                    if (tempDis > maxDistance)
                    {
                        optimizedcluster = idx;
                        maxDistance = tempDis;
                    }
                    ///This is case the number observation = number cluster (ideal)
                    //if (item == 0 && rangeOfcluster < MIN_CLUSTER )
                    //{
                    //    optimizedcluster = idx;
                    //}

                    idx++;
                }

                Debug.WriteLine("OptimizedCluster using Elbowmethod is: " + optimizedcluster.ToString());

            }

            #region hoi quy kmean
            //int indexObs = 0;

            //List<Observation> inputList = new List<Observation>();
            //foreach (var item in observations)
            //{
            //    var ob = new Observation();
            //    ob.observation = item;
            //    ob.index = indexObs;
            //    inputList.Add(ob);
            //    indexObs++;
            //}

            //KmeanObservation kmeanOb = new KmeanObservation();
            //kmeanOb.observationsList = inputList;
            //kmeanOb.continueKmean = true;

            //List<KmeanObservation> input_ = new List<KmeanObservation>();
            //input_.Add(kmeanOb);
            //var hoiquy = new List<KmeanObservation>();
            //hoiquy = RecursiveKmean(input_);

            //// return the coordinate of rect
            //List<Tuple<CvPoint, CvPoint>> _bbAll = new List<Tuple<CvPoint, CvPoint>>();
            //foreach (var kit in hoiquy)
            //{
            //    double minx = input.Width;
            //    double miny = input.Height;
            //    double maxx = 0;
            //    double maxy = 0;

            //    foreach (var obit in kit.observationsList)
            //    {
            //        miny = Math.Min(miny, ClonechainBB[obit.index].Item1.y);
            //        minx = Math.Min(minx, ClonechainBB[obit.index].Item1.x);
            //        maxy = Math.Max(maxy, ClonechainBB[obit.index].Item2.y);
            //        maxx = Math.Max(maxx, ClonechainBB[obit.index].Item2.x);
            //    }
            //    //Check outside image
            //    miny = Math.Max(miny - 10, 0);
            //    minx = Math.Max(minx - 10, 0);
            //    maxy = Math.Min(maxy + 10, input.Height);
            //    maxx = Math.Min(maxx + 10, input.Width);

            //    CvPoint p0 = new CvPoint((int)minx, (int)miny);//chu y ko + - here
            //    CvPoint p1 = new CvPoint((int)maxx, (int)maxy);
            //    _bbAll.Add(new Tuple<CvPoint, CvPoint>(p0, p1));

            //}

            // this is for Kmean Recursive 
            //return _bbAll;
            #endregion


            //CvMemStorage storage = new CvMemStorage();
            ////CvSeq<CvPoint> result = contoursRaw;
            //CvSeq seq = Cv.CreateSeq(SeqType.EltypeF32C2, CvSeq.SizeOf, CvPoint.SizeOf, storage);

            //List<Vector2d> listVector = new List<Vector2d>();
            
            List<List<OCRRegion>> _regionAll = new List<List<OCRRegion>>();
            int labelInd = 0;
            foreach (var item in bbAll)
            {
                int reInd = 0;
                List<OCRRegion> listRe = new List<OCRRegion>();
                foreach (var vit in listVectorAll[labelInd])
                {
                    
                    var mininalbox = MinimalBoundingBox.Calculate(vit.ToArray());
                    var rect = bbAll[labelInd][reInd];
                    var _region = new OCRRegion(rect, mininalbox);
                    listRe.Add(_region);

                    reInd++;
                }
                labelInd++;

                _regionAll.Add(listRe);

            }
            

            return _regionAll[optimizedcluster - 1];
            //return bbAll[optimizedcluster - 1];
        }
        /// <summary>
        /// This method is used to detect text region using ky thuat merger using morphological geometry
        /// </summary>
        /// <param name="input"></param>
        /// <param name="chains"></param>
        /// <param name="validComponents"></param>
        /// <param name="chainCenters"></param>
        /// <param name="chainMedians"></param>
        /// <param name="chainDimensions"></param>
        /// <param name="chainBB"></param>
        /// <returns></returns>
        private static List<Tuple<CvPoint, CvPoint>> makeMorphologicalChains(
            //return List<Tuple<CvPoint, CvPoint>>
            IplImage input,
            List<Chain> chains,
            List<List<Point2d>> validComponents,
            List<Point2dFloat> chainCenters,
            List<float> chainMedians,
            List<Point2d> chainDimensions,
            List<Tuple<Point2d, Point2d>> chainBB)
        {
            #region morphological cluster
            int merges2 = 1;
            while (merges2 > 0)
            {
                for (int i = 0; i < chains.Count(); i++)
                {
                    var t1 = chains[i];
                    t1.merged = false;
                    chains[i] = t1;
                }

                merges2 = 0;
                List<Chain> newchains = new List<Chain>();

                for (int i = 0; i < chains.Count(); i++)
                {
                    for (int j = i + 1; j < chains.Count(); j++)
                    {
                        if (!chains[i].merged && !chains[j].merged && shareBoundingPoints(chainBB[i], chainBB[j]))
                        {

                            foreach (int it in chains[j].components)
                            {
                                chains[i].components.Add(it);
                            }

                            var t5 = chains[j];
                            t5.merged = true;
                            chains[j] = t5;

                            merges2++;
                        }
                    }

                }

                for (int i = 0; i < chains.Count(); i++)
                {
                    if (!chains[i].merged)
                    {
                        newchains.Add(chains[i]);
                    }
                }
                chains = newchains;

            }
            #endregion

            List<Tuple<CvPoint, CvPoint>> _bbAll = new List<Tuple<CvPoint, CvPoint>>();

            for (int i = 0; i < chains.Count(); i++)
            {
                double minx = input.Width;
                double miny = input.Height;
                double maxx = 0;
                double maxy = 0;

                //int count = 0;
                
                foreach (var item in chains[i].components)
                {
                        miny = Math.Min(miny, chainBB[item].Item1.y);
                        minx = Math.Min(minx, chainBB[item].Item1.x);
                        maxy = Math.Max(maxy, chainBB[item].Item2.y);
                        maxx = Math.Max(maxx, chainBB[item].Item2.x);
                }
                    //Check outside image
                    miny = Math.Max(miny - 10, 0);
                    minx = Math.Max(minx - 10, 0);
                    maxy = Math.Min(maxy + 10, input.Height);
                    maxx = Math.Min(maxx + 10, input.Width);

                    CvPoint p0 = new CvPoint((int)minx, (int)miny);//chu y ko + - here
                    CvPoint p1 = new CvPoint((int)maxx, (int)maxy);
                    _bbAll.Add(new Tuple<CvPoint, CvPoint>(p0, p1));
                
            }
            return _bbAll;
            //return chains;
        }
        public static List<KmeanObservation> RecursiveKmean(List<KmeanObservation> inputs)
        {
            int runningKmean = 1;//tag 
            int loop = 0;
            while (runningKmean > 0)
            {
                for (int i = 0; i < inputs.Count(); i++)
                {
                    var t1 = inputs[i];
                    t1.continueKmean = true;
                    inputs[i] = t1;
                }

                runningKmean = 0;
                loop++;
                List<KmeanObservation> newinputs = new List<KmeanObservation>();
                int curCount = inputs.Count(); // important

                for (int i = 0; i < curCount; i++)
                {
                    if (inputs[i].continueKmean)
                    {
                       
                        var result = KmeanObservation(checkrunningKmean(inputs[i]), inputs[i] );
                        //if (true)
                        //if (checkrunningKmean(inputs[i]) && result.Count>=2)
                        if (result.Count >= 2 && loop <2)
                            {
                            var t2 = inputs[i];
                            t2.continueKmean = false;
                            inputs[i] = t2;
                            foreach (var item in result)
                            {
                                inputs.Add(item);
                            }
                            runningKmean++;

                            Debug.WriteLine("Recursive Recursive ...");
                        }
                    }

                }

                for (int i = 0; i < inputs.Count(); i++)
                {
                    if (inputs[i].continueKmean)
                    {
                        newinputs.Add(inputs[i]);
                    }
                }

                inputs = newinputs;

            }

            return inputs;
        }
        //method kmean for recursive kmean
        private static List<KmeanObservation> KmeanObservation(KmeanOption kmeanOption, KmeanObservation kmeanObservation)
        {
            Accord.Math.Random.Generator.Seed = 1234;
            double[][] observations = new double[kmeanObservation.observationsList.Count()][];
            int index = 0;
            foreach (var item in kmeanObservation.observationsList)
            {
                List<double> selFeature = new List<double>();
                //if (kmeanOption.posHorizontal)
                //{
                //    selFeature.Add(item.observation[0]);
                //}

                //if (kmeanOption.posVertical)
                //{
                //    selFeature.Add(item.observation[1]);
                //}

                //if (kmeanOption.pos2Dimesions)
                //{
                //    selFeature.Add(item.observation[0]);
                //    selFeature.Add(item.observation[1]);
                //}


                for (int i = 0; i < item.observation.Length; i++)
                {
                    selFeature.Add(item.observation[i]);
                }
                observations[index] = selFeature.ToArray();
                index++;
            }

            // Elbow  algorithm to get the optimized number cluster
            int MIN_CLUSTER = 10;
            var rangeOfcluster = 1;
            if (observations.Count() <= MIN_CLUSTER)
            {
                rangeOfcluster = observations.Count();
            }
            else
            {
                rangeOfcluster = MIN_CLUSTER;
            }


            double[] totalWithinSoS = new double[rangeOfcluster];
            List<List<KmeanObservation>> bbAll = new List<List<KmeanObservation>>();

            for (int cl = 0; cl < rangeOfcluster; cl++)
            {
                var initCluster = cl + 1;
                KMeans kmeans = new KMeans(k: initCluster)
                {
                    // Distance = new WeightedSquareEuclidean(new double[] { 0.01, 0.9})
                };

                // Compute and retrieve the data centroids
                var clusters = kmeans.Learn(observations);

                // Use the centroids to parition all the data
                int[] labels = clusters.Decide(observations);
                List<KmeanObservation> _bbAll = new List<KmeanObservation>();

                for (int label = 0; label < initCluster; label++)
                {
                    int count = 0;
                    int ind = 0;
                    var listOb = new List<Observation>();

                    foreach (var item in labels)
                    {

                        if (item == label)
                        {
                            //Debug.WriteLine("Cluster centroid: " + clusters.Centroids[i][0] + " " + clusters.Centroids[i][1]);
                            //Debug.WriteLine("      data point: " + observations[ind][0] + " " + observations[ind][1]);
                            //totalWithinSoS[cl] = totalWithinSoS[cl] + Distance.SquareEuclidean(observations[ind][0], observations[ind][1], clusters.Centroids[i][0], clusters.Centroids[i][1]);
                            totalWithinSoS[cl] = totalWithinSoS[cl] + Distance.SquareEuclidean(observations[ind], clusters.Centroids[label]);

                            var ob = kmeanObservation.observationsList[ind];
                            listOb.Add(ob);
                            count++;
                        }
                        ind++;
                    }

                    if (count != 0)
                    {
                        var kmeanOb = new KmeanObservation();
                        kmeanOb.observationsList = listOb;
                        kmeanOb.continueKmean = true;
                        _bbAll.Add(kmeanOb);
                    }
                }
                bbAll.Add(_bbAll);
            }

            // Based on the distance to refer to the optimized number of cluster
            int optimizedcluster = 1;
            if (rangeOfcluster == 1)
            {
                optimizedcluster = 1;
            }
            else
            {
                int idx = 1;
                Line ImagaryLine;
                //ImagaryLine = Line.FromPoints(new Accord.Point((float)totalWithinSoS[0], 1), new Accord.Point((float)totalWithinSoS[rangeOfcluster - 1], rangeOfcluster));
                //ImagaryLine = Line.FromPoints(new Accord.Point((float)totalWithinSoS[0], 1), new Accord.Point(0,  MIN_CLUSTER));
                ImagaryLine = Line.FromPoints(new Accord.Point(1, (float)totalWithinSoS[0]), new Accord.Point(MIN_CLUSTER, 0));
                float maxDistance = 0;

                foreach (var item in totalWithinSoS)
                {
                    Debug.WriteLine("totalWithinSoS of " + idx + "cluster: " + item);
                    Debug.WriteLine("   " + ImagaryLine.DistanceToPoint(new Accord.Point((float)item, idx)));
                    var tempDis = ImagaryLine.DistanceToPoint(new Accord.Point(idx, (float)item));
                    if (tempDis > maxDistance)
                    {
                        optimizedcluster = idx;
                        maxDistance = tempDis;
                    }
                    ///This is case the number observation = number cluster (ideal)
                    //if (item == 0 && rangeOfcluster < MIN_CLUSTER )
                    //{
                    //    optimizedcluster = idx;
                    //}

                    idx++;
                }

                Debug.WriteLine("OptimizedCluster using Elbowmethod is: " + optimizedcluster.ToString());

            }

            return bbAll[optimizedcluster - 1];
        }

        public static List<Tuple<CvPoint, CvPoint>> numberDetection(IplImage input, bool darkOnLight)
        {
            // convert to grayscale, could do better with color subtraction if we know what white will look like
            IplImage grayImg = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            Cv.CvtColor(input, grayImg, ColorConversion.BgrToGray);
            Cv.SaveImage("aGray.png", grayImg);

            //ConvertColorHue(input, grayImg);
            //Cv.SaveImage("hue.png", grayImg);

            // create canny --> hard to automatically find parameters...
            double threshLow = 10;//5
            double threshHigh = 200;//50
            IplImage edgeImg = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            Cv.Canny(grayImg, edgeImg, threshLow, threshHigh, ApertureSize.Size3);
            Cv.SaveImage("1Canny.png", edgeImg);

            // create gradient x, gradient y
            IplImage gaussianImg = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            Cv.ConvertScale(grayImg, gaussianImg, 1.0 / 255.0, 0);
            //Gaussian smoothing is commonly used with eadge detection
            Cv.Smooth(gaussianImg, gaussianImg, SmoothType.Gaussian, 5, 5); //Gaussian filter 5x5
            IplImage gradientX = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            IplImage gradientY = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            Cv.Sobel(gaussianImg, gradientX, 1, 0, ApertureSize.Scharr);
            Cv.Sobel(gaussianImg, gradientY, 0, 1, ApertureSize.Scharr);
            Cv.Smooth(gradientX, gradientX, SmoothType.Blur, 3, 3);
            Cv.Smooth(gradientY, gradientY, SmoothType.Blur, 3, 3);
            //Cv.SaveImage("GradientX.png", gradientX);
            //Cv.SaveImage("GradientY.png", gradientY);


            // Calculate SWT and return ray vectors
            List<Ray> rays = new List<Ray>();
            IplImage SWTImage = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            SWTImage.Set(-1);

            // Stroke width transform from edge image
            StrokeWidthTransform(edgeImg, gradientX, gradientY, darkOnLight, SWTImage, rays);
            IplImage saveSWT = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            Cv.ConvertScale(SWTImage, saveSWT, 255, 0);
            Cv.SaveImage("2SWT.png", saveSWT);

            // Stroke Width Transform using Median filter
            SWTMedianFilter(SWTImage, rays);
            Cv.ConvertScale(SWTImage, saveSWT, 255, 0);
            Cv.SaveImage("3SWTMedianFilter.png", saveSWT);

            //// not in the original algorithm... if rays are deviating too much from median, remove
            ////IplImage cleanSWTImage = Cv.CreateImage( input.GetSize(), BitDepth.F32, 1 );
            IplImage cleanSWTImage = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            cleanSWTImage.Set(-1);
            FilterRays(SWTImage, rays, cleanSWTImage);
            Cv.ConvertScale(cleanSWTImage, saveSWT, 255, 0);
            Cv.SaveImage("4CleanSWTImage.png", saveSWT);

            //// normalize
            //IplImage output2 = Cv.CreateImage(input.GetSize(), BitDepth.F32, 1);
            //NormalizeImage(SWTImage, output2);

            //// binarize and close with rectangle to fill gaps from cleaning
            //cleanSWTImage = SWTImage;
            var S1 = Cv.CreateStructuringElementEx(3, 1, 1, 0, ElementShape.Rect, null);
            var S2 = Cv.CreateStructuringElementEx(6, 1, 3, 0, ElementShape.Rect, null);
            IplImage binSWTImage = Cv.CreateImage(cleanSWTImage.GetSize(), BitDepth.U8, 1);
            IplImage mImage = Cv.CreateImage(cleanSWTImage.GetSize(), BitDepth.U8, 1);
            IplImage tempImg = Cv.CreateImage(cleanSWTImage.GetSize(), BitDepth.U8, 1);
            //Cv.Threshold(cleanSWTImage, binSWTImage, 1, 255, ThresholdType.Binary);
            //Cv.MorphologyEx(binSWTImage, binSWTImage, tempImg, new IplConvKernel(5, 17, 2, 8, ElementShape.Rect), MorphologyOperation.Close);
            Cv.MorphologyEx(grayImg, mImage, tempImg, new IplConvKernel(6, 1, 3, 0, ElementShape.Rect), MorphologyOperation.BlackHat);
            Cv.Normalize(mImage, mImage, 0, 255, NormType.MinMax);
            Cv.Threshold(mImage, binSWTImage, (int)10*Cv.Avg(mImage).Val0, 255, ThresholdType.Binary);

            Cv.ConvertScale(mImage, saveSWT, 255, 0);
            Cv.SaveImage("5SWT_Morphology.png", saveSWT);
            Cv.ConvertScale(binSWTImage, saveSWT, 255, 0);
            Cv.SaveImage("5Binary_Morphology.png", saveSWT);

            int cnt;
            int nonZero1, nonZero2, nonZero3, nonZero4;
            IplImage dstImage = Cv.CreateImage(cleanSWTImage.GetSize(), BitDepth.U8, 1);
            int nonZeroThresh = 15;

            CvRect rect;
            for (int i = 0; i < binSWTImage.Width-32; i+=4)
            {
                for (int j = 0; j < binSWTImage.Height-16; j+=4)
                {
                    rect = new CvRect(i, j, 16, 8);
                    Cv.SetImageROI(binSWTImage, rect);
                    nonZero1 = Cv.CountNonZero(binSWTImage);
                    Cv.ResetImageROI(binSWTImage);

                    rect = new CvRect(i+16, j, 16, 8);
                    Cv.SetImageROI(binSWTImage, rect);
                    nonZero2 = Cv.CountNonZero(binSWTImage);
                    Cv.ResetImageROI(binSWTImage);

                    rect = new CvRect(i, j+8, 16, 8);
                    Cv.SetImageROI(binSWTImage, rect);
                    nonZero3 = Cv.CountNonZero(binSWTImage);
                    Cv.ResetImageROI(binSWTImage);

                    rect = new CvRect(i+16, j+8, 16, 8);
                    Cv.SetImageROI(binSWTImage, rect);
                    nonZero4 = Cv.CountNonZero(binSWTImage);
                    Cv.ResetImageROI(binSWTImage);

                    cnt = 0;

                    if (nonZero1 > nonZeroThresh) { cnt++; }
                    if (nonZero2 > nonZeroThresh) { cnt++; }
                    if (nonZero3 > nonZeroThresh) { cnt++; }
                    if (nonZero4 > nonZeroThresh) { cnt++; }

                    if (cnt>2)
                    {
                        rect = new CvRect(i, j, 32, 16);
                        Cv.SetImageROI(dstImage, rect);
                        Cv.SetImageROI(binSWTImage, rect);
                        Cv.Copy(binSWTImage, dstImage);
                        Cv.ResetImageROI(dstImage);
                        Cv.ResetImageROI(binSWTImage);
                    }
                }
            }


            Cv.ConvertScale(dstImage, saveSWT, 255, 0);
            Cv.SaveImage("5MovingBinary0.png", saveSWT);

            Cv.Dilate(dstImage, dstImage, null, 2);
            Cv.ConvertScale(dstImage, saveSWT, 255, 0);
            Cv.SaveImage("5Dilation_Binary1.png", saveSWT);

            Cv.Erode(dstImage, dstImage, null, 2);
            Cv.ConvertScale(dstImage, saveSWT, 255, 0);
            Cv.SaveImage("5Erode_Binary1.png", saveSWT);

            Cv.Dilate(dstImage, dstImage, S1, 9);
            Cv.ConvertScale(binSWTImage, saveSWT, 255, 0);
            Cv.SaveImage("5Dilation_Binary2.png", saveSWT);

            Cv.Erode(dstImage, dstImage, S1, 10);
            Cv.ConvertScale(binSWTImage, saveSWT, 255, 0);
            Cv.SaveImage("5Erode_Binary2.png", saveSWT);
            Cv.Dilate(dstImage, dstImage);

            Cv.ConvertScale(dstImage, saveSWT, 255, 0);
            Cv.SaveImage("5Dilation_Binary3.png", saveSWT);

            //CvMemStorage storagePlate = Cv.CreateMemStorage(0);
            //CvSeq contour = Cv.CreateSeq(SeqType.Contour, sizeof(CvSeq), sizeof(CvPoint), storagePlate);
            //Cv.FindContours(dstImage, storagePlate, contour, firstContour, int headerSize, ContourRetrieval.External, ContourChain.ApproxSimple);

            List<CvPoint[]> listOfPoints = new List<CvPoint[]>();
            CvSeq<CvPoint> contoursRaw;
            IplImage RectdstImage = Cv.CreateImage(cleanSWTImage.GetSize(), BitDepth.U8, 3);
            Cv.CvtColor(dstImage, RectdstImage, ColorConversion.GrayToBgr);

            using (CvMemStorage storage = new CvMemStorage())
            {
                Cv.FindContours(dstImage, storage, out contoursRaw, CvContour.SizeOf, 
                    ContourRetrieval.External, ContourChain.ApproxSimple);
               
                while (contoursRaw!=null)
                {
                    int xmin = 1000000;
                    int ymin = 1000000;
                    int xmax = 0;
                    int ymax = 0;
                    int w, h, s;
                    int count;

                    CvSeq<CvPoint> result = contoursRaw;
                    
                    double area = Cv.ContourArea(result);
                    Debug.WriteLine("Contour Area: " + area);
                    count = result.Total;
                    CvPoint[] points = new CvPoint[result.Total];
                    Cv.CvtSeqToArray(result, out points, CvSlice.WholeSeq);
                    //List<CvPoint> point = new List<CvPoint>();
                    //var rotatedRect = Cv.MinAreaRect2(result);

                    for (int i = 0; i < count; i++)
                    {
                        ymin = Math.Min(ymin, points[i].Y);
                        xmin = Math.Min(xmin, points[i].X);
                        ymax = Math.Max(ymax, points[i].Y);
                        xmax = Math.Max(xmax, points[i].X);
                    }

                    w = xmax - xmin;
                    h = ymax - ymin;
                    s = w * h;
                    double whRatio = (double)w / h;
                    if (3.0 < whRatio && whRatio <8.0)
                    {
                        var p1 = new CvPoint(xmin, ymin);
                        var p2 = new CvPoint(xmax, ymax);
                        var c = new CvScalar(0, 0, 255);
                        Cv.Rectangle(RectdstImage, p1, p2, c, 3);

                    }
                    
                    contoursRaw = contoursRaw.HNext;
                }
            }
            //Cv.ConvertScale(RectdstImage, saveSWT, 255, 0);
            Cv.SaveImage("5DrawRect_Moving.png", RectdstImage);

            //List<Con> contour

            //IplImage saveSWT = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            //Cv.ConvertScale(output2, saveSWT, 255, 0);
            //Cv.SaveImage("SWT.png", saveSWT);


            //// Calculate legally connect components from SWT and gradient image.
            //// return type is a vector of vectors, where each outer vector is a component and
            //// the inner vector contains the (y,x) of each pixel in that component.
            //List<List<Point2d>> components = findLegallyConnectedComponents( SWTImage, rays );

            //Cung hay
            cleanSWTImage = SWTImage; //quan
            //cleanSWTImage = binSWTImage;

            //Components analysis corresponding to character: Original
            List<List<Point2d>> components = FindLegallyConnectedComponents(cleanSWTImage, rays);

            //Components analysis corresponding to character: Other
            //IplImage binFloatImg = Cv.CreateImage(binSWTImage.GetSize(), BitDepth.F32, 1);
            //Cv.Convert(binSWTImage, binFloatImg);
            //List<List<Point2d>> components = FindLegallyConnectedComponents(binFloatImg, rays);

            // Filter the components
            List<List<Point2d>> validComponents = new List<List<Point2d>>();
            List<Point2dFloat> compCenters = new List<Point2dFloat>();
            List<float> compMedians = new List<float>();
            List<Point2d> compDimensions = new List<Point2d>();
            List<Tuple<Point2d, Point2d>> compBB = new List<Tuple<Point2d, Point2d>>();

            FilterComponents(cleanSWTImage, components, ref validComponents, ref compCenters, ref compMedians, ref compDimensions, ref compBB);
            IplImage outComponent = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            RenderComponentsWithBoxes(cleanSWTImage, validComponents, compBB, outComponent);
            Cv.SaveImage("6SWT_Components.png", outComponent);

            List<Tuple<CvPoint, CvPoint>> numberResults = new List<Tuple<CvPoint, CvPoint>>();
            numberResults = ConvertcompBB2cvPoint(compBB);
            ////List<Tuple<CvPoint, CvPoint>> quanRect = new List<Tuple<CvPoint, CvPoint>>();
            ////quanRect = FindBoundingBoxesAll2(compCenters, compBB, cleanSWTImage);//quan them
            ////quanRect = FindBoundingBoxesAll(compBB, cleanSWTImage);//quan them lan 1

            ////IplImage output3 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3); //save
            ////RenderComponentsWithBoxes2(cleanSWTImage, compCenters, components, compBB, output3);
            ////Cv.SaveImage("componentsWithK-mean.png", output3);
            ////cvReleaseImage ( &output3 );

            ////// Make chains of components
            //List<Chain> chains;
            //chains = makeChains(input, validComponents, compCenters, compMedians, compDimensions, compBB);

            //IplImage outChain = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            //renderChainsWithBoxes(SWTImage, validComponents, compCenters, chains, compBB, outChain);
            //Cv.SaveImage("7SWT_Chains.png", outChain);

            //List<Tuple<CvPoint, CvPoint>> kmeanResult = new List<Tuple<CvPoint, CvPoint>>();
            ////kmeanResult = makeKmeanComponents(input, chains, validComponents, compCenters, compMedians, compDimensions, compBB);// original
            ////options for kmean
            //kmeanResult = makeKmeanOptionComponents(true, false, false, false, input, chains, validComponents, compCenters, compMedians, compDimensions, compBB);

            //IplImage outputRender = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            //RenderKmeanWithBoxesonImage(cleanSWTImage, components, kmeanResult, outputRender);
            //Cv.SaveImage("8AccordKmean.png", outputRender);

            //IplImage output4 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 1);
            //renderChains(SWTImage, validComponents, chains, output4);
            //Cv.SaveImage("9text.png", output4);

            //////IplImage output5 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            //////Cv.CvtColor(output4, output5, ColorConversion.GrayToRgb);
            //////Cv.SaveImage("text2.png", output5);

            ////IplImage output6 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3);
            //////List<Tuple<CvPoint, CvPoint>> quanRect2 = new List<Tuple<CvPoint, CvPoint>>();
            ////var quanRect2 = renderChainsWithBoxes(SWTImage, validComponents, compCenters, chains, compBB, output6);
            ////Cv.SaveImage("text3.png", output6);

            //////k-mean2
            ////List<Tuple<CvPoint, CvPoint>> quanRectKmean = new List<Tuple<CvPoint, CvPoint>>();
            //////quanRectKmean = FindBoundingBoxesAll3(compCenters, quanRect2, cleanSWTImage);//quan them
            ////quanRectKmean = FindBoundingBoxesAll2(compCenters, quanRect2, cleanSWTImage);//quan them
            //////quanRect = FindBoundingBoxesAll(compBB, cleanSWTImage);//quan them lan 1

            ////IplImage output7 = Cv.CreateImage(input.GetSize(), BitDepth.U8, 3); //save
            ////RenderComponentsWithBoxes3(cleanSWTImage, compCenters, components, quanRectKmean, output7);
            ////Cv.SaveImage("componentsWithK-mean2.png", output7);
            //return new List<Tuple<CvPoint, CvPoint>>();
            return numberResults;
        }

        private static List<Tuple<CvPoint, CvPoint>> ConvertcompBB2cvPoint(List<Tuple<Point2d, Point2d>> compBB)
        {
            List<Tuple<CvPoint, CvPoint>> _bb = new List<Tuple<CvPoint, CvPoint>>();
            foreach (var item in compBB)
            {
                var p1 = new CvPoint();
                p1.X = item.Item1.x-3;
                p1.Y = item.Item1.y-3;

                var p2 = new CvPoint();
                p2.X = item.Item2.x+3;
                p2.Y = item.Item2.y+3;

                _bb.Add(new Tuple<CvPoint, CvPoint>(p1, p2));
            }
            return _bb;

        }

        private static void RenderComponentsWithBoxes3(
            IplImage SWTImage,
            List<Point2dFloat> compCenters,
            List<List<Point2d>> components,
            List<Tuple<CvPoint, CvPoint>> quanRectKmean,
            IplImage output)
        {
            IplImage outTemp = Cv.CreateImage(output.GetSize(), BitDepth.F32, 1);
            RenderComponents(SWTImage, components, outTemp);
            //List<Tuple<CvPoint, CvPoint>> bb = new List<Tuple<CvPoint, CvPoint>>(compBB.Count());

            //foreach (Tuple<Point2d, Point2d> it in compBB)
            //{
            //    CvPoint p0 = new CvPoint(it.Item1.x, it.Item1.y);
            //    CvPoint p1 = new CvPoint(it.Item2.x, it.Item2.y);
            //    Tuple<CvPoint, CvPoint> pair = new Tuple<CvPoint, CvPoint>(p0, p1);
            //    bb.Add(pair);
            //}

            IplImage outImg = Cv.CreateImage(output.GetSize(), BitDepth.U8, 1);

            Cv.Convert(outTemp, outImg);
            Cv.CvtColor(outImg, output, ColorConversion.GrayToBgr);

            int count = 0;
            foreach (Tuple<CvPoint, CvPoint> it in quanRectKmean)
            {
                CvScalar c;
                if (count % 3 == 0) c = new CvScalar(255, 0, 0);
                else if (count % 3 == 1) c = new CvScalar(0, 255, 0);
                else c = new CvScalar(0, 0, 255);
                count++;
                Cv.Rectangle(output, it.Item1, it.Item2, c, 1);
            }

            // render bouding box after k-mean cluster
            //List<Tuple<CvPoint, CvPoint>> k_center = new List<Tuple<CvPoint, CvPoint>>(compBB.Count());
            //k_center = FindBoundingBoxesAll2(compCenters, compBB, output);
            int count2 = 0;
            foreach (Tuple<CvPoint, CvPoint> it in quanRectKmean)
            {
                CvScalar c;
                if (count2 % 3 == 0) c = new CvScalar(255, 0, 0);
                else if (count2 % 3 == 1) c = new CvScalar(0, 255, 0);
                else c = new CvScalar(0, 0, 255);
                count2++;

                Cv.Rectangle(output, it.Item1, it.Item2, c, 4);
            }

        }

       
        private static List<Tuple<CvPoint, CvPoint>> FindBoundingBoxesAll3(
            List<Point2dFloat> compCenters, 
            List<Tuple<CvPoint, CvPoint>> quanRect2, 
            IplImage outTemp)
        {
            //K_meanCluster kmean = new K_meanCluster();

            //foreach (var item in quanRect2)
            //{
            //    kmean._rawDataToCluster.Add(new DataPoint((int)(item.Item1.X+item.Item2.X)/2, (int)(item.Item1.Y + item.Item2.Y) / 2));
            //    Debug.WriteLine("{" + (int)((item.Item1.X + item.Item2.X) / 2) + "," + (item.Item1.Y + item.Item2.Y) / 2 + "}");
            //}

            //List<DataPoint> k_meanedCenter = new List<DataPoint>();
            //k_meanedCenter = kmean.runKmean();

            List<Tuple<CvPoint, CvPoint>> bbAll = new List<Tuple<CvPoint, CvPoint>>();
            //for (int i = 0; i < kmean._numberOfClusters; i++)
            //{
            //    double minx = outTemp.Width;
            //    double miny = outTemp.Height;
            //    double maxx = 0;
            //    double maxy = 0;

            //    int count = 0;
            //    foreach (var item in k_meanedCenter)
            //    {
            //        if (item.Cluster == i)
            //        {

            //            miny = Math.Min(miny, item.Y);
            //            minx = Math.Min(minx, item.X);
            //            maxy = Math.Max(maxy, item.Y);
            //            maxx = Math.Max(maxx, item.X);
            //            count++;
            //        }
            //    }

            //    if (count!=0)
            //    {
            //        CvPoint p0 = new CvPoint((int)minx, (int)miny);//chu y ko + - here
            //        CvPoint p1 = new CvPoint((int)maxx, (int)maxy);
            //        //CvPoint p0 = new CvPoint((int)minx, (int)miny);//chu y ko + - here
            //        //CvPoint p1 = new CvPoint((int)maxx, (int)maxy);
            //        bbAll.Add(new Tuple<CvPoint, CvPoint>(p0, p1));
            //    }
            //}
            return bbAll;
        }

        /// <summary>
        /// This method is used to calculate the chains
        /// </summary>
        /// <param name="SWTImage"></param>
        /// <param name="components"></param>
        /// <param name="chains"></param>
        /// <param name="compBB"></param>
        /// <param name="output6"></param>
        /// <returns></returns>
        public static List<Tuple<Point2d, Point2d>> renderChainsWithBoxes(
            IplImage SWTImage,
            List<List<Point2d>> components,
            List<Point2dFloat> compCenters,
            List<Chain> chains,
            List<Tuple<Point2d, Point2d>> compBB,
            IplImage output6)
        {
            // keep track of included components

            List<bool> included = new List<bool>(components.Count());

            //included.reserve(components.size());

            for (int i = 0; i != components.Count(); i++)
            {
                included.Add(false);
            }

            foreach (Chain it in chains)
            {
                foreach (int cit in it.components)
                {
                    included[cit] = true;
                }
            }

            List<List<Point2d>> componentsRed = new List<List<Point2d>>();

            for (int i = 0; i != components.Count(); i++)
            {
                if (included[i])
                {
                    componentsRed.Add(components[i]);
                }
            }

            //Mat outTemp(output.size(), CV_32FC1 );
            IplImage outTemp = Cv.CreateImage(output6.GetSize(), BitDepth.F32, 1);
            Debug.WriteLine("ComponentRed size: " + componentsRed.Count().ToString());


            //std::cout << componentsRed.size() << " components after chaining" << std::endl;

            RenderComponents(SWTImage, componentsRed, outTemp);

            //List<SWTPointPair2i> bb;
            List<Tuple<CvPoint, CvPoint>> bb = new List<Tuple<CvPoint, CvPoint>>(compBB.Count());

            bb = FindBoundingBoxes(components, chains, compBB, outTemp);

            List<Tuple<CvPoint, CvPoint>> bbAll = new List<Tuple<CvPoint, CvPoint>>(compBB.Count());
            bbAll = FindBoundingBoxesAll(bb, outTemp);



            //Mat out(output.size(), CV_8UC1);
            IplImage outz = Cv.CreateImage(output6.GetSize(), BitDepth.U8, 1);

            //outTemp.convertTo(out, CV_8UC1, 255);
            Cv.ConvertScale(outTemp, outz, 1, 0);

            //cvtColor(outz, output6, CV_GRAY2RGB);
            Cv.CvtColor(outz, output6, ColorConversion.GrayToRgb);


            int count = 0;
            List<Tuple<Point2d, Point2d>>  centerNew = new List<Tuple<Point2d, Point2d>>();
            foreach (Tuple<CvPoint, CvPoint> it in bb)
            {
                CvScalar c;
                if (count % 3 == 0) c = new CvScalar(255, 0, 0);
                else if (count % 3 == 1) c = new CvScalar(0, 255, 0);
                else c = new CvScalar(0, 0, 255);
                count++;
                Cv.Rectangle(output6, it.Item1, it.Item2, c, 1);

                var newbb1 = new Point2d();
                newbb1.x = it.Item1.X;
                newbb1.y = it.Item1.Y;

                var newbb2 = new Point2d();
                newbb2.x = it.Item2.X;
                newbb2.y = it.Item2.Y;
                centerNew.Add(new Tuple<Point2d, Point2d>(newbb1, newbb2));
                //Cv.Line(output6, it.Item1, it.Item2, c, 3); // ve duong
            }

            foreach (Tuple<CvPoint, CvPoint> it in bbAll)
            {
                CvScalar c2 = new CvScalar(125, 125, 125);
                Cv.Rectangle(output6, it.Item1, it.Item2, c2, 2);
                //Cv.Line(output6, it.Item1, it.Item2, c2, 3); // ve duong

                Debug.WriteLine("CompAll is : " + it.Item1.X + " " + it.Item1.Y + " " + it.Item2.X + " " + it.Item2.Y);
                Debug.WriteLine("CompAll: " + ((it.Item1.X + it.Item2.X) / 2).ToString() + " " + ((it.Item1.Y + it.Item2.Y) / 2).ToString());
                Debug.WriteLine("Chieu rong: " + (it.Item2.X - it.Item1.X).ToString());
                Debug.WriteLine("Chieu dai: " + (it.Item2.Y - it.Item1.Y).ToString());
            }

            count = 0;
            foreach (Chain it in chains)
            {
                CvScalar c;
                foreach (int cit in it.components)
                {
                    if (count % 3 == 0)
                    {
                        c = new CvScalar(255, 0, 0);
                        Cv.Line(output6, new CvPoint((int)compCenters[cit].x, (int)compCenters[cit].y), new CvPoint((int)compCenters[cit].x - 10, (int)compCenters[cit].y), c, 2);
                    }

                    else if (count % 3 == 1)
                    {
                        c = new CvScalar(0, 255, 0);
                        Cv.Line(output6, new CvPoint((int)compCenters[cit].x, (int)compCenters[cit].y), new CvPoint((int)compCenters[cit].x, (int)compCenters[cit].y - 10), c, 2);
                    }
                    else
                    { c = new CvScalar(0, 0, 255);
                        Cv.Line(output6, new CvPoint((int)compCenters[cit].x, (int)compCenters[cit].y), new CvPoint((int)compCenters[cit].x + 10, (int)compCenters[cit].y), c, 2);
                    }
                }
                count++;
            }
            return centerNew;
        }

        private static List<Tuple<CvPoint, CvPoint>> FindBoundingBoxesAll3(
            List<Point2dFloat> center,
            List<Tuple<Point2d, Point2d>> bb,
            IplImage outTemp)
        {
            //FindBoundingBoxes

            //K_meanCluster kmean = new K_meanCluster();

            //foreach (var item in center)
            //{
            //    kmean._rawDataToCluster.Add(new DataPoint(item.x, item.y));
            //    Debug.WriteLine("{" + item.x + "," + item.y + "}");
            //}

            //List<DataPoint> k_meanedCenter = new List<DataPoint>();
            //k_meanedCenter = kmean.runKmean();

            List<Tuple<CvPoint, CvPoint>> bbAll = new List<Tuple<CvPoint, CvPoint>>();
            //for (int i = 0; i < kmean._numberOfClusters; i++)
            //{
            //    double minx = outTemp.Width;
            //    double miny = outTemp.Height;
            //    double maxx = 0;
            //    double maxy = 0;
            //    foreach (var item in k_meanedCenter)
            //    {
            //        if (item.Cluster == i)
            //        {

            //            miny = Math.Min(miny, item.Y);
            //            minx = Math.Min(minx, item.X);
            //            maxy = Math.Max(maxy, item.Y);
            //            maxx = Math.Max(maxx, item.X);

            //        }
            //    }
            //    CvPoint p0 = new CvPoint((int)minx - 15, (int)miny - 15);//chu y ko + - here
            //    CvPoint p1 = new CvPoint((int)maxx + 15, (int)maxy + 15);
            //    //CvPoint p0 = new CvPoint((int)minx, (int)miny);//chu y ko + - here
            //    //CvPoint p1 = new CvPoint((int)maxx, (int)maxy);
            //    bbAll.Add(new Tuple<CvPoint, CvPoint>(p0, p1));

            //}

            return bbAll;
        }
        /// <summary>
        /// This method is used to get the bounding boxs
        /// </summary>
        /// <param name="center"></param>
        /// <param name="bb"></param>
        /// <param name="outTemp"></param>
        /// <returns></returns>
        private static List<Tuple<CvPoint, CvPoint>> FindBoundingBoxesAll2(
            List<Point2dFloat> center,
            List<Tuple<Point2d, Point2d>> bb,
            IplImage outTemp)
        {
            //Accord lib
            Accord.Math.Random.Generator.Seed = 1234;

            // Declare some observations
            double[][] observations = new double[center.Count()][];
            int index = 0;

            foreach (var item in center)
            {
                observations[index] = new double[] { item.x, item.y };
                index++;
            }

            // Elbow algorithm to get the optimized number cluster
            var rangeOfcluster = 10;
            double[] totalWithinSoS = new double[rangeOfcluster];

            List<List<Tuple<CvPoint, CvPoint>>>  bbAll = new List<List<Tuple<CvPoint, CvPoint>>>();

            for (int cl = 0; cl < rangeOfcluster; cl++)
            {
                var initCluster = cl+1;
                KMeans kmeans = new KMeans(k: initCluster);

                // Compute and retrieve the data centroids
                var clusters = kmeans.Learn(observations);

                // Use the centroids to parition all the data
                int[] labels = clusters.Decide(observations);

                // K_meanCluster kmean = new K_meanCluster();

                //foreach (var item in center)
                //{
                //    kmean._rawDataToCluster.Add(new DataPoint(item.x, item.y));
                //    Debug.WriteLine("new double[] { " + item.x + "," + item.y + "},");
                //}

                //List<DataPoint> k_meanedCenter = new List<DataPoint>();
                //k_meanedCenter = kmean.runKmean();

                List<Tuple<CvPoint, CvPoint>> _bbAll = new List<Tuple<CvPoint, CvPoint>>();

                for (int i = 0; i < initCluster; i++)
                {
                    double minx = outTemp.Width;
                    double miny = outTemp.Height;
                    double maxx = 0;
                    double maxy = 0;

                    int count = 0;
                    int ind = 0;
                    foreach (var item in labels)
                    {

                        if (item == i)
                        {
                            Debug.WriteLine("Cluster centroid: " + clusters.Centroids[i][0] + " " + clusters.Centroids[i][1]);
                            Debug.WriteLine("      data point: " + observations[ind][0] + " " + observations[ind][1]);
                            totalWithinSoS[cl] = totalWithinSoS[cl] + Distance.SquareEuclidean(observations[ind][0], observations[ind][1], clusters.Centroids[i][0], clusters.Centroids[i][1]);


                            //miny = Math.Min(miny, observations[ind][1]);
                            //minx = Math.Min(minx, observations[ind][0]);
                            //maxy = Math.Max(maxy, observations[ind][1]);
                            //maxx = Math.Max(maxx, observations[ind][0]);

                            miny = Math.Min(miny, center[ind].y);
                            minx = Math.Min(minx, center[ind].x);
                            maxy = Math.Max(maxy, center[ind].y);
                            maxx = Math.Max(maxx, center[ind].x);
                            count++;
                        }
                        ind++;
                    }

                    if (count != 0)
                    {
                        CvPoint p0 = new CvPoint((int)minx - 15, (int)miny - 15);//chu y ko + - here
                        CvPoint p1 = new CvPoint((int)maxx + 15, (int)maxy + 15);
                        //CvPoint p0 = new CvPoint((int)minx, (int)miny);//chu y ko + - here
                        //CvPoint p1 = new CvPoint((int)maxx, (int)maxy);
                        _bbAll.Add(new Tuple<CvPoint, CvPoint>(p0, p1));
                    }

                    
                }

                bbAll.Add(_bbAll);
            }


            int idx = 1;
            Line ImagaryLine;
            ImagaryLine = Line.FromPoints(new Accord.Point((float)totalWithinSoS[0], 1), new Accord.Point((float)totalWithinSoS[rangeOfcluster-1], rangeOfcluster));
            float maxDistance = 0;
            int optimizedcluster=0;
            foreach (var item in totalWithinSoS)
            {
                Debug.WriteLine("totalWithinSoS of "+idx+"cluster: "  + item);
                Debug.WriteLine("   "+ ImagaryLine.DistanceToPoint(new Accord.Point((float)item, idx)));
                var tempDis = ImagaryLine.DistanceToPoint(new Accord.Point((float)item, idx));
                if (tempDis > maxDistance)
                {
                    optimizedcluster = idx;
                    maxDistance = tempDis;
                }
               idx++;
            }

            Debug.WriteLine("OptimizedCluster using Elbowmethod is: "+ optimizedcluster.ToString());

            return bbAll[optimizedcluster-1];
        }

        private static List<Tuple<CvPoint, CvPoint>> FindBoundingBoxesAll(

            List<Tuple<Point2d, Point2d>> bb,
            IplImage outTemp)
        {



            List<Tuple<CvPoint, CvPoint>> bbAll = new List<Tuple<CvPoint, CvPoint>>();

            int minx = outTemp.Width;
            int miny = outTemp.Height;
            int maxx = 0;
            int maxy = 0;
            foreach (Tuple<Point2d, Point2d> cit in bb)
            {
                miny = Math.Min(miny, cit.Item1.y);
                minx = Math.Min(minx, cit.Item1.x);
                maxy = Math.Max(maxy, cit.Item2.y);
                maxx = Math.Max(maxx, cit.Item2.x);

            }
            CvPoint p0 = new CvPoint(minx, miny);
            CvPoint p1 = new CvPoint(maxx, maxy);
            bbAll.Add(new Tuple<CvPoint, CvPoint>(p0, p1));

            return bbAll;
        }

        private static List<Tuple<CvPoint, CvPoint>> FindBoundingBoxesAll(List<Tuple<CvPoint, CvPoint>> bb, IplImage outTemp)
        {
            List<Tuple<CvPoint, CvPoint>> bbAll = new List<Tuple<CvPoint, CvPoint>>();

            //bbAll.Capacity = components.Count();
            int minx = outTemp.Width;
            int miny = outTemp.Height;
            int maxx = 0;
            int maxy = 0;
            foreach (Tuple<CvPoint, CvPoint> cit in bb)
            {
                miny = Math.Min(miny, cit.Item1.Y);
                minx = Math.Min(minx, cit.Item1.X);
                maxy = Math.Max(maxy, cit.Item2.Y);
                maxx = Math.Max(maxx, cit.Item2.X);
            }
            CvPoint p0 = new CvPoint(minx, miny);
            CvPoint p1 = new CvPoint(maxx, maxy);
            bbAll.Add(new Tuple<CvPoint, CvPoint>(p0, p1));

            return bbAll;
        }

        private static void renderChains(
            IplImage SWTImage,
            List<List<Point2d>> components,
            List<Chain> chains,
            IplImage output)
        {
            // keep track of included components
            List<bool> included = new List<bool>(components.Count());
            //included.reserve(components.size());
            for (int i = 0; i != components.Count(); i++)
            {
                included.Add(false);
            }
            foreach (Chain it in chains)
            {
                foreach (int cit in it.components)
                {
                    included[cit] = true;
                }
            }

            List<List<Point2d>> componentsRed = new List<List<Point2d>>();
            for (int i = 0; i != components.Count(); i++)
            {
                if (included[i])
                {
                    componentsRed.Add(components[i]);
                }

            }
            Debug.WriteLine("Component after chaining" + componentsRed.Count());
            //quan std::cout << componentsRed.size() << " components after chaining" << std::endl;
            //Mat outTemp(output.size(), CV_32FC1 );
            IplImage outTemp = Cv.CreateImage(output.GetSize(), BitDepth.F32, 1);
            RenderComponents(SWTImage, componentsRed, outTemp);
            //Cv.CvtColor(outTemp, output, ColorConversion.GrayToBgr);
            //outTemp.convertTo(output, CV_8UC1, 255);
            Cv.ConvertScale(outTemp, output, 1.0, 0);

        }
        /// <summary>
        /// This method is used to linking character candidate into chains
        /// Firstly, character candidate are linked into pair using the heights and widths of their bouding boxes
        /// </summary>
        /// <param name="colorImage"></param>
        /// <param name="components"></param>
        /// <param name="compCenters"></param>
        /// <param name="compMedians"></param>
        /// <param name="compDimensions"></param>
        /// <param name="compBB"></param>
        /// <returns></returns>
        private static List<Chain> makeChains(
           IplImage colorImage,
           List<List<Point2d>> components,
           List<Point2dFloat> compCenters,
           List<float> compMedians,
           List<Point2d> compDimensions,
           List<Tuple<Point2d, Point2d>> compBB)
        {
            unsafe
            {
                byte* img2 = (byte*)colorImage.ImageData.ToPointer();
                int srcWidthStep = colorImage.WidthStep;

                // make vector of color averages
                List<Point3dFloat> colorAverages = new List<Point3dFloat>(components.Count());
                foreach (List<Point2d> component in components)
                {
                    Point3dFloat mean;
                    mean.x = 0;
                    mean.y = 0;
                    mean.z = 0;
                    int num_points = 0;

                    foreach (Point2d pit in component)
                    {
                        mean.x += (float)img2[pit.y * srcWidthStep + pit.x * 3 + 0];
                        mean.y += (float)img2[pit.y * srcWidthStep + pit.x * 3 + 1];
                        mean.z += (float)img2[pit.y * srcWidthStep + pit.x * 3 + 2];
                        num_points++;
                    }
                    mean.x = mean.x / ((float)num_points);
                    mean.y = mean.y / ((float)num_points);
                    mean.z = mean.z / ((float)num_points);
                    colorAverages.Add(mean);
                }

                // Form all eligible pairs and calculate the direction of each
                // This step consider chain as single candidate pair
                List<Chain> chains = new List<Chain>();

                var index = 0;
                for (int i = 0; i < components.Count(); i++)
                {
                    //int j = i + 1
                    for (int j = i + 1 ; j < components.Count(); j++)
                    {
                        // This is metric properties
                        // Two letter candidates should  have similar similar stroke width --> ratio between ones has tp be less than 2.0
                        // Two letter candidate also are eligible for height ratio --> not exceed 2.0 due to difference between captcal and lower case letters
                        if ((compMedians[i] / compMedians[j] <= 2.0 || compMedians[j] / compMedians[i] <= 2.0) &&
                             (compDimensions[i].y / compDimensions[j].y <= 2.0 || compDimensions[j].y / compDimensions[i].y <= 2.0))
                        {
                            // The spacial distance between letters must not exceed three time the width of the longer one
                            float dist = (compCenters[i].x - compCenters[j].x) * (compCenters[i].x - compCenters[j].x) +
                                         (compCenters[i].y - compCenters[j].y) * (compCenters[i].y - compCenters[j].y);

                            // Color distance between two letters is equal to sum of square on three channels
                            float colorDist = (colorAverages[i].x - colorAverages[j].x) * (colorAverages[i].x - colorAverages[j].x) +
                                              (colorAverages[i].y - colorAverages[j].y) * (colorAverages[i].y - colorAverages[j].y) +
                                              (colorAverages[i].z - colorAverages[j].z) * (colorAverages[i].z - colorAverages[j].z);

                            // dieu kien ve geometric property
                            if (dist < 2.25 * (float)(Math.Max(Math.Min(compDimensions[i].x, compDimensions[i].y), Math.Min(compDimensions[j].x, compDimensions[j].y)))
                                * (float)(Math.Max(Math.Min(compDimensions[i].x, compDimensions[i].y), Math.Min(compDimensions[j].x, compDimensions[j].y)))
                              )// && colorDist < 1600)
                            {
                                Chain c = new Chain(); //c loop quan
                                c.p = i;
                                c.q = j;

                                List<int> comps = new List<int>(); //comps list loop
                                comps.Add(c.p);
                                comps.Add(c.q);
                                c.components = comps;
                                c.dist = dist;

                                float d_x = (compCenters[i].x - compCenters[j].x);
                                float d_y = (compCenters[i].y - compCenters[j].y);
                            
                                float mag = (float)Math.Sqrt(d_x * d_x + d_y * d_y);
                                d_x = d_x / mag;
                                d_y = d_y / mag;
                                Point2dFloat dir;
                                dir.x = d_x;
                                dir.y = d_y;
                                c.direction = dir;
                                chains.Add(c);

                                Debug.WriteLine("YES: Chain from: " + c.p + " to: " + c.q);
                                Debug.WriteLine("       compCenter: [" + compCenters[c.p].x + "," + compCenters[c.p].y + "] to: [" + compCenters[c.q].x + "," + compCenters[c.q].y + "]");
                            }
                            else
                            {
                                Debug.WriteLine("NO: Chain not sastify 1: " + index++);
                                Debug.WriteLine("NO: Chain from: " + i + " to: " + j);
                            }
                        }
                        else
                        {
                            Debug.WriteLine("NO: Chain not sastify 0: " + index++);
                        }

                    }

                }

                // After previous step we consider chain as single candidate pair
                // Now two chains can be mergered togethor if they share one end and have similar direction
                // We use tag "merge" for this process --> process finish when no chains can be merged

                Debug.WriteLine("The number of chains when start MERGER phase: " + chains.Count());

                ChainSortDistComparer chainsComp = new ChainSortDistComparer(); //quan them
                chains.Sort(chainsComp);

                const float strictness = (float)Math.PI / (float)6.0;


                #region merger lan1
                //merge chains
                int merges = 1; // this variable as tag
                while (merges > 0)
                {
                    // Quan check: ko can thiet
                    for (int i = 0; i < chains.Count(); i++)
                    {
                        Chain t1 = chains[i];
                        t1.merged = false;
                        chains[i] = t1;

                        Debug.WriteLine("Gan FALSE to merged: " + chains[i].merged);
                    }

                    merges = 0;
                    List<Chain> newchains = new List<Chain>(); //quan them

                    for (int i = 0; i < chains.Count(); i++)
                    {
                        for (int j = 0; j < chains.Count(); j++)
                        {
                            if (i != j)
                            {
                                if (!chains[i].merged && !chains[j].merged && sharesOneEnd(chains[i], chains[j]))
                                //if (!chains[i].merged && !chains[j].merged && sharesSameComponents(chains[i], chains[j]))
                                {
                                    //Consider the 4 caces of components
                                    if (chains[i].p == chains[j].p)
                                    {
                                        Debug.WriteLine("Nhay vao case 1");

                                        if (Math.Acos(chains[i].direction.x * -chains[j].direction.x + chains[i].direction.y * -chains[j].direction.y) < strictness)
                                        {
                                            Debug.WriteLine("Nhay vao SAU case 1");
                                            Debug.WriteLine("        " + i + " " + j + " " + chains[i].p + "," + chains[i].q + " " + chains[j].p + "," + chains[j].q);

                                            var t2 = chains[i];
                                            t2.p = chains[j].q;
                                            chains[i] = t2;

                                            foreach (int it in chains[j].components)
                                            {
                                                chains[i].components.Add(it);
                                            }

                                            float d_x = (compCenters[chains[i].p].x - compCenters[chains[i].q].x);
                                            float d_y = (compCenters[chains[i].p].y - compCenters[chains[i].q].y);

                                            var t3 = chains[i];
                                            t3.dist = d_x * d_x + d_y * d_y;
                                            chains[i] = t3;

                                            float mag = (float)Math.Sqrt(d_x * d_x + d_y * d_y);
                                            d_x = d_x / mag;
                                            d_y = d_y / mag;

                                            Point2dFloat dir;
                                            dir.x = d_x;
                                            dir.y = d_y;

                                            var t4 = chains[i];
                                            t4.direction = dir;
                                            chains[i] = t4;

                                            var t5 = chains[j];
                                            t5.merged = true;
                                            chains[j] = t5;

                                            merges++;

                                        }

                                    }
                                    else if (chains[i].p == chains[j].q)
                                    {
                                        Debug.WriteLine("Nhay vao case 2");

                                        if (Math.Acos(chains[i].direction.x * chains[j].direction.x + chains[i].direction.y * chains[j].direction.y) < strictness)
                                        {
                                            Debug.WriteLine("Nhay vao SAU case2");
                                            Debug.WriteLine("        " + i + " " + j + " " + chains[i].p + "," + chains[i].q + " " + chains[j].p + "," + chains[j].q);

                                            var t6 = chains[i];
                                            t6.p = chains[j].p;
                                            chains[i] = t6;

                                            foreach (int it in chains[j].components)
                                            {
                                                chains[i].components.Add(it);
                                            }

                                            float d_x = (compCenters[chains[i].p].x - compCenters[chains[i].q].x);
                                            float d_y = (compCenters[chains[i].p].y - compCenters[chains[i].q].y);
                                            float mag = (float)Math.Sqrt(d_x * d_x + d_y * d_y);


                                            var t7 = chains[i];
                                            t7.dist = d_x * d_x + d_y * d_y;
                                            chains[i] = t7;

                                            d_x = d_x / mag;
                                            d_y = d_y / mag;

                                            Point2dFloat dir = new Point2dFloat();
                                            dir.x = d_x;
                                            dir.y = d_y;

                                            var t8 = chains[i];
                                            t8.direction = dir;
                                            chains[i] = t8;

                                            var t9 = chains[j];
                                            t9.merged = true;
                                            chains[j] = t9;

                                            merges++;


                                        }

                                    }
                                    else if (chains[i].q == chains[j].p)
                                    {
                                        Debug.WriteLine("Nhay vao case 3");
                                        if (Math.Acos(chains[i].direction.x * chains[j].direction.x + chains[i].direction.y * chains[j].direction.y) < strictness)
                                        {
                                            Debug.WriteLine("Nhay vao SAU case 3");
                                            Debug.WriteLine("        " + i + " " + j + " " + chains[i].p + "," + chains[i].q + " " + chains[j].p + "," + chains[j].q);

                                            var t10 = chains[i];
                                            t10.q = chains[j].q;
                                            chains[i] = t10;

                                            foreach (int it in chains[j].components)
                                            {
                                                chains[i].components.Add(it);
                                            }

                                            float d_x = (compCenters[chains[i].p].x - compCenters[chains[i].q].x);
                                            float d_y = (compCenters[chains[i].p].y - compCenters[chains[i].q].y);
                                            float mag = (float)Math.Sqrt(d_x * d_x + d_y * d_y);

                                            var t11 = chains[i];
                                            t11.dist = d_x * d_x + d_y * d_y;
                                            chains[i] = t11;

                                            d_x = d_x / mag;
                                            d_y = d_y / mag;

                                            Point2dFloat dir;
                                            dir.x = d_x;
                                            dir.y = d_y;

                                            var t12 = chains[i];
                                            t12.direction = dir;
                                            chains[i] = t12;

                                            var t13 = chains[j];
                                            t13.merged = true;
                                            chains[j] = t13;

                                            merges++;
                                        }
                                    }
                                    else if (chains[i].q == chains[j].q)
                                    {
                                        Debug.WriteLine("Nhay vao case 4");

                                        if (Math.Acos(chains[i].direction.x * -chains[j].direction.x + chains[i].direction.y * -chains[j].direction.y) < strictness)
                                        {
                                            Debug.WriteLine("Nhay vao SAU case 1");
                                            Debug.WriteLine("        " + i + " " + j + " " + chains[i].p + "," + chains[i].q + " " + chains[j].p + "," + chains[j].q);

                                            var t14 = chains[i];
                                            t14.q = chains[j].p;
                                            chains[i] = t14;

                                            foreach (int it in chains[j].components)
                                            {
                                                chains[i].components.Add(it);
                                            }

                                            float d_x = (compCenters[chains[i].p].x - compCenters[chains[i].q].x);
                                            float d_y = (compCenters[chains[i].p].y - compCenters[chains[i].q].y);

                                            var t15 = chains[i];
                                            t15.dist = d_x * d_x + d_y * d_y;
                                            chains[i] = t15;

                                            float mag = (float)Math.Sqrt(d_x * d_x + d_y * d_y);
                                            d_x = d_x / mag;
                                            d_y = d_y / mag;

                                            Point2dFloat dir;
                                            dir.x = d_x;
                                            dir.y = d_y;

                                            var t16 = chains[i];
                                            t16.direction = dir;
                                            chains[i] = t16;

                                            var t17 = chains[j];
                                            t17.merged = true;
                                            chains[j] = t17;

                                            merges++;
                                        }

                                    }

                                }

                            }

                        }

                    }

                    //List<Chain> newchains = new List<Chain>();
                    for (int i = 0; i < chains.Count(); i++)
                    {
                        if (!chains[i].merged)
                        {
                            newchains.Add(chains[i]);
                        }
                    }
                    chains = newchains;
                    ChainSortLengthComparer chainSortLengthComp = new ChainSortLengthComparer();
                    chains.Sort(chainSortLengthComp);
                }
                #endregion

                #region lan 2 merger winner take all
                int merges2 = 1;
                while (merges2>0)
                {
                    for (int i = 0; i < chains.Count(); i++)
                    {
                        var t1 = chains[i];
                        t1.merged = false;
                        chains[i] = t1;
                    }

                    merges2 = 0;
                    List<Chain> newchains = new List<Chain>();

                    for (int i = 0; i < chains.Count(); i++)
                    {
                        for (int j = i+1; j < chains.Count(); j++)
                        {
                            if (!chains[i].merged && !chains[j].merged && sharesSameComponents(chains[i], chains[j]))
                            {

                                foreach (int it in chains[j].components)
                                {
                                    chains[i].components.Add(it);
                                }

                                var t5 = chains[j];
                                t5.merged = true;
                                chains[j] = t5;

                                merges2++;

                            }
                        }

                    }

                    for (int i = 0; i < chains.Count(); i++)
                    {
                        if (!chains[i].merged)
                        {
                            newchains.Add(chains[i]);
                        }
                    }
                    chains = newchains;

                }

                #endregion


                List<Chain> newchains2 = new List<Chain>(chains.Count());
                // Constrain of the number of components in one chain
                foreach (Chain cit in chains)
                {
                    if (cit.components.Count() >= 1)
                    {
                        newchains2.Add(cit);
                    }
                }

                chains = newchains2;
                Debug.WriteLine("Sau khi merger: chieu dai chain con lai: " + chains.Count().ToString());
                foreach (var item in chains)
                {
                    Debug.WriteLine("Diem trung tam: " + item.direction.x + "," + item.direction.y);
                }

                //Refine chain quan
                //List<Chain> newchains3 = new List<Chain>();
                //List<int> tempcomponets = new List<int>();

                //foreach (Chain cit in chains)
                //{
                //    if (!checkExistingComponents(tempcomponets, cit.components))
                //    {
                //        foreach (var item in cit.components)
                //        {
                //            tempcomponets.Add(item);
                //        }
                //        newchains3.Add(cit);
                //        Debug.WriteLine("Refine roi nhe");
                //    }
                //}
                //chains = newchains3;

                return chains;
            }
        }

        /// <summary>
        /// This method is used to verify the connection between 2 components
        /// </summary>
        /// <param name="c0">This is the fist component that contains text candidate</param>
        /// <param name="c1">This is the second component that contains text candidate</param>
        /// <returns>Return false or true</returns>
        private static bool sharesOneEnd(Chain c0, Chain c1)
        {
            if (c0.p == c1.p || c0.p == c1.q || c0.q == c1.q || c0.q == c1.p)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool sharesSameComponents(Chain c0, Chain c1)
        {
            foreach (var item1 in c0.components)
            {
                foreach (var item2 in c1.components)
                {
                    if (item1==item2)
                    {
                        return true;
                    }

                }

            }

            return false;
        }
        private static bool shareBoundingPoints(Tuple<Point2d, Point2d> bb0, Tuple<Point2d, Point2d> bb1)
        {
            var r0 = new CvRect(bb0.Item1.x, bb0.Item1.y, bb0.Item2.x - bb0.Item1.x, bb0.Item2.y - bb0.Item1.y);
            var r1 = new CvRect(bb1.Item1.x, bb1.Item1.y, bb1.Item2.x - bb1.Item1.x, bb1.Item2.y - bb1.Item1.y);

            if (CvRect.Intersect(r0,r1) != CvRect.Empty)
            {
                return true;
            }
            else { return false; }
        }
        /// <summary>
        /// This method is used to refine the chain to optimize chain
        /// Make final chain is not overlap on components that it is containing
        /// </summary>
        /// <param name="mother"></param>
        /// <param name="child"></param>
        /// <returns></returns>
        private static bool checkExistingComponents(List<int> mother, List<int> child)
        {
            var countsame = 0;
            foreach (var item in child)
            {
                if (mother.Contains(item))
                    countsame++;
            }
            if (countsame >= child.Count())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static KmeanOption checkrunningKmean(KmeanObservation inputs)
        {
            KmeanOption reKmeanOption = new KmeanOption();
            float mean = 0, variance = 0, median = 0;
            float min = 0; float max = 0;
            //get the statistical property of connected components
            List<List<double>> featureAll = new List<List<double>>();
            for (int i = 0; i < inputs.observationsList[0].observation.Length; i++)
            {
                List<double> _feature = new List<double>();
                foreach (var item in inputs.observationsList)
                {
                    _feature.Add(item.observation[i]);

                }

                //normalize
                //var minfeatur = _feature.Min();
                //var maxfeatur = _feature.Max();
                //List<double> _featureN = new List<double>();
                //foreach (var item in _feature)
                //{
                //    var temp = (item - minfeatur) / (maxfeatur - minfeatur);
                //    _featureN.Add(temp);
                //}

                //min = (float)_featureN.Min();
                //max = (float)_featureN.Max();
                generalStatsOnObservation(_feature, ref mean, ref variance, ref median);

                Debug.WriteLine("=============On Check running Kmean===========");
                Debug.WriteLine("Mean chain's Position check: " + mean);
                Debug.WriteLine("Variance chain's Position check: " + Math.Sqrt(variance));
                Debug.WriteLine("Median chain's Position check: " + median);

                //_feature.Add(min);
                //_feature.Add(max);

                _feature.Add(mean);
                _feature.Add(variance);
                _feature.Add(median);

                featureAll.Add(_feature);
            }

            bool check = true;
            for (int i = 0; i < inputs.observationsList[0].observation.Length; i++)
            {
                check = check && ((Math.Sqrt(featureAll[i][featureAll[i].Count() - 2]) >= 0.5 * (featureAll[i][featureAll[i].Count() - 3])));
            }
            #region xet dieu kien de chon feature
            //if ((Math.Sqrt(featureAll[0][featureAll[0].Count() - 2]) >= 0.5 * (featureAll[0][featureAll[0].Count() - 3])))
            //{
            //    reKmeanOption.posHorizontal = true;
            //}
            //else
            //{
            //    reKmeanOption.posHorizontal = false;
            //}

            //if ((Math.Sqrt(featureAll[1][featureAll[1].Count() - 2]) >= 0.5 * (featureAll[1][featureAll[1].Count() - 3])))
            //{
            //    reKmeanOption.posVertical = true;
            //}
            //else
            //{
            //    reKmeanOption.posVertical = false;
            //}

            //if ((Math.Sqrt(featureAll[1][featureAll[1].Count() - 2]) >= 0.5 * (featureAll[1][featureAll[1].Count() - 3]))
            //    && (Math.Sqrt(featureAll[0][featureAll[0].Count() - 2]) >= 0.5 * (featureAll[0][featureAll[0].Count() - 3])))
            //{
            //    reKmeanOption.posVertical = false;
            //    reKmeanOption.posHorizontal = false;
            //    reKmeanOption.pos2Dimesions = true;

            //}
            #endregion
            //if ((Math.Sqrt(featureAll[0][featureAll[0].Count()-2]) >= 0.5 * (featureAll[0][featureAll[0].Count() - 3])) 
            //   || (Math.Sqrt(featureAll[1][featureAll[1].Count() - 2]) >= 0.5 * (featureAll[1][featureAll[1].Count() - 3])
            //   || (Math.Sqrt(featureAll[1][featureAll[1].Count() - 2]) > 0)
            //   || (Math.Sqrt(featureAll[0][featureAll[0].Count() - 2]) > 0)))
            //if (check)
            //{
            //    return true;

            //}

            //else
            //{
            //    return false;
            //}
            return reKmeanOption;
            
           
        }

        /// <summary>
        /// This method is used to draw all components that contains text candidate with color
        /// </summary>
        /// <param name="SWTImage">This is the input SWT image</param>
        /// <param name="components">This is components</param>
        /// <param name="compBB">This is bouding box of components</param>
        /// <param name="output">Th eoutput image</param>
        public static void RenderComponentsWithBoxes(
            IplImage SWTImage,
            List<List<Point2d>> components,
            List<Tuple<Point2d, Point2d>> compBB,
            IplImage output)
        {
            IplImage outTemp = Cv.CreateImage(output.GetSize(), BitDepth.F32, 1);
            RenderComponents(SWTImage, components, outTemp);
            List<Tuple<CvPoint, CvPoint>> bb = new List<Tuple<CvPoint, CvPoint>>(compBB.Count());

            foreach (Tuple<Point2d, Point2d> it in compBB)
            {
                CvPoint p0 = new CvPoint(it.Item1.x, it.Item1.y);
                CvPoint p1 = new CvPoint(it.Item2.x, it.Item2.y);
                Tuple<CvPoint, CvPoint> pair = new Tuple<CvPoint, CvPoint>(p0, p1);
                bb.Add(pair);
            }

            IplImage outImg = Cv.CreateImage(output.GetSize(), BitDepth.U8, 1);

            Cv.Convert(outTemp, outImg);
            Cv.CvtColor(outImg, output, ColorConversion.GrayToBgr);

            int count = 0;
            foreach (Tuple<CvPoint, CvPoint> it in bb)
            {
                CvScalar c;
                if (count % 3 == 0) c = new CvScalar(255, 0, 0);
                else if (count % 3 == 1) c = new CvScalar(0, 255, 0);
                else c = new CvScalar(0, 0, 255);
                count++;
                Cv.Rectangle(output, it.Item1, it.Item2, c, 1);
            }

        }

       

        public static void RenderComponentsWithBoxes2(
            IplImage SWTImage,
            List<Point2dFloat> compCenters,
            List<List<Point2d>> components,
            List<Tuple<Point2d, Point2d>> compBB,
            IplImage output)
        {
            IplImage outTemp = Cv.CreateImage(output.GetSize(), BitDepth.F32, 1);
            RenderComponents(SWTImage, components, outTemp);
            List<Tuple<CvPoint, CvPoint>> bb = new List<Tuple<CvPoint, CvPoint>>(compBB.Count());

            foreach (Tuple<Point2d, Point2d> it in compBB)
            {
                CvPoint p0 = new CvPoint(it.Item1.x, it.Item1.y);
                CvPoint p1 = new CvPoint(it.Item2.x, it.Item2.y);
                Tuple<CvPoint, CvPoint> pair = new Tuple<CvPoint, CvPoint>(p0, p1);
                bb.Add(pair);
            }

            IplImage outImg = Cv.CreateImage(output.GetSize(), BitDepth.U8, 1);

            Cv.Convert(outTemp, outImg);
            Cv.CvtColor(outImg, output, ColorConversion.GrayToBgr);

            int count = 0;
            foreach (Tuple<CvPoint, CvPoint> it in bb)
            {
                CvScalar c;
                if (count % 3 == 0) c = new CvScalar(255, 0, 0);
                else if (count % 3 == 1) c = new CvScalar(0, 255, 0);
                else c = new CvScalar(0, 0, 255);
                count++;
                Cv.Rectangle(output, it.Item1, it.Item2, c, 1);
            }

            // render bouding box after k-mean cluster
            List<Tuple<CvPoint, CvPoint>> k_center = new List<Tuple<CvPoint, CvPoint>>(compBB.Count());
            k_center = FindBoundingBoxesAll2(compCenters, compBB, output);
            int count2 = 0;
            foreach (Tuple<CvPoint, CvPoint> it in k_center)
            {
                CvScalar c;
                if (count2 % 3 == 0) c = new CvScalar(255, 0, 0);
                else if (count2 % 3 == 1) c = new CvScalar(0, 255, 0);
                else c = new CvScalar(0, 0, 255);
                count2++;

                Cv.Rectangle(output, it.Item1, it.Item2, c, 4);
            }

        }

        public static void RenderComponentsWithBoxesAll(
            IplImage SWTImage,
            List<List<Point2d>> components,
            List<Tuple<Point2d, Point2d>> compBB,
            IplImage output)
        {
            IplImage outTemp = Cv.CreateImage(output.GetSize(), BitDepth.F32, 1);
            RenderComponents(SWTImage, components, outTemp);
            List<Tuple<CvPoint, CvPoint>> bb = new List<Tuple<CvPoint, CvPoint>>(compBB.Count());

            foreach (Tuple<Point2d, Point2d> it in compBB)
            {
                CvPoint p0 = new CvPoint(it.Item1.x, it.Item1.y);
                CvPoint p1 = new CvPoint(it.Item2.x, it.Item2.y);
                Tuple<CvPoint, CvPoint> pair = new Tuple<CvPoint, CvPoint>(p0, p1);
                bb.Add(pair);
            }

            IplImage outImg = Cv.CreateImage(output.GetSize(), BitDepth.U8, 1);

            Cv.Convert(outTemp, outImg);
            Cv.CvtColor(outImg, output, ColorConversion.GrayToBgr);

            int count = 0;
            foreach (Tuple<CvPoint, CvPoint> it in bb)
            {
                CvScalar c;
                if (count % 3 == 0) c = new CvScalar(255, 0, 0);
                else if (count % 3 == 1) c = new CvScalar(0, 255, 0);
                else c = new CvScalar(0, 0, 255);
                count++;
                Cv.Rectangle(output, it.Item1, it.Item2, c, 2);
            }


        }

        public static void RenderComponents(
            IplImage SWTImage,
            List<List<Point2d>> components,
            IplImage output)
        {
            Cv.Zero(output);
            unsafe
            {
                float* swtPtr = (float*)SWTImage.ImageData.ToPointer();
                int swtWidthStep = SWTImage.WidthStep / 4;

                float* outPtr = (float*)output.ImageData.ToPointer();
                int outWidthStep = output.WidthStep / 4;

                foreach (List<Point2d> it in components)
                {
                    foreach (Point2d pixel in it)
                    {
                        outPtr[pixel.y * outWidthStep + pixel.x] = swtPtr[pixel.y * swtWidthStep + pixel.x];
                    }
                }

                for (int row = 0; row < output.Height; row++)
                {
                    for (int col = 0; col < output.Width; col++)
                    {
                        if (outPtr[row * outWidthStep + col] == 0)
                        {
                            outPtr[row * outWidthStep + col] = -1;
                        }
                    }
                }
                double maxVal;
                double minVal;

                Cv.MinMaxLoc(output, out minVal, out maxVal);

                float minFloat = (float)minVal;
                float maxFloat = (float)maxVal;

                float difference = (maxFloat - minFloat);
                for (int row = 0; row < output.Height; row++)
                {
                    for (int col = 0; col < output.Width; col++)
                    {
                        if (outPtr[row * outWidthStep + col] < 1)
                        {
                            outPtr[row * outWidthStep + col] = 1;
                        }
                        else
                        {
                            outPtr[row * outWidthStep + col] = ((outPtr[row * outWidthStep + col]) - minFloat) / difference * 255;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// This method is used to analysis components from previous step. Identify and eliminate the connected components that are
        /// unlikely part of text. To this end, we devise a two-layer filtering mechanism
        /// Layer 1: Set of heuristic rule runs on a collection of statistical and geometrical property of components. True text components
        /// usually have similar stroke width and compact structure (not too thin or too long)
        /// Layer 2: We should develop one classifier here
        /// </summary>
        /// <param name="SWTImage"></param>
        /// <param name="components"></param>
        /// <param name="validComponents"></param>
        /// <param name="compCenters"></param>
        /// <param name="compMedians"></param>
        /// <param name="compDimensions"></param>
        /// <param name="compBB"></param>
        public static void FilterComponents(IplImage SWTImage,
                                            List<List<Point2d>> components,
                                            ref List<List<Point2d>> validComponents,
                                            ref List<Point2dFloat> compCenters,
                                            ref List<float> compMedians,
                                            ref List<Point2d> compDimensions,
                                            ref List<Tuple<Point2d, Point2d>> compBB)
        {
            validComponents = new List<List<Point2d>>(components.Count());
            compCenters = new List<Point2dFloat>(components.Count());
            compMedians = new List<float>(components.Count());
            compDimensions = new List<Point2d>(components.Count());
            // bounding box
            compBB = new List<Tuple<Point2d, Point2d>>(components.Count());

            foreach (List<Point2d> component in components)
            {
                if (component.Count() > 0)
                {
                    //initialize mean, variance, median, mim, max values
                    float mean = 0, variance = 0, median = 0;
                    int minx = 0, miny = 0, maxx = 0, maxy = 0;
                    //get the statistical property of connected components
                    componentStats(SWTImage, component, ref mean, ref variance, ref median, ref minx, ref miny, ref maxx, ref maxy);
                    //check if variance is less than half the mean
                    //if (Math.Sqrt(variance) > 0.5 * mean)
                    //{
                    //    continue;
                    //}

                    float width = (float)(maxx - minx + 1);
                    float height = (float)(maxy - miny + 1);

                    // check font height too big (normal characters are 80 pixels high, for acA1300)
                    if (height > 300)
                    {
                        continue;
                    }

                    // check font height too small //quan 50
                    if (height < 10)
                    {
                        continue;
                    }

                    float area = width * height;
                    float rminx = (float)minx;
                    float rmaxx = (float)maxx;
                    float rminy = (float)miny;
                    float rmaxy = (float)maxy;
                    // compute the rotated bounding box
                    float increment = 1.0f / 36.0f;

                    for (float theta = increment * (float)Math.PI; theta < Math.PI / 2.0f; theta += increment * (float)Math.PI)
                    {
                        float xmin, xmax, ymin, ymax, xtemp, ytemp, ltemp, wtemp;
                        xmin = 1000000;
                        ymin = 1000000;
                        xmax = 0;
                        ymax = 0;
                        for (int i = 0; i < component.Count(); i++)
                        {
                            xtemp = (component)[i].x * (float)Math.Cos(theta) + (component)[i].y * -(float)Math.Sin(theta);
                            ytemp = (component)[i].x * (float)Math.Sin(theta) + (component)[i].y * (float)Math.Cos(theta);
                            xmin = Math.Min(xtemp, xmin);
                            xmax = Math.Max(xtemp, xmax);
                            ymin = Math.Min(ytemp, ymin);
                            ymax = Math.Max(ytemp, ymax);
                        }
                        ltemp = xmax - xmin + 1;
                        wtemp = ymax - ymin + 1;
                        if (ltemp * wtemp < area)
                        {
                            area = ltemp * wtemp;
                            width = ltemp;
                            height = wtemp;
                        }
                    }

                    // check if the aspect ratio is between 1/10 and 10
                    if (width / height < 1.0f / 10.0f || width / height > 10.0)
                    {
                        continue;
                        //break;
                    }

                    // compute the diameter TODO finish
                    // compute dense representation of component
                    List<List<float>> denseRepr = new List<List<float>>(maxx - minx + 1);
                    for (int i = 0; i < maxx - minx + 1; i++)
                    {
                        List<float> tmp = new List<float>(maxy - miny + 1);
                        denseRepr.Add(tmp);
                        for (int j = 0; j < maxy - miny + 1; j++)
                        {
                            denseRepr[i].Add(0);
                        }
                    }
                    foreach (Point2d pit in component)
                    {
                        (denseRepr[pit.x - minx])[pit.y - miny] = 1;
                    }
                    // create graph representing components
                    int num_nodes = component.Count();
                   
                    Point2dFloat center;
                    center.x = ((float)(maxx + minx)) / 2.0f;
                    center.y = ((float)(maxy + miny)) / 2.0f;

                    Point2d dimensions = new Point2d();
                    dimensions.x = maxx - minx + 1;
                    dimensions.y = maxy - miny + 1;

                    Point2d bb1 = new Point2d();
                    bb1.x = minx;
                    bb1.y = miny;

                    Point2d bb2 = new Point2d();
                    bb2.x = maxx;
                    bb2.y = maxy;
                    Tuple<Point2d, Point2d> pair = new Tuple<Point2d, Point2d>(bb1, bb2);

                    compBB.Add(pair);
                    compDimensions.Add(dimensions);
                    compMedians.Add(median);
                    compCenters.Add(center);
                    validComponents.Add(component);
                }
            }

            List<List<Point2d>> tempComp = new List<List<Point2d>>(validComponents.Count());
            List<Point2d> tempDim = new List<Point2d>(validComponents.Count());
            List<float> tempMed = new List<float>(validComponents.Count());
            List<Point2dFloat> tempCenters = new List<Point2dFloat>((validComponents.Count()));
            List<Tuple<Point2d, Point2d>> tempBB = new List<Tuple<Point2d, Point2d>>(validComponents.Count());

            for (int i = 0; i < validComponents.Count(); i++)
            {
                int count = 0;
                for (int j = 0; j < validComponents.Count(); j++)
                {
                    if (i != j)
                    {
                        // component center of component is inside another component
                        if (compBB[i].Item1.x <= compCenters[j].x && compBB[i].Item2.x >= compCenters[j].x &&
                            compBB[i].Item1.y <= compCenters[j].y && compBB[i].Item2.y >= compCenters[j].y)
                        {
                            count++;
                        }
                    }
                }
                if (count < 2)
                {// component is unique
                    tempComp.Add(validComponents[i]);
                    tempCenters.Add(compCenters[i]);
                    tempMed.Add(compMedians[i]);
                    tempDim.Add(compDimensions[i]);
                    tempBB.Add(compBB[i]);
                }
            }

            validComponents = tempComp;
            compDimensions = tempDim;
            compMedians = tempMed;
            compCenters = tempCenters;
            compBB = tempBB;

        }

        /// <summary>
        /// This method is used to calculate the properties of components including mean, vriance, median, and min max on x and y axis
        /// </summary>
        /// <param name="SWTImage">This is input SWT image</param>
        /// <param name="component">This is connected components</param>
        /// <param name="mean">Mean property</param>
        /// <param name="variance">Variance property</param>
        /// <param name="median">Meidian value</param>
        /// <param name="minx">minx value</param>
        /// <param name="miny">miny value</param>
        /// <param name="maxx">max x value</param>
        /// <param name="maxy">max y value</param>
        public static void componentStats(IplImage SWTImage,
                                        List<Point2d> component,
                                        ref float mean, 
                                        ref float variance, 
                                        ref float median,
                                        ref int minx, 
                                        ref int miny, 
                                        ref int maxx, 
                                        ref int maxy)
        {
            unsafe
            {
                float* swtPtr = (float*)SWTImage.ImageData.ToPointer();
                int swtWidthStep = SWTImage.WidthStep / 4;

                List<float> temp = new List<float>(component.Count());
                mean = 0;
                double varDouble = 0;
                variance = 0;
                minx = 1000000;
                miny = 1000000;
                maxx = 0;
                maxy = 0;

                foreach (Point2d it in component)
                {
                    float t = swtPtr[it.y * swtWidthStep + it.x];
                    if (t > 0)
                    {
                        mean += t;
                        temp.Add(t);
                    }
                    miny = Math.Min(miny, it.y);
                    minx = Math.Min(minx, it.x);
                    maxy = Math.Max(maxy, it.y);
                    maxx = Math.Max(maxx, it.x);

                }
                mean = mean / ((float)temp.Count());

                foreach (float it in temp)
                {

                    varDouble += (double)(it - mean) * (double)(it - mean);
                }
                variance = (float)varDouble / ((float)temp.Count());

                temp.Sort();
                median = temp[temp.Count() / 2];
            }
        }
        /// <summary>
        /// This method is used to calculate property of mean, variance and median on componet's SWT
        /// </summary>
        /// <param name="compMedians"></param>
        /// <param name="mean"></param>
        /// <param name="variance"></param>
        /// <param name="median"></param>
        public static void componentStatsOnSWT(
            List<float> compMedians,
            ref float mean,
            ref float variance,
            ref float median)
        {
            List<float> temp = new List<float>(compMedians.Count());
            mean = 0;
            double varDouble = 0;
            variance = 0;

            foreach (float it in compMedians)
            {
                mean += it;
                temp.Add(it);
            }
            
            mean = mean / ((float)temp.Count());

            foreach (float it in temp)
            {
                varDouble += (double)(it - mean) * (double)(it - mean);
            }
            variance = (float)varDouble / ((float)temp.Count());

            temp.Sort();
            median = temp[temp.Count() / 2];

        }
        /// <summary>
        /// This method is used to calculate property of mean, variance and mean of component's dimension
        /// </summary>
        /// <param name="compDimensions"></param>
        /// <param name="mean"></param>
        /// <param name="variance"></param>
        /// <param name="median"></param>
        public static void componentStatsOnDimension(
            List<Point2d> compDimensions,
            ref List<float> mean,
            ref List<float> variance,
            ref List<float> median)
        {
            List<float> tempX = new List<float>(compDimensions.Count());
            List<float> tempY = new List<float>(compDimensions.Count());
            float meanX = 0; float meanY = 0;
            double varDoubleX = 0;
            double varDoubleY = 0;
            

            foreach (var it in compDimensions)
            {
                meanX += it.x;
                meanY += it.y;
                tempX.Add(it.x);
                tempY.Add(it.y);
            }
            
            meanX = meanX/ ((float)tempX.Count());
            mean.Add(meanX);
            meanY = meanY / ((float)tempY.Count());
            mean.Add(meanY);

            foreach (var it in tempX)
            {
                varDoubleX += (double)(it - mean[0]) * (double)(it - mean[0]);
            }

            foreach (var it in tempY)
            {
                varDoubleY += (double)(it - mean[1]) * (double)(it - mean[1]);
            }

            variance.Add((float)varDoubleX / ((float)tempX.Count()));
            variance.Add((float)varDoubleY / ((float)tempY.Count()));

            tempX.Sort(); tempY.Sort();
            median.Add(tempX[tempX.Count() / 2]);
            median.Add(tempY[tempY.Count() / 2]);
        }
        /// <summary>
        /// This method is used to caluculate properties of mean, variance and median of the component position
        /// </summary>
        /// <param name="compCenters"></param>
        /// <param name="mean"></param>
        /// <param name="variance"></param>
        /// <param name="median"></param>
        public static void componentStatsOnPosition(
            List<Point2dFloat> compCenters,
            ref List<float> mean,
            ref List<float> variance,
            ref List<float> median
            )
        {
            List<float> tempX = new List<float>(compCenters.Count());
            List<float> tempY = new List<float>(compCenters.Count());
            float meanX = 0; float meanY = 0;
            double varDoubleX = 0;
            double varDoubleY = 0;


            foreach (var it in compCenters)
            {
                meanX += it.x;
                meanY += it.y;
                tempX.Add(it.x);
                tempY.Add(it.y);
            }

            meanX = meanX / ((float)tempX.Count());
            mean.Add(meanX);
            meanY = meanY / ((float)tempY.Count());
            mean.Add(meanY);

            foreach (var it in tempX)
            {
                varDoubleX += (double)(it - mean[0]) * (double)(it - mean[0]);
            }

            foreach (var it in tempY)
            {
                varDoubleY += (double)(it - mean[1]) * (double)(it - mean[1]);
            }

            variance.Add((float)varDoubleX / ((float)tempX.Count()));
            variance.Add((float)varDoubleY / ((float)tempY.Count()));

            tempX.Sort(); tempY.Sort();
            median.Add(tempX[tempX.Count() / 2]);
            median.Add(tempY[tempY.Count() / 2]);
        }
        /// <summary>
        /// This method is used to calculate properties of mean, variance and median of the list of double
        /// </summary>
        /// <param name="observations">the list of input double</param>
        /// <param name="mean">mean</param>
        /// <param name="variance">variance</param>
        /// <param name="median">median</param>
        public static void generalStatsOnObservation(
            List<double> observations,
            ref float mean,
            ref float variance,
            ref float median
            )
        {
            List<float> temp = new List<float>(observations.Count());
            mean = 0;
            double varDouble = 0;
            variance = 0;

            foreach (float it in observations)
            {
                mean += it;
                temp.Add(it);
            }

            mean = mean / ((float)temp.Count());

            foreach (float it in temp)
            {
                varDouble += (double)(it - mean) * (double)(it - mean);
            }
            variance = (float)varDouble / ((float)temp.Count());

            temp.Sort();
            median = temp[temp.Count() / 2];

        }

        /// <summary>
        /// This method is used to clean the SWT image one more time using Rays
        /// </summary>
        /// <param name="SWTImage">This is the SWT input image</param>
        /// <param name="rays">The rays from Stroke Width Transform operator </param>
        /// <param name="cleanSWTImage">This is output  clean SWT image</param>
        public static void FilterRays(IplImage SWTImage, List<Ray> rays, IplImage cleanSWTImage)
        {
            // get stats on rays
            Rays raySet = new Rays(rays);
            raySet.SetValid(true);

            cleanSWTImage.Set(-1);
            unsafe
            {
                float* swtPtr = (float*)SWTImage.ImageData.ToPointer();
                int swtWidthStep = SWTImage.WidthStep / 4;
                float* cleanSwtPtr = (float*)cleanSWTImage.ImageData.ToPointer();
                int cleanSwtWidthStep = SWTImage.WidthStep / 4;
                // filter rays that are not right length
                for (int i = 0; i < raySet.rayLengths.Count(); i++)
                {
                    if (raySet.rayLengths[i] < raySet.median - 0.5 * raySet.stdev || raySet.rayLengths[i] > raySet.median + 0.5 * raySet.stdev)
                    {
                        raySet.rays[i].valid = false;
                    }
                }

                for (int i = 0; i < raySet.rayLengths.Count(); i++)
                {
                    if (raySet.rays[i].valid)
                    {
                        List<Point2d> points = raySet.rays[i].points;
                        for (int j = 0; j < points.Count(); j++)
                            cleanSwtPtr[points[j].y * swtWidthStep + points[j].x] = swtPtr[points[j].y * swtWidthStep + points[j].x];

                    }
                }

            }

        }

        static List<List<Point2d>> FindLegallyConnectedComponentsRAY(IplImage SWTImage, List<Ray> rays)
        {
            Dictionary<int, int> map = new Dictionary<int, int>();
            Dictionary<int, Point2d> revMap = new Dictionary<int, Point2d>();

            return null;
        }


        /// <summary>
        /// This method is used to find the connected components
        /// Two neighboring pixels may be grouped togethor if they have similar stroke width
        /// This based on the classical Connected Components algorithm that using Graph Theory
        /// The ratio in SWT less than 3.0 is considered as similar
        /// </summary>
        /// <param name="SWTImage">The input SWT image</param>
        /// <param name="rays">The input rays</param>
        /// <returns></returns>
        public static List<List<Point2d>> FindLegallyConnectedComponents(
            IplImage SWTImage, 
            List<Ray> rays)
        {
            Dictionary<int, int> map = new Dictionary<int, int>();
            Dictionary<int, Point2d> revMap = new Dictionary<int, Point2d>();

            int numVertices = 0;
            unsafe
            {
                // adding each point to map of points and an index
                float* swtPtr = (float*)SWTImage.ImageData.ToPointer();
                int SWTWidthStep = SWTImage.WidthStep / 4;

                for (int row = 0; row < SWTImage.Height; row++)
                {
                    for (int col = 0; col < SWTImage.Width; col++)
                    {
                        if (swtPtr[row * SWTWidthStep + col] > 0)
                        {
                            map.Add(row * SWTImage.Width + col, numVertices);
                            Point2d p = new Point2d();
                            p.x = col;
                            p.y = row;
                            revMap.Add(numVertices, p);
                            numVertices++;
                        }
                    }
                }


                Graph graph = new Graph();// key is vertex, list of connected vertices, vertex value is current set vertex value, visited?

                // why connected only right, down, downright, downleft
                for (int row = 0; row < SWTImage.Height; row++)
                {
                    for (int col = 0; col < SWTImage.Width; col++)
                    {
                        float value = swtPtr[row * SWTWidthStep + col];
                        if (value > 0)
                        {
                            // check pixel to the right, right-down, down, left-down
                            int this_pixel = map[row * SWTImage.Width + col];
                            graph.Add(this_pixel);

                            if (row - 1 >= 0)
                            {// up
                                if (col + 1 < SWTImage.Width)
                                {// right up
                                    float right_up = swtPtr[(row - 1) * SWTWidthStep + col + 1];
                                    if (right_up > 0 && ((value) / right_up <= 3.0 || right_up / (value) <= 3.0))
                                    {
                                        graph.Add(this_pixel, map[(row - 1) * SWTImage.Width + col + 1]);
                                    }
                                }
                                float up = swtPtr[(row - 1) * SWTWidthStep + col];
                                if (up > 0 && ((value) / up <= 3.0 || up / (value) <= 3.0))
                                {
                                    graph.Add(this_pixel, map[(row - 1) * SWTImage.Width + col]);
                                }
                                if (col - 1 >= 0)
                                {// up left
                                    float left_up = swtPtr[(row - 1) * SWTWidthStep + col - 1];
                                    if (left_up > 0 && ((value) / left_up <= 3.0 || left_up / (value) <= 3.0))
                                    {
                                        graph.Add(this_pixel, map[(row - 1) * SWTImage.Width + col - 1]);
                                    }
                                }
                            }
                            // middle
                            if (col + 1 < SWTImage.Width)
                            {//right
                                float right = swtPtr[row * SWTWidthStep + col + 1];
                                if (right > 0 && ((value) / right <= 3.0 || right / (value) <= 3.0))
                                {
                                    graph.Add(this_pixel, map[row * SWTImage.Width + col + 1]);
                                }
                            }
                            if (col - 1 >= 0)
                            {//left
                                float left = swtPtr[row * SWTWidthStep + col - 1];
                                if (left > 0 && ((value) / left <= 3.0 || left / (value) <= 3.0))
                                {
                                    graph.Add(this_pixel, map[row * SWTImage.Width + col - 1]);
                                }
                            }
                            // down
                            if (row + 1 < SWTImage.Height)
                            {
                                if (col + 1 < SWTImage.Width)
                                {//right down
                                    float right_down = swtPtr[(row + 1) * SWTWidthStep + col + 1];
                                    if (right_down > 0 && ((value) / right_down <= 3.0 || right_down / (value) <= 3.0))
                                    {
                                        graph.Add(this_pixel, map[(row + 1) * SWTImage.Width + col + 1]);
                                    }
                                }
                                float down = swtPtr[(row + 1) * SWTWidthStep + col];
                                if (down > 0 && ((value) / down <= 3.0 || down / (value) <= 3.0))
                                {//down
                                    graph.Add(this_pixel, map[(row + 1) * SWTImage.Width + col]);
                                }
                                if (col - 1 >= 0)
                                {// left down
                                    float left_down = swtPtr[(row + 1) * SWTWidthStep + col - 1];
                                    if (left_down > 0 && ((value) / left_down <= 3.0 || left_down / (value) <= 3.0))
                                    {
                                        graph.Add(this_pixel, map[(row + 1) * SWTImage.Width + col - 1]);
                                    }
                                }
                            }
                        }
                    }
                }

                 
                List<int> c = new List<int>();
                int numComp = connectedComponentsBFS(graph, ref c);

                List<List<Point2d>> components = new List<List<Point2d>>(numComp);
                components.Add(new List<Point2d>());
                for (int j = 0; j < numComp; j++)
                {
                    components.Add(new List<Point2d>());
                }

                for (int j = 0; j < numVertices; j++)
                {
                    Point2d p = revMap[j];
                    components[c[j]].Add(p);
                }
                return components;
            }
        }

        public static int connectedComponents(Graph graph, ref List<int> components)
        {// key is vertex, list of connected vertices, vertex value is current set vertex value, visited?

            // reset graph visited
            graph.ResetVisited();

            // do until all components of graphs have been visited
            GraphNode graphNode = graph.NextUnvisited();
            int vertexNumber = 1;
            while (graphNode != null)
            {
                graphNode.visited = true;

                DepthFirstSearch(ref graph, graphNode.vertexValue, vertexNumber);
                graphNode.vertexValue = vertexNumber;
                vertexNumber++;
                graphNode = graph.NextUnvisited();
            }

            // as many components as vertices --> component contains the components that the edge point belongs to
            components = new List<int>();
            foreach (KeyValuePair<int, GraphNode> graphnode in graph)
            {
                components.Add(graphnode.Value.vertexValue);
            }

            // return the number of different components
            return vertexNumber - 1;
        }
        /// <summary>
        /// This method is used to find connected components using Breadth First Search algorithm
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        public static int connectedComponentsBFS(Graph graph, ref List<int> components)
        {// key is vertex, list of connected vertices, vertex value is current set vertex value, visited?

            // reset graph visited
            graph.ResetVisited();

            // do until all components of graphs have been visited
            GraphNode graphNode = graph.NextUnvisited();
            int vertexNumber = 1;
            while (graphNode != null)
            {
                graphNode.visited = true;

                BreadthFirstSearch(ref graph, graphNode.vertexValue, vertexNumber);
                graphNode.vertexValue = vertexNumber;
                vertexNumber++;
                graphNode = graph.NextUnvisited();
            }

            // as many components as vertices --> component contains the components that the edge point belongs to
            components = new List<int>();
            foreach (KeyValuePair<int, GraphNode> graphnode in graph)
            {
                components.Add(graphnode.Value.vertexValue);
            }

            // return the number of different components
            return vertexNumber - 1;
        }

        public static void BreadthFirstSearch(ref Graph graph, int key, int vertexNumber)
        {
            Stack<GraphNode> connComp = new Stack<GraphNode>();
            connComp.Push(graph[key]);

            while (connComp.Count() > 0)
            {
                GraphNode gn = connComp.Pop();
                gn.visited = true;
                gn.vertexValue = vertexNumber;
                for (int i = 0; i < gn.adjacency.Count(); i++)
                {
                    if (graph[gn.adjacency[i]].visited == false)
                    {
                        connComp.Push(graph[gn.adjacency[i]]);
                    }
                }
            }

        }

        public static void DepthFirstSearch(ref Graph graph, int key, int vertexNumber)
        {
            if (vertexNumber == 2 && key == 10667)
            {

            }
            List<int> nodes = graph[key].adjacency;
            for (int i = 0; i < nodes.Count(); i++)
            {
                if (graph[nodes[i]].visited == false)
                {
                    graph[nodes[i]].vertexValue = vertexNumber;
                    graph[nodes[i]].visited = true;
                    DepthFirstSearch(ref graph, nodes[i], vertexNumber);
                }
            }


        }
        /// <summary>
        /// This method is used to filter SWT image using median filter
        /// Step1: Sorting
        /// Step2: Select the median value
        /// </summary>
        /// <param name="SWTImage"></param>
        /// <param name="rays"></param>
        public static void SWTMedianFilter(
            IplImage SWTImage,
            List<Ray> rays)
        {
            unsafe
            {
                float* swtPtr = (float*)SWTImage.ImageData.ToPointer();
                int SWTWidthStep = SWTImage.WidthStep / 4;

                Point2dComparer p2dComp = new Point2dComparer();

                foreach (Ray ray in rays)
                {
                    for (int i = 0; i < ray.points.Count(); i++)
                    {
                        Point2d pt = ray.points[i];
                        //Assign the SWT from image to point in rays
                        pt.SWT = swtPtr[pt.y * SWTWidthStep + pt.x];
                        ray.points[i] = pt;
                    }


                    ray.points.Sort(p2dComp);

                    float median = ray.points[ray.points.Count() / 2].SWT;
                    foreach (Point2d point in ray.points)
                    {
                        //Filtering occur here
                        swtPtr[point.y * SWTWidthStep + point.x] = Math.Min(point.SWT, median);
                        //swtPtr[point.y * SWTWidthStep + point.x] = median;
                    }
                }
            }

        }
        /// <summary>
        /// This method is used to fed the edge image to the SWT module to product the SWT image
        /// SWT is an image operator which computes per pixel width of the the most likely stroke containing the pixels
        /// This method is to discover connected components from edge map directly.
        /// Step1: Edge detection using Canny method from the input original image
        /// Step2: Stroked Width Transform from the edge map image
        /// </summary>
        /// <param name="edgeImage">This is the edge image</param>
        /// <param name="gradientX">This is the gradient image in x axis</param>
        /// <param name="gradientY">This is the gradient image in y axis</param>
        /// <param name="darkOnLight">Text is darker than background</param>
        /// <param name="SWTImage">This is the output SWT image</param>
        /// <param name="rays">This is rays</param>
        public static void StrokeWidthTransform(
            IplImage edgeImage,
            IplImage gradientX,
            IplImage gradientY,
            bool darkOnLight,
            IplImage SWTImage,
            List<Ray> rays)
        {
            unsafe// looks safe
            {
                float prec = 0.05f;
                byte* img1 = (byte*)edgeImage.ImageData.ToPointer();//ImageData la contro toi hang dau tien cua du lieu anh
                int srcWidthStep = edgeImage.WidthStep;//so byte trong mot hang anh

                float* gradX = (float*)gradientX.ImageData.ToPointer();
                int gradXWidthStep = gradientX.WidthStep / 4;

                float* gradY = (float*)gradientY.ImageData.ToPointer();
                int gradYWidthStep = gradientY.WidthStep / 4;

                float* swtPtr = (float*)SWTImage.ImageData.ToPointer();
                int SWTWidthStep = SWTImage.WidthStep / 4;

                //Accessing Pixel
                //Slow
                //IplImage img = new IplImage("test.png");
                //for (int row = 0; row < img.Height; row++)
                //{
                //    for (int col = 0; col < img.Width; col++)
                //    {
                //        CvColor c = img[row, col];
                //        img[row, col] = new CvColor()
                //        {
                //            B = (byte)Math.Round(c.B * 0.7 + 10),
                //            G = (byte)Math.Round(c.G * 1.0),
                //            R = (byte)Math.Round(c.R * 0.0),
                //        };
                //    }

                //}

                //Fast(unsafe)
                for (int row = 0; row < edgeImage.Height; row++)
                {
                    for (int col = 0; col < edgeImage.Width; col++)
                    {
                        if (img1[row * srcWidthStep + col] > 0)
                        {
                            Ray r = new Ray();

                            Point2d p = new Point2d();
                            p.x = col;
                            p.y = row;
                            r.p = p;
                            List<Point2d> points = new List<Point2d>();
                            points.Add(p);

                            float curX = (float)col + 0.5f;
                            float curY = (float)row + 0.5f;
                            int curPixX = col;
                            int curPixY = row;
                            float Gx = gradX[row * gradXWidthStep + col];
                            float Gy = gradY[row * gradYWidthStep + col];
                            // normalize gradient
                            float mag = (float)Math.Sqrt(Gx * Gx + Gy * Gy);
                            // text darker than background or not
                            if (darkOnLight)
                            {
                                Gx = -Gx / mag;
                                Gy = -Gy / mag;
                            }
                            else
                            {
                                Gx = Gx / mag;
                                Gy = Gy / mag;
                            }

                            while (true)
                            {
                                curX += Gx * prec;
                                curY += Gy * prec;

                                if ((int)(Math.Floor(curX)) != curPixX || (int)(Math.Floor(curY)) != curPixY)
                                {
                                    curPixX = (int)(Math.Floor(curX));
                                    curPixY = (int)(Math.Floor(curY));

                                    // check if pixel is outside boundary of image
                                    if (curPixX < 0 || (curPixX >= SWTImage.Width) || curPixY < 0 || (curPixY >= SWTImage.Height))
                                    {
                                        break;
                                    }
                                    Point2d pnew = new Point2d();
                                    pnew.x = curPixX;
                                    pnew.y = curPixY;
                                    points.Add(pnew);


                                    if (img1[curPixY * srcWidthStep + curPixX] > 0)
                                    {
                                        r.q = pnew;
                                        float Gxt = gradX[curPixY * gradXWidthStep + curPixX];
                                        float Gyt = gradY[curPixY * gradYWidthStep + curPixX];
                                        //float Gxt = 0;
                                        //float Gyt = 0;
                                        mag = (float)Math.Sqrt(Gxt * Gxt + Gyt * Gyt);
                                        if (darkOnLight)
                                        {
                                            Gxt = -Gxt / mag;
                                            Gyt = -Gyt / mag;
                                        }
                                        else
                                        {
                                            Gxt = Gxt / mag;
                                            Gyt = Gyt / mag;
                                        }

                                        if (Math.Acos(Gx * -Gxt + Gy * -Gyt) < Math.PI / 2.0)
                                        {
                                            //This is stroked width transform
                                            float length = (float)Math.Sqrt(((float)r.q.x - (float)r.p.x) * ((float)r.q.x - (float)r.p.x) + ((float)r.q.y - (float)r.p.y) * ((float)r.q.y - (float)r.p.y));
                                            foreach (Point2d point in points)
                                            {
                                                if (swtPtr[point.y * SWTWidthStep + point.x] < 0)
                                                {
                                                    swtPtr[point.y * SWTWidthStep + point.x] = length;
                                                }
                                                else
                                                {
                                                    swtPtr[point.y * SWTWidthStep + point.x] = Math.Min(length, swtPtr[point.y * SWTWidthStep + point.x]);
                                                }
                                            }
                                            r.points = points;
                                            rays.Add(r);

                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

            }
        }

    }
}

