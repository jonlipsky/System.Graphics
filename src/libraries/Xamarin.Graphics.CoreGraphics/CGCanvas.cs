#if MONOMAC
#else
using UIKit;
#endif
using System;
using CoreGraphics;
using CoreText;
using Foundation;
using Xamarin.Text;

namespace Xamarin.Graphics.CoreGraphics
{
    public class CGCanvas : AbstractCanvas<CGCanvasState>
    {
        private static readonly nfloat[] EmptyNFloatArray = { };
        private static readonly CGAffineTransform FlipTransform = new CGAffineTransform(1.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f);

        private static string _systemFontName;
        private static string _boldSystemFontName;

        private readonly string _defaultFontName;

        private bool _antialias = true;
        private CGContext _context;
        private readonly Func<CGColorSpace> _getColorspace;

        private EWColor _fontColor = StandardColors.Black;
        private string _fontName;
        private float _fontSize = 10f;
        private CGGradient _gradient;

        private CGCanvas _fillPatternCanvas;

        private EWPattern _fillPattern;
        private CGRect _fillPatternRect;

        private EWImage _fillImage;

        private CGPoint _gradientEnd = new CGPoint(0, 0);
        private CGPoint _gradientStart = new CGPoint(0, 0);
        private CGPoint _radialFocalPoint = new CGPoint(0, 0);
        private EWPaint _paint;

        // A local instance of a rectangle to avoid lots of object creation.
        private CGRect _rect = new CGRect(0, 0, 0, 0);
        public override bool PixelShifted { get; set; }

        public CGCanvas(Func<CGColorSpace> getColorspace) : base(CreateNewState, CreateStateCopy)
        {
            _getColorspace = getColorspace;

            if (_systemFontName == null)
            {
#if MONOMAC || __MACOS__
                var systemFont = NSFont.SystemFontOfSize(12f);
                _systemFontName = systemFont.FontName;
                systemFont.Dispose();

                var boldSystemFont = NSFont.BoldSystemFontOfSize(12f);
                _boldSystemFontName = boldSystemFont.FontName;
                boldSystemFont.Dispose();
#else
                var systemFont = UIFont.SystemFontOfSize(12f);
                _systemFontName = systemFont.Name;
                systemFont.Dispose();

                var boldSystemFont = UIFont.BoldSystemFontOfSize(12f);
                _boldSystemFontName = boldSystemFont.Name;
                boldSystemFont.Dispose();
#endif
            }

            _defaultFontName = "Helvetica";
            _fontName = _defaultFontName;
        }

        private static CGCanvasState CreateNewState(object context)
        {
            return new CGCanvasState();
        }

        private static CGCanvasState CreateStateCopy(CGCanvasState prototype)
        {
            return new CGCanvasState(prototype);
        }

        public CGContext Context
        {
            get => _context;
            set
            {
                _context = value;
                if (_context != null)
                {
                    var colorspace = _getColorspace?.Invoke() ?? CGColorSpace.CreateDeviceRGB();
                    _context.SetFillColorSpace(colorspace);
                    _context.SetStrokeColorSpace(colorspace);
                }
                ResetState();
            }
        }

        public override bool Antialias
        {
            set => _antialias = value;
        }

        protected override float NativeStrokeSize
        {
            set => _context.SetLineWidth(value);
        }

        public override float MiterLimit
        {
            set => _context.SetMiterLimit(value);
        }

        public override float Alpha
        {
            set => _context.SetAlpha(value);
        }

        public override EWLineCap StrokeLineCap
        {
            set
            {
                if (value == EWLineCap.BUTT)
                {
                    _context.SetLineCap(CGLineCap.Butt);
                }
                else if (value == EWLineCap.ROUND)
                {
                    _context.SetLineCap(CGLineCap.Round);
                }
                else if (value == EWLineCap.SQUARE)
                {
                    _context.SetLineCap(CGLineCap.Square);
                }
            }
        }

        public override EWLineJoin StrokeLineJoin
        {
            set
            {
                if (value == EWLineJoin.MITER)
                {
                    _context.SetLineJoin(CGLineJoin.Miter);
                }
                else if (value == EWLineJoin.ROUND)
                {
                    _context.SetLineJoin(CGLineJoin.Round);
                }
                else if (value == EWLineJoin.BEVEL)
                {
                    _context.SetLineJoin(CGLineJoin.Bevel);
                }
            }
        }

        public override EWColor StrokeColor
        {
            set
            {
                if (value != null)
                {
                    _context.SetStrokeColor(value.Red, value.Green, value.Blue, value.Alpha);
                }
                else
                {
                    _context.SetStrokeColor(0, 0, 0, 1); // Black
                }
            }
        }

        public override EWColor FontColor
        {
            set => _fontColor = value ?? StandardColors.Black;
        }

        public override string FontName
        {
            set => _fontName = value ?? _defaultFontName;
        }

        public override float FontSize
        {
            set => _fontSize = value;
        }

        public override EWColor FillColor
        {
            set
            {
                if (value != null)
                {
                    _context.SetFillColor(value.Red, value.Green, value.Blue, value.Alpha);
                }
                else
                {
                    _context.SetFillColor(1, 1, 1, 1); // White
                }

                if (_gradient != null)
                {
                    _gradient.Dispose();
                    _gradient = null;
                }

                _fillPattern = null;
                _fillImage = null;
                _paint = null;
            }
        }

