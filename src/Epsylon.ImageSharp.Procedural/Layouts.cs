using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace Epsylon.ImageSharp.Procedural
{
    // proposed layouts: Canvas, Grid, Dock

    // in Canvas, OffsetX and OffsetY are literal x,y values
    // in Grid, they're row/columns
    // in Dock, valid values are only -1, 0 and 1, representing up/down/left/right


    public class CanvasLayout<TPixel> where TPixel : struct, IPixel<TPixel>
    {
        public CanvasLayout() {}

        public CanvasLayout(IEnumerable<Image<TPixel>> images) { _Elements.AddRange(images); }

        private readonly List<Image<TPixel>> _Elements = new List<Image<TPixel>>();

        public IList<Image<TPixel>> Elements => _Elements;

        public Image<TPixel> Flatten()
        {
            Image<TPixel> flattened = null;

            foreach (var layer in _Elements.Where(item => item != null))
            {
                flattened = _Blend(flattened, layer);

                layer.Dispose();
            }

            return flattened;
        }

        private static Image<TPixel> _Blend(Image<TPixel> flattened, Image<TPixel> layer)
        {
            if (layer == null) return flattened;

            var opacity = layer.MetaData.GetInternalOpacity();
            var offset = layer.MetaData.GetInternalPixelOffset();
            var bmode = layer.MetaData.GetInternalBlendMode();

            if (flattened == null) flattened = new Image<TPixel>(layer.Width, layer.Height);

            flattened.Mutate(dc => dc.DrawImage(layer, offset, bmode, opacity));

            

            return flattened;
        }

        public static void SetOffset(Image<TPixel> image, int offsetX, int offsetY)
        {
            // this is equivalent as Grid.SetRow(element, rowIndex); , so we could have different offsets for different layouts
        }
    }
}
