using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Xisom.OCR.Geometry
{
    public static class MinimalBoundingBox
    {
        /// <summary>
        /// This method is used to calculate the minimum bouding box
        /// Step1: Find convex hull using monnochain approach
        /// Step2: Minimum bouding box
        /// </summary>
        /// <param name="points"></param>
        /// <returns>Bouding box</returns>
        public static Polygon2d Calculate(Vector2d[] points)
        {
            //calculate the convex hull
            var hullPoints = GeoAlgos.MonotoneChainConvexHull(points);

            //check if no bouding box available
            if (hullPoints.Length <= 1)
            {
                return new Polygon2d { Points = hullPoints.ToList() };
            }

            //Start
            Rectangle2d minBox = null;
            var minAngle = 0d;

            //for each edge (segmenr between 2 nodes) of the convex hull
            for (int i = 0; i < hullPoints.Length; i++)
            {
                var nextIndex = i + 1;

                var current = hullPoints[i];
                var next = hullPoints[nextIndex % hullPoints.Length];

                var segment = new Segment2d(current, next);

                //min max points
                var top = double.MinValue;
                var bottom = double.MaxValue;
                var left = double.MaxValue;
                var right = double.MinValue;

                //get the angle of considered segment and x axis
                var angle = AngleToXAxis(segment);

                //rotate every point and get the min max values for each direction
                foreach (var p in hullPoints)
                {
                    var rotatedPoint = RotateToXAxis(p, angle);

                    top = Math.Max(top, rotatedPoint.Y);
                    bottom = Math.Min(bottom, rotatedPoint.Y);

                    left = Math.Min(left, rotatedPoint.X);
                    right = Math.Max(right, rotatedPoint.X);
                }

                //create axis aligned bounding box
                var box = new Rectangle2d(new Vector2d(left, bottom), new Vector2d(right, top));
                if (minBox == null || minBox.Area() > box.Area())
                {
                    minBox = box;
                    minAngle = angle;

                }
            }

            //rotate axis aligned box back
            var minimalBoudingBox = new Polygon2d
            {
                Points = minBox.Points.Select(p => RotateToXAxis(p, -minAngle)).ToList()
            };

            return minimalBoudingBox;

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