        public override EWBlendMode BlendMode
        {
            set
            {
                var blendMode = CGBlendMode.Normal;

                switch (value)
                {
                    case EWBlendMode.Clear:
                        blendMode = CGBlendMode.Clear;
                        break;
                    case EWBlendMode.Color:
                        blendMode = CGBlendMode.Color;
                        break;
                    case EWBlendMode.ColorBurn:
                        blendMode = CGBlendMode.ColorBurn;
                        break;
                    case EWBlendMode.ColorDodge:
                        blendMode = CGBlendMode.ColorDodge;
                        break;
                    case EWBlendMode.Copy:
                        blendMode = CGBlendMode.Copy;
                        break;
                    case EWBlendMode.Darken:
                        blendMode = CGBlendMode.Darken;
                        break;
                    case EWBlendMode.DestinationAtop:
                        blendMode = CGBlendMode.DestinationAtop;
                        break;
                    case EWBlendMode.DestinationIn:
                        blendMode = CGBlendMode.DestinationIn;
                        break;
                    case EWBlendMode.DestinationOut:
                        blendMode = CGBlendMode.DestinationOut;
                        break;
                    case EWBlendMode.DestinationOver:
                        blendMode = CGBlendMode.DestinationOver;
                        break;
                    case EWBlendMode.Difference:
                        blendMode = CGBlendMode.Difference;
                        break;
                    case EWBlendMode.Exclusion:
                        blendMode = CGBlendMode.Exclusion;
                        break;
                    case EWBlendMode.HardLight:
                        blendMode = CGBlendMode.HardLight;
                        break;
                    case EWBlendMode.Hue:
                        blendMode = CGBlendMode.Hue;
                        break;
                    case EWBlendMode.Lighten:
                        blendMode = CGBlendMode.Lighten;
                        break;
                    case EWBlendMode.Luminosity:
                        blendMode = CGBlendMode.Luminosity;
                        break;
                    case EWBlendMode.Multiply:
                        blendMode = CGBlendMode.Multiply;
                        break;
                    case EWBlendMode.Normal:
                        blendMode = CGBlendMode.Normal;
                        break;
                    case EWBlendMode.Overlay:
                        blendMode = CGBlendMode.Overlay;
                        break;
                    case EWBlendMode.PlusDarker:
                        blendMode = CGBlendMode.PlusDarker;
                        break;
                    case EWBlendMode.PlusLighter:
                        blendMode = CGBlendMode.PlusLighter;
                        break;
                    case EWBlendMode.Saturation:
                        blendMode = CGBlendMode.Saturation;
                        break;
                    case EWBlendMode.Screen:
                        blendMode = CGBlendMode.Screen;
                        break;
                    case EWBlendMode.SoftLight:
                        blendMode = CGBlendMode.SoftLight;
                        break;
                    case EWBlendMode.SourceAtop:
                        blendMode = CGBlendMode.SourceAtop;
                        break;
                    case EWBlendMode.SourceIn:
                        blendMode = CGBlendMode.SourceIn;
                        break;
                    case EWBlendMode.SourceOut:
                        blendMode = CGBlendMode.SourceOut;
                        break;
                    case EWBlendMode.XOR:
                        blendMode = CGBlendMode.XOR;
                        break;
                }

                _context.SetBlendMode(blendMode);
            }
        }

        protected override void NativeSetStrokeDashPattern(float[] pattern, float strokeSize)
        {
            if (pattern == null)
            {
                _context.SetLineDash(0, EmptyNFloatArray);
            }
            else
            {
                float actualStrokeSize = strokeSize;

                if (LimitStrokeScalingEnabled)
                {
                    var strokeLimit = AssignedStrokeLimit;
                    var scale = CurrentState.Scale;
                    var scaledStrokeSize = scale * actualStrokeSize;
                    if (scaledStrokeSize < strokeLimit)
                        actualStrokeSize = strokeLimit / scale;
                }

                var actualDashPattern = new nfloat[pattern.Length];
                for (var i = 0; i < pattern.Length; i++)
                {
                    actualDashPattern[i] = pattern[i] * actualStrokeSize;
                }

                _context.SetLineDash(0, actualDashPattern, actualDashPattern.Length);
            }
        }

        public override void SetToSystemFont()
        {
            FontName = _systemFontName;
        }

        public override void SetToBoldSystemFont()
        {
            FontName = _boldSystemFontName;
        }

        public override void SetFillPaint(
            EWPaint paint,
            float x1,
            float y1,
            float x2,
            float y2)
        {
            _gradientStart.X = x1;
            _gradientStart.Y = y1;
            _gradientEnd.X = x2;
            _gradientEnd.Y = y2;
            _radialFocalPoint.X = x1;
            _radialFocalPoint.Y = y1;

            if (paint == null)
            {
                paint = StandardColors.White.AsPaint();
            }

            if (_gradient != null)
            {
                _gradient.Dispose();
                _gradient = null;
            }

            _fillPattern = null;
            _fillImage = null;
            _paint = null;

            if (paint.PaintType == EWPaintType.SOLID)
            {
                FillColor = paint.StartColor;
            }
            else if (paint.PaintType == EWPaintType.LINEAR_GRADIENT)
            {
                var gradientColors = new nfloat[paint.Stops.Length * 4];
                var offsets = new nfloat[paint.Stops.Length];

                int g = 0;
                for (int i = 0; i < paint.Stops.Length; i++)
                {
                    EWColor vColor = paint.Stops[i].Color;
                    offsets[i] = paint.Stops[i].Offset;

                    if (vColor == null) vColor = StandardColors.White;

                    gradientColors[g++] = vColor.Red;
                    gradientColors[g++] = vColor.Green;
                    gradientColors[g++] = vColor.Blue;
                    gradientColors[g++] = vColor.Alpha;
                }

                var colorspace = _getColorspace?.Invoke() ?? CGColorSpace.CreateDeviceRGB();
                _gradient = new CGGradient(colorspace, gradientColors, offsets);
                _paint = paint;
            }
            else if (paint.PaintType == EWPaintType.RADIAL_GRADIENT)
            {
                var gradientColors = new nfloat[paint.Stops.Length * 4];
                var offsets = new nfloat[paint.Stops.Length];

                int g = 0;
                for (int i = 0; i < paint.Stops.Length; i++)
                {
                    EWColor vColor = paint.Stops[i].Color;
                    offsets[i] = paint.Stops[i].Offset;

                    if (vColor == null) vColor = StandardColors.White;

                    gradientColors[g++] = vColor.Red;
                    gradientColors[g++] = vColor.Green;
                    gradientColors[g++] = vColor.Blue;
                    gradientColors[g++] = vColor.Alpha;
                }

                var colorspace = _getColorspace?.Invoke() ?? CGColorSpace.CreateDeviceRGB();
                _gradient = new CGGradient(colorspace, gradientColors, offsets);
                _paint = paint;
            }
            else if (paint.PaintType == EWPaintType.PATTERN)
            {
                _fillPattern = paint.Pattern;
            }
            else if (paint.PaintType == EWPaintType.IMAGE)
            {
                _fillImage = paint.Image;
            }
            else
            {
                FillColor = paint.StartColor;
            }

            //Logger.Debug("Gradient Set To: "+aPaint.PaintType);
        }

