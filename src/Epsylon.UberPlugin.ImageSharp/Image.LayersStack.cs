using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using SixLabors.ImageSharp;

namespace Epsylon.UberPlugin
{
    using UberFactory;

    using Epsylon.ImageSharp.Procedural;

    using XYZ = System.Numerics.Vector3;
    using XYZW = System.Numerics.Vector4;

    using PIXEL32 = SixLabors.ImageSharp.Rgba32;
    using IMAGE32 = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.Rgba32>;       

    using SIZE = SixLabors.Primitives.Size;
    using POINT = SixLabors.Primitives.Point;

    // ideally, the structure passed between layers would be:
    // OffsetX,Y , Image

    // this is important because if at some point we want to overlay a sprite in the negative quadrant,
    // the resulting image would be expanded, and the offset would be set accordingly, so any subsequent operation
    // would match the proper coordinates.

    public sealed class LayerImage
    {        
        private IMAGE32 _Image;

        public IMAGE32 Image => _Image;

        public void Blend(IMAGE32 src, SixLabors.ImageSharp.PixelFormats.PixelBlenderMode mode, float opacity)
        {
            if (src == null) return;

            if (_Image == null)
            {
                _Image = src;                

                if (opacity < 1) _Image.Mutate(dc => dc.Alpha(opacity));
                return;
            }

            // https://github.com/JimBobSquarePants/ImageSharp/issues/16

            _Image.Mutate(dc => dc.DrawImage(src, mode, opacity, SIZE.Empty, src.GetInternalPixelOffset() ) );
        }
    }

    public sealed class LayerInfo : IDisposable
    {
        #region lifecycle

        public LayerInfo(bool enabled, IMAGE32 img,int offsetX,int offsetY, int opacity, SixLabors.ImageSharp.PixelFormats.PixelBlenderMode mode)
        {
            _Enabled = enabled;
            _Image = img;

            _Image.SetInternalPixelOffset(offsetX, offsetY);
            
            _Opacity = opacity;
            _Mode = mode;
        }

        public void Dispose()
        {
            if (_Image != null) { _Image.Dispose(); _Image = null; }            
        }

        #endregion

        #region data

        private bool _Enabled;

        private int _OffsetX;
        private int _OffsetY;

        internal IMAGE32 _Image;        

        private int _Opacity;
        private SixLabors.ImageSharp.PixelFormats.PixelBlenderMode _Mode;

        #endregion

        #region properties

        public bool Enabled => _Enabled;

        public POINT Offset => new POINT(_OffsetX, _OffsetY);

        public IMAGE32 Image => _Image;

        public SixLabors.ImageSharp.PixelFormats.PixelBlenderMode Mode => _Mode;

        public float Opacity => ((float)_Opacity / 100.0f);

        #endregion        
    }

    [SDK.ContentNode("LayersStack")]
    [SDK.Title("Layers")]
    [SDK.TitleFormat("{0} Layers")]
    public sealed class LayersStack : ImageFilter
    {        
        [SDK.InputValue("Width")]
        [SDK.Title("Width"), SDK.Group("Image Size")]
        [SDK.Minimum(1),SDK.Default(256)]        
        public int Width { get; set; }

        [SDK.InputValue("Height")]
        [SDK.Title("Height"), SDK.Group("Image Size")]
        [SDK.Minimum(1),SDK.Default(256)]        
        public int Height { get; set; }

        [SDK.InputValue("DotsPerInch")]
        [SDK.Title("DPI"), SDK.Group("Image Size")]
        [SDK.Minimum(0.001f), SDK.Default(96)]
        public double DotsPerInch { get; set; }        

        [SDK.InputNode("Layers", true)]
        [SDK.Title("Layers")]
        [SDK.ItemsPanel("VerticalList")]
        public LayerInfo[] Layers { get; set; }

        protected override IMAGE32 Evaluate()
        {
            var limg = new LayerImage();            

            foreach (var layer in Layers)
            {
                if (layer == null) continue;

                limg.Blend(layer.Image, layer.Mode,layer.Opacity);

                layer.Dispose();
            }

            if (limg.Image == null) return null;

            limg.Image.MetaData.HorizontalResolution = DotsPerInch;
            limg.Image.MetaData.VerticalResolution = DotsPerInch;

            

            return limg.Image;
        }

        protected override object EvaluatePreview(SDK.PreviewContext context) { return Evaluate().CreatePreview(context); }
    }
    
    [SDK.ContentNode("Layer")]
    [SDK.Title("Layer")]
    [SDK.TitleFormat("Layer {0}")]
    public sealed class Layer : SDK.ContentFilter<LayerInfo>
    {
        [SDK.InputValue("Enabled")]
        [SDK.Title(""), SDK.Group("Opacity")]
        [SDK.Default(true)]
        public Boolean Enabled { get; set; }

        [SDK.InputValue("Opacity")]
        [SDK.Title(""), SDK.Group("Opacity")]
        [SDK.Minimum(0),SDK.Default(100),SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        public int Opacity { get; set; }

        [SDK.InputValue("BlendMode")]
        [SDK.Title("Mode"), SDK.Group("Opacity")]
        [SDK.Default(SixLabors.ImageSharp.PixelFormats.PixelBlenderMode.Normal)]
        public SixLabors.ImageSharp.PixelFormats.PixelBlenderMode BlendMode { get; set; }

        [SDK.InputValue("OffsetX")]
        [SDK.Title("X"), SDK.Group("Offset")]
        [SDK.Default(0)]
        public int OffsetX { get; set; }

        [SDK.InputValue("OffsetY")]
        [SDK.Title("Y"), SDK.Group("Offset")]
        [SDK.Default(0)]
        public int OffsetY { get; set; }

        [SDK.InputNode("Image")]        
        public IMAGE32 Image { get; set; }        

        protected override LayerInfo Evaluate()
        {
            return new LayerInfo(Opacity == 0 ? false : Enabled, Image , OffsetX,OffsetY, Opacity, BlendMode);
        }

        protected override object EvaluatePreview(SDK.PreviewContext context)
        {
            return Evaluate()._Image.CreatePreview(context);
        }
    }

}
