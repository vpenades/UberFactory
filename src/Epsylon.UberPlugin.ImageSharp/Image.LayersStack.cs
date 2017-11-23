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
    [SDK.ContentMetaData("Title", "Layers")]
    [SDK.ContentMetaData("TitleFormat", "{0} Layers")]
    public sealed class LayersStack : SDK.ContentFilter<IMAGE32>
    {        
        [SDK.InputValue("Width")]
        [SDK.InputMetaData("Group", "Image Size"), SDK.InputMetaData("Title", "Width")]
        [SDK.InputMetaData("Minimum", 1),SDK.InputMetaData("Default", 256)]        
        public int Width { get; set; }

        [SDK.InputValue("Height")]
        [SDK.InputMetaData("Group", "Image Size"),SDK.InputMetaData("Title", "Height")]
        [SDK.InputMetaData("Minimum", 1),SDK.InputMetaData("Default", 256)]        
        public int Height { get; set; }

        [SDK.InputValue("DotsPerInch")]
        [SDK.InputMetaData("Group", "Image Size"), SDK.InputMetaData("Title", "DPI")]
        [SDK.InputMetaData("Minimum", 0.001f), SDK.InputMetaData("Default", 96)]
        public double DotsPerInch { get; set; }        

        [SDK.InputNode("Layers", true)]
        [SDK.InputMetaData("Title", "Layers")]
        [SDK.InputMetaData("Panel", "VerticalList")]
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
    [SDK.ContentMetaData("Title", "Layer")]
    [SDK.ContentMetaData("TitleFormat", "Layer {0}")]
    public sealed class Layer : SDK.ContentFilter<LayerInfo>
    {
        [SDK.InputValue("Enabled")]
        [SDK.InputMetaData("Group", "Opacity"),SDK.InputMetaData("Title", "")]
        [SDK.InputMetaData("Default", true)]
        public Boolean Enabled { get; set; }

        [SDK.InputValue("Opacity")]
        [SDK.InputMetaData("Group", "Opacity"),SDK.InputMetaData("Title", "")]
        [SDK.InputMetaData("Minimum", 0),SDK.InputMetaData("Default", 100),SDK.InputMetaData("Maximum", 100)]
        [SDK.InputMetaData("ViewStyle", "Slider")]
        public int Opacity { get; set; }

        [SDK.InputValue("BlendMode")]
        [SDK.InputMetaData("Group", "Opacity"), SDK.InputMetaData("Title", "Mode")]
        [SDK.InputMetaData("Default", SixLabors.ImageSharp.PixelFormats.PixelBlenderMode.Normal)]
        public SixLabors.ImageSharp.PixelFormats.PixelBlenderMode BlendMode { get; set; }

        [SDK.InputValue("OffsetX")]
        [SDK.InputMetaData("Group", "Offset"), SDK.InputMetaData("Title", "X")]
        [SDK.InputMetaData("Default", 0)]
        public int OffsetX { get; set; }

        [SDK.InputValue("OffsetY")]
        [SDK.InputMetaData("Group", "Offset"), SDK.InputMetaData("Title", "Y")]
        [SDK.InputMetaData("Default", 0)]
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
