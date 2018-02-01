using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using Epsylon.ImageSharp.Procedural;

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

    [SDK.Icon(Constants.ICON_IMAGE)]
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

    [SDK.Icon(Constants.ICON_IMAGE)]
    [SDK.ContentNode("LayeredTransformStack")]
    [SDK.Title("Layered Transforms"), SDK.TitleFormat("{0} Layered Transforms")]
    public sealed class LayeredTransformStack : ImageFilter
    {
        [SDK.InputNode("Source")]
        [SDK.Title("Source Image")]
        public IMAGE32 Source { get; set; }

        [SDK.InputNode("Layers", true)]
        [SDK.Title("Layers")]
        [SDK.ItemsPanel("VerticalList")]
        public TransformLayer.Description[] Layers { get; set; }

        protected override IMAGE32 Evaluate()
        {
            if (Source == null) return null;

            var target = new IMAGE32(Source.Width, Source.Height);

            foreach(var layer in Layers.Where(l => l != null))
            {
                using (var clone = layer.ProcessStack(Source))
                {
                    target.Mutate(dc => dc.DrawImage(clone, layer.Mode, layer.Opacity, SIZE.Empty, POINT.Empty));
                }
            }

            Source.Dispose();

            return target;
        }        
    }



    [SDK.ContentNode("TransformLayer")]
    [SDK.Title("Layer"), SDK.TitleFormat("{0} Layer")]
    public sealed class TransformLayer : SDK.ContentFilter<TransformLayer.Description>
    {
        [SDK.InputValue("Enabled")]
        [SDK.Title(Constants.ICON_VISIBILITY), SDK.Group("Opacity")]
        [SDK.Default(true)]
        public Boolean Enabled { get; set; }

        [SDK.InputValue("Opacity")]
        [SDK.Title(""), SDK.Group("Opacity")]
        [SDK.Minimum(0), SDK.Default(100), SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        public int Opacity { get; set; }

        [SDK.InputValue("BlendMode")]
        [SDK.Title("Mode"), SDK.Group("Opacity")]
        [SDK.Default(PixelBlenderMode.Normal)]
        public PixelBlenderMode BlendMode { get; set; }

        [SDK.InputNode("Transforms", true)]
        [SDK.Title("Transforms")]
        [SDK.ItemsPanel("VerticalList")]
        public IMGTRANSFORM[] Transforms { get; set; }

        protected override Description Evaluate()
        {
            if (Transforms == null || Enabled == false) return null;

            return new Description(this.Transforms,this.BlendMode,this.Opacity);
        }        

        public sealed class Description
        {
            public Description(IMGTRANSFORM[] transforms, PixelBlenderMode mode, int opacity)
            {
                this._Transforms = transforms;
                this.Mode = mode;
                this.Opacity = opacity;
            }

            private readonly IMGTRANSFORM[] _Transforms;

            public PixelBlenderMode Mode { get; private set; }

            public int Opacity { get; private set; }            

            public IMAGE32 ProcessStack(IMAGE32 src)
            {
                return src.Clone
                (
                    dc =>
                    {
                        foreach (var xform in _Transforms)
                        {
                            xform?.Invoke(dc);
                        }
                    }
                );                
            }
        }
    }



    [SDK.Icon("Fx")]
    public abstract class BaseImageTransform : SDK.ContentFilter<IMGTRANSFORM>
    {
        [SDK.InputValue("Enabled")]
        [SDK.Title(Constants.ICON_VISIBILITY)]
        [SDK.Default(true)]
        [SDK.Group(0)]
        public Boolean Enabled { get; set; }

        // TODO: add boolean to enable TILEABLE mode:
        // before applying the effect, we create a multitile, then apply the effect, then crop back to the original.

        // TODO: unfortunately, this doesn't return anything that can be used as a preview.
        // Ideally, the preview for this object would be the preview of the parent TransformStack up to this node.
        // so the preview would be to add a SDK.PreviewContext property called UseParentPreview.
        // when the parent preview is called, it should somehow, know which child node requested the preview
        protected override object EvaluatePreview(SDK.PreviewContext previewContext) { return null; }

        protected override IMGTRANSFORM Evaluate()
        {
            if (!Enabled) return dc => { };

            return TransformImage();
        }

        protected abstract IMGTRANSFORM TransformImage();
    }

    [SDK.ContentNode("OpacityTransform")]
    [SDK.Title("Opacity"), SDK.TitleFormat("{0} Opacity")]
    public sealed class OpacityTransform : BaseImageTransform
    {
        [SDK.Group(0), SDK.Title("Amount")]        
        [SDK.Minimum(0), SDK.Default(1), SDK.Maximum(1)]
        [SDK.ViewStyle("Slider")]
        [SDK.InputValue("Amount")] public float Amount { get; set; }        

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.Opacity(Amount);
        }
    }

    [SDK.ContentNode("AlphaMaskTransform")]
    [SDK.Title("Alpha Mask"), SDK.TitleFormat("{0} Alpha Mask")]
    public sealed class AlphaMaskTransform : BaseImageTransform
    {
        [SDK.Group(0), SDK.Title("Mask")]
        [SDK.InputNode("Mask")] public Image<Alpha8> Mask { get; set; }

        
        [SDK.Group(0), SDK.Title("Mode")]
        [SDK.Default(PixelBlenderMode.Normal)]
        [SDK.InputValue("BlendMode")] public PixelBlenderMode BlendMode { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => { dc.SetAlphaMask(Mask, BlendMode); };
        }
    }


    [SDK.ContentNode("OuterGlowTransform")]
    [SDK.Title("Outer Glow"), SDK.TitleFormat("{0} with Outer Glow")]
    public sealed class ImageOuterGlowTransform : BaseImageTransform
    {
        [SDK.Group(0)]        
        [SDK.Title("Radius")]
        [SDK.Minimum(0), SDK.Default(1)]
        [SDK.InputValue("Radius")] public float Radius { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.OuterGlow(Radius);
        }
    }    

    [SDK.ContentNode("BlurTransform")]
    [SDK.Title("Blur"), SDK.TitleFormat("{0} Blurred")]
    public sealed class ImageBlurTransform : BaseImageTransform
    {
        public enum BlurMode { Box, Gaussian }

        [SDK.Group(0)]
        [SDK.InputValue("Mode")]
        [SDK.Title("Mode")]
        [SDK.Default(Epsylon.ImageSharp.Procedural.BlurMode.Gaussian)]
        public Epsylon.ImageSharp.Procedural.BlurMode Mode { get; set; }

        [SDK.Group(0)]
        [SDK.InputValue("Radius")]
        [SDK.Title("Radius")]
        [SDK.Minimum(0), SDK.Default(5)]
        public float Radius { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.Blur(Mode, Radius);
        }        
    }

    [SDK.ContentNode("DetectEdgesTransform")]
    [SDK.Title("Detect Edges"), SDK.TitleFormat("{0} Edges")]
    public sealed class DetectEdgesTransform : BaseImageTransform
    {
        [SDK.Group(0)]
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
        [SDK.Group(0)]
        [SDK.InputValue("Levels")]
        [SDK.Title("Levels")]
        [SDK.Minimum(1), SDK.Default(10)]
        public int Levels { get; set; }

        [SDK.Group(0)]
        [SDK.InputValue("BrushSize")]
        [SDK.Title("Brush Size")]
        [SDK.Minimum(1), SDK.Default(15)]
        public int BrushSize { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.OilPaint(Levels, BrushSize);
        }
    }

    public enum OldPhotoEffect { BlackWhite, Kodachrome, Polaroid, Lomograph,Sepia }

    [SDK.ContentNode("OldPhotoTransform")]
    [SDK.Title("Old Photo"), SDK.TitleFormat("{0} as Old Photo")]
    public sealed class ImageOldPhotoTransform : BaseImageTransform
    {
        [SDK.Group(0)]
        [SDK.InputValue("Effect")]
        [SDK.Title("Camera Effect")]
        [SDK.Default(OldPhotoEffect.Kodachrome)]        
        public OldPhotoEffect Effect { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            if (Effect == OldPhotoEffect.BlackWhite) return dc => dc.BlackWhite();
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

    [SDK.ContentNode("LevelsTransform")]
    [SDK.Title("Levels"), SDK.TitleFormat("{0} Levels")]
    public sealed class LevelsTransform : BaseImageTransform
    {
        /*
        [SDK.InputValue("Black")]
        [SDK.Title("Black"), SDK.Group("Levels")]
        [SDK.Minimum(0), SDK.Default(0), SDK.Maximum(255)]
        [SDK.ViewStyle("Slider")]
        public int Black { get; set; }

        [SDK.InputValue("White")]
        [SDK.Title("White"), SDK.Group("Levels")]
        [SDK.Minimum(0), SDK.Default(255), SDK.Maximum(255)]
        [SDK.ViewStyle("Slider")]
        public int White { get; set; }
        */

        [SDK.InputValue("Brightness")]
        [SDK.Title("Brightness"),SDK.Group("Adjust")]
        [SDK.Minimum(-100), SDK.Default(0), SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        public int Brightness { get; set; }

        [SDK.InputValue("Saturation")]
        [SDK.Title("Saturation"), SDK.Group("Adjust")]
        [SDK.Minimum(-100), SDK.Default(0), SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        public int Saturation { get; set; }

        [SDK.InputValue("Contrast")]
        [SDK.Title("Contrast"), SDK.Group("Adjust")]
        [SDK.Minimum(-100), SDK.Default(0), SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        public int Contrast { get; set; }        

        protected override IMGTRANSFORM TransformImage()
        {
            return dc =>
            {
                if (this.Brightness != 0) dc.Brightness(this.Brightness);
                if (this.Saturation != 0) dc.Saturate(this.Saturation);
                if (this.Contrast != 0) dc.Contrast(this.Contrast);            
            };
        }
    }






    [SDK.ContentNode("HueTransform")]
    [SDK.Title("Hue"), SDK.TitleFormat("{0} Hued")]
    public sealed class ImageHueTransform : BaseImageTransform
    {
        [SDK.InputValue("Degrees")]
        [SDK.Title("Degrees")]
        [SDK.Default(0)]
        [SDK.Group(0)]
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


    [SDK.ContentNode("ImageFlipTransform")]
    [SDK.Title("Flip"), SDK.TitleFormat("{0} Flipped")]
    public sealed class ImageFlipTransform : BaseImageTransform
    {
        [SDK.InputValue("Horizontal")]
        [SDK.Title("W"), SDK.Group("Direction")]        
        public Boolean Horizontal { get; set; }

        [SDK.InputValue("Vertical")]
        [SDK.Title("H"), SDK.Group("Direction")]        
        public bool Vertical { get; set; }        

        protected override IMGTRANSFORM TransformImage()
        {
            var ft = SixLabors.ImageSharp.Processing.FlipType.None;

            if (Horizontal) ft |= SixLabors.ImageSharp.Processing.FlipType.Horizontal;
            if (Vertical) ft |= SixLabors.ImageSharp.Processing.FlipType.Vertical;

            return dc => dc.Flip(ft);
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
        [SDK.Title("Mode"), SDK.Group(0)]
        [SDK.Default(Resampler.Bicubic)]
        public Resampler Resampler { get; set; }

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc.Resize(Width, Height, Resampler.GetInstance());
        }
               
    }

    [SDK.ContentNode("PolarDistortTransform")]
    [SDK.Title("Polar"), SDK.TitleFormat("{0} Polar")]
    public sealed class PolarDistortTransform : BaseImageTransform
    {
        protected override IMGTRANSFORM TransformImage()
        {
            return dc => dc
                .ApplyPolarDistort()
                .Flip(SixLabors.ImageSharp.Processing.FlipType.Vertical);
        }

    }



    
    [SDK.ContentNode("SpecialEffectsTransform")]
    [SDK.Title("Photoshop Effects"), SDK.TitleFormat("{0} Effects")]
    public sealed class SpecialEffectsTransform : BaseImageTransform
    {
        #region drop shadow

        [SDK.Title(""), SDK.Group("Drop Shadow")]        
        [SDK.InputValue("DropShadowEnabled")] public bool DropShadowEnabled { get; set; }

        [SDK.Title(""), SDK.Group("Drop Shadow")]        
        [SDK.Default((UInt32)0xff000000)]
        [SDK.ViewStyle("ColorPicker")]
        [SDK.InputValue("DropShadowColor")] public UInt32 DropShadowColor { get; set; }
        
        [SDK.Title("Opacity"), SDK.Group("Drop Shadow")]
        [SDK.Minimum(0), SDK.Default(75), SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        [SDK.InputValue("DropShadowOpacity")] public int DropShadowOpacity { get; set; }        

        [SDK.Title("Radius"), SDK.Group("Drop Shadow")]
        [SDK.Minimum(0), SDK.Default(5)]
        [SDK.InputValue("DropShadowRadius")] public int DropShadowRadius { get; set; }

        [SDK.Title("Intensity"), SDK.Group("Drop Shadow")]
        [SDK.Minimum(1), SDK.Default(1)]
        [SDK.InputValue("DropShadowIntensity")] public int DropShadowIntensity { get; set; }

        [SDK.Title("Angle"), SDK.Group("Drop Shadow")]
        [SDK.Minimum(0), SDK.Default(0), SDK.Maximum(360)]
        [SDK.InputValue("DropShadowAngle")] public int DropShadowAngle { get; set; }

        [SDK.Title("Distance"), SDK.Group("Drop Shadow")]
        [SDK.Minimum(0), SDK.Default(5)]
        [SDK.InputValue("DropShadowDistance")] public int DropShadowDistance { get; set; }

        #endregion

        #region inner shadow

        [SDK.Title(""), SDK.Group("Inner Shadow")]
        [SDK.InputValue("InnerShadowEnabled")] public bool InnerShadowEnabled { get; set; }

        [SDK.Title(""), SDK.Group("Inner Shadow")]
        [SDK.Default((UInt32)0xff000000)]
        [SDK.ViewStyle("ColorPicker")]
        [SDK.InputValue("InnerShadowColor")] public UInt32 InnerShadowColor { get; set; }

        [SDK.Title("Opacity"), SDK.Group("Inner Shadow")]
        [SDK.Minimum(0), SDK.Default(75), SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        [SDK.InputValue("InnerShadowOpacity")] public int InnerShadowOpacity { get; set; }        

        [SDK.Title("Radius"), SDK.Group("Inner Shadow")]
        [SDK.Minimum(0), SDK.Default(5)]
        [SDK.InputValue("InnerShadowRadius")] public int InnerShadowRadius { get; set; }

        [SDK.Title("Intensity"), SDK.Group("Inner Shadow")]
        [SDK.Minimum(1), SDK.Default(1)]
        [SDK.InputValue("InnerShadowIntensity")] public int InnerShadowIntensity { get; set; }

        [SDK.Title("Angle"), SDK.Group("Inner Shadow")]
        [SDK.Minimum(0), SDK.Default(0), SDK.Maximum(360)]
        [SDK.InputValue("InnerShadowAngle")] public int InnerShadowAngle { get; set; }

        [SDK.Title("Distance"), SDK.Group("Inner Shadow")]
        [SDK.Minimum(0), SDK.Default(5)]
        [SDK.InputValue("InnerShadowDistance")] public int InnerShadowDistance { get; set; }

        #endregion

        #region outer glow

        [SDK.Title(""), SDK.Group("Outer Glow")]
        [SDK.InputValue("OuterGlowEnabled")] public bool OuterGlowEnabled { get; set; }
        
        [SDK.Title(""), SDK.Group("Outer Glow")]
        [SDK.Default((UInt32)0xff0080ff)]
        [SDK.ViewStyle("ColorPicker")]
        [SDK.InputValue("OuterGlowColor")] public UInt32 OuterGlowColor { get; set; }
        
        [SDK.Title("Opacity"), SDK.Group("Outer Glow")]        
        [SDK.Minimum(0), SDK.Default(75), SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        [SDK.InputValue("OuterGlowOpacity")] public int OuterGlowOpacity { get; set; }

        [SDK.Title("Radius"), SDK.Group("Outer Glow")]
        [SDK.Minimum(0), SDK.Default(5)]
        [SDK.InputValue("OuterGlowRadius")] public int OuterGlowRadius { get; set; }

        [SDK.Title("Intensity"), SDK.Group("Outer Glow")]
        [SDK.Minimum(1), SDK.Default(1)]
        [SDK.InputValue("OuterGlowIntensity")] public int OuterGlowIntensity { get; set; }

        #endregion

        #region inner glow

        [SDK.Title(""), SDK.Group("Inner Glow")]
        [SDK.InputValue("InnerGlowEnabled")] public bool InnerGlowEnabled { get; set; }

        [SDK.Title(""), SDK.Group("Inner Glow")]
        [SDK.Default((UInt32)0xff0080ff)]
        [SDK.ViewStyle("ColorPicker")]
        [SDK.InputValue("InnerGlowColor")] public UInt32 InnerGlowColor { get; set; }

        [SDK.Title("Opacity"), SDK.Group("Inner Glow")]
        [SDK.Minimum(0), SDK.Default(75), SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        [SDK.InputValue("InnerGlowOpacity")] public int InnerGlowOpacity { get; set; }

        [SDK.Title("Radius"), SDK.Group("Inner Glow")]
        [SDK.Minimum(0), SDK.Default(5)]
        [SDK.InputValue("InnerGlowRadius")] public int InnerGlowRadius { get; set; }

        [SDK.Title("Intensity"), SDK.Group("Inner Glow")]
        [SDK.Minimum(1), SDK.Default(1)]
        [SDK.InputValue("InnerGlowIntensity")] public int InnerGlowIntensity { get; set; }

        #endregion

        protected override IMGTRANSFORM TransformImage()
        {
            return dc => _ApplyEffects(dc);
        }

        private void _ApplyEffects(IMAGE32DC dc)
        {
            var effectContext = new CommonEffect<Rgba32>();

            effectContext.DropShadow.Enabled   = DropShadowEnabled;
            effectContext.DropShadow.Color     = DropShadowColor;
            effectContext.DropShadow.Opacity   = DropShadowOpacity;
            effectContext.DropShadow.BlendMode = PixelBlenderMode.Multiply;
            effectContext.DropShadow.Angle     = DropShadowAngle;
            effectContext.DropShadow.Distance  = DropShadowDistance;
            effectContext.DropShadow.BlurRadius = DropShadowRadius;
            effectContext.DropShadow.Intensity = DropShadowIntensity;

            effectContext.InnerShadow.Enabled   = InnerShadowEnabled;
            effectContext.InnerShadow.Color     = InnerShadowColor;
            effectContext.InnerShadow.Opacity   = InnerShadowOpacity;
            effectContext.InnerShadow.BlendMode = PixelBlenderMode.Multiply;
            effectContext.InnerShadow.Angle     = InnerShadowAngle;
            effectContext.InnerShadow.Distance  = InnerShadowDistance;
            effectContext.InnerShadow.BlurRadius = InnerShadowRadius;
            effectContext.InnerShadow.Intensity = InnerShadowIntensity;
            effectContext.InnerShadow.InvertAlpha = true;

            effectContext.OuterGlow.Enabled = OuterGlowEnabled;
            effectContext.OuterGlow.Color = OuterGlowColor;
            effectContext.OuterGlow.Opacity = OuterGlowOpacity;
            effectContext.OuterGlow.BlendMode = PixelBlenderMode.Add;            
            effectContext.OuterGlow.BlurRadius = OuterGlowRadius;
            effectContext.OuterGlow.Intensity = OuterGlowIntensity;

            effectContext.InnerGlow.Enabled = InnerGlowEnabled;
            effectContext.InnerGlow.Color = InnerGlowColor;
            effectContext.InnerGlow.Opacity = InnerGlowOpacity;
            effectContext.InnerGlow.BlendMode = PixelBlenderMode.Add;
            effectContext.InnerGlow.BlurRadius = InnerGlowRadius;
            effectContext.InnerGlow.Intensity = InnerGlowIntensity;
            effectContext.InnerShadow.InvertAlpha = true;

            dc.ApplyCommonEffect(effectContext);
        }
    }

    
}