        protected override void NativeDrawLine(float x1, float y1, float x2, float y2)
        {
            if (!_antialias) _context.SetShouldAntialias(false);

            _context.MoveTo(x1, y1);
            _context.AddLineToPoint(x2, y2);
            _context.StrokePath();

            if (!_antialias) _context.SetShouldAntialias(true);
        }

        protected override void NativeDrawArc(float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise, bool close)
        {
            var strokeLocation = CurrentState.StrokeLocation;
            if (strokeLocation == EWStrokeLocation.CENTER)
            {
                _rect.X = x;
                _rect.Y = y;
                _rect.Width = width;
                _rect.Height = height;
            }
            else if (strokeLocation == EWStrokeLocation.INSIDE)
            {
                var strokeSize = CurrentState.StrokeSize;
                _rect.X = x + (strokeSize / 2);
                _rect.Y = y + (strokeSize / 2);
                _rect.Width = width - strokeSize;
                _rect.Height = height - strokeSize;
            }
            else if (strokeLocation == EWStrokeLocation.OUTSIDE)
            {
                var strokeSize = CurrentState.StrokeSize;
                _rect.X = x - (strokeSize / 2);
                _rect.Y = y - (strokeSize / 2);
                _rect.Width = width + strokeSize;
                _rect.Height = height + strokeSize;
            }

            if (!_antialias) _context.SetShouldAntialias(false);
            var startAngleInRadians = Geometry.DegreesToRadians(-startAngle);
            var endAngleInRadians = Geometry.DegreesToRadians(-endAngle);

            while (startAngleInRadians < 0)
                startAngleInRadians += (float) Math.PI * 2;

            while (endAngleInRadians < 0)
                endAngleInRadians += (float) Math.PI * 2;

            if (width == height)
            {
                _context.AddArc(_rect.GetMidX(), _rect.GetMidY(), _rect.Width / 2, startAngleInRadians, endAngleInRadians, !clockwise);
                if (close)
                {
                    _context.ClosePath();
                }

                _context.StrokePath();
            }
            else
            {
                var cx = _rect.GetMidX();
                var cy = _rect.GetMidY();
                var transform = CGAffineTransform.MakeTranslation(cx, cy);
                transform = CGAffineTransform.Multiply(CGAffineTransform.MakeScale(1, _rect.Height / _rect.Width), transform);

                var path = new CGPath();
                path.AddArc(transform, 0, 0, _rect.Width / 2, startAngleInRadians, endAngleInRadians, !clockwise);
                if (close)
                {
                    path.CloseSubpath();
                }

                _context.AddPath(path);
                _context.StrokePath();
                path.Dispose();
            }

            if (!_antialias) _context.SetShouldAntialias(true);
        }

        public override void FillArc(float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise)
        {
            _rect.X = x;
            _rect.Y = y;
            _rect.Width = width;
            _rect.Height = height;

            var startAngleInRadians = Geometry.DegreesToRadians(-startAngle);
            var endAngleInRadians = Geometry.DegreesToRadians(-endAngle);

            while (startAngleInRadians < 0)
                startAngleInRadians += (float) Math.PI * 2;

            while (endAngleInRadians < 0)
                endAngleInRadians += (float) Math.PI * 2;

            if (width == height)
            {
                if (_gradient != null)
                {
                    FillWithGradient(
                        () =>
                        {
                            _context.AddArc(_rect.GetMidX(), _rect.GetMidY(), _rect.Width / 2, startAngleInRadians, endAngleInRadians, !clockwise);
                            return true;
                        });
                }
                else if (_fillPattern != null)
                {
                    _context.AddArc(_rect.GetMidX(), _rect.GetMidY(), _rect.Width / 2, startAngleInRadians, endAngleInRadians, !clockwise);
                    FillWithPattern(x, y, () => _context.FillPath());
                }
                else if (_fillImage != null)
                {
                    _context.AddArc(_rect.GetMidX(), _rect.GetMidY(), _rect.Width / 2, startAngleInRadians, endAngleInRadians, !clockwise);
                    FillWithImage(x, y, () => _context.FillPath());
                }
                else
                {
                    _context.AddArc(_rect.GetMidX(), _rect.GetMidY(), _rect.Width / 2, startAngleInRadians, endAngleInRadians, !clockwise);
                    _context.FillPath();
                }
            }
            else
            {
                var cx = _rect.GetMidX();
                var cy = _rect.GetMidY();
                var transform = CGAffineTransform.MakeTranslation(cx, cy);
                transform = CGAffineTransform.Multiply(CGAffineTransform.MakeScale(1, _rect.Height / _rect.Width), transform);
                var path = new CGPath();
                path.AddArc(transform, 0, 0, _rect.Width / 2, startAngleInRadians, endAngleInRadians, !clockwise);

                if (_gradient != null)
                {
                    FillWithGradient(
                        () =>
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            _context.AddPath(path);
                            return true;
                        });
                }
                else if (_fillPattern != null)
                {
                    _context.AddPath(path);
                    FillWithPattern(x, y, () => _context.FillPath());
                }
                else if (_fillImage != null)
                {
                    _context.AddPath(path);
                    FillWithImage(x, y, () => _context.FillPath());
                }
                else
                {
                    _context.AddPath(path);
                    _context.FillPath();
                }

                path.Dispose();
            }
        }

