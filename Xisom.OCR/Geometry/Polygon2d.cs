using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using Xisom.OCR.Draw;


namespace Xisom.OCR.Geometry
{
	public class Polygon2d : IGeometry
	{
		public List<Vector2d> Points { get; set; } = new List<Vector2d>();

		public Polygon2d ()
		{
		}

		#region IGeometry implementation

		public GraphicsPath GraphicsPath {
			get {
				var gp = new GraphicsPath ();
				gp.AddLines (Points.Select (p => new PointF ((float)p.X, (float)p.Y)).ToArray ());
				gp.CloseFigure ();
				return gp;
			}
		}

		#endregion
	}
}

