// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ApiDump
{
    /* The following (and probably some more) need to be handled correctly:
     *  - Nullable reference types.
     *  - Nullable reference type generic parameter constraints.
     *  - Default interface implementations.
     *  - Show fixed buffers with their sizes.
     *  - Hide fixed buffers compiler auto-generated types.
     *  - Type names should be qualified where ambiguous or nested and not in scope.
     */

    static class Program
    {
        private static bool showAllInterfaces = false;

        static int Main(string[] args)
        {
            var dlls = new List<string>();
            bool useInternalBCL = true;
            foreach (string arg in args)
            {
                if (arg.StartsWith('-'))
                {
                    switch (arg)
                    {
                    case "--no-bcl":
                        useInternalBCL = false;
                        break;
                    case "--all-interfaces":
                        showAllInterfaces = true;
                        break;
                    case "--help":
                        PrintHelp();
                        return 0;
                    case "--version":
                        if (args.Contains("--help")) goto case "--help";
                        PrintVersion();
                        return 0;
                    default:
                        Console.Error.WriteLine("Error: Unknown option '{0}'", arg);
                        return 1;
                    }
                }
                else
                {
                    dlls.Add(arg);
                }
            }
            if (dlls.Count == 0)
            {
                Console.Error.WriteLine("Error: No input files. Use --help to show usage.");
                return 1;
            }
            try
            {
                var refs = new List<MetadataReference>();
                foreach (string path in dlls)
                {
                    refs.Add(MetadataReference.CreateFromFile(path));
                }
                if (useInternalBCL)
                {
                    using var zip = new ZipArchive(
                        typeof(Program).Assembly.GetManifestResourceStream("ApiDump.DummyBCL.zip"));
                    foreach (var entry in zip.Entries)
                    {
                        // CreateFromStream() requires a seekable stream for some reason.
                        // ZipArchiveEntry streams are not, so we need to copy.
                        using var memStream = new MemoryStream((int)entry.Length);
                        using (var zipStream = entry.Open()) zipStream.CopyTo(memStream);
                        memStream.Position = 0;
                        refs.Add(MetadataReference.CreateFromStream(memStream));
                    }
                }
                var comp = CSharpCompilation.Create("dummy", null, refs,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                var errors = comp.GetDiagnostics();
                if (!errors.IsDefaultOrEmpty)
                {
                    throw new Exception($"Compilation has errors:\n{string.Join('\n', errors)}");
                }
                var globalNamespaces = comp.GlobalNamespace.ConstituentNamespaces;
                if (globalNamespaces.Length != refs.Count + 1)
                {
                    throw new Exception($"Unexpected number of global namespaces: {globalNamespaces.Length}");
                }
                if (!SymbolEqualityComparer.Default.Equals(
                    globalNamespaces[0], comp.Assembly.GlobalNamespace))
                {
                    throw new Exception("Unexpected first global namespace:"
                        + $" {globalNamespaces[0].ContainingAssembly.Name}");
                }
                for (int i = 1; i <= dlls.Count; ++i)
                {
                    PrintNamespace(globalNamespaces[i]);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: {0}", ex);
                return -1;
            }
            return 0;
        }

        private static void PrintVersion(Assembly? assembly = null)
        {
            assembly ??= typeof(Program).Assembly;
            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            Console.WriteLine($"{nameof(ApiDump)} version {{0}}",
                attribute?.InformationalVersion ?? assembly.GetName().Version!.ToString(3));

            var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            if (copyright != null) Console.WriteLine(copyright.Copyright);
        }

        private static void PrintHelp()
        {
            // TODO: Improve help text. Just say [options] and list them below with descriptions.
            var assembly = typeof(Program).Assembly;
            PrintVersion(assembly);
            Console.WriteLine();
            Console.WriteLine("Usage: {0} [--no-bcl] [--all-interfaces] <dllpaths>...",
                Path.GetFileNameWithoutExtension(assembly.Location));
        }

        private static bool emptyBlockOpen = false;

        private static void PrintLine(string line, int indent, bool openBlock = false)
        {
            if (emptyBlockOpen)
            {
                Console.WriteLine();
                for (int i = 1; i < indent; ++i) Console.Write("    ");
                Console.WriteLine('{');
            }
            for (int i = 0; i < indent; ++i) Console.Write("    ");
            Console.Write(line);
            if (!(emptyBlockOpen = openBlock))
            {
                Console.WriteLine();
            }
        }

        private static void PrintEndBlock(int indent)
        {
            if (emptyBlockOpen)
            {
                Console.WriteLine(" { }");
                emptyBlockOpen = false;
            }
            else
            {
                for (int i = 0; i < indent; ++i) Console.Write("    ");
                Console.WriteLine('}');
            }
        }

        private static readonly Func<ISymbol?, ISymbol?> identity = s => s;

        private static IOrderedEnumerable<T> Sort<T>(IEnumerable<T> symbols) where T : class?, ISymbol?
            => symbols.OrderBy<T, ISymbol?>(identity, MemberOrdering.Comparer);

        private static void PrintNamespace(INamespaceSymbol ns)
        {
            bool printed = false;
            foreach (var type in Sort(ns.GetTypeMembers()))
            {
                if (type.DeclaredAccessibility == Accessibility.Public)
                {
                    if (!printed && !ns.IsGlobalNamespace)
                    {
                        PrintLine($"namespace {FullName(ns)}", 0);
                        PrintLine("{", 0);
                        printed = true;
                    }
                    PrintType(type, ns.IsGlobalNamespace ? 0 : 1);
                }
            }
            if (printed) PrintLine("}", 0);
            foreach (var subNs in ns.GetNamespaceMembers().OrderBy(t => t.MetadataName))
            {
                PrintNamespace(subNs);
            }
        }

        private static string FullName(INamespaceSymbol ns)
        {
            var parent = ns.ContainingNamespace;
            return parent.IsGlobalNamespace ? ns.Name : $"{FullName(parent)}.{ns.Name}";
        }

        private static void PrintType(INamedTypeSymbol type, int indent)
        {
            var sb = new StringBuilder();
            switch (type.DeclaredAccessibility)
            {
            case Accessibility.Public:
                sb.Append("public ");
                break;
            case Accessibility.Protected:
            case Accessibility.ProtectedOrInternal:
                sb.Append("protected ");
                break;
            default:
                throw new Exception($"Type {type} has unexpected visibility {type.DeclaredAccessibility}");
            }
            switch (type.TypeKind)
            {
            case TypeKind.Class:
                if (type.IsStatic) sb.Append("static ");
                else if (type.IsAbstract) sb.Append("abstract ");
                else if (type.IsSealed) sb.Append("sealed ");
                sb.Append("class ").Append(type.Name);
                break;
            case TypeKind.Struct:
                if (type.IsReadOnly) sb.Append("readonly ");
                if (type.IsRefLikeType) sb.Append("ref ");
                sb.Append("struct ").Append(type.Name);
                break;
            case TypeKind.Interface:
                sb.Append("interface ").Append(type.Name);
                break;
            case TypeKind.Enum:
                sb.Append("enum ").Append(type.Name);
                break;
            case TypeKind.Delegate:
                if (type.DelegateInvokeMethod == null)
                {
                    throw new Exception($"Delegate type has null invoke method: {type}");
                }
                PrintLine(sb.Append("delegate ")
                    .AppendReturnSignature(type.DelegateInvokeMethod)
                    .Append(' ').Append(type.Name)
                    .AppendTypeParameters(type.TypeParameters, out var delegateConstraints)
                    .AppendParameters(type.DelegateInvokeMethod.Parameters)
                    .AppendTypeConstraints(delegateConstraints)
                    .Append(';').ToString(), indent);
                return;
            default:
                throw new Exception($"Named type {type} has unexpected kind {type.TypeKind}");
            }
            sb.AppendTypeParameters(type.TypeParameters, out var constraints);
            var bases = new List<INamedTypeSymbol>();
            if (type.BaseType != null && type.TypeKind == TypeKind.Class
                && type.BaseType.SpecialType != SpecialType.System_Object)
            {
                bases.Add(type.BaseType);
            }
            if (type.TypeKind != TypeKind.Enum)
            {
                foreach (var iface in Sort(type.Interfaces))
                {
                    if (!showAllInterfaces)
                    {
                        // Don't add an interface inherited by one already added.
                        if (bases.Any(t => t.Interfaces.Contains(iface, SymbolEqualityComparer.Default)))
                        {
                            continue;
                        }
                        // Remove any previously added interfaces inherited by the one we're adding now.
                        bases.RemoveAll(t => iface.Interfaces.Contains(t, SymbolEqualityComparer.Default));
                    }
                    bases.Add(iface);
                }
            }
            else if (type.EnumUnderlyingType != null)
            {
                bases.Add(type.EnumUnderlyingType);
            }
            for (int i = 0; i < bases.Count; i++)
            {
                sb.Append(i == 0 ? " : " : ", ").AppendType(bases[i]);
            }
            sb.AppendTypeConstraints(constraints);
            PrintLine(sb.ToString(), indent, openBlock: true);
            foreach (var member in Sort(type.GetMembers()))
            {
                if (type.TypeKind != TypeKind.Enum)
                {
                    PrintMember(member, indent + 1, type.TypeKind == TypeKind.Interface);
                }
                else if (member is IFieldSymbol field)
                {
                    PrintLine($"{field.Name} = {field.ConstantValue},", indent + 1);
                }
            }
            PrintEndBlock(indent);
        }

        private static readonly char[] escapes
            = { '0', '\0', '\0', '\0', '\0', '\0', '\0', 'a', 'b', 't', 'n', 'v', 'f', 'r' };

        private static readonly Dictionary<string, string> conversionNames
            = new Dictionary<string, string>
            {
                [WellKnownMemberNames.ExplicitConversionName] = "explicit",
                [WellKnownMemberNames.ImplicitConversionName] = "implicit",
            };

        private static readonly Dictionary<string, string> operators
            = new Dictionary<string, string>
            {
                [WellKnownMemberNames.AdditionOperatorName] = "+",
                [WellKnownMemberNames.BitwiseAndOperatorName] = "&",
                [WellKnownMemberNames.BitwiseOrOperatorName] = "|",
                [WellKnownMemberNames.DecrementOperatorName] = "--",
                [WellKnownMemberNames.DivisionOperatorName] = "/",
                [WellKnownMemberNames.EqualityOperatorName] = "==",
                [WellKnownMemberNames.ExclusiveOrOperatorName] = "^",
                [WellKnownMemberNames.FalseOperatorName] = "false",
                [WellKnownMemberNames.GreaterThanOperatorName] = ">",
                [WellKnownMemberNames.GreaterThanOrEqualOperatorName] = ">=",
                [WellKnownMemberNames.IncrementOperatorName] = "++",
                [WellKnownMemberNames.InequalityOperatorName] = "!=",
                [WellKnownMemberNames.LeftShiftOperatorName] = "<<",
                [WellKnownMemberNames.LessThanOperatorName] = "<",
                [WellKnownMemberNames.LessThanOrEqualOperatorName] = "<=",
                [WellKnownMemberNames.LogicalNotOperatorName] = "!",
                [WellKnownMemberNames.ModulusOperatorName] = "%",
                [WellKnownMemberNames.MultiplyOperatorName] = "*",
                [WellKnownMemberNames.OnesComplementOperatorName] = "~",
                [WellKnownMemberNames.RightShiftOperatorName] = ">>",
                [WellKnownMemberNames.SubtractionOperatorName] = "-",
                [WellKnownMemberNames.TrueOperatorName] = "true",
                [WellKnownMemberNames.UnaryNegationOperatorName] = "-",
                [WellKnownMemberNames.UnaryPlusOperatorName] = "+",
            };

        private static void PrintMember(ISymbol member, int indent, bool inInterface)
        {
            if (member.Kind == SymbolKind.Method)
            {
                switch (((IMethodSymbol)member).MethodKind)
                {
                case MethodKind.Destructor:
                    PrintLine($"~{member.ContainingType.Name}();", indent);
                    return;
                case MethodKind.Constructor:
                    if (member.ContainingType.TypeKind == TypeKind.Struct
                        && ((IMethodSymbol)member).Parameters.IsDefaultOrEmpty)
                    {
                        return;
                    }
                    break;
                case MethodKind.ExplicitInterfaceImplementation:
                case MethodKind.StaticConstructor:
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                case MethodKind.EventAdd:
                case MethodKind.EventRemove:
                    return;
                }
            }
            else if ((member is IEventSymbol eventSymbol
                && !eventSymbol.ExplicitInterfaceImplementations.IsDefaultOrEmpty)
                || (member is IPropertySymbol property
                && !property.ExplicitInterfaceImplementations.IsDefaultOrEmpty))
            {
                return;
            }
            var sb = new StringBuilder();
            if (!inInterface)
            {
                switch (member.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                        sb.Append("public ");
                        break;
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        sb.Append("protected ");
                        break;
                    case Accessibility.Private:
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.Internal:
                        return;
                    default:
                        throw new Exception($"{member.Kind} member has unexpected"
                            + $" visibility {member.DeclaredAccessibility}: {member}");
                }
            }
            switch (member)
            {
            case INamedTypeSymbol nestedType:
                PrintType(nestedType, indent);
                break;
            case IFieldSymbol field:
                bool isFixed = field.IsFixedSizeBuffer;
                if (isFixed)
                {
                    sb.Append("fixed ");
                }
                else if (field.IsConst)
                {
                    sb.Append("const ");
                }
                else
                {
                    if (field.IsStatic) sb.Append("static ");
                    if (field.IsReadOnly) sb.Append("readonly ");
                    else if (field.IsVolatile) sb.Append("volatile ");
                }
                sb.AppendType(isFixed ? ((IPointerTypeSymbol)field.Type).PointedAtType : field.Type);
                sb.Append(' ').Append(field.Name);
                if (isFixed)
                {
                    sb.Append("[]");
                }
                else if (field.HasConstantValue)
                {
                    sb.Append(" = ");
                    if (field.ConstantValue is string s)
                    {
                        sb.Append('"');
                        foreach (int c in s)
                        {
                            if (c < escapes.Length && escapes[c] != 0) sb.Append('\\').Append(escapes[c]);
                            else if (c < 32 || c == 127) sb.Append($"\\x{c:x2}");
                            else if (c == '\\') sb.Append("\\\\");
                            else if (c == '"') sb.Append("\\\"");
                            else if (c < 127) sb.Append((char)c);
                            else sb.Append($"\\u{c:x4}");
                        }
                        sb.Append('"');
                    }
                    else
                    {
                        sb.Append(field.ConstantValue ?? "null");
                    }
                }
                PrintLine(sb.Append(';').ToString(), indent);
                break;
            case IEventSymbol eventSymbol:
                if (eventSymbol.IsStatic)
                {
                    sb.Append("static ");
                }
                else if (eventSymbol.IsOverride)
                {
                    if (eventSymbol.IsSealed) sb.Append("sealed ");
                    sb.Append("override ");
                }
                else if (!inInterface)
                {
                    if (eventSymbol.IsAbstract) sb.Append("abstract ");
                    else if (eventSymbol.IsVirtual) sb.Append("virtual ");
                }
                PrintLine(sb.Append("event ").AppendType(eventSymbol.Type).Append(' ')
                    .Append(eventSymbol.Name).Append(';').ToString(), indent);
                break;
            case IMethodSymbol method:
                switch (method.MethodKind)
                {
                case MethodKind.Constructor:
                    PrintLine(sb.Append(method.ContainingType.Name)
                        .AppendParameters(method.Parameters).Append(';').ToString(), indent);
                    return;
                case MethodKind.Ordinary:
                case MethodKind.Conversion:
                case MethodKind.UserDefinedOperator:
                    if (method.IsStatic)
                    {
                        sb.Append("static ");
                    }
                    else if (method.IsOverride)
                    {
                        if (method.IsSealed) sb.Append("sealed ");
                        sb.Append("override ");
                    }
                    else if (!inInterface)
                    {
                        if (method.IsAbstract) sb.Append("abstract ");
                        else if (method.IsVirtual) sb.Append("virtual ");
                    }
                    if (method.MethodKind == MethodKind.Conversion
                        && conversionNames.TryGetValue(method.Name, out var keyword))
                    {
                        sb.Append(keyword).Append(" operator ").AppendType(method.ReturnType);
                    }
                    else
                    {
                        sb.AppendReturnSignature(method).Append(' ');
                        if (method.MethodKind == MethodKind.UserDefinedOperator
                            && operators.TryGetValue(method.Name, out var opToken))
                        {
                            sb.Append("operator ").Append(opToken);
                        }
                        else
                        {
                            sb.Append(method.Name);
                        }
                    }
                    sb.AppendTypeParameters(method.TypeParameters, out var constraints);
                    sb.AppendParameters(method.Parameters);
                    sb.AppendTypeConstraints(constraints);
                    PrintLine(sb.Append(';').ToString(), indent);
                    break;
                default:
                    throw new Exception($"Unexpected method kind {method.MethodKind}: {method}");
                }
                break;
            case IPropertySymbol property:
                if (property.IsStatic)
                {
                    sb.Append("static ");
                }
                else if (property.IsOverride)
                {
                    if (property.IsSealed) sb.Append("sealed ");
                    sb.Append("override ");
                }
                else if (!inInterface)
                {
                    if (property.IsAbstract) sb.Append("abstract ");
                    else if (property.IsVirtual) sb.Append("virtual ");
                }
                if (property.ReturnsByRefReadonly) sb.Append("ref readonly ");
                else if (property.ReturnsByRef) sb.Append("ref ");
                sb.AppendType(property.Type).Append(' ');
                if (property.IsIndexer)
                {
                    sb.Append("this[").AppendParameters(property.Parameters, false).Append(']');
                }
                else
                {
                    sb.Append(property.Name);
                }
                sb.Append(" { ");
                if (property.GetMethod != null)
                {
                    sb.AppendAccessor("get", property.GetMethod, property);
                }
                if (property.SetMethod != null)
                {
                    sb.AppendAccessor("set", property.SetMethod, property);
                }
                PrintLine(sb.Append('}').ToString(), indent);
                break;
            default:
                throw new Exception($"Unexpected member kind {member.Kind}: {member}");
            }
        }
    }
}