        /// <summary>
        /// Fills the with gradient.
        /// 
        /// The function should return whether or not this method should handle clipping.
        /// </summary>
        /// <param name="action">Action.</param>
        public void FillWithGradient(Func<bool> action)
        {
            // If we are doing a fill, then we need to fill the shape with a solid color
            // to get the shadow because the gradient fills are done withing a clipped
            // area.
            if (CurrentState.Shadowed)
            {
                float vMinimumTransparent = Math.Min(_paint.StartColor.Alpha, _paint.EndColor.Alpha);
                var color = StandardColors.White.GetTransparentCopy(vMinimumTransparent);
                _context.SetFillColor(color.Red, color.Green, color.Blue, color.Alpha);
                action();
                _context.FillPath();
            }

            _context.SaveState();
            if (action())
            {
                _context.Clip();
            }

            DrawGradient();
            _context.RestoreState();
        }

        protected override void NativeDrawRectangle(float x, float y, float width, float height)
        {
            var strokeLocation = CurrentState.StrokeLocation;
            if (strokeLocation == EWStrokeLocation.CENTER)
            {
                _rect.X = x;
                _rect.Y = y;
                _rect.Width = width;
                _rect.Height = height;
            }
            else if (strokeLocation == EWStrokeLocation.INSIDE)
            {
                var strokeSize = CurrentState.StrokeSize;
                _rect.X = x + (strokeSize / 2);
                _rect.Y = y + (strokeSize / 2);
                _rect.Width = width - strokeSize;
                _rect.Height = height - strokeSize;
            }
            else if (strokeLocation == EWStrokeLocation.OUTSIDE)
            {
                var strokeSize = CurrentState.StrokeSize;
                _rect.X = x - (strokeSize / 2);
                _rect.Y = y - (strokeSize / 2);
                _rect.Width = width + strokeSize;
                _rect.Height = height + strokeSize;
            }

            if (!_antialias) _context.SetShouldAntialias(false);
            _context.StrokeRect(_rect);
            if (!_antialias) _context.SetShouldAntialias(true);
        }

        private void DrawGradient()
        {
            if (_paint.PaintType == EWPaintType.LINEAR_GRADIENT)
            {
                _context.DrawLinearGradient(_gradient, _gradientStart, _gradientEnd, CGGradientDrawingOptions.DrawsAfterEndLocation | CGGradientDrawingOptions.DrawsBeforeStartLocation);
            }
            else if (_paint.PaintType == EWPaintType.RADIAL_GRADIENT)
            {
                float vDistance = GetDistance(_gradientStart, _gradientEnd);
                _context.DrawRadialGradient(_gradient, _radialFocalPoint, 0, _gradientStart, vDistance,
                    CGGradientDrawingOptions.DrawsBeforeStartLocation | CGGradientDrawingOptions.DrawsAfterEndLocation);
            }

            _gradient.Dispose();
            _gradient = null;
            _paint = null;
            //Logger.Debug("Gradient Painted and Cleared");
        }

        private static float GetDistance(CGPoint point1, CGPoint point2)
        {
            var a = point2.X - point1.X;
            var b = point2.Y - point1.Y;

            return (float) Math.Sqrt(a * a + b * b);
        }

        private void DrawPatternCallback(CGContext context, EWPattern fillPattern, object currentFigure)
        {
            if (fillPattern != null)
            {
                context.SetLineDash(0, EmptyNFloatArray);

                if (_fillPatternCanvas == null)
                    _fillPatternCanvas = new CGCanvas(_getColorspace);

                _fillPatternCanvas.PixelShifted = PixelShifted;
                _fillPatternCanvas.Context = context;

                if (currentFigure != null)
                    _fillPatternCanvas.StartFigure(currentFigure);

                fillPattern.Draw(_fillPatternCanvas);

                if (currentFigure != null)
                    _fillPatternCanvas.EndFigure();
            }
        }

        private void DrawImageCallback(CGContext context)
        {
#if MONOMAC
			var nativeWrapper = _fillImage as MMImage;
#else
            var nativeWrapper = _fillImage as MTImage;
#endif

            var nativeImage = nativeWrapper?.NativeImage;
            if (nativeImage != null)
            {
                var rect = new CGRect
                {
                    Width = nativeWrapper.Width, 
                    Height = nativeWrapper.Height
                };
#if MONOMAC
				var cgimage = nativeImage.AsCGImage (ref rect, null, null);
#else
                var cgimage = nativeImage.CGImage;
#endif
                context.TranslateCTM(0, rect.Height);
                context.ScaleCTM(1, -1);
                context.DrawImage(rect, cgimage);
                context.ScaleCTM(1, -1);
                context.TranslateCTM(0, -rect.Height);
            }
        }

        public override void DrawImage(EWImage image, float x, float y, float width, float height)
        {
#if MONOMAC
			var nativeWrapper = image as MMImage;
#else
            var nativeWrapper = image as MTImage;
#endif

            var nativeImage = nativeWrapper?.NativeImage;
            if (nativeImage != null)
            {
                _rect.X = x;
                _rect.Y = -y;
                _rect.Width = width;
                _rect.Height = height;

                var cgimage = nativeImage.CGImage;
                _context.SaveState();
                _context.ScaleCTM(1, -1);
                _context.TranslateCTM(0, -_rect.Height);
                _context.DrawImage(_rect, cgimage);
                _context.RestoreState();
            }
        }

        public override void FillRectangle(float x, float y, float width, float height)
        {
            _rect.X = x;
            _rect.Y = y;
            _rect.Width = width;
            _rect.Height = height;

            if (_gradient != null)
            {
                FillWithGradient(
                    () =>
                    {
                        _context.AddRect(_rect);
                        return true;
                    });
            }
            else if (_fillPattern != null)
            {
                FillWithPattern(x, y, () => _context.FillRect(_rect));
            }
            else if (_fillImage != null)
            {
                FillWithImage(x, y, () => _context.FillRect(_rect));
            }
            else
            {
                _context.FillRect(_rect);
            }
        }

