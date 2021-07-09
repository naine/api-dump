// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ApiDump
{
    static class StringBuilderExtensions
    {
        public static StringBuilder AppendAccessor(this StringBuilder sb, bool isSetter,
            IMethodSymbol accessor, IPropertySymbol property, bool inMutableStruct)
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
            bool isInitAccessor = isSetter && accessor.IsInitOnly;
            if (inMutableStruct && !isInitAccessor && accessor.IsReadOnly)
            {
                sb.Append("readonly ");
            }
            return sb.Append(isInitAccessor ? "init" : (isSetter ? "set" : "get")).Append("; ");
        }

        private static readonly Dictionary<SpecialType, string> keywordTypes = new()
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
            if (type.IsNativeIntegerType)
            {
                return sb.Append(type.SpecialType switch
                {
                    SpecialType.System_IntPtr => "nint",
                    SpecialType.System_UIntPtr => "nuint",
                    _ => throw new($"Unknown native integer type '{type}' (SpecialType={type.SpecialType},"
                        + $" UnderlyingType={(type as INamedTypeSymbol)?.NativeIntegerUnderlyingType})"),
                });
            }
            switch (type)
            {
            case INamedTypeSymbol namedType:
                if (keywordTypes.TryGetValue(namedType.SpecialType, out var keyword))
                {
                    sb.Append(keyword);
                }
                else if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    return sb.AppendType(namedType.TypeArguments[0]).Append('?');
                }
                else if (namedType.IsTupleType && namedType.TupleElements.Length > 1)
                {
                    sb.Append('(');
                    for (int i = 0; i < namedType.TupleElements.Length; ++i)
                    {
                        if (i != 0) sb.Append(", ");
                        var element = namedType.TupleElements[i];
                        sb.AppendType(element.Type);
                        if (!SymbolEqualityComparer.Default.Equals(element, element.CorrespondingTupleField))
                        {
                            sb.Append(' ').Append(element.Name);
                        }
                    }
                    return sb.Append(')');
                }
                else
                {
                    sb.Append(namedType.Name);
                    var typeArguments = namedType.TypeArguments;
                    if (!typeArguments.IsDefaultOrEmpty)
                    {
                        sb.Append('<');
                        for (int i = 0; i < typeArguments.Length; ++i)
                        {
                            if (i != 0) sb.Append(", ");
                            sb.AppendType(typeArguments[i]);
                        }
                        sb.Append('>');
                    }
                }
                break;
            case IPointerTypeSymbol pointerType:
                return sb.AppendType(pointerType.PointedAtType).Append('*');
            case IArrayTypeSymbol arrayType:
                sb.AppendType(arrayType.ElementType).Append('[');
                if (!arrayType.IsSZArray)
                {
                    int rank = arrayType.Rank;
                    if (rank < 2) sb.Append('*');
                    else for (int i = 1; i < rank; ++i) sb.Append(',');
                }
                sb.Append(']');
                break;
            case IFunctionPointerTypeSymbol fnptrType:
                sb.Append("delegate*");
                var fnSig = fnptrType.Signature;
                if (fnSig.CallingConvention == SignatureCallingConvention.Unmanaged)
                {
                    sb.Append(" unmanaged");
                    var ccMods = fnSig.UnmanagedCallingConventionTypes;
                    if (!ccMods.IsDefaultOrEmpty)
                    {
                        sb.Append('[');
                        for (int i = 0; i < ccMods.Length; ++i)
                        {
                            if (i != 0) sb.Append(", ");
                            var ccModType = ccMods[i];
                            string modName = ccModType.Name;
                            sb.Append(modName.Length > 8
                                && modName.StartsWith("CallConv", StringComparison.Ordinal)
                                && ccModType.ContainingNamespace?.FullName() == "System.Runtime.CompilerServices"
                                ? modName.AsSpan()[8..] : modName);
                        }
                        sb.Append(']');
                    }
                }
                else
                {
                    sb.Append(fnSig.CallingConvention switch
                    {
                        SignatureCallingConvention.Default => "",
                        SignatureCallingConvention.CDecl => " unmanaged[Cdecl]",
                        SignatureCallingConvention.StdCall => " unmanaged[Stdcall]",
                        SignatureCallingConvention.ThisCall => " unmanaged[Thiscall]",
                        SignatureCallingConvention.FastCall => " unmanaged[Fastcall]",
                        _ => throw new($"Unknown calling convention {fnSig.CallingConvention}"),
                    });
                }
                sb.AppendParameters(fnSig.Parameters, false, '<', null);
                if (fnSig.Parameters.Length != 0) sb.Append(", ");
                return sb.AppendReturnSignature(fnSig).Append('>');
            default:
                sb.Append(type.TypeKind switch
                {
                    TypeKind.Dynamic => "dynamic",
                    TypeKind.TypeParameter => type.Name,
                    _ => throw new($"Type {type} has unexpected kind {type.TypeKind}"),
                });
                break;
            }
            if (type.NullableAnnotation == NullableAnnotation.Annotated
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
                _ => throw new($"Invalid ref kind for return: {method.RefKind}"),
            }).AppendType(method.ReturnType);
        }

        public static StringBuilder AppendParameters(this StringBuilder sb,
            ImmutableArray<IParameterSymbol> parameters,
            bool isExtension = false, char open = '(', char? close = ')')
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
                    _ => throw new($"Invalid ref kind for parameter: {p.RefKind}"),
                }).AppendType(p.Type);
                if (!string.IsNullOrEmpty(p.Name)) sb.Append(' ').Append(p.Name);
                if (p.HasExplicitDefaultValue)
                {
                    sb.Append(" = ").AppendConstant(p.ExplicitDefaultValue, p.Type);
                }
            }
            if (close.HasValue) sb.Append(close.GetValueOrDefault());
            return sb;
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
                nint x => (nuint)x,
                nuint x => x,
                _ => throw new($"Enum value has invalid type: {value.GetType()}"),
            };

