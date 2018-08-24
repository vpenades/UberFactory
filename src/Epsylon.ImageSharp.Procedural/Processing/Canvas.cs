using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


namespace Epsylon.ImageSharp.Procedural.Processing
{
    using COLOR = Rgba32;
    using IMAGE = Image<Rgba32>;

    // https://www.processing.org/
    // http://processingjs.org/
    // https://github.com/processing/processing
    // https://github.com/jeresig/processing-js
    // https://johnresig.com/blog/processingjs/

    public class Canvas
    {
        #region lifecycle

        public Canvas(IMAGE target, int seed)
        {
            _Canvas = target;
            _Randomizer = new Random(seed);

            var mode = PixelColorBlendingMode.Normal;

            // _Blender = PixelOperations<Rgba32>.Instance.GetPixelBlender(mode);
            _GfxMode = new GraphicsOptions(true, mode, 1);
        }

        #endregion

        #region data

        private readonly Random _Randomizer;
        private readonly IMAGE _Canvas;

        // most probably, having a direct "pixel blender" will be much faster than doing per pixel mutates.
        // private readonly PixelBlender<TPixel> _Blender;
        private readonly GraphicsOptions _GfxMode;

        #endregion

        #region properties

        public int Width => _Canvas.Width;
        public int Height => _Canvas.Height;

        #endregion

        #region API

        protected int NextRandomInt(int max) { return _Randomizer.Next(max); }

        protected float NextRandomFloat(float min, float max)
        {
            var k = _Randomizer.NextDouble();

            return (float)(k * (max - min) + min);
        }

        protected void DrawPoint(float x, float y, COLOR color)
        {
            _Canvas.DrawPixel((int)x, (int)y, color, _GfxMode);
        }        

        #endregion
    }
}
