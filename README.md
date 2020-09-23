# ApiDump
Tool for displaying the public types and API signatures of a .NET IL assembly.

ApiDump is licensed under the [MIT license](LICENSE).

## Building

ApiDump runs on .NET Core, version 3.1 or newer. Building it requires a compatible
version of the [.NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1)
to be installed and the `dotnet` CLI tool to be available on the command PATH.
Including git version information in the build requires `git` to also be available
on the PATH and the build to be done from a git worktree, but this is not required.

In most cases, ApiDump can be built by typing `make` at the root of the repository.
The included `Makefile` assumes you are building on and for Linux x86_64. For Windows
x86_64, use the `make.bat` file instead. These makefiles build framework-dependent
single-file executables. For other platforms or custom build configurations, use
[`dotnet publish`](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish)
on the [ApiDump](ApiDump/ApiDump.csproj) project.

## Usage

```
$ apidump [options] <dllpaths>...
```

#### Options

- `-h`, `--help`

  Display this help information.

- `-v`, `--version`

  Display the version of ApiDump being used.

- `--all-interfaces`

  Do not omit interfaces that are implied through inheritance.

- `--no-bcl`

  Do not refer to internal BCL assemblies to resolve references to core types.
  By default, ApiDump uses trimmed down BCL reference assemblies to resolve external
  references to types such as `System.ValueType`, `System.Enum`, and `System.Delegate`.
  This is necessary to correctly identify struct, enum, and delegate types as such,
  due to the way ApiDump uses a dummy [Roslyn](https://github.com/dotnet/roslyn)
  compilation to inspect assembly metadata. Disabling this is useful if an assembly
  you are running ApiDump on is itself a BCL assembly that defines the core types.
