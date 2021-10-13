// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

[module: SkipLocalsInit]

namespace ApiDump
{
    /* TODO: The following need to be handled correctly:
     *  - Type names should be qualified where ambiguous or nested and not in scope.
     *  - Add option to control whether forwarded types are hidden, listed, or defined.
     *  - Some attributes should be displayed.
     *    + Use command line options to explicitly show or hide specific attributes.
     *    + Need to support attributes that compile into IL metadata, such as StructLayout.
     *    + There should be a default list of known attributes that are shown/hidden.
     *      ~ Includes attributes that visibly affect consumers, like Obsolete, etc.
     *      ~ Excludes attributes that should be implementation details.
     *      ~ Includes nullability attributes like AllowNull only if not --no-nullable.
     *      ~ Allow specifying default for unknown attributes.
     *  - C# 9 features:
     *    + Unmanaged calling convention methods.
     *    + Records (distinguished from classes with ITypeSymbol.IsRecord).
     *  - C# 10 features:
     *    + Record structs.
     *    + Parameterless struct constructors.
     *    + Reevaluate when 10.0 leaves preview.
     */

    static class Program
    {
        private static bool showAllInterfaces;
        private static bool showUnsafeValueTypes;
        internal static bool ShowNullable { get; private set; } = true;

        static int Main(string[] args)
        {
            var dlls = new List<string>(args.Length);
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
                    case "--show-array-structs":
                        showUnsafeValueTypes = true;
                        break;
                    case "--no-nullable":
                        ShowNullable = false;
                        break;
                    case "-h":
                    case "--help":
                        PrintHelp();
                        return 0;
                    case "-v":
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
                var refs = new List<MetadataReference>(4 + dlls.Count);
                foreach (string path in dlls)
                {
                    try
                    {
                        refs.Add(MetadataReference.CreateFromFile(path));
                    }
                    catch (ArgumentException)
                    {
                        Console.Error.WriteLine("Error: Invalid file path: '{0}'", path);
                        return 1;
                    }
                    catch (FileNotFoundException)
                    {
                        Console.Error.WriteLine("Error: File not found: '{0}'", path);
                        return 1;
                    }
                    catch (IOException e)
                    {
                        Console.Error.WriteLine("Error: Failed to open '{0}': {1}", path, e.Message);
                        return 1;
                    }
                    catch (BadImageFormatException)
                    {
                        Console.Error.WriteLine("Error: File is not a valid IL assembly: '{0}'", path);
                        return 1;
                    }
                }
                if (useInternalBCL)
                {
                    using var zip = new ZipArchive(
                        typeof(Program).Assembly.GetManifestResourceStream("ApiDump.DummyBCL.zip")!);
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
                    new(OutputKind.DynamicallyLinkedLibrary));
                var diagnostics = comp.GetDiagnostics();
                if (!diagnostics.IsDefaultOrEmpty)
                {
                    Console.Error.WriteLine("Warning: Compilation has diagnostics:");
                    foreach (var diagnostic in diagnostics) Console.Error.WriteLine(diagnostic);
                }
                var globalNamespaces = comp.GlobalNamespace.ConstituentNamespaces;
                if (globalNamespaces.Length != refs.Count + 1)
                {
                    throw new($"Unexpected number of global namespaces: {globalNamespaces.Length}");
                }
                if (!SymbolEqualityComparer.Default.Equals(
                    globalNamespaces[0], comp.Assembly.GlobalNamespace))
                {
                    throw new("Unexpected first global namespace:"
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
            if (copyright is not null)
            {
                Console.WriteLine(copyright.Copyright.Replace("\u00A9", "(C)", StringComparison.Ordinal));
            }
        }

        public static bool StartsWith(this ReadOnlySpan<char> span, char value)
            => !span.IsEmpty && span[0] == value;

        private static void PrintHelp()
        {
            var assembly = typeof(Program).Assembly;
            PrintVersion(assembly);
            Console.WriteLine();
            Console.WriteLine("Usage: {0} [options] <dllpaths>...", assembly.GetName().Name);

            string readme;
            using (var stream = assembly.GetManifestResourceStream($"{nameof(ApiDump)}.README.md"))
            {
                if (stream is null) return;
                if (stream is MemoryStream ms && ms.TryGetBuffer(out var msBuffer))
                {
                    // Optimistic case that avoids an extra copy/buffering layer.
                    // Realistically only one of these paths should ever be used on any
                    // given runtime, but it can't hurt to have both this and a fallback.
                    readme = Encoding.UTF8.GetString(msBuffer);
                }
                else
                {
                    using var reader = new StreamReader(stream, Encoding.UTF8, false);
                    readme = reader.ReadToEnd();
                }
            }

            int start = 1 + readme.IndexOf('\n', 9 + readme.IndexOf("# Options", StringComparison.Ordinal));
            readme = readme[start..].Replace("`", "", StringComparison.Ordinal);
            readme = Regex.Replace(readme, @"\[([^\]]*)\]\([^\)]*\)", "$1").Trim();

            Console.WriteLine();
            Console.WriteLine("Options:");
            int width = Math.Max(Console.WindowWidth, 80) - 6;
            foreach (string para in Regex.Split(readme, @"\s*\n(?:\s*\n)+\s*", RegexOptions.ECMAScript))
            {
                if (para.AsSpan().TrimStart().StartsWith('-'))
                {
                    // Option line, eg: "- `-h`, `--help`"
                    Console.WriteLine();
                    Console.Write("  ");
                    Console.Out.WriteLine(para.AsSpan(1 + para.IndexOf('-', StringComparison.Ordinal)).Trim());
                }
                else if (!para.Contains('\n', StringComparison.Ordinal))
                {
                    // Single-line option description
                    Console.WriteLine();
                    Console.Write("    ");
                    Console.Out.WriteLine(para.AsSpan().Trim());
                }
                else
                {
                    // Multi-line option description
                    int pos = width;
                    foreach (var word in new SpanSplitter(para))
                    {
                        if (pos + 1 + word.Length > width)
                        {
                            Console.WriteLine();
                            Console.Write("    ");
                            Console.Out.Write(word);
                            pos = word.Length;
                        }
                        else
                        {
                            Console.Write(' ');
                            Console.Out.Write(word);
                            pos += 1 + word.Length;
                        }
                    }
                    Console.WriteLine();
                }
            }
        }

        private static bool emptyBlockOpen;

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
                        PrintLine(AppendName(new StringBuilder("namespace ", 64), ns).ToString(), 0);
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

            static StringBuilder AppendName(StringBuilder sb, INamespaceSymbol ns)
            {
                var parent = ns.ContainingNamespace;
                if (parent is not null && !parent.IsGlobalNamespace)
                {
                    AppendName(sb, parent).Append('.');
                }
                return sb.Append(ns.Name);
            }
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
                throw new($"Type {type} has unexpected visibility {type.DeclaredAccessibility}");
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
                if (!showUnsafeValueTypes && type.IsUnsafeValueType()) return;
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
                if (type.DelegateInvokeMethod is null)
                {
                    throw new($"Delegate type has null invoke method: {type}");
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
                throw new($"Named type {type} has unexpected kind {type.TypeKind}");
            }
            sb.AppendTypeParameters(type.TypeParameters, out var constraints);
            var bases = new List<INamedTypeSymbol>();
            if (type.BaseType is not null && type.TypeKind == TypeKind.Class
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
            else if (type.EnumUnderlyingType is not null)
            {
                bases.Add(type.EnumUnderlyingType);
            }
            for (int i = 0; i < bases.Count; ++i)
            {
                sb.Append(i == 0 ? " : " : ", ").AppendType(bases[i]);
            }
            sb.AppendTypeConstraints(constraints);
            PrintLine(sb.ToString(), indent, openBlock: true);
            foreach (var member in Sort(type.GetMembers()))
            {
                if (type.TypeKind != TypeKind.Enum)
                {
                    PrintMember(member, indent + 1);
                }
                else if (member is IFieldSymbol field)
                {
                    PrintLine(new StringBuilder().Append(field.Name).Append(" = ")
                        .AppendConstant(field.ConstantValue, type.EnumUnderlyingType!)
                        .Append(',').ToString(), indent + 1);
                }
            }
            PrintEndBlock(indent);
        }

        private static readonly Dictionary<string, string> conversionNames = new()
        {
            [WellKnownMemberNames.ExplicitConversionName] = "explicit",
            [WellKnownMemberNames.ImplicitConversionName] = "implicit",
        };

        private static readonly Dictionary<string, string> operators = new()
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

        private static void PrintMember(ISymbol member, int indent)
        {
            var containingType = member.ContainingType;
            if (member is IMethodSymbol m)
            {
                switch (m.MethodKind)
                {
                case MethodKind.Destructor:
                    PrintLine($"~{containingType.Name}();", indent);
                    return;
                case MethodKind.Constructor:
                    if (containingType.TypeKind == TypeKind.Struct && m.Parameters.IsDefaultOrEmpty)
                    {
                        // TODO: This may no longer be correct under C# 10.
                        // Need to distinguish between a real default ctor loaded from metadata,
                        // and the dummy MethodSymbols created to support new() on all structs.
                        return;
                    }
                    break;
                case MethodKind.StaticConstructor:
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                case MethodKind.EventAdd:
                case MethodKind.EventRemove:
                    return;
                }
                if (!m.ExplicitInterfaceImplementations.IsDefaultOrEmpty)
                {
                    foreach (var impl in m.ExplicitInterfaceImplementations)
                    {
                        PrintExplicitImplementation(m, impl, indent);
                    }
                    if (!m.CanBeReferencedByName) return;
                }
            }
            else if (member is IEventSymbol eventSymbol
                && !eventSymbol.ExplicitInterfaceImplementations.IsDefaultOrEmpty)
            {
                foreach (var impl in eventSymbol.ExplicitInterfaceImplementations)
                {
                    PrintExplicitImplementation(eventSymbol, impl, indent);
                }
                if (!eventSymbol.CanBeReferencedByName) return;
            }
            else if (member is IPropertySymbol property
                && !property.ExplicitInterfaceImplementations.IsDefaultOrEmpty)
            {
                foreach (var impl in property.ExplicitInterfaceImplementations)
                {
                    PrintExplicitImplementation(property, impl, indent);
                }
                if (!property.CanBeReferencedByName) return;
            }
            var sb = new StringBuilder();
            bool inInterface = containingType.TypeKind == TypeKind.Interface;
            switch (member.DeclaredAccessibility)
            {
            case Accessibility.Public:
                // Try to use pre-8.0 C# syntax for interface members wherever possible.
                // This means that for public abstract members, we hide these modifiers,
                // as they are implied and previously could not be made explicit.
                if (!inInterface || !member.IsAbstract) sb.Append("public ");
                break;
            case Accessibility.Protected:
            case Accessibility.ProtectedOrInternal:
                sb.Append("protected ");
                break;
            case Accessibility.Private:
            case Accessibility.ProtectedAndInternal:
            case Accessibility.Internal:
                return;
            case Accessibility.NotApplicable when inInterface:
                break;
            default:
                throw new($"{member.Kind} member has unexpected"
                    + $" visibility {member.DeclaredAccessibility}: {member}");
            }
            bool inMutableStruct = containingType.TypeKind == TypeKind.Struct
                && !containingType.IsReadOnly && !member.IsStatic;
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
                if (isFixed)
                {
                    sb.AppendType(((IPointerTypeSymbol)field.Type).PointedAtType);
                }
                else
                {
                    sb.AppendType(field.Type);
                }
                sb.Append(' ').Append(field.Name);
                if (isFixed)
                {
                    sb.Append('[');
                    if (field.TryGetFixedBufferSize(out int size)) sb.Append(size);
                    sb.Append(']');
                }
                else if (field.HasConstantValue)
                {
                    sb.Append(" = ").AppendConstant(field.ConstantValue, field.Type);
                }
                PrintLine(sb.Append(';').ToString(), indent);
                break;
            case IEventSymbol eventSymbol:
                bool showAccessors = false;
                IMethodSymbol? add = null, remove = null;
                if (inMutableStruct)
                {
                    add = eventSymbol.AddMethod;
                    remove = eventSymbol.RemoveMethod;
                    switch ((add?.IsReadOnly, remove?.IsReadOnly))
                    {
                    case (true, true):
                    case (true, null):
                    case (null, true):
                        sb.Append("readonly ");
                        break;
                    case (true, false):
                    case (false, true):
                        // Although not allowed in C#, it is technically possible in IL
                        // to mark add/remove accessors individually as readonly.
                        // Show this case using accessor syntax similar to properties.
                        showAccessors = true;
                        break;
                    }
                }
                sb.AppendCommonModifiers(eventSymbol, false).Append("event ")
                    .AppendType(eventSymbol.Type).Append(' ').Append(eventSymbol.Name);
                if (showAccessors)
                {
                    sb.Append(" { ");
                    if (add!.IsReadOnly) sb.Append("readonly ");
                    sb.Append("add; ");
                    if (remove!.IsReadOnly) sb.Append("readonly ");
                    sb.Append("remove; }");
                }
                else
                {
                    sb.Append(';');
                }
                PrintLine(sb.ToString(), indent);
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
                    if (inMutableStruct && method.IsReadOnly) sb.Append("readonly ");
                    sb.AppendCommonModifiers(method, false);
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
                    sb.AppendParameters(method.Parameters, method.IsExtensionMethod);
                    sb.AppendTypeConstraints(constraints);
                    PrintLine(sb.Append(';').ToString(), indent);
                    break;
                default:
                    throw new($"Unexpected method kind {method.MethodKind}: {method}");
                }
                break;
            case IPropertySymbol property:
                if (inMutableStruct && property.GetMethod?.IsReadOnly != false
                    && (property.SetMethod is null or { IsInitOnly: true } or { IsReadOnly: true }))
                {
                    // All non-init accessors are readonly, so display the keyword on the property.
                    // Unsetting inMutableStruct prevents duplicating it onto the accessors.
                    sb.Append("readonly ");
                    inMutableStruct = false;
                }
                sb.AppendCommonModifiers(property, false);
                if (property.ReturnsByRefReadonly) sb.Append("ref readonly ");
                else if (property.ReturnsByRef) sb.Append("ref ");
                sb.AppendType(property.Type).Append(' ');
                if (property.IsIndexer)
                {
                    sb.Append("this").AppendParameters(property.Parameters, false, '[', ']');
                }
                else
                {
                    sb.Append(property.Name);
                }
                sb.Append(" { ");
                if (property.GetMethod is not null)
                {
                    sb.AppendAccessor(false, property.GetMethod, property, inMutableStruct);
                }
                if (property.SetMethod is not null)
                {
                    sb.AppendAccessor(true, property.SetMethod, property, inMutableStruct);
                }
                PrintLine(sb.Append('}').ToString(), indent);
                break;
            default:
                throw new($"Unexpected member kind {member.Kind}: {member}");
            }
        }

