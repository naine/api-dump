// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
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

        private static readonly Dictionary<SpecialType, string> keywordTypes
            = new Dictionary<SpecialType, string>
            {
                [SpecialType.System_Object] = "object",
                [SpecialType.System_Void] = "void",
                [SpecialType.System_Byte] = "byte",
                [SpecialType.System_SByte] = "sbyte",
                [SpecialType.System_Int16] = "short",
                [SpecialType.System_UInt16] = "ushort",
                [SpecialType.System_Int32] = "int",
                [SpecialType.System_UInt32] = "uint",
                [SpecialType.System_Int64] = "long",
                [SpecialType.System_UInt64] = "ulong",
                [SpecialType.System_Single] = "float",
                [SpecialType.System_Double] = "double",
                [SpecialType.System_Decimal] = "decimal",
                [SpecialType.System_Boolean] = "bool",
                [SpecialType.System_Char] = "char",
                [SpecialType.System_String] = "string",
            };

        public static StringBuilder AppendType(this StringBuilder sb, ITypeSymbol type)
        {
            switch (type)
            {
            case INamedTypeSymbol namedType:
                if (keywordTypes.TryGetValue(namedType.SpecialType, out var keyword))
                {
                    return sb.Append(keyword);
                }
                if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    return sb.AppendType(namedType.TypeArguments[0]).Append('?');
                }
                if (namedType.IsTupleType)
                {
                    sb.Append('(');
                    for (int i = 0; i < namedType.TupleElements.Length; ++i)
                    {
                        if (i != 0) sb.Append(", ");
                        var element = namedType.TupleElements[i];
                        sb.AppendType(element.Type, element.NullableAnnotation);
                        if (!SymbolEqualityComparer.Default.Equals(element, element.CorrespondingTupleField))
                        {
                            sb.Append(' ').Append(element.Name);
                        }
                    }
                    return sb.Append(')');
                }
                sb.Append(namedType.Name);
                var typeArguments = namedType.TypeArguments;
                if (typeArguments.IsDefaultOrEmpty) return sb;
                var nullabilities = namedType.TypeArgumentNullableAnnotations;
                if (nullabilities.Length != typeArguments.Length)
                {
                    throw new Exception(
                        $"TypeArgumentNullableAnnotations.Length ({nullabilities.Length})"
                        + $" != TypeArguments.Length ({typeArguments.Length})");
                }
                sb.Append('<');
                for (int i = 0; i < typeArguments.Length; ++i)
                {
                    if (i != 0) sb.Append(", ");
                    sb.AppendType(typeArguments[i], nullabilities[i]);
                }
                return sb.Append('>');
            case IPointerTypeSymbol pointerType:
                return sb.AppendType(pointerType.PointedAtType).Append('*');
            case IArrayTypeSymbol arrayType:
                sb.AppendType(arrayType.ElementType, arrayType.ElementNullableAnnotation).Append('[');
                if (!arrayType.IsSZArray)
                {
                    if (arrayType.Rank < 2) sb.Append('*');
                    else for (int i = 1; i < arrayType.Rank; ++i) sb.Append(',');
                }
                return sb.Append(']');
            }
            return sb.Append(type.TypeKind switch
            {
                TypeKind.Dynamic => "dynamic",
                TypeKind.TypeParameter => type.Name,
                // TODO: Support FunctionPointers.
                _ => throw new Exception($"Type {type} has unexpected kind {type.TypeKind}"),
            });
        }

        public static StringBuilder AppendType(this StringBuilder sb,
            ITypeSymbol type, NullableAnnotation nullability)
        {
            sb.AppendType(type);
            if (nullability == NullableAnnotation.Annotated
                && Program.ShowNullable && !type.IsValueType)
            {
                sb.Append('?');
            }
            return sb;
        }

        public static StringBuilder AppendReturnSignature(this StringBuilder sb, IMethodSymbol method)
        {
            return method.ReturnsVoid
                ? sb.Append("void")
                : sb.Append(method.RefKind switch
            {
                RefKind.Ref => "ref ",
                RefKind.RefReadOnly => "ref readonly ",
                RefKind.None => "",
                _ => throw new Exception($"Invalid ref kind for return: {method.RefKind}"),
            }).AppendType(method.ReturnType, method.ReturnNullableAnnotation);
        }

        public static StringBuilder AppendParameters(this StringBuilder sb,
            ImmutableArray<IParameterSymbol> parameters, bool withParentheses = true)
        {
            // TODO: Display optional parameter defaults.
            if (withParentheses) sb.Append("(");
            for (int i = 0; i < parameters.Length; ++i)
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
                    _ => throw new Exception($"Invalid ref kind for parameter: {p.RefKind}"),
                }).AppendType(p.Type, p.NullableAnnotation).Append(' ').Append(p.Name);
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
                for (int i = 0; i < tParams.Length; ++i)
                {
                    if (i != 0) sb.Append(", ");
                    var param = tParams[i];
                    sb.Append(param.Variance switch
                    {
                        VarianceKind.In => "in ",
                        VarianceKind.Out => "out ",
                        VarianceKind.None => "",
                        _ => throw new Exception($"Invalid variance kind: {param.Variance}"),
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
                        constraint.Add(Program.ShowNullable
                            && param.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated
                            ? "class?" : "class");
                    }
                    else if (Program.ShowNullable && param.HasNotNullConstraint)
                    {
                        constraint.Add("notnull");
                    }
                    var constraintTypes = param.ConstraintTypes;
                    if (!constraintTypes.IsDefaultOrEmpty)
                    {
                        var nullabilities = param.ConstraintNullableAnnotations;
                        if (nullabilities.Length != constraintTypes.Length)
                        {
                            throw new Exception(
                                $"ConstraintNullableAnnotations.Length ({nullabilities.Length})"
                                + $" != ConstraintTypes.Length ({constraintTypes.Length})");
                        }
                        for (int j = 0; j < constraintTypes.Length; ++j)
                        {
                            constraint.Add(new StringBuilder()
                                .AppendType(constraintTypes[j], nullabilities[j]).ToString());
                        }
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
            if (!(constraints is null))
            {
                foreach ((string param, var constraint) in constraints)
                {
                    sb.Append(" where ").Append(param).Append(" : ");
                    for (int i = 0; i < constraint.Count; ++i)
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
