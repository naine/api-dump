# ApiDump
Tool for displaying the public types and API signatures of a .NET IL assembly.

Usage:
```
apidump [--no-bcl] <dllpaths>...
```

By default, ApiDump uses internal versions of BCL assemblies to resolve external
references to core types such as `System.ValueType`, `System.Enum`, and `System.Delegate`.
This is necessary to correctly identify struct, enum, and delegate types as such,
due to the way ApiDump uses a dummy [Roslyn](https://github.com/dotnet/roslyn)
compilation to inspect assembly metadata. This is usually what you want, but the
use of the internal BCL assemblies can be disabled using the `--no-bcl` option.
This is useful if, for instance, an assembly you are running ApiDump on is itself
a BCL assembly that defines the core types.