#pragma warning disable RS1024 // False-positive: https://github.com/dotnet/roslyn-analyzers/issues/4568

        private static readonly Dictionary<INamedTypeSymbol,
            (bool IsFlags, Dictionary<ulong, string> Values)> enumCache = new(SymbolEqualityComparer.Default);

#pragma warning restore RS1024

        private static StringBuilder AppendEnumValue(this StringBuilder sb, object value, INamedTypeSymbol type)
        {
            type = type.OriginalDefinition;
            if (!enumCache.TryGetValue(type, out var enumInfo))
            {
                enumCache[type] = enumInfo = (type.IsFlagsEnum(), new());
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
            else if (value is not null)
            {
                return type.TypeKind == TypeKind.Enum
                    ? sb.AppendEnumValue(value, (INamedTypeSymbol)type) : sb.Append(value);
            }
            else if (type.TypeKind == TypeKind.Pointer || type.TypeKind == TypeKind.FunctionPointer)
            {
                return sb.Append("null");
            }
            else if (type.TypeKind == TypeKind.Enum || type.IsNativeIntegerType)
            {
                return sb.Append('0');
            }
            else if (type.IsReferenceType)
            {
                // If a named type is not known to the compilation, Roslyn seems
                // to assume that it's a class and returns true on IsReferenceType,
                // but this may not be correct, so we use "default" for this case.
                return sb.Append(type.TypeKind == TypeKind.Error ? "default" : "null");
            }
            else if (type is INamedTypeSymbol namedType)
            {
                switch (namedType.OriginalDefinition.SpecialType)
                {
                case SpecialType.System_Boolean:
                    return sb.Append("false");
                case SpecialType.System_Char:
                    return sb.Append("'\\0'");
                case SpecialType.System_Nullable_T:
                    return sb.Append("null");
                case >= SpecialType.System_SByte and <= SpecialType.System_UIntPtr:
                    // This range includes string, but if a symbol identifies as string
                    // then the reference type path above will have already been taken.
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
                        _ => throw new($"Invalid variance kind: {param.Variance}"),
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
                        for (int j = 0; j < constraintTypes.Length; ++j)
                        {
                            constraint.Add(new StringBuilder().AppendType(constraintTypes[j]).ToString());
                        }
                    }
                    if (param.HasConstructorConstraint)
                    {
                        constraint.Add("new()");
                    }
                    if (constraint.Count != 0)
                    {
                        constraints ??= new();
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
            if (constraints is not null)
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

        public static StringBuilder AppendCommonModifiers(this StringBuilder sb,
            ISymbol member, bool explicitInterfaceImplementation)
        {
            if (member.IsStatic)
            {
                sb.Append("static ");
            }
            else if (member.IsOverride)
            {
                if (member.IsSealed) sb.Append("sealed ");
                sb.Append("override ");
            }
            else if (member.ContainingType.TypeKind != TypeKind.Interface)
            {
                if (member.IsAbstract) sb.Append("abstract ");
                else if (member.IsVirtual) sb.Append("virtual ");
            }
            else if (member.IsAbstract)
            {
                // Abstract by default. See comment in PrintMember about public accessbility.
                if (member.DeclaredAccessibility != Accessibility.NotApplicable
                    && member.DeclaredAccessibility != Accessibility.Public)
                {
                    sb.Append("abstract ");
                }
            }
            else if (!explicitInterfaceImplementation)
            {
                // Non-abstract interface members are implicitly virtual unless declared sealed.
                // Show either way to distinguish virtual from defaultly abstract members.
                sb.Append(member.IsVirtual ? "virtual " : "sealed ");
            }
            return sb;
        }
    }
}
