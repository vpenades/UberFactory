using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{

    interface IImagePixelSampler<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        int Width { get; }
        int Height { get; }

        TPixel this[int x, int y] { get; set; }
    }

    sealed class ImageRectangleSampler<TPixel> : IImagePixelSampler<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        public ImageRectangleSampler(Image<TPixel> src, Rectangle rect)
        {
            _Source = src;
            _Rect = rect;
        }

        private readonly Image<TPixel> _Source;
        private readonly Rectangle _Rect;

        public int Width => _Rect.Width;

        public int Height => _Rect.Height;

        public TPixel this[int x, int y]
        {
            get
            {
                if (x < 0) return default(TPixel);
                if (x >= _Rect.Width) return default(TPixel);

                if (y < 0) return default(TPixel);
                if (y >= _Rect.Width) return default(TPixel);

                return _Source[x + _Rect.X, y + _Rect.Y];
            }

            set
            {
                if (x < 0) return;
                if (x >= _Rect.Width) return;

                if (y < 0) return;
                if (y >= _Rect.Width) return;

                _Source[x + _Rect.X, y + _Rect.Y] = value;
            }
        }
    }
    

    sealed class _BleedTransparentColor<TPixel> : IImageProcessor<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        public _BleedTransparentColor(int minIterations, Alpha8 alphaThreshold)
        {
            _MinIterations = minIterations;
            _AlphaThreshold = alphaThreshold;
        }

        private readonly int _MinIterations;
        private readonly Alpha8 _AlphaThreshold;        

        public void Apply(Image<TPixel> source, Rectangle sourceRectangle)
        {
            using (var mask = new Image<Alpha8>(sourceRectangle.Width,sourceRectangle.Height))
            {
                var sampler = new ImageRectangleSampler<TPixel>(source, sourceRectangle);

                for(int y=0; y < sampler.Height; ++y)
                {
                    for(int x=0; x < sampler.Width; ++x)
                    {

                    }
                }


            }            
        }        
    }
}
