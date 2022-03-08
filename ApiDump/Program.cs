// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
     *  - Type names should be qualified where ambiguous or not in scope.
     *  - Add option to control whether forwarded types are hidden, listed, or defined.
     *  - Some attributes should be displayed.
     *    + Use command line options to explicitly show or hide specific attributes.
     *    + Need to support attributes that compile into IL metadata, such as StructLayout.
     *    + There should be a default list of known attributes that are shown/hidden.
     *      ~ Includes attributes that affect consumers, like Obsolete, UnmanagedCallersOnly, etc.
     *      ~ Excludes attributes that only affect implementation.
     *      ~ Includes nullability attributes like AllowNull only if not --no-nullable.
     *      ~ Allow specifying default for unknown attributes.
     *  - C# 9 features:
     *    + Records (distinguished from classes with ITypeSymbol.IsRecord).
     *  - C# 10 features:
     *    + Record structs.
     *    + Static abstract members in interfaces.
     *  - C# Preview/vNext features:
     *    + Ref fields.
     *    + Checked operators.
     *    + Required members.
     */

    static class Program
    {
        private static bool showAllInterfaces;
        private static bool showUnsafeValueTypes;
        internal static bool ShowNullable { get; private set; } = true;

        static int Main(string[] args)
        {
            var dlls = new FList<string>(args.Length);
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
                var refs = new FList<MetadataReference>(4 + dlls.Count);
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

        private static readonly Func<ISymbol?, ISymbol?> identity = static s => s;

        private static IOrderedEnumerable<T> Sort<T>(IEnumerable<T> symbols) where T : class?, ISymbol?
            => symbols.OrderBy<T, ISymbol?>(identity, MemberOrdering.Comparer);

        private static void PrintNamespace(INamespaceSymbol ns)
        {
            bool printed = false;
            bool isGlobal = ns.IsGlobalNamespace;
            foreach (var type in Sort(ns.GetTypeMembers()))
            {
                if (type.DeclaredAccessibility == Accessibility.Public)
                {
                    if (!printed && !isGlobal)
                    {
                        var sb = new StringBuilder("namespace ", 64);
                        AppendName(sb, ns);
                        PrintLine(sb.ToString(), 0);
                        PrintLine("{", 0);
                        printed = true;
                    }
                    PrintType(type, isGlobal ? 0 : 1);
                }
            }
            if (printed) PrintLine("}", 0);
            foreach (var subNs in ns.GetNamespaceMembers().OrderBy(static t => t.MetadataName))
            {
                PrintNamespace(subNs);
            }

            static void AppendName(StringBuilder sb, INamespaceSymbol ns)
            {
                var parent = ns.ContainingNamespace;
                if (parent is not null && !parent.IsGlobalNamespace)
                {
                    AppendName(sb, parent);
                    sb.Append('.');
                }
                sb.Append(ns.Name);
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
            var kind = type.TypeKind;
            switch (kind)
            {
            case TypeKind.Class:
                if (type.IsStatic) sb.Append("static ");
                else if (type.IsAbstract) sb.Append("abstract ");
                else if (type.IsSealed) sb.Append("sealed ");
                sb.Append("class ");
                sb.Append(type.Name);
                break;
            case TypeKind.Struct:
                if (!showUnsafeValueTypes && type.IsUnsafeValueType()) return;
                if (type.IsReadOnly) sb.Append("readonly ");
                if (type.IsRefLikeType) sb.Append("ref ");
                sb.Append("struct ");
                sb.Append(type.Name);
                break;
            case TypeKind.Interface:
                sb.Append("interface ");
                sb.Append(type.Name);
                break;
            case TypeKind.Enum:
                sb.Append("enum ");
                sb.Append(type.Name);
                var underlyingType = type.EnumUnderlyingType;
                if (underlyingType is not null)
                {
                    sb.Append(" : ");
                    sb.AppendType(underlyingType);
                }
                PrintLine(sb.ToString(), indent, openBlock: true);
                foreach (var member in Sort(type.GetMembers()))
                {
                    if (member is IFieldSymbol field)
                    {
                        sb.Clear();
                        sb.Append(field.Name);
                        sb.Append(" = ");
                        sb.AppendConstant(field.ConstantValue, underlyingType);
                        sb.Append(',');
                        PrintLine(sb.ToString(), indent + 1);
                    }
                }
                PrintEndBlock(indent);
                return;
            case TypeKind.Delegate:
                var invokeMethod = type.DelegateInvokeMethod;
                if (invokeMethod is null)
                {
                    throw new($"Delegate type has null invoke method: {type}");
                }
                sb.Append("delegate ");
                sb.AppendReturnSignature(invokeMethod);
                sb.Append(' ');
                sb.Append(type.Name);
                sb.AppendTypeParameters(type.TypeParameters, out var delegateConstraints);
                sb.AppendParameters(invokeMethod.Parameters);
                sb.AppendTypeConstraints(delegateConstraints);
                sb.Append(';');
                PrintLine(sb.ToString(), indent);
                return;
            default:
                throw new($"Named type {type} has unexpected kind {kind}");
            }
            sb.AppendTypeParameters(type.TypeParameters, out var constraints);
            FList<INamedTypeSymbol> bases = default;
            if (kind == TypeKind.Class)
            {
                var baseType = type.BaseType;
                if (baseType is not null && baseType.SpecialType != SpecialType.System_Object)
                {
                    bases.Add(baseType);
                }
            }
            foreach (var iface in Sort(type.Interfaces))
            {
                if (!showAllInterfaces)
                {
                    // Don't add an interface inherited by one already added.
                    bool alreadyImplied = false;
                    foreach (var t in bases)
                    {
                        if (t.Interfaces.Contains(iface, SymbolEqualityComparer.Default))
                        {
                            alreadyImplied = true;
                            break;
                        }
                    }
                    if (alreadyImplied) continue;
                    // Remove any previously added interfaces inherited by the one we're adding now.
                    var inherited = iface.Interfaces;
                    for (int firstToRemove = 0; firstToRemove < bases.Count; ++firstToRemove)
                    {
                        if (inherited.Contains(bases[firstToRemove], SymbolEqualityComparer.Default))
                        {
                            int pos = firstToRemove;
                            while (++pos < bases.Count)
                            {
                                var current = bases[pos];
                                if (!inherited.Contains(current, SymbolEqualityComparer.Default))
                                {
                                    bases[firstToRemove++] = current;
                                }
                            }
                            bases.TrimEnd(firstToRemove);
                            break;
                        }
                    }
                }
                bases.Add(iface);
            }
            for (int i = 0; i < bases.Count; ++i)
            {
                sb.Append(i == 0 ? " : " : ", ");
                sb.AppendType(bases[i]);
            }
            sb.AppendTypeConstraints(constraints);
            PrintLine(sb.ToString(), indent, openBlock: true);
            foreach (var member in Sort(type.GetMembers()))
            {
                PrintMember(member, indent + 1);
            }
            PrintEndBlock(indent);
        }

        private static bool IsWellKnownConversionName(string name, [NotNullWhen(true)] out string? keyword)
        {
            switch (name)
            {
            case WellKnownMemberNames.ExplicitConversionName:
                keyword = "explicit";
                break;
            case WellKnownMemberNames.ImplicitConversionName:
                keyword = "implicit";
                break;
            default:
                keyword = null;
                return false;
            }
            return true;
        }

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
            var containingTypeKind = containingType.TypeKind;
            MethodKind methodKind = default;
            if (member is IMethodSymbol m)
            {
                methodKind = m.MethodKind;
                switch (methodKind)
                {
                case MethodKind.Destructor:
                    PrintLine($"~{containingType.Name}();", indent);
                    return;
                case MethodKind.Constructor:
                    if (containingTypeKind == TypeKind.Struct
                        && m.IsImplicitlyDeclared && m.Parameters.IsDefaultOrEmpty)
                    {
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
                var explicitImpls = m.ExplicitInterfaceImplementations;
                if (!explicitImpls.IsDefaultOrEmpty)
                {
                    foreach (var impl in explicitImpls)
                    {
                        PrintExplicitImplementation(m, impl, indent);
                    }
                    if (!m.CanBeReferencedByName) return;
                }
            }
            else if (member is IEventSymbol eventSymbol)
            {
                var explicitImpls = eventSymbol.ExplicitInterfaceImplementations;
                if (!explicitImpls.IsDefaultOrEmpty)
                {
                    foreach (var impl in explicitImpls)
                    {
                        PrintExplicitImplementation(eventSymbol, impl, indent);
                    }
                    if (!eventSymbol.CanBeReferencedByName) return;
                }
            }
            else if (member is IPropertySymbol property)
            {
                var explicitImpls = property.ExplicitInterfaceImplementations;
                if (!explicitImpls.IsDefaultOrEmpty)
                {
                    foreach (var impl in explicitImpls)
                    {
                        PrintExplicitImplementation(property, impl, indent);
                    }
                    if (!property.CanBeReferencedByName) return;
                }
            }
            var sb = new StringBuilder();
            bool inInterface = containingTypeKind == TypeKind.Interface;
            switch (member.DeclaredAccessibility)
            {
            case Accessibility.Public:
                // Try to use pre-8.0 C# syntax for interface members where possible.
                // This means that for public or abstract members, we hide these modifiers,
                // as they are the default and previously could not be specified.
                if (!inInterface) sb.Append("public ");
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
            bool inMutableStruct = containingTypeKind == TypeKind.Struct
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
                var type = field.Type;
                if (isFixed)
                {
                    sb.AppendType(((IPointerTypeSymbol)type).PointedAtType);
                }
                else
                {
                    sb.AppendType(type);
                }
                sb.Append(' ');
                sb.Append(field.Name);
                if (isFixed)
                {
                    sb.Append('[');
                    sb.Append(field.FixedSize);
                    sb.Append(']');
                }
                else if (field.HasConstantValue)
                {
                    sb.Append(" = ");
                    sb.AppendConstant(field.ConstantValue, type);
                }
                sb.Append(';');
                PrintLine(sb.ToString(), indent);
                break;
            case IEventSymbol eventSymbol:
                bool showAccessors = false;
                bool? addIsReadOnly = null, removeIsReadOnly = null;
                if (inMutableStruct)
                {
                    addIsReadOnly = eventSymbol.AddMethod?.IsReadOnly;
                    removeIsReadOnly = eventSymbol.RemoveMethod?.IsReadOnly;
                    switch ((addIsReadOnly, removeIsReadOnly))
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
                sb.AppendCommonModifiers(eventSymbol, false);
                sb.Append("event ");
                sb.AppendType(eventSymbol.Type);
                sb.Append(' ');
                sb.Append(eventSymbol.Name);
                if (showAccessors)
                {
                    sb.Append(" { ");
                    if (addIsReadOnly.GetValueOrDefault()) sb.Append("readonly ");
                    sb.Append("add; ");
                    if (removeIsReadOnly.GetValueOrDefault()) sb.Append("readonly ");
                    sb.Append("remove; }");
                }
                else
                {
                    sb.Append(';');
                }
                PrintLine(sb.ToString(), indent);
                break;
            case IMethodSymbol method:
                switch (methodKind)
                {
                case MethodKind.Constructor:
                    sb.Append(containingType.Name);
                    sb.AppendParameters(method.Parameters);
                    sb.Append(';');
                    PrintLine(sb.ToString(), indent);
                    return;
                case MethodKind.Ordinary:
                case MethodKind.Conversion:
                case MethodKind.UserDefinedOperator:
                    if (inMutableStruct && method.IsReadOnly) sb.Append("readonly ");
                    sb.AppendCommonModifiers(method, false);
                    string name = method.Name;
                    if (methodKind == MethodKind.Conversion
                        && IsWellKnownConversionName(name, out var keyword))
                    {
                        sb.Append(keyword);
                        sb.Append(" operator ");
                        sb.AppendType(method.ReturnType);
                    }
                    else
                    {
                        sb.AppendReturnSignature(method);
                        sb.Append(' ');
                        if (methodKind == MethodKind.UserDefinedOperator
                            && operators.TryGetValue(name, out var opToken))
                        {
                            sb.Append("operator ");
                            sb.Append(opToken);
                        }
                        else
                        {
                            sb.Append(name);
                        }
                    }
                    sb.AppendTypeParameters(method.TypeParameters, out var constraints);
                    sb.AppendParameters(method.Parameters, method.IsExtensionMethod);
                    sb.AppendTypeConstraints(constraints);
                    sb.Append(';');
                    PrintLine(sb.ToString(), indent);
                    break;
                default:
                    throw new($"Unexpected method kind {methodKind}: {method}");
                }
                break;
            case IPropertySymbol property:
                var getter = property.GetMethod;
                var setter = property.SetMethod;
                if (inMutableStruct && getter?.IsReadOnly != false
                    && (setter is null or { IsInitOnly: true } or { IsReadOnly: true }))
                {
                    // All non-init accessors are readonly, so display the keyword on the property.
                    // Unsetting inMutableStruct prevents duplicating it onto the accessors.
                    sb.Append("readonly ");
                    inMutableStruct = false;
                }
                sb.AppendCommonModifiers(property, false);
                if (property.ReturnsByRefReadonly) sb.Append("ref readonly ");
                else if (property.ReturnsByRef) sb.Append("ref ");
                sb.AppendType(property.Type);
                sb.Append(' ');
                if (property.IsIndexer)
                {
                    sb.Append("this");
                    sb.AppendParameters(property.Parameters, false, '[', ']');
                }
                else
                {
                    sb.Append(property.Name);
                }
                sb.Append(" { ");
                if (getter is not null)
                {
                    sb.AppendAccessor(false, getter, property, inMutableStruct);
                }
                if (setter is not null)
                {
                    sb.AppendAccessor(true, setter, property, inMutableStruct);
                }
                sb.Append('}');
                PrintLine(sb.ToString(), indent);
                break;
            default:
                throw new($"Unexpected member kind {member.Kind}: {member}");
            }
        }

        private static void PrintExplicitImplementation(
            IMethodSymbol method, IMethodSymbol implemented, int indent)
        {
            var sb = new StringBuilder();
            sb.AppendCommonModifiers(method, true);
            sb.AppendReturnSignature(method);
            sb.Append(' ');
            sb.AppendType(implemented.ContainingType);
            sb.Append('.');
            sb.Append(implemented.Name);
            sb.AppendTypeParameters(method.TypeParameters, out var constraints);
            sb.AppendParameters(method.Parameters, method.IsExtensionMethod);
            sb.AppendTypeConstraints(constraints);
            sb.Append(';');
            PrintLine(sb.ToString(), indent);
        }

        private static void PrintExplicitImplementation(
            IPropertySymbol property, IPropertySymbol implemented, int indent)
        {
            var sb = new StringBuilder();
            sb.AppendCommonModifiers(property, true);
            if (property.ReturnsByRefReadonly) sb.Append("ref readonly ");
            else if (property.ReturnsByRef) sb.Append("ref ");
            sb.AppendType(property.Type);
            sb.Append(' ');
            sb.AppendType(implemented.ContainingType);
            sb.Append('.');
            if (property.IsIndexer)
            {
                sb.Append("this");
                sb.AppendParameters(property.Parameters, false, '[', ']');
            }
            else
            {
                sb.Append(implemented.Name);
            }
            sb.Append(" { ");
            var getter = property.GetMethod;
            if (getter is not null)
            {
                sb.AppendAccessor(false, getter, property, false);
            }
            var setter = property.SetMethod;
            if (setter is not null)
            {
                sb.AppendAccessor(true, setter, property, false);
            }
            sb.Append('}');
            PrintLine(sb.ToString(), indent);
        }

        private static void PrintExplicitImplementation(
            IEventSymbol eventSymbol, IEventSymbol implemented, int indent)
        {
            var sb = new StringBuilder();
            sb.AppendCommonModifiers(eventSymbol, true);
            sb.Append("event ");
            sb.AppendType(eventSymbol.Type);
            sb.Append(' ');
            sb.AppendType(implemented.ContainingType);
            sb.Append('.');
            sb.Append(implemented.Name);
            sb.Append(';');
            PrintLine(sb.ToString(), indent);
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
    }
}
