using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


namespace Epsylon.ImageSharp.Procedural.Processing
{    
    using COLOR = Rgba32;
    using IMAGE = Image<Rgba32>;

    // http://www.complexification.net/gallery/
    // http://pcg.wikidot.com/pcg-algorithm:worley-noise
    // http://pcg.wikidot.com/person:hugo-elias

    // https://johnresig.com/apps/processing.js/examples/custom/substrate.html

    // https://github.com/jeresig/processing-js/blob/master/examples/custom/substrate.html
    // https://github.com/jeresig/processing-js/blob/master/examples/custom/data/pollockShimmering.gif
    // https://github.com/processing/processing/blob/master/core/api.txt
    // https://github.com/processing/processing/tree/6adf63427a2e46b7a48e6eaabdd6d2b6b15656ca/core/src/processing/javafx

    // https://crossjam.net/wp/mpr/2009/10/pollockshimmeringgif/

    public class Substrate : Canvas
    {
        // Substrate Watercolor
        // j.tarbell   June, 2004
        // Albuquerque, New Mexico
        // complexification.net

        // Processing 0085 Beta syntax update
        // j.tarbell   April, 2005

        #region lifecycle

        public static Substrate Create(IMAGE target, int seed, IMAGE palette)
        {
            var colors = new HashSet<COLOR>();

            for (int x = 0; x < palette.Width; x++)
            {
                for (int y = 0; y < palette.Height; y++)
                {
                    var c = palette[x, y];

                    colors.Add(c);
                }
            }

            return Create(target, seed, colors.ToArray());
        }

        public static Substrate Create(IMAGE target, int seed, COLOR[] palette)
        {
            return new Substrate(target, seed, palette);
        }

        private Substrate(IMAGE target, int seed, COLOR[] palette) : base(target,seed)
        {
            _Palette = palette;

            _CrackGrid = new int[this.Width * this.Height];

            _Begin();
        }

        private void _Begin()
        {
            // erase crack grid
            for (int i = 0; i < _CrackGrid.Length; i++)
            {
                _CrackGrid[i] = 10001;                
            }

            // make random crack seeds
            for (int k = 0; k < 16; k++)
            {
                int i = this.NextRandomInt(_CrackGrid.Length);
                _CrackGrid[i] = this.NextRandomInt(360);
            }

            // make just three cracks
            _MakeCrack();
            _MakeCrack();
            _MakeCrack();            
        }

        #endregion

        #region data

        // grid of cracks
        private readonly int[] _CrackGrid;        

        private readonly List<Crack> _Cracks = new List<Crack>();

        // color parameters
        private readonly COLOR[] _Palette;

        #endregion

        #region API        

        public void DrawStep()
        {
            // crack all cracks

            for(int i=0; i< _Cracks.Count; ++i)
            {
                _Cracks[i].Move();
            }
        }        

        private void _MakeCrack()
        {
            if (_Cracks.Count >= 200) return;

            var crack = new Crack(this);

            _Cracks.Add(crack);            
        }        

        private COLOR _GetSomeColor()
        {
            // pick some random good color
            return _Palette[this.NextRandomInt(_Palette.Length)];
        }        

        private int GetCell(int x, int y)
        {
            if (x < 0 || x >= this.Width) return 10001;
            if (y < 0 || y >= this.Height) return 10001;

            return _CrackGrid[y * this.Width + x];
        }

        private void SetCell(int x, int y, int value)
        {
            if (x < 0 || x >= this.Width) return;
            if (y < 0 || y >= this.Height) return;
            _CrackGrid[y * this.Width + x] = value;
        }

        #endregion

        #region helper classes        

        class Crack
        {
            #region lifecycle

            public Crack(Substrate parent)
            {
                _Parent = parent;
                _SandPainter = new SandPainter(_Parent);

                // find placement along existing crack
                _FindStart();
            }

            #endregion

            #region data

            private readonly Substrate _Parent;

            // sand painter
            private readonly SandPainter _SandPainter;

            private float _X, _Y;
            private float _Angle;    // direction of travel in degrees            

            #endregion

            #region API

            private void _FindStart()
            {
                // pick random point
                int px = 0;
                int py = 0;

                // shift until crack is found
                bool found = false;
                int timeout = 0;

                while ((!found) || (timeout++ > 1000))
                {
                    px = _Parent.NextRandomInt(_Parent.Width);
                    py = _Parent.NextRandomInt(_Parent.Height);
                    if (_Parent.GetCell(px,py) < 10000)
                    {
                        found = true;
                    }
                }

                if (!found) return; // error!?
                
                // start crack
                int a = _Parent.GetCell(px,py);

                if (_Parent.NextRandomInt(100) < 50)
                {
                    a -= 90 + (int)_Parent.NextRandomFloat(-2f, 2.1f);
                }
                else
                {
                    a += 90 + (int)_Parent.NextRandomFloat(-2f, 2.1f);
                }

                _StartCrack(px, py, a);
                
            }