        private void FillWithPattern(nfloat x, nfloat y, Action drawingAction)
        {
            _context.SaveState();
            var baseColorspace = _getColorspace?.Invoke();
            var colorspace = CGColorSpace.CreatePattern(baseColorspace);
            _context.SetFillColorSpace(colorspace);

            _fillPatternRect.X = 0;
            _fillPatternRect.Y = 0;
            _fillPatternRect.Width = _fillPattern.Width;
            _fillPatternRect.Height = _fillPattern.Height;

            var currentTransform = CurrentState.Transform.AsCGAffineTransform();
            var transform = CGAffineTransform.MakeTranslation(x, y);
            transform.Multiply(currentTransform);
#if MONOMAC
            transform.Multiply(FlipTransform);
#endif

            // Note: We need to create a local variable for the pattern, and send that to the callback to be drawn because when
            // creating PDF documents, the callback is called with the PDF context is closed, not immediately as when rendering
            // to the screen.
            var patternToDraw = _fillPattern;
            var currentFigure = CurrentFigure;
            var pattern = new CGPattern(_fillPatternRect, transform, _fillPattern.StepX, _fillPattern.StepY, CGPatternTiling.ConstantSpacing, true,
                (handle) => DrawPatternCallback(handle, patternToDraw, currentFigure));
            _context.SetFillPattern(pattern, new nfloat[] {1});
            drawingAction();

            // Dispose of the patterns and it's colorspace
            pattern.Dispose();
            colorspace.Dispose();
            _context.RestoreState();
        }

        private void FillWithImage(nfloat x, nfloat y, Action drawingAction)
        {
            _context.SaveState();
            var baseColorspace = _getColorspace?.Invoke();
            var colorspace = CGColorSpace.CreatePattern(baseColorspace);
            _context.SetFillColorSpace(colorspace);

            _fillPatternRect.X = 0;
            _fillPatternRect.Y = 0;
            _fillPatternRect.Width = _fillImage.Width;
            _fillPatternRect.Height = _fillImage.Height;

            var currentTransform = CurrentState.Transform.AsCGAffineTransform();
            var transform = CGAffineTransform.MakeTranslation(x, y);
            transform.Multiply(currentTransform);
            transform.Multiply(new CGAffineTransform(1.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f));

            var pattern = new CGPattern(_fillPatternRect, transform, _fillImage.Width, _fillImage.Height, CGPatternTiling.NoDistortion, true, DrawImageCallback);
            _context.SetFillPattern(pattern, new nfloat[] {1});
            drawingAction();

            // Dispose of the patterns and it's colorspace
            pattern.Dispose();
            colorspace.Dispose();
            _context.RestoreState();
        }

        protected override void NativeDrawRoundedRectangle(float x, float y, float width, float height, float cornerRadius)
        {
            var strokeLocation = CurrentState.StrokeLocation;
            if (strokeLocation == EWStrokeLocation.CENTER)
            {
                _context.AddRoundedRectangle(x, y, width, height, cornerRadius);
            }
            else if (strokeLocation == EWStrokeLocation.INSIDE)
            {
                var strokeSize = CurrentState.StrokeSize;
                _rect.X = x + (strokeSize / 2);
                _rect.Y = y + (strokeSize / 2);
                _rect.Width = width - strokeSize;
                _rect.Height = height - strokeSize;

                _context.AddRoundedRectangle(_rect, cornerRadius - (strokeSize / 2));
            }
            else if (strokeLocation == EWStrokeLocation.OUTSIDE)
            {
                var strokeSize = CurrentState.StrokeSize;
                _rect.X = x - (strokeSize / 2);
                _rect.Y = y - (strokeSize / 2);
                _rect.Width = width + strokeSize;
                _rect.Height = height + strokeSize;

                _context.AddRoundedRectangle(_rect, cornerRadius + (strokeSize / 2));
            }

            _context.DrawPath(CGPathDrawingMode.Stroke);
        }

        public override void FillRoundedRectangle(float x, float y, float width, float height, float cornerRadius)
        {
            var halfHeight = Math.Abs(height / 2);
            if (cornerRadius > halfHeight)
            {
                cornerRadius = halfHeight;
            }

            var halfWidth = Math.Abs(width / 2);
            if (cornerRadius > halfWidth)
            {
                cornerRadius = halfWidth;
            }

            if (_gradient != null)
            {
                FillWithGradient(
                    () =>
                    {
                        _context.AddRoundedRectangle(x, y, width, height, cornerRadius);
                        return true;
                    });
            }
            else if (_fillPattern != null)
            {
                _context.AddRoundedRectangle(x, y, width, height, cornerRadius);
                FillWithPattern(x, y, _context.FillPath);
            }
            else if (_fillImage != null)
            {
                _context.AddRoundedRectangle(x, y, width, height, cornerRadius);
                FillWithImage(x, y, _context.FillPath);
            }
            else
            {
                _context.AddRoundedRectangle(x, y, width, height, cornerRadius);
                _context.FillPath();
            }
        }

        protected override void NativeDrawOval(float x, float y, float width, float height)
        {
            var strokeLocation = CurrentState.StrokeLocation;
            if (strokeLocation == EWStrokeLocation.CENTER)
            {
                _rect.X = x;
                _rect.Y = y;
                _rect.Width = width;
                _rect.Height = height;
            }
            else if (strokeLocation == EWStrokeLocation.INSIDE)
            {
                var strokeSize = CurrentState.StrokeSize;
                _rect.X = x + (strokeSize / 2);
                _rect.Y = y + (strokeSize / 2);
                _rect.Width = width - strokeSize;
                _rect.Height = height - strokeSize;
            }
            else if (strokeLocation == EWStrokeLocation.OUTSIDE)
            {
                var strokeSize = CurrentState.StrokeSize;
                _rect.X = x - (strokeSize / 2);
                _rect.Y = y - (strokeSize / 2);
                _rect.Width = width + strokeSize;
                _rect.Height = height + strokeSize;
            }

            _context.StrokeEllipseInRect(_rect);
        }

