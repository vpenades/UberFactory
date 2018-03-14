using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.ImageSharp.Procedural
{
    using V4 = System.Numerics.Vector4;

    [Flags]
    public enum Channel
    {
        Zero = 0,
        One = 1,
        Red = 2,
        Green = 4,
        Blue = 8,
        Alpha = 16        
    }

    public struct ChannelMask
    {
        public static ChannelMask Default => new ChannelMask(Channel.Red, Channel.Green, Channel.Blue, Channel.Alpha);

        ChannelMask(Channel r, Channel g, Channel b, Channel a)
        {
            Red = r;
            Green = g;
            Blue = b;
            Alpha = a;
        }

        public Channel Red;
        public Channel Green;
        public Channel Blue;
        public Channel Alpha;

        public V4 ShuffleComponents(V4 color)
        {
            return new V4
                (
                GetComponent(Red, color),
                GetComponent(Green, color),
                GetComponent(Blue, color),
                GetComponent(Alpha, color)
                );
        }

        public static float GetComponent(Channel c, V4 color)
        {
            float r = 0;
            float w = 0;

            if (c.HasFlag(Channel.One)) { r += 1; w += 1; }
            if (c.HasFlag(Channel.Red)) { r += color.X; w += 1; }
            if (c.HasFlag(Channel.Green)) { r += color.Y; w += 1; }
            if (c.HasFlag(Channel.Blue)) { r += color.Z; w += 1; }
            if (c.HasFlag(Channel.Alpha)) { r += color.W; w += 1; }

            return w == 0 ? 0 : r / w;
        }
    }   
}
