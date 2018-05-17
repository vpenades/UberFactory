## Roadmap

#### Build process

A better version number system is required.

[GitVersion](https://github.com/GitTools/GitVersion)

for the SDK and Core assemblies, it could be desirable to have an automatic mechanism
to prevent API breaking across releases, some candidate libraries are:

[BreakDance](https://github.com/CloudNimble/Breakdance)


#### Document Serialization

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

#### Previewing

Right now the current mechanism for preview is to create a document,
store it in a TMP folder and launch it with a Shell Process, letting
the OS to choose the appropiate viewer based on user's preferences.

This is good for know formats like images, but in some cases, a plugin
might want to preview a custom file with a specific program.

A solution would come in two ways:

- At SDK level, allow to pass an executable path with arguments to be used for preview.

- At editor level, allow selecting exectuables for preview, like Visual Studio.

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

- Add a Button binding to launch actions within the plugin; NOTE: this is incompatible with decoupling plugin load
from the editor, so we have to choose between dynamically loading the plugins in the editor to allow some features or not.
Interesting actions would be: advanced preview, advanced edition in an external tool; query for information that can only
be available from an unmanaged executable, etc;


- Right now, the editor is limited when working with scripts that use different plugins;
Plugins remain loaded and can cause conflicts. A way to overcome this limitation is to load
the plugins on execution in a separate context, that is, executing the script by calling the
CLI application. But at the same time, we do need access to the plugins declared types, which
are currently retrieved by reflection. A future variation would serialize the retrieved types
to a file, which would be consumed by the editor.
- 