        public override void FillOval(float x, float y, float width, float height)
        {
            _rect.X = x;
            _rect.Y = y;
            _rect.Width = width;
            _rect.Height = height;

            if (_gradient != null)
            {
                FillWithGradient(
                    () =>
                    {
                        _context.AddEllipseInRect(_rect);
                        return true;
                    });
            }
            else if (_fillPattern != null)
            {
                FillWithPattern(x, y, () => _context.FillEllipseInRect(_rect));
            }
            else if (_fillImage != null)
            {
                FillWithImage(x, y, () => _context.FillEllipseInRect(_rect));
            }
            else
            {
                _context.FillEllipseInRect(_rect);
            }
        }

        public override void SubtractFromClip(float x, float y, float width, float height)
        {
            var orginalClip = _context.GetClipBoundingBox();
            var innerClip = new CGRect(x, y, width, height);

            var clip = new CGPath();
            clip.AddRect(orginalClip);
            clip.AddRect(innerClip);

            _context.AddPath(clip);
            _context.EOClip();
            clip.Dispose();
        }
        
        private CGPath GetNativePath(EWPath path)
        {
            var nativePath = path.NativePath as CGPath;

            if (nativePath == null || nativePath.Handle == IntPtr.Zero)
            {
                nativePath = path.AsCGPath();
                path.NativePath = nativePath;
            }

            return nativePath;
        }

        protected override void NativeDrawPath(EWPath path)
        {
            var nativePath = GetNativePath(path);

            var strokeLocation = CurrentState.StrokeLocation;
            if (strokeLocation == EWStrokeLocation.CENTER)
            {
                _context.AddPath(nativePath);
                _context.DrawPath(CGPathDrawingMode.Stroke);
            }
            else if (strokeLocation == EWStrokeLocation.INSIDE)
            {
                _context.SaveState();
                _context.AddPath(nativePath);
                _context.Clip();
                _context.SetLineWidth(CurrentState.StrokeSize * 2);
                _context.AddPath(nativePath);
                _context.DrawPath(CGPathDrawingMode.Stroke);
                _context.RestoreState();
            }
            else if (strokeLocation == EWStrokeLocation.OUTSIDE)
            {
                CGRect vOrigClip = _context.GetClipBoundingBox();

                var vClippingPath = new CGPath();
                vClippingPath.AddRect(vOrigClip);
                vClippingPath.AddPath(nativePath);

                _context.SaveState();
                _context.AddPath(vClippingPath);
                _context.EOClip();

                _context.SetLineWidth(CurrentState.StrokeSize * 2);
                _context.AddPath(nativePath);
                _context.DrawPath(CGPathDrawingMode.Stroke);
                _context.RestoreState();

                vClippingPath.Dispose();
            }
        }

        public override void ClipPath(EWPath path, EWWindingMode windingMode = EWWindingMode.NonZero)
        {
            var nativePath = GetNativePath(path);
            _context.AddPath(nativePath);

            if (windingMode == EWWindingMode.EvenOdd)
            {
                _context.EOClip();
            }
            else
            {
                _context.Clip();
            }
        }

        public override void ClipRectangle(
            float x,
            float y,
            float width,
            float height)
        {
            _rect.X = x;
            _rect.Y = y;
            _rect.Width = width;
            _rect.Height = height;

            _context.AddRect(_rect);
            _context.Clip();
        }

        public override void FillPath(EWPath path, EWWindingMode windingMode)
        {
            var nativePath = GetNativePath(path);

            if (_gradient != null)
            {
                FillWithGradient(
                    () =>
                    {
                        _context.AddPath(nativePath);
                        if (windingMode == EWWindingMode.EvenOdd)
                        {
                            _context.EOClip();
                            return false;
                        }

                        return true;
                    });
            }
            else if (_fillPattern != null)
            {
                var boundingBox = nativePath.PathBoundingBox;
                var x = boundingBox.Left;
                var y = boundingBox.Top;

                _context.AddPath(nativePath);
                if (windingMode == EWWindingMode.EvenOdd)
                {
                    FillWithPattern(x, y, _context.EOFillPath);
                }
                else
                {
                    FillWithPattern(x, y, _context.FillPath);
                }
            }
            else if (_fillImage != null)
            {
                var boundingBox = nativePath.PathBoundingBox;
                var x = boundingBox.Left;
                var y = boundingBox.Top;

                _context.AddPath(nativePath);
                if (windingMode == EWWindingMode.EvenOdd)
                {
                    FillWithImage(x, y, _context.EOFillPath);
                }
                else
                {
                    FillWithImage(x, y, _context.FillPath);
                }
            }
            else
            {
                _context.AddPath(nativePath);
                if (windingMode == EWWindingMode.EvenOdd)
                {
                    _context.EOFillPath();
                }
                else
                {
                    _context.FillPath();
                }
            }
        }

        public override void DrawString(
            string value,
            float x,
            float y,
            EwHorizontalAlignment horizontalAlignment)
        {
            if (_fontName != null && _fontName[0] == '.')
                _fontName = "Helvetica";

            if (horizontalAlignment == EwHorizontalAlignment.Left)
            {
                DrawString(value, x, y);
            }
            else if (horizontalAlignment == EwHorizontalAlignment.Right)
            {
                var size = GraphicsPlatform.CurrentService.GetStringSize(value, _fontName, _fontSize);
                x -= size.Width;
                DrawString(value, x, y);
            }
            else
            {
                var size = GraphicsPlatform.CurrentService.GetStringSize(value, _fontName, _fontSize);
                x -= (size.Width / 2f);
                DrawString(value, x, y);
            }
        }

        private void DrawString(string value, float x, float y)
        {
            _context.SetFillColor(_fontColor);
            _context.SelectFont(_fontName, _fontSize, CGTextEncoding.MacRoman);
            _context.SetTextDrawingMode(CGTextDrawingMode.Fill);
            _context.TextMatrix = FlipTransform;
            _context.ShowTextAtPoint(x, y, value);
        }

