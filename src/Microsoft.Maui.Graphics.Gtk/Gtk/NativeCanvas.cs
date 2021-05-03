using System;
using Microsoft.Maui.Graphics.Text;

namespace Microsoft.Maui.Graphics.Native.Gtk {

	public class NativeCanvas : AbstractCanvas<NativeCanvasState> {

		public NativeCanvas() : base(CreateNewState, CreateStateCopy) { }

		private Cairo.Context _context;

		public Cairo.Context Context {
			get => _context;
			set {
				_context = null;
				ResetState();
				_context = value;
			}
		}

		private static NativeCanvasState CreateNewState(object context) {
			return new NativeCanvasState() { };
		}

		private static NativeCanvasState CreateStateCopy(NativeCanvasState prototype) {
			return new NativeCanvasState(prototype);
		}

		public override bool Antialias {
			set => CurrentState.Antialias = CanvasExtensions.ToAntialias(value);
		}

		public override float MiterLimit {
			set => CurrentState.MiterLimit = value;
		}

		public override Color StrokeColor {
			set => CurrentState.StrokeColor = value.ToCairoColor();
		}

		public override LineCap StrokeLineCap {
			set => CurrentState.LineCap = value.ToLineCap();
		}

		public override LineJoin StrokeLineJoin {
			set => CurrentState.LineJoin = value.ToLineJoin();
		}

		protected override float NativeStrokeSize {
			set => throw new NotImplementedException();
		}

		public override Color FillColor {
			set => CurrentState.FillColor = value.ToCairoColor();
		}

		public override Color FontColor {
			set => CurrentState.FontColor = value.ToCairoColor();
		}

		public override string FontName {
			set => CurrentState.FontName = value;
		}

		public override float FontSize {
			set => CurrentState.FontSize = value;
		}

		public override float Alpha {
			set => CurrentState.Alpha = value;
		}

		public override BlendMode BlendMode {
			set => CurrentState.BlendMode = value;
		}

		protected override void NativeSetStrokeDashPattern(float[] pattern, float strokeSize) {
			throw new NotImplementedException();
		}

		protected override void NativeDrawLine(float x1, float y1, float x2, float y2) {
			Context.MoveTo(x1, y1);
			Context.LineTo(x2, y2);
		}

		void AddArc(Cairo.Context context, float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise, bool closed) {
			// https://developer.gnome.org/cairo/stable/cairo-Paths.html#cairo-arc
			// Angles are measured in radians
			var startAngleInRadians = Geometry.DegreesToRadians(-startAngle);
			var endAngleInRadians = Geometry.DegreesToRadians(-endAngle);
			var center = new PointF(x + width / 2f, y + height / 2f);
			context.Scale(width / 2f, height / 2f);
			context.Arc(center.X, center.Y, 1, startAngleInRadians, endAngleInRadians);
		}

		protected override void NativeDrawArc(float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise, bool closed) {
			Context.Save();
			AddArc(Context, x, y, width, height, startAngle, endAngle, clockwise, closed);
			Context.Paint();
			Context.Restore();

		}

		void AddRectangle(Cairo.Context context, float x, float y, float width, float height) {
			context.Rectangle(x, y, width, height);
		}

		protected override void NativeDrawRectangle(float x, float y, float width, float height) {
			AddRectangle(Context, x, y, width, height);
			Context.Paint();
		}

		void AddRoundedRectangle(Cairo.Context context, float x, float y, float width, float height, float radius) {
			if (radius > width - radius)
				radius = width / 2;

			if (radius > height - radius)
				radius = height / 2;

			// top-left corner
			context.MoveTo(x + radius, y);

			// top edge
			context.LineTo(x + width - radius, y);

			// top-right corner
			if (radius > 0)
				context.Arc(x + width - radius, y + radius, radius, -90, 0);

			// right edge
			context.LineTo(x + width, y + height - radius);

			// bottom-right corner
			if (radius > 0)
				context.Arc(x + width - radius, y + height - radius, radius, 0, 90);

			// bottom edge
			context.LineTo(x + radius, y + height);

			// bottom-left corner
			if (radius > 0)
				context.Arc(x + radius, y + height - radius, radius, 90, 180);

			// left edge
			context.LineTo(x, y + radius);

			// top-left corner
			if (radius > 0)
				context.Arc(x + radius, y + radius, radius, 180, 270);
		}

		protected override void NativeDrawRoundedRectangle(float x, float y, float width, float height, float radius) {
			AddRoundedRectangle(Context, x, y, width, height, radius);
			Context.Paint();
		}

		protected override void NativeDrawEllipse(float x, float y, float width, float height) {
			NativeDrawArc(x, y, width, height, 0, (float) (Math.PI * 2d), true, true);
		}

		protected override void NativeDrawPath(PathF path) {
			AddPath(Context, path);
			Context.Paint();
		}