            private void _StartCrack(int x, int y, int angle)
            {
                _X = x;
                _Y = y;
                _Angle = angle; //%360;
                _X += (float)(0.61 * Math.Cos(_Angle * Math.PI / 180));
                _Y += (float)(0.61 * Math.Sin(_Angle * Math.PI / 180));
            }

            public void Move()
            {
                // continue cracking
                _X += (float)(0.42 * Math.Cos(_Angle * Math.PI / 180));
                _Y += (float)(0.42 * Math.Sin(_Angle * Math.PI / 180));

                // bound check
                const float fuzz = 0.33f;
                int cx = (int)(_X + _Parent.NextRandomFloat(-fuzz, fuzz));  // add fuzz
                int cy = (int)(_Y + _Parent.NextRandomFloat(-fuzz, fuzz));

                // draw sand painter
                _RegionColor();                

                if ((cx >= 0) && (cx < _Parent.Width) && (cy >= 0) && (cy < _Parent.Height))
                {
                    // draw black crack                    
                    _Parent.DrawPoint(cx, cy, Rgba32.Black.WithAlpha((int)(255.0f * 0.85f)));

                    // safe to check
                    if ((_Parent.GetCell(cx,cy) > 10000) || (Math.Abs( _Parent.GetCell(cx,cy) - _Angle) < 5))
                    {
                        // continue cracking
                        _Parent.SetCell(cx,cy, (int)_Angle);
                    }
                    else if (Math.Abs(_Parent.GetCell(cx,cy) - _Angle) > 2)
                    {
                        // crack encountered (not self), stop cracking
                        _FindStart();
                        _Parent._MakeCrack();
                    }
                }
                else
                {
                    // out of bounds, stop cracking
                    _FindStart();
                    _Parent._MakeCrack();
                }
            }

            private void _RegionColor()
            {
                // start checking one step away
                float rx = _X;
                float ry = _Y;
                bool openspace = true;

                // find extents of open space
                while (openspace)
                {
                    // move perpendicular to crack
                    rx += (float)(0.81 * Math.Sin(_Angle * Math.PI / 180));
                    ry -= (float)(0.81 * Math.Cos(_Angle * Math.PI / 180));
                    int cx = (int)rx;
                    int cy = (int)ry;

                    if ((cx >= 0) && (cx < _Parent.Width) && (cy >= 0) && (cy < _Parent.Height))
                    {
                        // safe to check
                        if (_Parent.GetCell(cx,cy) > 10000)
                        {
                            // space is open
                        }
                        else
                        {
                            openspace = false;
                        }
                    }
                    else
                    {
                        openspace = false;
                    }
                }
                // draw sand painter
                _SandPainter.Render(rx, ry, _X, _Y);
            }

            #endregion
        }

        class SandPainter
        {
            #region lifecycle

            public SandPainter(Substrate parent)
            {
                _Parent = parent;

                _Color = _Parent._GetSomeColor();
                _Gain = _Parent.NextRandomFloat(0.01f, 0.1f);
            }

            #endregion

            #region data
       
            private readonly Substrate _Parent;
            private readonly COLOR _Color;

            private float _Gain;

            #endregion

            #region API

            public void Render(float x, float y, float ox, float oy)
            {
                // modulate gain
                _Gain += _Parent.NextRandomFloat(-0.05f, 0.05f);
                _Gain = _Gain.Clamp(0, 1);

                // calculate grains by distance
                //int grains = int(sqrt((ox-x)*(ox-x)+(oy-y)*(oy-y)));
                const int grains = 64;

                // lay down grains of sand (transparent pixels)
                float w = _Gain / (grains - 1);

                for (int i = 0; i < grains; i++)
                {
                    var cx = ox + (x - ox) * (float)Math.Sin(Math.Sin(i * w));
                    var cy = oy + (y - oy) * (float)Math.Sin(Math.Sin(i * w));

                    float a = 0.1f - (float)i / (grains * 10.0f);
                    a *= 10;

                    _Parent.DrawPoint(cx, cy, _Color.WithAlpha((int)(a * 256)));
                }
            }

            #endregion
        }

        #endregion        
    }


}