        public override void DrawString(
            string value,
            float x,
            float y,
            float width,
            float height,
            EwHorizontalAlignment horizontalAlignment,
            EwVerticalAlignment verticalAlignment,
            EWTextFlow textFlow = EWTextFlow.CLIP_BOUNDS,
            float lineSpacingAdjustment = 0)
        {
            if (width == 0 || height == 0 || string.IsNullOrEmpty(value))
            {
                return;
            }

            // Initialize a rectangular path.
            var path = new CGPath();
            float rx = x;
            float ry = -y;
            float rw = width;
            float rh = height;
            var rect = new CGRect(rx, ry, rw, rh);
            path.AddRect(rect);

#if DEBUG_FONT
            _context.SaveState();
            _context.TranslateCTM(0, rect.Height);
            _context.ScaleCTM(1, -1f);
            _context.SetStrokeColor(StandardColors.Blue.AsCGColor());
            _context.SetLineWidth(1);
            _context.AddPath(path);
            _context.StrokePath();
            _context.RestoreState();            
            
            Logger.Debug("[x:{1:0.000},y:{2:0.000}] [w:{3:0.000},h:{4:0.000}] {0}",value,rx,ry,rw,rh);
#endif

            _context.SaveState();
            DrawStringInNativePath(path, value, horizontalAlignment, verticalAlignment, textFlow, _context, _fontName, _fontSize, _fontColor, 0, 0);
            _context.RestoreState();
            path.Dispose();
        }

        public override void DrawString(EWPath path,
            string value,
            EwHorizontalAlignment horizontalAlignment,
            EwVerticalAlignment verticalAlignment,
            EWTextFlow textFlow = EWTextFlow.CLIP_BOUNDS,
            float lineSpacingAdjustment = 0)
        {
            float fx = 1;
            float fy = 1;

            var nativePath = _context.AddPath(path, 0,0, fx, fy);
            DrawStringInNativePath(nativePath, value, horizontalAlignment, verticalAlignment, textFlow, _context, _fontName, _fontSize, _fontColor, 0, 0);
            nativePath.Dispose();
        }

        public override void DrawText(IAttributedText value, float x, float y, float width, float height)
        {
            var rect = new CGRect(x, y, width, height);
            DrawAttributedText(_context, value, rect, _fontName, _fontSize, _fontColor);
        }

        private void DrawStringInNativePath(
            CGPath path,
            string value,
            EwHorizontalAlignment horizontalAlignment,
            EwVerticalAlignment verticalAlignment,
            EWTextFlow textFlow,
            CGContext context,
            String fontName,
            float fontSize,
            EWColor fontColor,
            float ix,
            float iy)
        {
            var rect = path.PathBoundingBox;

            context.SaveState();
            context.TranslateCTM(0, rect.Height);
            context.ScaleCTM(1, -1f);

            context.TextMatrix = CGAffineTransform.MakeIdentity();
            context.TextMatrix.Translate(ix, iy);

            var attributedString = new NSMutableAttributedString(value);

            var attributes = new CTStringAttributes();

            // Create a color and add it as an attribute to the string.
            attributes.ForegroundColor = new CGColor(fontColor.Red, fontColor.Green, fontColor.Blue, fontColor.Alpha);

            // Load the font
#if MONOMAC
            var font = MMFontService.Instance.LoadFont(fontName, fontSize);
#else
            var font = MTFontService.Instance.LoadFont(fontName, fontSize);
#endif
            if (font != null && font.Handle != IntPtr.Zero)
                attributes.Font = font;

            if (verticalAlignment ==  EwVerticalAlignment.Center)
            {
               iy += -(float)(font.DescentMetric / 2);
            }
            else if (verticalAlignment == EwVerticalAlignment.Bottom)
            {
                iy += -(float)(font.DescentMetric);
            }

            // Set the horizontal alignment
            var paragraphSettings = new CTParagraphStyleSettings();
            switch (horizontalAlignment)
            {
                case EwHorizontalAlignment.Left:
                    paragraphSettings.Alignment = CTTextAlignment.Left;
                    break;
                case EwHorizontalAlignment.Center:
                    paragraphSettings.Alignment = CTTextAlignment.Center;
                    break;
                case EwHorizontalAlignment.Right:
                    paragraphSettings.Alignment = CTTextAlignment.Right;
                    break;
                case EwHorizontalAlignment.Justified:
                    paragraphSettings.Alignment = CTTextAlignment.Justified;
                    break;
            }

            var paragraphStyle = new CTParagraphStyle(paragraphSettings);
            attributes.ParagraphStyle = paragraphStyle;

            // Set the attributes for the complete length of the string
            attributedString.SetAttributes(attributes, new NSRange(0, value.Length));

            // Create the framesetter with the attributed string.
            var frameSetter = new CTFramesetter(attributedString);

            // Create the frame and draw it into the graphics context
            var frame = frameSetter.GetFrame(new NSRange(0, 0), path, null);

            if (frame != null)
            {
                if (verticalAlignment != EwVerticalAlignment.Top)
                {
#if MONOMAC
                    var textFrameSize = MMGraphicsService.GetTextSize(frame);
#else
                    var textFrameSize = MTGraphicsService.GetTextSize(frame);
#endif

                    if (textFrameSize.Height > 0)
                    {
                        if (verticalAlignment == EwVerticalAlignment.Bottom)
                        {
                            var dy = rect.Height - textFrameSize.Height + iy;
                            context.TranslateCTM(-ix, -dy);
                        }
                        else
                        {
                            var dy = (rect.Height - textFrameSize.Height) / 2 + iy;
                            context.TranslateCTM(-ix, -dy);
                        }
                    }
                }
                else
                {
                    context.TranslateCTM(-ix, -iy);
                }

                frame.Draw(context);
                frame.Dispose();
            }

            frameSetter.Dispose();
            attributedString.Dispose();
            paragraphStyle.Dispose();
            //font.Dispose();
            path.Dispose();

            context.RestoreState();
        }