        private static void PrintExplicitImplementation(
            IMethodSymbol method, IMethodSymbol implemented, int indent)
        {
            PrintLine(new StringBuilder().AppendCommonModifiers(method, true).AppendReturnSignature(method)
                .Append(' ').AppendType(implemented.ContainingType).Append('.').Append(implemented.Name)
                .AppendTypeParameters(method.TypeParameters, out var constraints)
                .AppendParameters(method.Parameters, method.IsExtensionMethod)
                .AppendTypeConstraints(constraints).Append(';').ToString(), indent);
        }

        private static void PrintExplicitImplementation(
            IPropertySymbol property, IPropertySymbol implemented, int indent)
        {
            var sb = new StringBuilder().AppendCommonModifiers(property, true);
            if (property.ReturnsByRefReadonly) sb.Append("ref readonly ");
            else if (property.ReturnsByRef) sb.Append("ref ");
            sb.AppendType(property.Type).Append(' ').AppendType(implemented.ContainingType).Append('.');
            if (property.IsIndexer)
            {
                sb.Append("this").AppendParameters(property.Parameters, false, '[', ']');
            }
            else
            {
                sb.Append(implemented.Name);
            }
            sb.Append(" { ");
            if (property.GetMethod is not null)
            {
                sb.AppendAccessor(false, property.GetMethod, property, false);
            }
            if (property.SetMethod is not null)
            {
                sb.AppendAccessor(true, property.SetMethod, property, false);
            }
            PrintLine(sb.Append('}').ToString(), indent);
        }

