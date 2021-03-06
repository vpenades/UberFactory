﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Epsylon.UberPlugin
{
    using UberFactory;

    using Epsylon.ImageSharp.Procedural;    

    using PIXEL32 = Rgba32;
    using BITMAP = Image<Rgba32>;

    [SDK.Icon(Constants.ICON_IMAGE)]
    [SDK.ContentNode("LayersStack")]
    [SDK.Title("Layers")]
    [SDK.TitleFormat("{0} Layers")]
    public sealed class LayersStack : ImageFilter // GridLayout
    {
        [SDK.InputNode("Layers", true)]
        [SDK.Title("Layers")]
        [SDK.ItemsPanel("VerticalList")]
        public BITMAP[] Layers { get; set; }

        protected override BITMAP Evaluate()
        {
            var layout = new CanvasLayout<PIXEL32>(Layers);

            return layout.Flatten();
        }        

        protected override object EvaluatePreview(SDK.PreviewContext context) { return Evaluate().CreatePreview(context); }
    }
    
    [SDK.Icon(Constants.ICON_IMAGE)]
    [SDK.ContentNode("Layer")]
    [SDK.Title("Layer Transform")]
    [SDK.TitleFormat("Layer {0}")]
    public sealed class Layer : ImageFilter
    {
        // https://github.com/JimBobSquarePants/ImageSharp/issues/16
        // https://github.com/SixLabors/ImageSharp/issues/429
        // https://www.w3.org/TR/compositing-1/

        private const string _GroupTitle = "Overlay Settings";

        [SDK.InputValue("Enabled")]
        [SDK.Title("👁"), SDK.Group(_GroupTitle)]
        [SDK.Default(true)]
        public Boolean Enabled { get; set; }

        [SDK.InputValue("Opacity")]
        [SDK.Title("◿"), SDK.Group(_GroupTitle)]
        [SDK.Minimum(0),SDK.Default(100),SDK.Maximum(100)]
        [SDK.ViewStyle("Slider")]
        public int Opacity { get; set; }

        [SDK.InputValue("BlendMode")]
        [SDK.Title("⚭"), SDK.Group(_GroupTitle)]
        [SDK.Default(SixLabors.ImageSharp.PixelFormats.PixelColorBlendingMode.Normal)]
        public SixLabors.ImageSharp.PixelFormats.PixelColorBlendingMode BlendMode { get; set; }

        [SDK.InputValue("OffsetX")]
        [SDK.Title("X"), SDK.Group(_GroupTitle)]
        [SDK.Default(0)]
        public int OffsetX { get; set; }

        [SDK.InputValue("OffsetY")]
        [SDK.Title("Y"), SDK.Group(_GroupTitle)]
        [SDK.Default(0)]
        public int OffsetY { get; set; }

        [SDK.InputNode("Image")]        
        public BITMAP Image { get; set; }        

        protected override BITMAP Evaluate()
        {
            if (Image == null || Opacity <= 0) return null;

            Image.MetaData.SetInternalPixelOffset(OffsetX, OffsetY);
            Image.MetaData.SetInternalOpacity((float)Opacity / 100.0f);
            Image.MetaData.SetInternalBlendMode(BlendMode);

            return Image;
        }        
    }

}
