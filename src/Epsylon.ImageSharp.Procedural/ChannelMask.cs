using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.ImageSharp.Procedural
{
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
    }
}
