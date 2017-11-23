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

    using XYZ = System.Numerics.Vector3;
    using XYZW = System.Numerics.Vector4;

    using PIXEL32 = SixLabors.ImageSharp.Rgba32;
    using IMAGE32 = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.Rgba32>;       

    using SIZE = SixLabors.Primitives.Size;
    using POINT = SixLabors.Primitives.Point;

    public sealed class LayerInfo : IDisposable
    {
        #region lifecycle

        public LayerInfo(bool enabled, IMAGE32 img,int offsetX,int offsetY, int opacity, SixLabors.ImageSharp.PixelFormats.PixelBlenderMode mode)
        {
            _Enabled = enabled;
            _Color = img;
            _OffsetX = offsetX;
            _OffsetY = offsetY;
            _Opacity = opacity;
            _Mode = mode;
        }

        public void Dispose()
        {
            if (_Color != null) { _Color.Dispose(); _Color = null; }
            if (_Alpha != null) { _Alpha.Dispose(); _Alpha = null; }
        }

        #endregion

        #region data

        private bool _Enabled;

        private int _OffsetX;
        private int _OffsetY;

        internal IMAGE32 _Color;
        private IMAGE32 _Alpha;

        private int _Opacity;
        private SixLabors.ImageSharp.PixelFormats.PixelBlenderMode _Mode;

        #endregion

        #region API

        // https://github.com/JimBobSquarePants/ImageSharp/issues/16

        public void FlattenTo(IMAGE32 target)
        {
            if (!_Enabled) return;

            if (target == null || _Color == null || _Opacity == 0) return;

            target.Mutate(dc => dc.DrawImage(_Color, _Mode, _Opacity, SIZE.Empty, new POINT(_OffsetX, _OffsetY)) );
        }

        #endregion
    }

    [SDK.ContentNode("LayersStack")]
    [SDK.Title("Layers")]
    [SDK.TitleFormat("{0} Layers")]
    public sealed class LayersStack : SDK.ContentFilter<IMAGE32>
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
            var img = new IMAGE32(Width, Height);
            img.MetaData.HorizontalResolution = DotsPerInch;
            img.MetaData.VerticalResolution = DotsPerInch;

            foreach (var layer in Layers)
            {
                if (layer == null) continue;

                layer.FlattenTo(img);

                layer.Dispose();
            }

            return img;
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

        protected override object EvaluatePreview(SDK.PreviewContext context) { return Evaluate()._Color.CreatePreview(context); }
    }

}