        private static void PrintExplicitImplementation(
            IEventSymbol eventSymbol, IEventSymbol implemented, int indent)
        {
            PrintLine(new StringBuilder().AppendCommonModifiers(eventSymbol, true).Append("event ")
                .AppendType(eventSymbol.Type).Append(' ').AppendType(implemented.ContainingType)
                .Append('.').Append(implemented.Name).Append(';').ToString(), indent);
        }

        // TODO: Generalise this attribute handling code.

        private static bool IsSystem(INamespaceSymbol? ns)
        {
            if (ns is null || ns.IsGlobalNamespace || ns.Name != "System") return false;
            ns = ns.ContainingNamespace;
            return ns is null || ns.IsGlobalNamespace;
        }

        public static bool IsCompilerServices(INamespaceSymbol? ns)
        {
            if (ns is null || ns.IsGlobalNamespace || ns.Name != "CompilerServices") return false;
            ns = ns.ContainingNamespace;
            if (ns is null || ns.IsGlobalNamespace || ns.Name != "Runtime") return false;
            return IsSystem(ns.ContainingNamespace);
        }

        public static bool IsUnsafeValueType(this INamedTypeSymbol structType)
        {
            foreach (var attr in structType.GetAttributes())
            {
                var type = attr.AttributeClass;
                if (type is not null && type.Name == "UnsafeValueTypeAttribute"
                    && IsCompilerServices(type.ContainingNamespace)
                    && type.Arity == 0 && type.ContainingType is null)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsFlagsEnum(this INamedTypeSymbol enumType)
        {
            foreach (var attr in enumType.GetAttributes())
            {
                var type = attr.AttributeClass;
                if (type is not null && type.Name == "FlagsAttribute"
                    && IsSystem(type.ContainingNamespace)
                    && type.Arity == 0 && type.ContainingType is null)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetFixedBufferSize(this IFieldSymbol field, out int size)
        {
            foreach (var attr in field.GetAttributes())
            {
                var type = attr.AttributeClass;
                if (type is null || type.Name != "FixedBufferAttribute"
                    || !IsCompilerServices(type.ContainingNamespace)
                    || type.Arity != 0 || type.ContainingType is not null) continue;
                var args = attr.ConstructorArguments;
                if (args.IsDefault || args.Length != 2 || args[0].Kind != TypedConstantKind.Type) continue;
                var sizeArg = args[1];
                if (sizeArg.Kind == TypedConstantKind.Primitive && sizeArg.Value is int value)
                {
                    size = value;
                    return true;
                }
            }
            Unsafe.SkipInit(out size);
            return false;
        }
    }
}
