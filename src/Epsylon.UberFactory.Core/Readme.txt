
TODO:

Consider supporting JSON as an alternative document format.
	Pros: increased robustness and interoperability
	Cons: adds a dependency with newtownsoft, which might be a problem for plugins, a potential solution would be to use a custom/DotNet.System parser

	https://msdn.microsoft.com/en-us/library/system.runtime.serialization.json(v=vs.100).aspx
	System.Runtime.Serialization.Json

	https://stackoverflow.com/questions/15894091/how-to-parse-this-string-using-system-runtime-serialization-json


A solution is required on how to handle collections on child configurations. Right now, a child collection completely overlaps the parent.

	We can have true arrays that might use the "replace" rule, and reference arrays that use the "union" rule.

	Solution: the merge rule can come from the Core, defined in the SDK.

	



Distinguish between values and node references at document level

Support for nullable values?

Suport for curve edition control
	Define a Curve object as a primitive value; it might be part of the SDK, so it's powerful enough and common.

