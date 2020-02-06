// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
                Console.WriteLine("OK");
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
            if (!ns.IsGlobalNamespace) PrintLine($"namespace {ns.Name} {{", 0, LineOption.LeaveOpen);
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
                    .Append(FormatReturnSignature(type.DelegateInvokeMethod!))
                    .Append(' ').Append(type.Name);
                break;
            default:
                throw new SimpleException($"Named type {type} has unexpected kind {type.TypeKind}");
            }
            var constraints = new List<(string, List<string>)>();
            if (type.TypeParameters.Length != 0)
            {
                sb.Append('<');
                for (int i = 0; i < type.TypeParameters.Length; i++)
                {
                    if (i != 0) sb.Append(", ");
                    var param = type.TypeParameters[i];
                    sb.Append(param.Variance switch
                    {
                        VarianceKind.In => "in ",
                        VarianceKind.Out => "out ",
                        VarianceKind.None => "",
                        _ => throw new SimpleException($"Invalid variance kind: {param.Variance}"),
                    }).Append(param.Name);
                    var constraint = new List<string>();
                    if (param.HasUnmanagedTypeConstraint)
                    {
                        constraint.Add("unmanaged");
                    }
                    else if (param.HasValueTypeConstraint)
                    {
                        constraint.Add("struct");
                    }
                    else if (param.HasReferenceTypeConstraint)
                    {
                        constraint.Add("class");
                    }
                    else if (param.HasNotNullConstraint)
                    {
                        constraint.Add("notnull");
                    }
                    foreach (var cType in param.ConstraintTypes)
                    {
                        constraint.Add(FormatType(cType).ToString());
                    }
                    if (param.HasConstructorConstraint)
                    {
                        constraint.Add("new()");
                    }
                    if (constraint.Count != 0) constraints.Add((param.Name, constraint));
                }
                sb.Append('>');
            }
            if (type.TypeKind == TypeKind.Delegate)
            {
                sb.Append(FormatParameters(type.DelegateInvokeMethod!.Parameters));
            }
            else
            {
                var bases = new List<StringBuilder>();
                if (type.BaseType != null && type.TypeKind == TypeKind.Class
                    && type.BaseType.SpecialType != SpecialType.System_Object)
                {
                    bases.Add(FormatType(type.BaseType));
                }
                if (type.TypeKind != TypeKind.Enum)
                {
                    foreach (var iface in type.Interfaces)
                    {
                        bases.Add(FormatType(iface));
                    }
                }
                else if (type.EnumUnderlyingType != null)
                {
                    bases.Add(FormatType(type.EnumUnderlyingType));
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
            foreach ((string param, var constraint) in constraints)
            {
                sb.Append(" where ").Append(param).Append(" : ");
                for (int i = 0; i < constraint.Count; i++)
                {
                    if (i != 0) sb.Append(", ");
                    sb.Append(constraint[i]);
                }
            }
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

        private static readonly char[] escapes
            = { '0', '\0', '\0', '\0', '\0', '\0', '\0', 'a', 'b', 't', 'n', 'v', 'f', 'r' };

        private static void PrintMember(ISymbol member, int indent, bool inInterface)
        {
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
                sb.Append(FormatType(field.Type)).Append(' ').Append(field.Name);
                if (field.IsConst)
                {
                    sb.Append(" = ");
                    if (field.ConstantValue is string s)
                    {
                        sb.Append('"').Append(s.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
                        foreach (char c in s)
                        {
                            if (c < escapes.Length && escapes[c] != 0) sb.Append('\\').Append(escapes[c]);
                            else if (c < 32 || c == 127) sb.Append($"\\x{(int)c:x2}");
                            else if (c == '\\') sb.Append("\\\\");
                            else if (c == '"') sb.Append("\\\"");
                            else if (c < 127) sb.Append(c);
                            else sb.Append($"\\u{(int)c:x4}");
                        }
                    }
                    else
                    {
                        sb.Append(field.ConstantValue ?? "null");
                    }
                }
                PrintLine(sb.Append(';').ToString(), indent);
                return;
            case IEventSymbol eventSymbol:
                if (eventSymbol.IsStatic) sb.Append("static ");
                else if (eventSymbol.IsOverride) sb.Append("override ");
                else if (eventSymbol.IsAbstract && !inInterface) sb.Append("abstract ");
                else if (eventSymbol.IsSealed) sb.Append("sealed ");
                else if (eventSymbol.IsVirtual && !inInterface) sb.Append("virtual ");
                PrintLine(sb.Append("event ").Append(FormatType(eventSymbol.Type)).Append(' ')
                    .Append(eventSymbol.Name).Append(';').ToString(), indent);
                return;
            case IMethodSymbol method:
                // TODO method, ctor, dtor, optor
                throw new NotImplementedException();
            case IPropertySymbol property:
                // TODO property, indexer
                throw new NotImplementedException();
            default:
                throw new SimpleException($"Unexpected member kind {member.Kind}: {member}");
            }
        }

        private static StringBuilder FormatType(ITypeSymbol type)
        {
            switch (type)
            {
            case INamedTypeSymbol namedType:
                var sb = new StringBuilder(namedType.Name);
                if (!namedType.IsGenericType) return sb;
                sb.Append('<');
                for (int i = 0; i < namedType.TypeArguments.Length; i++)
                {
                    if (i != 0) sb.Append(", ");
                    sb.Append(FormatType(namedType.TypeArguments[i]));
                }
                return sb.Append('>');
            case IPointerTypeSymbol pointerType:
                return FormatType(pointerType.PointedAtType).Append('*');
            case IArrayTypeSymbol arrayType:
                sb = FormatType(arrayType.ElementType).Append('[');
                for (int i = 1; i < arrayType.Rank; ++i) Console.Write(',');
                return sb.Append(']');
            }
            return new StringBuilder(type.TypeKind switch
            {
                TypeKind.Dynamic => "dynamic",
                TypeKind.TypeParameter => type.Name,
                _ => throw new SimpleException($"Type {type} has unexpected kind {type.TypeKind}"),
            });
        }

        private static StringBuilder FormatReturnSignature(IMethodSymbol method)
        {
            var sb = new StringBuilder();
            if (method.ReturnsVoid) return sb.Append("void");
            else if (method.ReturnsByRefReadonly) sb.Append("ref readonly ");
            else if (method.ReturnsByRef) sb.Append("ref ");
            return sb.Append(FormatType(method.ReturnType));
        }

        private static StringBuilder FormatParameters(ImmutableArray<IParameterSymbol> parameters)
        {
            var sb = new StringBuilder("(");
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0) sb.Append(", ");
                var p = parameters[i];
                if (p.IsParams) sb.Append("params ");
                else if (p.IsThis) sb.Append("this ");
                sb.Append(p.RefKind switch
                {
                    RefKind.Ref => "ref ",
                    RefKind.Out => "out ",
                    RefKind.In => "in ",
                    RefKind.None => "",
                    _ => throw new SimpleException($"Invalid ref kind: {p.RefKind}"),
                }).Append(FormatType(p.Type)).Append(' ').Append(p.Name);
            }
            return sb.Append(')');
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
