﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Epsylon.UberPlugin
{
    using UberFactory;

    using PIXEL32 = Rgba32;
    using IMAGE32 = Image<Rgba32>;
    using IMAGE32DC = IImageProcessingContext<Rgba32>;
    using IMGTRANSFORM = Action<IImageProcessingContext<Rgba32>>;


    using SIZE = SixLabors.Primitives.Size;
    using POINT = SixLabors.Primitives.Point;
    using RECT = SixLabors.Primitives.Rectangle;


    [SDK.ContentNode("TransformStack")]
    [SDK.Title("Transforms"), SDK.TitleFormat("{0} Transforms")]
    public sealed class TransformStack : ImageFilter
    {
        [SDK.InputNode("Source")]
        [SDK.Title("Source Image")]
        public IMAGE32 Source { get; set; }

        [SDK.InputNode("Transforms",true)]
        [SDK.Title("Transforms")]
        [SDK.ItemsPanel("VerticalList")]
        public IMGTRANSFORM[] Transforms { get; set; }

        protected override IMAGE32 Evaluate()
        {
            if (Source == null) return null;            

            Source.Mutate(dc => _ProcessStack(dc));

            return Source;
        }

        private void _ProcessStack(IMAGE32DC dc)
        {
            foreach (var xform in Transforms)
            {
                xform?.Invoke(dc);
            }
        }        
    }



    public abstract class BaseImageTransform : SDK.ContentFilter<IMGTRANSFORM>
    {
        [SDK.InputValue("Enabled")]
        [SDK.Title("Enabled")]
        [SDK.Default(true)]
        public Boolean Enabled { get; set; }

        // TODO: add boolean to enable TILEABLE mode:
        // before applying the effect, we create a multitile, then apply the effect, then crop back to the original.

        protected override object EvaluatePreview(SDK.PreviewContext previewContext) { return null; }

        protected override IMGTRANSFORM Evaluate()
        {
            if (!Enabled) return dc => { };

            return TransformImage();
        }

        protected abstract IMGTRANSFORM TransformImage();
    }



    [SDK.ContentNode("GlowTransform")]
    [SDK.Title("Glow"), SDK.TitleFormat("{0} with Glow")]
    public sealed class ImageGlowTransform : BaseImageTransform
    {
        [SDK.InputValue("Radius")]
        [SDK.Title("Radius")]
        [SDK.Default(1)]
        public float Radius { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.Glow(this.Radius);
        }
    }    

    

    [SDK.ContentNode("BlurTransform")]
    [SDK.Title("Blur"), SDK.TitleFormat("{0} Blurred")]
    public sealed class ImageBlurTransform : BaseImageTransform
    {
        public enum BlurMode { Box, Gaussian }

        [SDK.InputValue("Mode")]
        [SDK.Title("Mode")]
        [SDK.Default(BlurMode.Gaussian)]
        public BlurMode Mode { get; set; }

        [SDK.InputValue("Radius")]
        [SDK.Title("Radius")]
        [SDK.Minimum(0), SDK.Default(5)]
        public float Radius { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            if (Mode == BlurMode.Gaussian) return dc => dc.GaussianBlur(this.Radius);
            if (Mode == BlurMode.Box) return dc => dc.BoxBlur((int)Radius);

            throw new NotSupportedException(Mode.ToString());
        }        
    }

    [SDK.ContentNode("DetectEdgeTransform")]
    [SDK.Title("Detect Edges"), SDK.TitleFormat("{0} Edges")]
    public sealed class DetectEdgeTransform : BaseImageTransform
    {
        [SDK.InputValue("Filter")]
        [SDK.Title("Filter")]
        [SDK.Default(SixLabors.ImageSharp.Processing.EdgeDetection.Lapacian5X5)]
        public SixLabors.ImageSharp.Processing.EdgeDetection Filter { get; set; }        

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.DetectEdges(this.Filter);
        }
    }

    [SDK.ContentNode("OilPaintTransform")]
    [SDK.Title("Oil Paint"), SDK.TitleFormat("{0} as Old Paint")]
    public sealed class ImageOilPaintTransform : BaseImageTransform
    {
        [SDK.InputValue("Levels")]
        [SDK.Title("Levels")]
        [SDK.Minimum(1), SDK.Default(10)]
        public int Levels { get; set; }

        [SDK.InputValue("BrushSize")]
        [SDK.Title("Brush Size")]
        [SDK.Minimum(1), SDK.Default(15)]
        public int BrushSize { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.OilPaint(Levels, BrushSize);
        }
    }

    public enum OldPhotoEffect { Kodachrome, Polaroid, Lomograph,Sepia }

    [SDK.ContentNode("OldPhotoTransform")]
    [SDK.Title("Old Photo"), SDK.TitleFormat("{0} as Old Photo")]
    public sealed class ImageOldPhotoTransform : BaseImageTransform
    {
        [SDK.InputValue("Effect")]
        [SDK.Title("Camera Effect")]
        [SDK.Default(OldPhotoEffect.Kodachrome)]
        public OldPhotoEffect Effect { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            if (Effect == OldPhotoEffect.Lomograph) return dc => dc.Lomograph();
            if (Effect == OldPhotoEffect.Kodachrome) return dc => dc.Kodachrome();
            if (Effect == OldPhotoEffect.Polaroid) return dc => dc.Polaroid();
            if (Effect == OldPhotoEffect.Sepia) return dc => dc.Sepia();            
            throw new NotSupportedException(Effect.ToString());
        }
    }

    

    [SDK.ContentNode("InvertTransform")]
    [SDK.Title("Invert"), SDK.TitleFormat("{0} Inverted")]
    public sealed class ImageInvertTransform : BaseImageTransform
    {
        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.Invert();            
        }
    }

    [SDK.ContentNode("HueTransform")]
    [SDK.Title("Hue"), SDK.TitleFormat("{0} Hued")]
    public sealed class ImageHueTransform : BaseImageTransform
    {
        [SDK.InputValue("Degrees")]
        [SDK.Title("Degrees")]
        [SDK.Default(0)]
        public float Degrees { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.Hue(Degrees);
        }
    }

    [SDK.ContentNode("CropTransform")]
    [SDK.Title("Crop"), SDK.TitleFormat("{0} Crop")]
    public sealed class ImageCropTransform : BaseImageTransform
    {
        [SDK.InputValue("OriginX")]
        [SDK.Title("X"), SDK.Group("Origin")]
        public int OriginX { get; set; }

        [SDK.InputValue("OriginY")]
        [SDK.Title("Y"), SDK.Group("Origin")]
        public int OriginY { get; set; }

        [SDK.InputValue("Width")]
        [SDK.Title("W"), SDK.Group("Size")]
        [SDK.Minimum(1), SDK.Default(256)]
        public int Width { get; set; }

        [SDK.InputValue("Height")]
        [SDK.Title("H"), SDK.Group("Size")]
        [SDK.Minimum(1), SDK.Default(256)]
        public int Height { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            var rect = new RECT(OriginX, OriginY, Width, Height);
            return dc => dc.Crop(rect);
        }
    }

    

    [SDK.ContentNode("ResizeTransform")]
    [SDK.Title("Resize"), SDK.TitleFormat("{0} Resized")]
    public sealed class ImageResizeTransform : BaseImageTransform
    {
        [SDK.InputValue("Width")]
        [SDK.Title("W"), SDK.Group("Size")]
        [SDK.Minimum(1), SDK.Default(256)]
        public int Width { get; set; }

        [SDK.InputValue("Height")]
        [SDK.Title("H"), SDK.Group("Size")]
        [SDK.Minimum(1), SDK.Default(256)]
        public int Height { get; set; }

        [SDK.InputValue("Resampler")]
        [SDK.Title("Mode"), SDK.Group("Size")]
        [SDK.Default(Resampler.Bicubic)]
        public Resampler Resampler { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.Resize(Width, Height, Resampler.GetInstance());
        }
               
    }

    


    [SDK.ContentNode("SpecialEffectsTransform")]
    [SDK.Title("Photoshop Effects"), SDK.TitleFormat("{0} Effects")]
    public sealed class SpecialEffectsTransform : BaseImageTransform
    {
        #region drop shadow

        [SDK.InputValue("EnableDropShadow")]
        [SDK.Title(""), SDK.Group("Drop Shadow")]        
        public bool EnableDropShadow { get; set; }

        [SDK.InputValue("DropShadowColor")]
        [SDK.Title("Title"), SDK.Group("Drop Shadow")]
        [SDK.Default((UInt32)0xff000000)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 DropShadowColor { get; set; }

        [SDK.InputValue("DropShadowOpacity")]
        [SDK.Title("Opacity"), SDK.Group("Drop Shadow")]
        [SDK.Minimum(0), SDK.Default(75), SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        public int DropShadowOpacity { get; set; }

        #endregion

        #region outer shadow

        [SDK.InputValue("EnableOuterGlow")]
        [SDK.Title(""), SDK.Group("Outer Glow")]        
        public bool EnableOuterGlow { get; set; }

        [SDK.InputValue("OuterGlowColor")]
        [SDK.Title(""), SDK.Group("Outer Glow")]
        [SDK.Default((UInt32)0xffff8000)]
        [SDK.ViewStyle("ColorPicker")]
        public UInt32 OuterGlowColor { get; set; }

        [SDK.InputValue("OuterGlowOpacity")]
        [SDK.Title("Opacity"), SDK.Group("Outer Glow")]        
        [SDK.Minimum(0), SDK.Default(75), SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        public int OuterGlowOpacity { get; set; }

        #endregion

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => _ApplyEffects(dc);
        }

        private void _ApplyEffects(IMAGE32DC dc)
        {
            /*
            var newImg = new IMAGE32(img.Width, img.Height);

            if (EnableDropShadow)
            {
                

                var shadow = new IMAGE32(img.Width, img.Height);

                var ShadowColor = new PIXEL32(DropShadowColor);

                shadow.Flatten(img, 0, 0, (b, s, o) => { b = ShadowColor; b.A = s.A; return b; }, 100);

                shadow.GaussianBlur(6);

                newImg.DrawImage(shadow, PixelBlenderMode.Normal, DropShadowOpacity, SIZE.Empty, POINT.Empty);
            }

            if (EnableOuterGlow)
            {
                var glow = new IMAGE32(img.Width, img.Height);

                var glowColor = new PIXEL32(OuterGlowColor);

                glow.Flatten(img, 0, 0, (b, s, o) => { b = glowColor; b.A = s.A; return b; }, 100);

                glow.GaussianBlur(6);



                newImg.DrawImage(glow, PixelBlenderMode.Screen, OuterGlowOpacity, SIZE.Empty, POINT.Empty);
            }



            newImg.DrawImage(img, PixelBlenderMode.Normal, 100, SIZE.Empty, POINT.Empty);

            // inner shadow

            // inner glow: create copy, invert alpha, gauss, clamp alpha, apply

            img.Dispose();

            return newImg;
            */
        }

    }
    
}
