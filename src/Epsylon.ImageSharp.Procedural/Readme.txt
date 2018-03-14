Suggestions for ImageSharp:

- Drawing methods that have an array as the last parameter, could use params PointF[] for ease of use.
- some exception with SignedRational writing Konica_Minolta_DiMAGE_Z3.jpg metadata
- ExifProfile.RemoveValue should not throw if the value doesn't exist

- Rectangle and RectangleF could have TopLeft TopRight BottomLeft and BottomRight Point properties
- Missing a DrawPoint with the same features as all other drawing primitives

- Image could have a TPixel this[Point] { get; set; }

- MemoryManager cannot be inherited in a derived class because abstract internal methods. If that's the expected behavior, 
