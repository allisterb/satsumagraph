#region License
/*This file is part of Satsuma Graph Library
Copyright © 2013 Balázs Szalkai

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

   1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.

   2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.

   3. This notice may not be removed or altered from any source
   distribution.*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Satsuma.Drawing
{
	/// Abstract base for shapes used to draw graph nodes.
	public interface INodeShape
	{
		/// The size of the shape, in graphic units.
		PointF Size { get; }
		/// Draws the shape.
		/// The center of the shape is supposed to be at <em>(0, 0)</em>.
		void Draw(Graphics graphics, Pen pen, Brush brush);
		/// Returns the furthermost point of the shape boundary at the given angular position.
		/// The center of the shape is supposed to be at <em>(0, 0)</em>.
		PointF GetBoundary(double angle);
	}

	/// The possible types of StandardShape.
	public enum NodeShapeKind
	{
		Diamond, Ellipse, Rectangle, Triangle, UpsideDownTriangle
	}

	/// A standard implementation of INodeShape (immutable).
	public sealed class NodeShape : INodeShape
	{
		/// The kind of the shape.
		public NodeShapeKind Kind { get; private set; }
		/// The size of the shape, in graphic units.
		public PointF Size { get; private set; }

		private readonly RectangleF rect;
		private readonly PointF[] points;

		public NodeShape(NodeShapeKind kind, PointF size)
		{
			Kind = kind;
			Size = size;

			rect = new RectangleF(-size.X * 0.5f, -size.Y * 0.5f, size.X, size.Y);
			switch (Kind)
			{
				case NodeShapeKind.Diamond: points = new PointF[] { P(rect, 0, 0.5f), P(rect, 0.5f, 1), 
						P(rect, 1, 0.5f), P(rect, 0.5f, 0) }; break;
				case NodeShapeKind.Rectangle: points = new PointF[] { rect.Location, new PointF(rect.Left, rect.Bottom),
							new PointF(rect.Right, rect.Bottom), new PointF(rect.Right, rect.Top) }; break;
				case NodeShapeKind.Triangle: points = new PointF[] { P(rect, 0.5f, 0), P(rect, 0, 1), 
						P(rect, 1, 1) }; break;
				case NodeShapeKind.UpsideDownTriangle: points = new PointF[] { P(rect, 0.5f, 1), P(rect, 0, 0), 
						P(rect, 1, 0) }; break;
			}
		}

		private static PointF P(RectangleF rect, float x, float y)
		{
			return new PointF(rect.Left + rect.Width * x, rect.Top + rect.Height * y);
		}

		public void Draw(Graphics graphics, Pen pen, Brush brush)
		{
			switch (Kind)
			{
				case NodeShapeKind.Ellipse:
					graphics.FillEllipse(brush, rect);
					graphics.DrawEllipse(pen, rect);
					break;

				default:
					graphics.FillPolygon(brush, points);
					graphics.DrawPolygon(pen, points);
					break;
			}
		}

		public PointF GetBoundary(double angle)
		{
			double cos = Math.Cos(angle), sin = Math.Sin(angle);
			switch (Kind)
			{
				case NodeShapeKind.Ellipse:
					return new PointF((float)(Size.X * 0.5f * cos), (float)(Size.Y * 0.5f * sin));

				default:
					// we have a polygon, try to intersect all sides with the ray
					for (int i = 0; i < points.Length; i++)
					{
						int i2 = (i + 1) % points.Length;
						float t = (float)((points[i].Y * cos - points[i].X * sin) /
							((points[i2].X - points[i].X) * sin - (points[i2].Y - points[i].Y) * cos));
						if (t >= 0 && t <= 1)
						{
							var result = new PointF(points[i].X + t * (points[i2].X - points[i].X),
								points[i].Y + t * (points[i2].Y - points[i].Y));
							if (result.X * cos + result.Y * sin > 0) return result;
						}
					}
					return new PointF(0, 0); // should not happen
			}
		}
	}

	/// The visual style for a drawn node.
	public sealed class NodeStyle
	{
		/// The pen used to draw the node.
		/// Default: Pens.Black.
		public Pen Pen { get; set; }
		/// The brush used to draw the node.
		/// Default: Brushes.White.
		public Brush Brush { get; set; }
		/// The shape of the node.
		/// Default: #DefaultShape.
		public INodeShape Shape { get; set; }
		/// The font used to draw the caption.
		/// Default: SystemFonts.DefaultFont.
		public Font TextFont { get; set; }
		/// The brush used to draw the caption.
		/// Default: Brushes.Black.
		public Brush TextBrush { get; set; }

		/// The default node shape.
		public static readonly INodeShape DefaultShape = new NodeShape(NodeShapeKind.Ellipse, new PointF(10, 10));
		
		public NodeStyle()
		{
			Pen = Pens.Black;
			Brush = Brushes.White;
			Shape = DefaultShape;
			TextFont = SystemFonts.DefaultFont;
			TextBrush = Brushes.Black;
		}

		internal void DrawNode(Graphics graphics, float x, float y, string text)
		{
			var state = graphics.Save();
			graphics.TranslateTransform(x, y);
			Shape.Draw(graphics, Pen, Brush);
			if (text != "")
				graphics.DrawString(text, TextFont, TextBrush, 0, 0,
					new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
			graphics.Restore(state);
		}
	}

	/// Draws a graph on a Graphics.
	///
	/// Example:
	/// \code
	/// var graph = new CompleteGraph(7);
	/// // compute a nice layout of the graph
	/// var layout = new ForceDirectedLayout(graph);
	/// layout.Run();
	/// // draw the graph using the computed layout
	/// var nodeShape = new NodeShape(NodeShapeKind.Diamond, new PointF(40, 40));
	/// var nodeStyle = new NodeStyle { Brush = Brushes.Yellow, Shape = nodeShape };
	/// var drawer = new GraphDrawer()
	/// {
	/// 	Graph = graph,
	/// 	NodePositions = (node => (PointF)layout.NodePositions[node]),
	/// 	NodeCaptions = (node => graph.GetNodeIndex(node).ToString()),
	/// 	NodeStyles = (node => nodeStyle)
	/// };
	/// drawer.Draw(300, 300, Color.White).Save(@"c:\graph.png", ImageFormat.Png);
	/// \endcode
	public sealed class GraphDrawer
	{
		/// The graph to draw.
		public IGraph Graph { get; set; }
		/// Assigns its position to a node.
		public Func<Node, PointF> NodePosition { get; set; }
		/// Assigns its caption to a node.
		/// Default value: assign the empty string (i.e. no caption) to each node.
		public Func<Node, string> NodeCaption { get; set; }
		/// Assigns its style to a node.
		/// Default value: assign a default NodeStyle to each node.
		/// \warning This function is called lots of times (at least twice for each arc).
		/// Avoid creating a NodeStyle object on each call.
		/// Return pre-made objects from some collection instead.
		public Func<Node, NodeStyle> NodeStyle { get; set; }
		/// Assigns a pen to each arc.
		/// Default value: assign #DirectedPen to directed arcs, and #UndirectedPen to edges.
		public Func<Arc, Pen> ArcPen { get; set; }
		/// The pen used for directed arcs.
		/// Default value: a black pen with an arrow end.
		/// Unused if ArcPens is set to a custom value.
		public Pen DirectedPen { get; set; }
		/// The pen used for undirected arcs. 
		/// Default value: Pens.Black.
		/// Unused if #ArcPen is set to a custom value.
		public Pen UndirectedPen { get; set; }

		public GraphDrawer()
		{
			NodeCaption = (node => "");
			var defaultNodeStyle = new NodeStyle();
			NodeStyle = (node => defaultNodeStyle);
			ArcPen = (arc => Graph.IsEdge(arc) ? UndirectedPen : DirectedPen);
			DirectedPen = new Pen(Color.Black) { CustomEndCap = new AdjustableArrowCap(3, 5) };
			UndirectedPen = Pens.Black;
		}

		/// Draws the graph.
		/// \param matrix The transformation matrix to be applied to the node positions
		/// (but not to the node and arc shapes).
		/// If null, the identity matrix is used.
		public void Draw(Graphics graphics, Matrix matrix = null)
		{
			// draw arcs
			PointF[] arcPos = new PointF[2];
			PointF[] boundary = new PointF[2];
			foreach (var arc in Graph.Arcs())
			{
				Node u = Graph.U(arc);
				arcPos[0] = NodePosition(u);
				Node v = Graph.V(arc);
				arcPos[1] = NodePosition(v);
				if (matrix != null) matrix.TransformPoints(arcPos);

				// an arc should run between shape boundaries
				double angle = Math.Atan2(arcPos[1].Y - arcPos[0].Y, arcPos[1].X - arcPos[0].X);
				boundary[0] = NodeStyle(u).Shape.GetBoundary(angle);
				boundary[1] = NodeStyle(v).Shape.GetBoundary(angle + Math.PI);

				graphics.DrawLine(ArcPen(arc), arcPos[0].X + boundary[0].X, arcPos[0].Y + boundary[0].Y,
					arcPos[1].X + boundary[1].X, arcPos[1].Y + boundary[1].Y);
			}

			// draw nodes
			PointF[] nodePos = new PointF[1];
			foreach (var node in Graph.Nodes())
			{
				nodePos[0] = NodePosition(node);
				if (matrix != null) matrix.TransformPoints(nodePos);
				NodeStyle(node).DrawNode(graphics, nodePos[0].X, nodePos[0].Y, NodeCaption(node));
			}
		}

		/// Draws the graph to fit the given bounding box.
		/// \param box The desired bounding box for the drawn graph.
		public void Draw(Graphics graphics, RectangleF box)
		{
			if (!Graph.Nodes().Any()) return;

			float maxShapeWidth = 0, maxShapeHeight = 0;
			float xmin = float.PositiveInfinity, ymin = float.PositiveInfinity;
			float xmax = float.NegativeInfinity, ymax = float.NegativeInfinity;
			foreach (var node in Graph.Nodes())
			{
				PointF size = NodeStyle(node).Shape.Size;
				maxShapeWidth = Math.Max(maxShapeWidth, size.X);
				maxShapeHeight = Math.Max(maxShapeHeight, size.Y);

				PointF pos = NodePosition(node);
				xmin = Math.Min(xmin, pos.X);
				xmax = Math.Max(xmax, pos.X);
				ymin = Math.Min(ymin, pos.Y);
				ymax = Math.Max(ymax, pos.Y);
			}

			float xspan = xmax - xmin;
			if (xspan == 0) xspan = 1;
			float yspan = ymax - ymin;
			if (yspan == 0) yspan = 1;

			Matrix matrix = new Matrix();
			matrix.Translate(maxShapeWidth*0.6f, maxShapeHeight*0.6f);
			matrix.Scale((box.Width - maxShapeWidth * 1.2f) / xspan, (box.Height - maxShapeHeight * 1.2f) / yspan);
			matrix.Translate(-xmin, -ymin);
			Draw(graphics, matrix);
		}

		/// Draws the graph to a new bitmap and returns the bitmap.
		/// \param width The width of the bitmap.
		/// \param height The height of the bitmap.
		/// \param backColor The background color for the bitmap.
		/// \param antialias Specifies whether anti-aliasing should take place when drawing.
		/// \param pixelFormat The pixel format of the bitmap. Default value: 32-bit ARGB.
		public Bitmap Draw(int width, int height, Color backColor, 
			bool antialias = true, PixelFormat pixelFormat = PixelFormat.Format32bppArgb)
		{
			Bitmap bm = new Bitmap(width, height, pixelFormat);
			using (var g = Graphics.FromImage(bm))
			{
				g.SmoothingMode = antialias ? SmoothingMode.AntiAlias : SmoothingMode.None;
				g.Clear(backColor);
				Draw(g, new RectangleF(0, 0, width, height));
			}
			return bm;
		}
	}
}
