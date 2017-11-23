using System;
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


    [SDK.ContentNode("TransformStack")]
    [SDK.ContentMetaData("Title", "Transforms")]
    [SDK.ContentMetaData("TitleFormat", "{0} Transforms")]
    public sealed class TransformStack : SDK.ContentFilter<IMAGE32>
    {
        [SDK.InputNode("Source")]
        [SDK.InputMetaData("Title", "Source Image")]
        public IMAGE32 Source { get; set; }

        [SDK.InputNode("Transforms",true)]
        [SDK.InputMetaData("Title", "Transforms")]
        [SDK.InputMetaData("Panel", "VerticalList")]
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

        protected override object EvaluatePreview(SDK.PreviewContext context) { return Evaluate().CreatePreview(context); }
    }



    public abstract class BaseImageTransform : SDK.ContentFilter<IMGTRANSFORM>
    {
        [SDK.InputValue("Enabled")]
        [SDK.InputMetaData("Title", "Enabled")]
        [SDK.InputMetaData("Default", true)]
        public Boolean Enabled { get; set; }        

        protected override IMGTRANSFORM Evaluate()
        {
            if (!Enabled) return dc => { };

            return TransformImage();
        }

        protected abstract IMGTRANSFORM TransformImage();
    }



    [SDK.ContentNode("GlowTransform")]
    [SDK.ContentMetaData("Title", "Glow")]
    [SDK.ContentMetaData("TitleFormat", "{0} with Glow")]
    public sealed class ImageGlowTransform : BaseImageTransform
    {
        [SDK.InputValue("Radius")]
        [SDK.InputMetaData("Title", "Radius")]
        [SDK.InputMetaData("Default", 1)]
        public float Radius { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.Glow(this.Radius);            
        }
    }

    

    public enum BlurMode { Box,Gaussian }

    [SDK.ContentNode("BlurTransform")]
    [SDK.ContentMetaData("Title", "Blur")]
    [SDK.ContentMetaData("TitleFormat", "{0} Blurred")]
    public sealed class ImageBlurTransform : BaseImageTransform
    {

        [SDK.InputValue("Mode")]
        [SDK.InputMetaData("Title", "Mode")]
        [SDK.InputMetaData("Default", BlurMode.Gaussian)]
        public BlurMode Mode { get; set; }

        [SDK.InputValue("Radius")]
        [SDK.InputMetaData("Title", "Radius")]
        [SDK.InputMetaData("Default", 5)]
        public float Radius { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            if (Mode == BlurMode.Gaussian) return dc => dc.GaussianBlur(this.Radius);
            if (Mode == BlurMode.Box) return dc => dc.BoxBlur((int)Radius);

            throw new NotSupportedException(Mode.ToString());
        }        
    }
    
    [SDK.ContentNode("OilPaintTransform")]
    [SDK.ContentMetaData("Title", "Oil Paint")]
    [SDK.ContentMetaData("TitleFormat", "{0} as Old Paint")]
    public sealed class ImageOilPaintTransform : BaseImageTransform
    {
        [SDK.InputValue("Levels")]
        [SDK.InputMetaData("Title", "Levels")]
        [SDK.InputMetaData("Minimum", 1)]
        [SDK.InputMetaData("Default", 10)]
        public int Levels { get; set; }

        [SDK.InputValue("BrushSize")]
        [SDK.InputMetaData("Title", "Brush Size")]
        [SDK.InputMetaData("Minimum", 1)]
        [SDK.InputMetaData("Default", 15)]
        public int BrushSize { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.OilPaint(Levels, BrushSize);
        }
    }

    public enum OldPhotoEffect { Kodachrome, Polaroid, Lomograph,Sepia }

    [SDK.ContentNode("OldPhotoTransform")]
    [SDK.ContentMetaData("Title", "Old Photo")]
    [SDK.ContentMetaData("TitleFormat", "{0} as Old Photo")]
    public sealed class ImageOldPhotoTransform : BaseImageTransform
    {
        [SDK.InputValue("Effect")]
        [SDK.InputMetaData("Title", "Camera Effect")]
        [SDK.InputMetaData("Default", OldPhotoEffect.Kodachrome)]
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
    [SDK.ContentMetaData("Title", "Invert")]
    [SDK.ContentMetaData("TitleFormat", "{0} Inverted")]
    public sealed class ImageInvertTransform : BaseImageTransform
    {
        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.Invert();            
        }
    }

    [SDK.ContentNode("HueTransform")]
    [SDK.ContentMetaData("Title", "Hue")]
    [SDK.ContentMetaData("TitleFormat", "{0} Hued")]
    public sealed class ImageHueTransform : BaseImageTransform
    {
        [SDK.InputValue("Degrees")]
        [SDK.InputMetaData("Title", "Degrees")]
        [SDK.InputMetaData("Default", 0)]
        public float Degrees { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.Hue(Degrees);
        }
    }




    

    [SDK.ContentNode("ResizeTransform")]
    [SDK.ContentMetaData("Title", "Resize")]
    [SDK.ContentMetaData("TitleFormat", "{0} Resized")]
    public sealed class ImageResizeTransform : BaseImageTransform
    {
        [SDK.InputValue("ResizeMode")]
        [SDK.InputMetaData("Group", "Resize")]
        [SDK.InputMetaData("Title", "Mode")]
        public SixLabors.ImageSharp.Processing.ResizeMode Mode { get; set; }

        [SDK.InputValue("AnchorPosition")]
        [SDK.InputMetaData("Group", "Resize"),SDK.InputMetaData("Title", "Anchor")]
        public SixLabors.ImageSharp.Processing.AnchorPosition Position { get; set; }

        [SDK.InputValue("CenterX")]
        [SDK.InputMetaData("Group", "Center"),SDK.InputMetaData("Title", "X")]
        public float CenterX { get; set; }

        [SDK.InputValue("Center")]
        [SDK.InputMetaData("Group", "Center"),SDK.InputMetaData("Title", "Y")]
        public float CenterY { get; set; }

        [SDK.InputValue("Width")]
        [SDK.InputMetaData("Group", "Size"),SDK.InputMetaData("Title", "W")]
        [SDK.InputMetaData("Minimum",1),SDK.InputMetaData("Default",256)]
        public int Width { get; set; }

        [SDK.InputValue("Height")]
        [SDK.InputMetaData("Group", "Size"),SDK.InputMetaData("Title", "H")]
        [SDK.InputMetaData("Minimum", 1), SDK.InputMetaData("Default", 256)]
        public int Height { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            // support
            // img.EntropyCrop            

            var options = new SixLabors.ImageSharp.Processing.ResizeOptions()
            {
                Mode = this.Mode,
                Position = this.Position,
                CenterCoordinates = new float[] { CenterX, CenterY },
                Size = new SIZE(Width, Height)
            };

            return  dc => dc.Resize(options);
        }
               
    }


    

    [SDK.ContentNode("SpecialEffectsTransform")]
    [SDK.ContentMetaData("Title", "Photoshop Effects")]
    [SDK.ContentMetaData("TitleFormat", "{0} Effects")]
    public sealed class SpecialEffectsTransform : BaseImageTransform
    {
        #region drop shadow

        [SDK.InputValue("EnableDropShadow")]
        [SDK.InputMetaData("Group", "Drop Shadow"), SDK.InputMetaData("Title", "")]        
        public bool EnableDropShadow { get; set; }

        [SDK.InputValue("DropShadowColor")]
        [SDK.InputMetaData("Group", "Drop Shadow"), SDK.InputMetaData("Title", "")]
        [SDK.InputMetaData("Default", (UInt32)0xff000000)]
        [SDK.InputMetaData("ViewStyle", "ColorPicker")]
        public UInt32 DropShadowColor { get; set; }

        [SDK.InputValue("DropShadowOpacity")]
        [SDK.InputMetaData("Group", "Drop Shadow"), SDK.InputMetaData("Title", "Opacity")]
        [SDK.InputMetaData("Minimum", 0), SDK.InputMetaData("Default", 75), SDK.InputMetaData("Maximum", 100)]
        [SDK.InputMetaData("ViewStyle", "Slider")]
        public int DropShadowOpacity { get; set; }

        #endregion

        #region outer shadow

        [SDK.InputValue("EnableOuterGlow")]
        [SDK.InputMetaData("Group","Outer Glow"), SDK.InputMetaData("Title", "")]        
        public bool EnableOuterGlow { get; set; }

        [SDK.InputValue("OuterGlowColor")]
        [SDK.InputMetaData("Group", "Outer Glow"), SDK.InputMetaData("Title", "")]
        [SDK.InputMetaData("Default", (UInt32)0xffff8000)]
        [SDK.InputMetaData("ViewStyle", "ColorPicker")]
        public UInt32 OuterGlowColor { get; set; }

        [SDK.InputValue("OuterGlowOpacity")]
        [SDK.InputMetaData("Group", "Outer Glow"), SDK.InputMetaData("Title", "Opacity")]        
        [SDK.InputMetaData("Minimum", 0), SDK.InputMetaData("Default", 75), SDK.InputMetaData("Maximum", 100)]
        [SDK.InputMetaData("ViewStyle", "Slider")]
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
