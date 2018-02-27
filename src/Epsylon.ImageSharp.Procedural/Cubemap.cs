using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

// http://paulbourke.net/miscellaneous/cubemaps/

// ONLINE TOOLS:
// http://wwwtyro.github.io/procedural.js/space/
// http://wwwtyro.github.io/
// https://wwwtyro.github.io/space-3d
// https://www.360toolkit.co/convert-cubemap-to-spherical-equirectangular.html


namespace Epsylon.ImageSharp.Procedural
{
    public static class CubemapFactory
    {
        public static CubemapSampler<Vector4> ToSampler(this Cubemap<Rgba32> cubemap)
        {
            var front = cubemap.Front.ToTextureSampler(true);
            var back = cubemap.Back.ToTextureSampler(true);

            var left = cubemap.Left.ToTextureSampler(true);
            var right = cubemap.Right.ToTextureSampler(true);

            var top = cubemap.Top.ToTextureSampler(true);
            var bottom = cubemap.Bottom.ToTextureSampler(true);

            return new CubemapSampler<Vector4>(front, back, left, right, top, bottom);
        }
    }

    struct PixelView<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        private readonly _Bitmap<TPixel> _Bitmap;
        private readonly int _X;
        private readonly int _Y;
        private readonly Vector3 _Point;

        public Vector3 Position => _Point;

        public TPixel Value
        {
            get => _Bitmap[_X, _Y];
            set => _Bitmap[_X, _Y] = value;
        }
    }

    public class Cubemap<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        // TODO: at creation time, ensure map dimensions match

        #region data

        private IBitmap<TPixel> _Front;
        private IBitmap<TPixel> _Back;

        private IBitmap<TPixel> _Left;
        private IBitmap<TPixel> _Right;

        private IBitmap<TPixel> _Top;
        private IBitmap<TPixel> _Bottom;

        #endregion

        #region properties        

        public IBitmap<TPixel> Front => _Front;
        public IBitmap<TPixel> Back => _Back;

        public IBitmap<TPixel> Left => _Left;
        public IBitmap<TPixel> Right => _Right;

        public IBitmap<TPixel> Top => _Top;
        public IBitmap<TPixel> Bottom => _Bottom;

        #endregion

        #region API

        

        #endregion
    }


    public class CubemapSampler<T>
    {
        public CubemapSampler(ITextureSampler<T> f, ITextureSampler<T> b, ITextureSampler<T> l, ITextureSampler<T> r, ITextureSampler<T> top, ITextureSampler<T> bottom)
        {
            _Front = f;
            _Back = b;
            _Left = l;
            _Right = r;
            _Top = top;
            _Bottom = bottom;
        }

        #region data

        private ITextureSampler<T> _Front;
        private ITextureSampler<T> _Back;

        private ITextureSampler<T> _Left;
        private ITextureSampler<T> _Right;

        private ITextureSampler<T> _Top;
        private ITextureSampler<T> _Bottom;

        #endregion        
    }


    
}
