using System;
using System.Runtime.CompilerServices;

// TODO: Determine if these are actually necessary to identify C# tuples.
// Must be done after tuples are handled specially in ApiDump.

[assembly: TypeForwardedTo(typeof(TupleElementNamesAttribute))]
[assembly: TypeForwardedTo(typeof(ValueTuple))]
[assembly: TypeForwardedTo(typeof(ValueTuple<>))]
[assembly: TypeForwardedTo(typeof(ValueTuple<,>))]
[assembly: TypeForwardedTo(typeof(ValueTuple<,,>))]
[assembly: TypeForwardedTo(typeof(ValueTuple<,,,>))]
[assembly: TypeForwardedTo(typeof(ValueTuple<,,,,>))]
[assembly: TypeForwardedTo(typeof(ValueTuple<,,,,,>))]
[assembly: TypeForwardedTo(typeof(ValueTuple<,,,,,,>))]
[assembly: TypeForwardedTo(typeof(ValueTuple<,,,,,,,>))]
