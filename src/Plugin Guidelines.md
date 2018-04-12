#### Plugin Development Guidelines

The most important thing to take into account when developing
and using plugins is shared dependencies.

All plugins are loaded into the same runtime context,
this can be problematic when two plugins use different versions
of the same dependency.

The first loaded plugin triggers the load of its dependencies.
When the second plugin loads, it tries to load its dependencies,
but the runtime detects they're already loaded,
so it reuses the dependencies of the previous plugin.
This results in the second plugin using the dependencies of the first plugin.
If the dependencies happen to be of the same version, there's no problem,
But if they're of different versions,
they might work,
they might have undefined behaviour,
or might they crash at runtime.


Best plugin development practices:

- Try to make the plugins as self-contained as possible, the less dependencies, the better.
- Try to use *major* versions of dependencies, avoid using betas, previews, etc.
- If a common hub for a dependency is provided, use it.
- For large projects, develop a single, one for all plugin.

Best plugin usage practices:

- If a script requires too many plugins, split the script into smaller ones.