        public static void DrawAttributedText(
            CGContext context,
            IAttributedText text,
            CGRect rect,
            string fontName,
            float fontSize,
            EWColor fontColor,
            EWTextFlow textFlow = EWTextFlow.CLIP_BOUNDS,
            float ix = 0,
            float iy = 0)
        {
            var path = new CGPath();
            path.AddRect(rect);
            DrawAttributedText(context, text, path, fontName, fontSize, fontColor, textFlow, ix, iy);
            path.Dispose();
        }

        public static void DrawAttributedText(
            CGContext context,
            IAttributedText text,
            CGPath path,
            string fontName,
            float fontSize,
            EWColor fontColor,
            EWTextFlow textFlow = EWTextFlow.CLIP_BOUNDS,
            float ix = 0,
            float iy = 0)
        {
            var rect = path.PathBoundingBox;
            var verticalAlignment = EwVerticalAlignment.Top;

            context.SaveState();
            context.TranslateCTM(0, rect.Height);
            context.ScaleCTM(1, -1f);
            context.TranslateCTM(0, rect.GetMinY() * -2);

            context.TextMatrix = CGAffineTransform.MakeIdentity();
            context.TextMatrix.Translate(ix, iy);

            var attributedString = text.AsNSAttributedString(fontName, fontSize, fontColor?.ToHexString(), true);
            if (attributedString == null)
                return;

            // Create the frame setter with the attributed string.
            var framesetter = new CTFramesetter(attributedString);

            // Create the frame and draw it into the graphics context
            var frame = framesetter.GetFrame(new NSRange(0, 0), path, null);

            if (frame != null)
            {
                if (verticalAlignment != EwVerticalAlignment.Top)
                {
#if MONOMAC
					var textSize = MMGraphicsService.GetTextSize(frame);
#else
                    var textSize = MTGraphicsService.GetTextSize(frame);
#endif

                    if (textSize.Height > 0)
                    {
                        if (verticalAlignment == EwVerticalAlignment.Bottom)
                        {
                            var dy = rect.Height - textSize.Height + iy;
                            context.TranslateCTM(-ix, -dy);
                        }
                        else
                        {
                            var dy = (rect.Height - textSize.Height) / 2 + iy;
                            context.TranslateCTM(-ix, -dy);
                        }
                    }
                }
                else
                {
                    context.TranslateCTM(-ix, -iy);
                }

                frame.Draw(context);
                frame.Dispose();
            }

            framesetter.Dispose();
            attributedString.Dispose();
            context.RestoreState();
        }

        public override void SetShadow(EWSize offset, float blur, EWColor color, float zoom)
        {
            var actualOffset = offset ?? DefaultShadowOffset;

            var sizeF = actualOffset.AsSizeF();
            sizeF.Width *= zoom;
            sizeF.Height *= zoom;

#if MONOMAC
			sizeF.Height *= -1;
#endif

            var actualBlur = (blur * zoom);

            if (color == null)
            {
                _context.SetShadow(sizeF, actualBlur);
            }
            else
            {
                _context.SetShadow(sizeF, actualBlur, new CGColor(color.Red, color.Green, color.Blue, color.Alpha));
            }

            CurrentState.Shadowed = true;
        }

        protected override void NativeRotate(float degrees, float radians, float x, float y)
        {
            _context.TranslateCTM(x, y);
            _context.RotateCTM(radians);
            _context.TranslateCTM(-x, -y);
        }

        protected override void NativeRotate(float degrees, float radians)
        {
            _context.RotateCTM(radians);
        }

        protected override void NativeScale(float sx, float sy)
        {
            _context.ScaleCTM(sx, sy);
        }

        protected override void NativeTranslate(float tx, float ty)
        {
            _context.TranslateCTM(tx, ty);
        }

        protected override void NativeConcatenateTransform(EWAffineTransform transform)
        {
            _context.ConcatCTM(transform.AsCGAffineTransform());
        }

        public override void SaveState()
        {
            base.SaveState();
            _context.SaveState();
        }

        public override void ResetState()
        {
            base.ResetState();

            _gradient = null;
            _fillPattern = null;
            _fillImage = null;
            _paint = null;

            _fontColor = StandardColors.Black;
        }

        public override bool RestoreState()
        {
            var success = base.RestoreState();

            if (_gradient != null)
            {
                _gradient.Dispose();
                _gradient = null;
            }

            _fillPattern = null;
            _fillImage = null;
            _paint = null;
            _context.RestoreState();

            return success;
        }
    }

    public static class CgContextExtensions
    {
        public static CGPath AddPath(this CGContext target, EWPath path, float ox, float oy, float fx, float fy)
        {
            var nativePath = path.AsCGPath(ox, oy, fx, fy);
            target.AddPath(nativePath);
            return nativePath;
        }

        public static void AddRoundedRectangle(this CGContext target, CGRect rect, float cornerRadius)
        {
            AddRoundedRectangle(target, rect.X, rect.Y, rect.Width, rect.Height, cornerRadius);
        }

        public static void AddRoundedRectangle(
            this CGContext target,
            nfloat x,
            nfloat y,
            nfloat width,
            nfloat height,
            nfloat cornerRadius)
        {
            var finalCornerRadius = cornerRadius;

            var rect = new CGRect(x, y, width, height);

            if (finalCornerRadius > rect.Width)
                finalCornerRadius = rect.Width / 2;

            if (finalCornerRadius > rect.Height)
                finalCornerRadius = rect.Height / 2;

            var minx = rect.X;
            var miny = rect.Y;
            var maxx = minx + rect.Width;
            var maxy = miny + rect.Height;
            var midx = minx + (rect.Width / 2);
            var midy = miny + (rect.Height / 2);

            target.MoveTo(minx, midy);
            target.AddArcToPoint(minx, miny, midx, miny, finalCornerRadius);
            target.AddArcToPoint(maxx, miny, maxx, midy, finalCornerRadius);
            target.AddArcToPoint(maxx, maxy, midx, maxy, finalCornerRadius);
            target.AddArcToPoint(minx, maxy, minx, midy, finalCornerRadius);
            target.ClosePath();
        }
    }
}