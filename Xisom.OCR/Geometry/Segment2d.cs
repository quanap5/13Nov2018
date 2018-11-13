﻿using System;
using System.Drawing.Drawing2D;
using Xisom.OCR.Draw;
using System.Diagnostics;

namespace Xisom.OCR.Geometry
{
    //Conect all points to create the convex hull together to list of segment
	public class Segment2d : Line2d, IGeometry
	{
		/// <summary>
		/// Second Point of Line.
		/// </summary>
		/// <value>The b.</value>
		public Vector2d B{ get; set; }

		public Segment2d (Vector2d point1, Vector2d point2) :
			base (point1, point2 - point1)
		{
			B = point2;
		}

		public double Length ()
		{
			return Math.Abs (A.Distance (B));
		}

		public override string ToString ()
		{
			return string.Format ("[Segment2d: A={0} B={1}]", A, B);
		}

		#region IGeometry implementation
		public new GraphicsPath GraphicsPath {
			get {
				var gp = new GraphicsPath ();
				gp.AddLine ((float)A.X, (float)A.Y, (float)B.X, (float)B.Y);
				return gp;
			}
		}
		#endregion
	}
}

