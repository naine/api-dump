// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ApiDump
{
    static class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: {0} <dllpaths>...",
                    Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location));
                return 1;
            }
            try
            {
                var refs = new List<MetadataReference>();
                foreach (string path in args)
                {
                    refs.Add(MetadataReference.CreateFromFile(path));
                }
                var comp = CSharpCompilation.Create("dummy", null, refs,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                var errors = comp.GetDiagnostics();
                if (!errors.IsEmpty)
                {
                    throw new SimpleException($"Compilation has errors:\n{string.Join('\n', errors)}");
                }
                var globalNamespaces = comp.GlobalNamespace.ConstituentNamespaces;
                if (globalNamespaces.Length != args.Length + 1)
                {
                    throw new SimpleException("Unexpected number of global namespaces: {0}",
                        globalNamespaces.Length);
                }
                if (!ReferenceEquals(globalNamespaces[0], comp.Assembly.GlobalNamespace))
                {
                    throw new SimpleException("Unexpected first global namespace: {0}",
                        globalNamespaces[0].ContainingAssembly.Name);
                }
                for (int i = 1; i < globalNamespaces.Length; ++i)
                {
                    PrintNamespace(globalNamespaces[i]);
                }
            }
            catch (SimpleException ex)
            {
                Console.Error.WriteLine("Error: {0}", ex.Message);
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: {0}", ex);
                return -1;
            }
            return 0;
        }

        private static bool lineOpen = false;

        private enum LineOption
        {
            None, LeaveOpen, Continue
        }

        private static void PrintLine(string line, int indent, LineOption option = LineOption.None)
        {
            if (lineOpen && option != LineOption.Continue) Console.WriteLine();
            if (lineOpen && option == LineOption.Continue)
            {
                Console.Write(' ');
            }
            else
            {
                for (int i = 0; i < indent; ++i) Console.Write("    ");
            }
            Console.Write(line);
            if (!(lineOpen = option == LineOption.LeaveOpen))
            {
                Console.WriteLine();
            }
        }

        private static void PrintNamespace(INamespaceSymbol ns)
        {
            if (!ns.IsGlobalNamespace) PrintLine($"namespace {FullName(ns)} {{", 0, LineOption.LeaveOpen);
            foreach (var type in ns.GetTypeMembers().OrderBy(t => t.MetadataName))
            {
                if (type.DeclaredAccessibility == Accessibility.Public)
                {
                    PrintType(type, ns.IsGlobalNamespace ? 0 : 1);
                }
            }
            if (!ns.IsGlobalNamespace) PrintLine("}", 0, LineOption.Continue);
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
                throw new SimpleException("Type {0} has unexpected visibility {1}",
                    type, type.DeclaredAccessibility);
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
                sb.Append("delegate ")
                    .AppendReturnSignature(type.DelegateInvokeMethod!)
                    .Append(' ').Append(type.Name);
                break;
            default:
                throw new SimpleException($"Named type {type} has unexpected kind {type.TypeKind}");
            }
            sb.AppendTypeParameters(type.TypeParameters, out var constraints);
            if (type.TypeKind == TypeKind.Delegate)
            {
                sb.AppendParameters(type.DelegateInvokeMethod!.Parameters);
            }
            else
            {
                var bases = new List<StringBuilder>();
                if (type.BaseType != null && type.TypeKind == TypeKind.Class && !IsObject(type.BaseType))
                {
                    bases.Add(new StringBuilder().AppendType(type.BaseType));
                }
                if (type.TypeKind != TypeKind.Enum)
                {
                    foreach (var iface in type.Interfaces)
                    {
                        bases.Add(new StringBuilder().AppendType(iface));
                    }
                }
                else if (type.EnumUnderlyingType != null)
                {
                    bases.Add(new StringBuilder().AppendType(type.EnumUnderlyingType));
                }
                if (bases.Count != 0)
                {
                    sb.Append(" : ");
                    for (int i = 0; i < bases.Count; i++)
                    {
                        if (i != 0) sb.Append(", ");
                        sb.Append(bases[i]);
                    }
                }
            }
            sb.AppendTypeConstraints(constraints);
            if (type.TypeKind == TypeKind.Delegate)
            {
                PrintLine(sb.Append(';').ToString(), indent);
            }
            else
            {
                PrintLine(sb.Append(" {").ToString(), indent, LineOption.LeaveOpen);
                foreach (var member in type.GetMembers())
                {
                    if (type.TypeKind == TypeKind.Enum)
                    {
                        if (member is IFieldSymbol field)
                        {
                            PrintLine($"{field.Name} = {field.ConstantValue},", indent + 1);
                        }
                    }
                    else
                    {
                        PrintMember(member, indent + 1, type.TypeKind == TypeKind.Interface);
                    }
                }
                PrintLine("}", indent, LineOption.Continue);
            }
        }

        private static bool IsObject(INamedTypeSymbol type)
            => type.BaseType == null && type.Name.ToLowerInvariant() == "object";

        private static readonly char[] escapes
            = { '0', '\0', '\0', '\0', '\0', '\0', '\0', 'a', 'b', 't', 'n', 'v', 'f', 'r' };

        private static void PrintMember(ISymbol member, int indent, bool inInterface)
        {
            if (member.Kind == SymbolKind.Method)
            {
                switch (((IMethodSymbol)member).MethodKind)
                {
                case MethodKind.Destructor:
                    PrintLine($"~{member.ContainingType.Name}();", indent);
                    return;
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
                && !eventSymbol.ExplicitInterfaceImplementations.IsEmpty)
                || (member is IPropertySymbol property
                && !property.ExplicitInterfaceImplementations.IsEmpty))
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
                        throw new SimpleException("{0} member has unexpected visibility {1}: {2}",
                            member.Kind, member.DeclaredAccessibility, member);
                }
            }
            if (member is INamedTypeSymbol nestedType)
            {
                PrintType(nestedType, indent);
                return;
            }
            switch (member)
            {
            case IFieldSymbol field:
                if (field.IsConst)
                {
                    sb.Append("const ");
                }
                else
                {
                    if (field.IsStatic) sb.Append("static ");
                    if (field.IsReadOnly) sb.Append("readonly ");
                    else if (field.IsVolatile) sb.Append("volatile ");
                }
                sb.AppendType(field.Type).Append(' ').Append(field.Name);
                if (field.IsConst)
                {
                    sb.Append(" = ");
                    if (field.ConstantValue is string s)
                    {
                        sb.Append('"');
                        foreach (char c in s)
                        {
                            if (c < escapes.Length && escapes[c] != 0) sb.Append('\\').Append(escapes[c]);
                            else if (c < 32 || c == 127) sb.Append($"\\x{(int)c:x2}");
                            else if (c == '\\') sb.Append("\\\\");
                            else if (c == '"') sb.Append("\\\"");
                            else if (c < 127) sb.Append(c);
                            else sb.Append($"\\u{(int)c:x4}");
                        }
                        sb.Append('"');
                    }
                    else
                    {
                        sb.Append(field.ConstantValue ?? "null");
                    }
                }
                PrintLine(sb.Append(';').ToString(), indent);
                return;
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
                return;
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
                    sb.AppendReturnSignature(method).Append(' ').Append(method.Name);
                    sb.AppendTypeParameters(method.TypeParameters, out var constraints);
                    sb.AppendParameters(method.Parameters);
                    sb.AppendTypeConstraints(constraints);
                    PrintLine(sb.Append(';').ToString(), indent);
                    break;
                default:
                    throw new SimpleException($"Unexpected method kind {method.MethodKind}: {method}");
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
                throw new SimpleException($"Unexpected member kind {member.Kind}: {member}");
            }
        }
    }

    class SimpleException : Exception
    {
        public SimpleException(string message)
            : base(message) { }

        public SimpleException(string format, params object?[] args)
            : base(string.Format(format, args)) { }
    }
}
