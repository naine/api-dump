// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
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
            ImmutableArray<IParameterSymbol> parameters,
            bool isExtension = false, char open = '(', char close = ')')
        {
            sb.Append(open);
            for (int i = 0; i < parameters.Length; ++i)
            {
                if (i != 0) sb.Append(", ");
                var p = parameters[i];
                if (isExtension && i == 0) sb.Append("this ");
                else if (p.IsParams) sb.Append("params ");
                sb.Append(p.RefKind switch
                {
                    RefKind.Ref => "ref ",
                    RefKind.Out => "out ",
                    RefKind.In => "in ",
                    RefKind.None => "",
                    _ => throw new Exception($"Invalid ref kind for parameter: {p.RefKind}"),
                }).AppendType(p.Type, p.NullableAnnotation).Append(' ').Append(p.Name);
                if (p.HasExplicitDefaultValue)
                {
                    sb.Append(" = ").AppendConstant(p.ExplicitDefaultValue, p.Type);
                }
            }
            return sb.Append(close);
        }
        private static StringBuilder AppendChar(this StringBuilder sb, int c)
        {
            const string escapes = "0\0\0\0\0\0\0abtnvfr";

            if (c < escapes.Length && escapes[c] != 0) return sb.Append('\\').Append(escapes[c]);
            else if (c < 32 || c == 127) return sb.Append("\\x").AppendHexLiteral(c, 2);
            else if (c == '\\') return sb.Append("\\\\");
            else if (c == '"') return sb.Append("\\\"");
            else if (c < 127) return sb.Append((char)c);
            return sb.Append("\\u").AppendHexLiteral(c, 4);
        }

        // Equivalent to AppendFormat("{0:xN}", c), but avoids a box and a format string parse.
        private static StringBuilder AppendHexLiteral(this StringBuilder sb, int value, int padWidth)
        {
            const string hexLiterals = "0123456789abcdef";

            if (value == 0) return sb.Append('0', padWidth);

            int numDigits = Math.Max(padWidth, (BitOperations.Log2((uint)value) >> 2) + 1);
            for (int i = numDigits - 1; i >= 0; --i)
            {
                sb.Append(hexLiterals[(int)((uint)value >> (i << 2)) & 0xf]);
            }
            return sb;
        }

        private static ulong UnboxEnumValue(object value)
            => value switch
            {
                int x => (uint)x,
                byte x => x,
                uint x => x,
                sbyte x => (byte)x,
                short x => (ushort)x,
                ushort x => x,
                long x => (ulong)x,
                ulong x => x,
                // TODO: This can be simplified under C# 9
                IntPtr x => (ulong)x.ToInt64(),
                UIntPtr x => x.ToUInt64(),
                _ => throw new Exception($"Enum value has invalid type: {value.GetType()}"),
            };

        private static readonly Dictionary<INamedTypeSymbol,
            (bool IsFlags, Dictionary<ulong, string> Values)> enumCache
            = new Dictionary<INamedTypeSymbol,
                (bool, Dictionary<ulong, string>)>(SymbolEqualityComparer.Default);

        private static StringBuilder AppendEnumValue(this StringBuilder sb, object value, INamedTypeSymbol type)
        {
            type = type.OriginalDefinition;
            if (!enumCache.TryGetValue(type, out var enumInfo))
            {
                enumCache[type] = enumInfo = (type.IsFlagsEnum(), new Dictionary<ulong, string>());
                foreach (var member in type.GetMembers())
                {
                    if (member is IFieldSymbol field)
                    {
                        enumInfo.Values.TryAdd(UnboxEnumValue(field.ConstantValue ?? 0), field.Name);
                    }
                }
            }
            ulong rawValue = UnboxEnumValue(value);
            if (enumInfo.IsFlags)
            {
                ulong leftover = 0;
                bool started = false;
                while (rawValue != 0)
                {
                    ulong lowestBit = rawValue & ((ulong)-(long)rawValue);
                    rawValue &= ~lowestBit;
                    if (enumInfo.Values.TryGetValue(lowestBit, out var name))
                    {
                        if (started) sb.Append(" | ");
                        started = true;
                        sb.Append(type.Name).Append('.').Append(name);
                    }
                    else
                    {
                        leftover |= lowestBit;
                    }
                }
                if (leftover != 0)
                {
                    if (started) sb.Append(" | ");
                    started = true;
                    sb.Append(leftover);
                }
                return started ? sb : sb.Append('0');
            }
            else if (enumInfo.Values.TryGetValue(rawValue, out var name))
            {
                return sb.Append(type.Name).Append('.').Append(name);
            }
            return sb.Append(value);
        }

        public static StringBuilder AppendConstant(this StringBuilder sb, object? value, ITypeSymbol type)
        {
            if (value is string s)
            {
                sb.Append('"');
                foreach (int c in s) sb.AppendChar(c);
                return sb.Append('"');
            }
            else if (value is char c)
            {
                return sb.Append('\'').AppendChar(c).Append('\'');
            }
            else if (value is double d)
            {
                // double.ToString() does not produce valid C# expressions for these values.
                // Display references to the member constants instead.
                if (double.IsNaN(d)) return sb.Append("double.NaN");
                else if (double.IsNegativeInfinity(d)) return sb.Append("double.NegativeInfinity");
                else if (double.IsInfinity(d)) return sb.Append("double.PositiveInfinity");
                return sb.Append(d);
            }
            else if (value is float f)
            {
                if (float.IsNaN(f)) return sb.Append("float.NaN");
                else if (float.IsNegativeInfinity(f)) return sb.Append("float.NegativeInfinity");
                else if (float.IsInfinity(f)) return sb.Append("float.PositiveInfinity");
                return sb.Append(f);
            }
            else if (!(value is null))
            {
                return type.TypeKind == TypeKind.Enum
                    ? sb.AppendEnumValue(value, (INamedTypeSymbol)type) : sb.Append(value);
            }
            else if (type.IsReferenceType || type.TypeKind == TypeKind.Pointer)
            {
                return sb.Append("null");
            }
            else if (type.TypeKind == TypeKind.Enum)
            {
                return sb.Append('0');
            }
            else if (type is INamedTypeSymbol namedType)
            {
                var specialType = namedType.OriginalDefinition.SpecialType;
                switch (specialType)
                {
                case SpecialType.System_Boolean:
                    return sb.Append("false");
                case SpecialType.System_Char:
                    return sb.Append("'\\0'");
                case SpecialType.System_Nullable_T:
                    return sb.Append("null");
                }
                // TODO: In C# 9, this can be inlined into the switch using relational patterns.
                if (specialType >= SpecialType.System_SByte
                    && specialType <= SpecialType.System_Double)
                {
                    return sb.Append('0');
                }
            }
            return sb.Append("default");
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
