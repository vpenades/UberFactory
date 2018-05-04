#### ROADMAP

- Consider supporting JSON as an alternative document format.
  - Pros: increased robustness and interoperability
  - Cons: adds a dependency with newtownsoft, which might be a problem for plugins,
             a potential solution would be to use a custom/DotNet.System parser		
		
   [System.Runtime.Serialization.Json - StackOverflow](https://stackoverflow.com/questions/15894091/how-to-parse-this-string-using-system-runtime-serialization-json)

   [System.Runtime.Serialization.Json - Microsoft](https://msdn.microsoft.com/en-us/library/system.runtime.serialization.json(v=vs.100).aspx)

- A solution is required on how to handle collections on child configurations. Right now,
a child collection completely overlaps the parent.

		We can have true arrays that might use the "replace" rule, and reference
        arrays that use the "union" rule.

		Solution: the merge rule can come from the Core, defined in the SDK.

- Distinguish between values and node references at document level

- Support for nullable values?

- Suport for curve edition control

		Define a Curve object as a primitive value;
		it might be part of the SDK, so it's powerful enough and common.

#### Plugin Issues

- Right now, a script references plugins with relative paths. This has several problems:
  - Moving the script to another location breaks the dependencies.
  - Scripts are not self contained.

  Solutions for these issues are:
  - Use multiple hint paths
  - Embed the plugin binaries as Mime64
  - Have one UberFactory.cfg per directory, pointing to plugin locations (like nuget.cfg)



- The current editor keeps loaded plugins in the current AppDomain and cannot be unloaded.
This presents a problem when loading scripts consecutively.
A short term solution is to force the editor to close and restart iteself when loading a new script.
A definitive solution is proposed below.

- The document stores plugins as plain relative paths to the plugin assembly.
This is problematic in development scenarios where multiple versions of the plugin can exist
within a solution project.
  - Instead of using a plain path, we can use a pair of "filename" and "hint paths", so the runtime can look into subdirectories
    - for the most appropiated assembly based on runtime/platform
    - for the most recent assembly.
  - We can try using nuget packages, but this is a whole new ball game.
    


#### FUTURE

- Right now, the editor is limited when working with scripts that use different plugins;
Plugins remain loaded and can cause conflicts. A way to overcome this limitation is to load
the plugins on execution in a separate context, that is, executing the script by calling the
CLI application. But at the same time, we do need access to the plugins declared types, which
are currently retrieved by reflection. A future variation would serialize the retrieved types
to a file, which would be consumed by the editor.
- 





