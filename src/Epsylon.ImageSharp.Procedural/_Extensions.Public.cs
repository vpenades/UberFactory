﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    using V2 = System.Numerics.Vector2;
    using V3 = System.Numerics.Vector3;
    using V4 = System.Numerics.Vector4;

    public static class _PublicExtensions
    {
        private static readonly V2 V2HALF = V2.One / 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<int, int> GetFunction(this SamplerAddressMode mode, int size)
        {
            switch (mode)
            {
                case SamplerAddressMode.Clamp: return idx => idx.Clamp(0, size - 1);
                case SamplerAddressMode.Wrap: return idx => idx.Wrap(size);

                default: throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFinalIndex(this SamplerAddressMode mode, int index, int size)
        {
            switch (mode)
            {
                case SamplerAddressMode.Clamp: return index.Clamp(0, size - 1);
                case SamplerAddressMode.Wrap: return index.Wrap(size);

                default: throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int _GetFinalIndex(int index, int size, SamplerAddressMode mode)
        {
            switch (mode)
            {
                case SamplerAddressMode.Clamp: return index.Clamp(0, size - 1);
                case SamplerAddressMode.Wrap: return index.Wrap(size);

                default: throw new NotImplementedException();
            }
        }

        public static TPixel GetSample<TPixel>(this Image<TPixel> image, V2 p, SamplerAddressMode u = SamplerAddressMode.Wrap, SamplerAddressMode v = SamplerAddressMode.Wrap) where TPixel : struct, IPixel<TPixel>
        {
            p -= V2HALF;

            int ix = p.X.RoundedTextureCoordinate();
            int iy = p.Y.RoundedTextureCoordinate();

            int w = image.Width;
            int h = image.Height;

            ix = _GetFinalIndex(ix, w, u);
            iy = _GetFinalIndex(iy, h, v);

            var jx = _GetFinalIndex(ix + 1, w, u);
            var jy = _GetFinalIndex(iy + 1, h, v);

            var A = image[ix, iy].ToVector4();
            var B = image[jx, iy].ToVector4();
            var C = image[ix, jy].ToVector4();
            var D = image[jx, jy].ToVector4();

            p.X -= ix;
            p.Y -= iy;

            A = A.AlphaAwareLerp(B, p.X); // first row
            C = C.AlphaAwareLerp(D, p.X); // second row
            A = A.AlphaAwareLerp(C, p.Y); // column

            var r = default(TPixel);
            r.PackFromVector4(A);

            return r;
        }

        /// <summary>
        /// Vector4 LERP method that interprets the W element as Alpha transparency, and uses it to weight the LERP result.
        /// </summary>
        /// <param name="a">first color value</param>
        /// <param name="b">second color value</param>
        /// <param name="amountOfB">weight of b</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V4 AlphaAwareLerp(this V4 a, V4 b, float amountOfB)
        {
            System.Diagnostics.Debug.Assert(amountOfB >= 0 && amountOfB <= 1);

            var amountOfA = 1.0f - amountOfB;

            if (a.W == 0) return new V4(b.X, b.Y, b.Z, b.W * amountOfB);
            if (b.W == 0) return new V4(a.X, a.Y, a.Z, a.W * amountOfA);

            return a * amountOfA + b * amountOfB;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundedTextureCoordinate(this float v)
        {
            // standard Math.Floor rounds to the integer closest to ZERO, but for texture sampling, we need to round to the "immediate left" integer (1.3 = 1;  -2.3 = -3)

            int i = (int)v; return (float)i <= v ? i : i - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Wrap(this int idx, int count) { return idx >= 0 ? idx % count : count - ((-idx - 1) % count) - 1; }

        private sealed class _ActionProcessor<TPixel> : IImageProcessor<TPixel> where TPixel : struct, IPixel<TPixel>
        {
            public static _ActionProcessor<TPixel> Create(Action<Image<TPixel>> action) { return new _ActionProcessor<TPixel>(action); }

            private _ActionProcessor(Action<Image<TPixel>> action) { _Action = action; }

            private readonly Action<Image<TPixel>> _Action;

            public void Apply(Image<TPixel> source, Rectangle sourceRectangle)
            {
                _Action?.Invoke(source);
            }
        }

        public static IImageProcessingContext<TPixel> ApplyProcessor<TPixel>(this IImageProcessingContext<TPixel> source, Action<Image<TPixel>> action) where TPixel : struct, IPixel<TPixel>
        {
            var processor = _ActionProcessor<TPixel>.Create(action);

            return source.ApplyProcessor(processor);
        }

        public static ITexture ToTexture<TPixel>(this Image<TPixel> source) where TPixel : struct, IPixel<TPixel>
        {
            return new _ImageTexture<TPixel>(source);
        }

        public static ITexture ToTexture<TPixel>(this Image<TPixel> source, Image<Alpha8> mask) where TPixel : struct, IPixel<TPixel>
        {
            return new _ImageMaskedTexture<TPixel>(source, mask);
        }

        public static bool Contains<TPixel>(this Image<TPixel> image, int x, int y) where TPixel : struct, IPixel<TPixel>
        {
            if (image == null) return false;
            if (x < 0 || y < 0) return false;
            if (x >= image.Width || y >= image.Height) return false;
            return true;
        }

        public static bool Contains<TPixel>(this Image<TPixel> image, Point p) where TPixel : struct, IPixel<TPixel>
        {
            return image.Contains(p.X, p.Y);
        }

        public static bool Contains(this ITexture image, int x, int y)
        {
            if (image == null) return false;
            if (x < 0 || y < 0) return false;
            if (x >= image.Width || y >= image.Height) return false;
            return true;
        }

        public static bool Contains(this ITexture image, Point p)
        {
            return image.Contains(p.X, p.Y);
        }
    }
}