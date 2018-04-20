Suggestions for ImageSharp:

- some exception with SignedRational writing Konica_Minolta_DiMAGE_Z3.jpg metadata
- ExifProfile.RemoveValue should not throw if the value doesn't exist

- Rectangle and RectangleF could have TopLeft TopRight BottomLeft and BottomRight Point properties
- Missing a DrawPoint with the same features as all other drawing primitives

- Image could have a TPixel this[Point] { get; set; }

- MemoryManager cannot be inherited in a derived class because abstract internal methods. If that's the expected behavior, 

- Rectangle(F) don't have a "Invalid" constant defined with infinite negative size and an .IsValid property

- Porter-Duff:

[Porter-Duff](http://ssp.impulsetrain.com/porterduff.html)  
[Porter-Duff about "Normal" blend mode](http://ssp.impulsetrain.com/translucency.html)

Current ImageSharp implementation is a plain enum, but:

Main values: (note that "Normal" is Source-Over)

Source
Destination
Multiply
Add
Substract
Screen
Darken
Lighten

Modifiers:

- Empty
- Atop
- Over
- In
- Out
 