		private void AddPath(Cairo.Context context, PathF target) {
			var pointIndex = 0;
			var arcAngleIndex = 0;
			var arcClockwiseIndex = 0;

			foreach (var type in target.SegmentTypes) {
				if (type == PathOperation.Move) {
					var point = target[pointIndex++];
					context.MoveTo(point.X, point.Y);
				} else if (type == PathOperation.Line) {
					var endPoint = target[pointIndex++];
					context.LineTo(endPoint.X, endPoint.Y);

				} else if (type == PathOperation.Quad) {
					var p1 = pointIndex > 0 ? target[pointIndex - 1] : context.CurrentPoint.ToPointF();
					var c = target[pointIndex++];
					var p2 = target[pointIndex++];

					// quad bezier to cubic bezier:
					// C1 = 2/3•C + 1/3•P1
					// C2 = 2/3•C + 1/3•P2

					var c1 = new PointF(c.X * 2 / 3 + p1.X / 3, c.Y * 2 / 3 + p1.Y / 3);
					var c2 = new PointF(c.X * 2 / 3 + p2.X / 3, c.Y * 2 / 3 + p2.Y / 3);

					// Adds a cubic Bézier spline to the path
					context.CurveTo(
						c1.X, c1.Y,
						c2.X, c2.Y,
						p2.X, p2.Y);

				} else if (type == PathOperation.Cubic) {
					var controlPoint1 = target[pointIndex++];
					var controlPoint2 = target[pointIndex++];
					var endPoint = target[pointIndex++];

					// https://developer.gnome.org/cairo/stable/cairo-Paths.html#cairo-curve-to
					// Adds a cubic Bézier spline to the path from the current point to position (x3, y3) in user-space coordinates,
					// using (x1, y1) and (x2, y2) as the control points. After this call the current point will be (x3, y3).
					// If there is no current point before the call to cairo_curve_to() this function will behave as if preceded by a call to cairo_move_to(cr, x1, y1).
					context.CurveTo(
						controlPoint1.X, controlPoint1.Y,
						controlPoint2.X, controlPoint2.Y,
						endPoint.X, endPoint.Y);

				} else if (type == PathOperation.Arc) {
					var topLeft = target[pointIndex++];
					var bottomRight = target[pointIndex++];
					var startAngle = target.GetArcAngle(arcAngleIndex++);
					var endAngle = target.GetArcAngle(arcAngleIndex++);
					var clockwise = target.GetArcClockwise(arcClockwiseIndex++);

					var startAngleInRadians = Geometry.DegreesToRadians(-startAngle);
					var endAngleInRadians = Geometry.DegreesToRadians(-endAngle);

					while (startAngleInRadians < 0) {
						startAngleInRadians += (float) Math.PI * 2;
					}

					while (endAngleInRadians < 0) {
						endAngleInRadians += (float) Math.PI * 2;
					}

					var cx = (bottomRight.X + topLeft.X) / 2;
					var cy = (bottomRight.Y + topLeft.Y) / 2;
					var width = bottomRight.X - topLeft.X;
					var height = bottomRight.Y - topLeft.Y;
					var r = width / 2;

					if (clockwise) {
						context.Arc(cx, cy, r, startAngle, endAngle);
					} else {
						context.ArcNegative(cx, cy, r, startAngle, endAngle);
					}
				} else if (type == PathOperation.Close) {
					context.ClosePath();
				}
			}
		}

		protected override void NativeRotate(float degrees, float radians, float x, float y) {
		}

		protected override void NativeRotate(float degrees, float radians) { }

		protected override void NativeScale(float fx, float fy) {
			Context.Scale(fx, fy);
		}

		protected override void NativeTranslate(float tx, float ty) {
			Context.Translate(tx, ty);
		}

		protected override void NativeConcatenateTransform(AffineTransform transform) {
		}

		public override void ClipPath(PathF path, WindingMode windingMode = WindingMode.NonZero) { }

		public override void SetShadow(SizeF offset, float blur, Color color) { }

		public override void SetFillPaint(Paint paint, RectangleF rectangle) { }

		public override void SetToSystemFont() { }

		public override void SetToBoldSystemFont() { }

		public override void DrawImage(IImage image, float x, float y, float width, float height) { }

		public override void ClipRectangle(float x, float y, float width, float height) { }

		public override void FillArc(float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise) {
			Context.Save();
			AddArc(Context, y, y, width, height, startAngle, endAngle, clockwise, true);
			Context.Fill();
			Context.Restore();
		}

		public override void FillRectangle(float x, float y, float width, float height) {
			AddRectangle(Context, x, y, width, height);
			Context.Fill();
		}

		public override void FillRoundedRectangle(float x, float y, float width, float height, float cornerRadius) {
			AddRoundedRectangle(Context, x, y, width, height, cornerRadius);
			Context.Fill();
		}

		public override void FillEllipse(float x, float y, float width, float height) {
			FillArc(x, y, width, height, 0, (float) (Math.PI * 2f), true);
		}

		public override void DrawString(string value, float x, float y, HorizontalAlignment horizontalAlignment) {
		}

		public override void DrawString(string value, float x, float y, float width, float height, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, TextFlow textFlow = TextFlow.ClipBounds, float lineSpacingAdjustment = 0) {
		}

		public override void DrawText(IAttributedText value, float x, float y, float width, float height) {
		}

		public override void FillPath(PathF path, WindingMode windingMode) {
			AddPath(Context, path);
			Context.ClosePath();
			Context.Fill();
		}

		public override void SubtractFromClip(float x, float y, float width, float height) {
		}

	}

}
