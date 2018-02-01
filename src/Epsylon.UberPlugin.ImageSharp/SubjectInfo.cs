using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberPlugin.ImageSharp
{
    using Epsylon.ImageSharp.Procedural;
    using SixLabors.Primitives;
    using UberFactory;

    
    
    
    public abstract class SubjectInfoBase : SDK.ContentFilter<SubjectInfo>
    {        
        [SDK.Title("Center X"),SDK.Group(0)]
        [SDK.Minimum(0)]
        [SDK.InputValue("X")] public int X { get; set; }

        [SDK.Title("Center Y"), SDK.Group(0)]
        [SDK.Minimum(0)]
        [SDK.InputValue("Y")] public int Y { get; set; }        
    }

    [SDK.Icon("✛")]    
    [SDK.Title("Origin")]
    [SDK.TitleFormat("{0} Origin")]
    [SDK.ContentNode("SubjectInfoOrigin")]
    public sealed class SubjectInfoOrigin : SubjectInfoBase
    {
        protected override SubjectInfo Evaluate()
        {
            return new SubjectInfo
            {
                Center = new Point(this.X, this.Y)
            };
        }
    }

    [SDK.Icon("◯")]
    [SDK.Title("Circle")]
    [SDK.TitleFormat("{0} Circle")]
    [SDK.ContentNode("SubjectInfoCircle")]
    public sealed class SubjectInfoCircle : SubjectInfoBase
    {
        [SDK.Title("Diameter"), SDK.Group(0)]
        [SDK.Minimum(0)]
        [SDK.InputValue("Diameter")] public int Diameter { get; set; }

        protected override SubjectInfo Evaluate()
        {
            return new SubjectInfo
            {
                Center = new Point(this.X, this.Y),
                Diameter = this.Diameter
            };
        }
    }

    [SDK.Icon("◻")]
    [SDK.Title("Rectangle")]
    [SDK.TitleFormat("{0} Rectangle")]
    [SDK.ContentNode("SubjectInfoRectangle")]
    public sealed class SubjectInfoRectangle : SubjectInfoBase
    {
        [SDK.Title("Width"), SDK.Group(0)]
        [SDK.Minimum(0)]
        [SDK.InputValue("Width")]
        public int Width { get; set; }

        [SDK.Title("Height"), SDK.Group(0)]
        [SDK.Minimum(0)]
        [SDK.InputValue("Height")]
        public int Height { get; set; }

        protected override SubjectInfo Evaluate()
        {
            return new SubjectInfo
            {
                Center = new Point(this.X, this.Y),
                Size = new Size(this.Width,this.Height)
            };
        }
    }
}
