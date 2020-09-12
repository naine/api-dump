// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ApiDump
{
    static class StringBuilderExtensions
    {
        public static StringBuilder AppendAccessor(this StringBuilder sb,
            string name, IMethodSymbol accessor, IPropertySymbol property)
        {
            switch (accessor.DeclaredAccessibility)
            {
            case Accessibility.Protected:
            case Accessibility.ProtectedOrInternal:
                if (property.DeclaredAccessibility == Accessibility.Public)
                {
                    sb.Append("protected ");
                }
                break;
            case Accessibility.Private:
            case Accessibility.ProtectedAndInternal:
            case Accessibility.Internal:
                return sb;
            }
            return sb.Append(name).Append("; ");
        }

        public static StringBuilder AppendType(this StringBuilder sb, ITypeSymbol type)
        {
            switch (type)
            {
            case INamedTypeSymbol namedType:
                switch (namedType.SpecialType)
                {
                case SpecialType.System_Boolean:
                    return sb.Append("bool");
                case SpecialType.System_Object:
                    return sb.Append("object");
                case SpecialType.System_Void:
                    return sb.Append("void");
                case SpecialType.System_Char:
                    return sb.Append("char");
                case SpecialType.System_SByte:
                    return sb.Append("sbyte");
                case SpecialType.System_Byte:
                    return sb.Append("byte");
                case SpecialType.System_Int16:
                    return sb.Append("short");
                case SpecialType.System_UInt16:
                    return sb.Append("ushort");
                case SpecialType.System_Int32:
                    return sb.Append("int");
                case SpecialType.System_UInt32:
                    return sb.Append("uint");
                case SpecialType.System_Int64:
                    return sb.Append("long");
                case SpecialType.System_UInt64:
                    return sb.Append("ulong");
                case SpecialType.System_Decimal:
                    return sb.Append("decimal");
                case SpecialType.System_Single:
                    return sb.Append("float");
                case SpecialType.System_Double:
                    return sb.Append("double");
                case SpecialType.System_String:
                    return sb.Append("string");
                }
                sb.Append(namedType.Name);
                if (namedType.TypeArguments.IsDefaultOrEmpty) return sb;
                sb.Append('<');
                for (int i = 0; i < namedType.TypeArguments.Length; i++)
                {
                    if (i != 0) sb.Append(", ");
                    sb.AppendType(namedType.TypeArguments[i]);
                }
                return sb.Append('>');
            case IPointerTypeSymbol pointerType:
                return sb.AppendType(pointerType.PointedAtType).Append('*');
            case IArrayTypeSymbol arrayType:
                sb.AppendType(arrayType.ElementType).Append('[');
                for (int i = 1; i < arrayType.Rank; ++i) sb.Append(',');
                return sb.Append(']');
            }
            return sb.Append(type.TypeKind switch
            {
                TypeKind.Dynamic => "dynamic",
                TypeKind.TypeParameter => type.Name,
                _ => throw new SimpleException($"Type {type} has unexpected kind {type.TypeKind}"),
            });
        }

        public static StringBuilder AppendReturnSignature(this StringBuilder sb, IMethodSymbol method)
        {
            if (method.ReturnsVoid) return sb.Append("void");
            else if (method.ReturnsByRefReadonly) sb.Append("ref readonly ");
            else if (method.ReturnsByRef) sb.Append("ref ");
            return sb.AppendType(method.ReturnType);
        }

        public static StringBuilder AppendParameters(this StringBuilder sb,
            ImmutableArray<IParameterSymbol> parameters, bool withParentheses = true)
        {
            if (withParentheses) sb.Append("(");
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
                }).AppendType(p.Type).Append(' ').Append(p.Name);
            }
            return withParentheses ? sb.Append(')') : sb;
        }

        public static StringBuilder AppendTypeParameters(this StringBuilder sb,
            ImmutableArray<ITypeParameterSymbol> tParams,
            out List<(string TParamName, List<string> Constraint)>? constraints)
        {
            constraints = null;
            if (tParams.Length != 0)
            {
                sb.Append('<');
                for (int i = 0; i < tParams.Length; i++)
                {
                    if (i != 0) sb.Append(", ");
                    var param = tParams[i];
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
                    foreach (var type in param.ConstraintTypes)
                    {
                        constraint.Add(new StringBuilder().AppendType(type).ToString());
                    }
                    if (param.HasConstructorConstraint)
                    {
                        constraint.Add("new()");
                    }
                    if (constraint.Count != 0)
                    {
                        constraints ??= new List<(string, List<string>)>();
                        constraints.Add((param.Name, constraint));
                    }
                }
                sb.Append('>');
            }
            return sb;
        }

        public static StringBuilder AppendTypeConstraints(this StringBuilder sb,
            List<(string, List<string>)>? constraints)
        {
            if (constraints != null)
            {
                foreach ((string param, var constraint) in constraints)
                {
                    sb.Append(" where ").Append(param).Append(" : ");
                    for (int i = 0; i < constraint.Count; i++)
                    {
                        if (i != 0) sb.Append(", ");
                        sb.Append(constraint[i]);
                    }
                }
            }
            return sb;
        }
    }
}
