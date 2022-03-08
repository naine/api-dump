// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ApiDump
{
    static class StringBuilderExtensions
    {
        public static void AppendAccessor(this StringBuilder sb, bool isSetter,
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
                return;
            }
            bool isInitAccessor = isSetter && accessor.IsInitOnly;
            if (inMutableStruct && !isInitAccessor && accessor.IsReadOnly)
            {
                sb.Append("readonly ");
            }
            sb.Append(isInitAccessor ? "init" : (isSetter ? "set" : "get"));
            sb.Append("; ");
        }

        private static string?[]? typeKeywords;

        private static bool IsKeywordType(SpecialType type, [NotNullWhen(true)] out string? keyword)
        {
            var keywords = typeKeywords ?? Init();
            if ((uint)type < (uint)keywords.Length)
            {
                return (keyword = keywords[(uint)type]) is not null;
            }
            keyword = null;
            return false;

            [MethodImpl(MethodImplOptions.NoInlining)]
            static string?[] Init()
            {
                var keywords = new string?[1 + (uint)SpecialType.System_String];
                keywords[(uint)SpecialType.System_Object] = "object";
                keywords[(uint)SpecialType.System_Void] = "void";
                keywords[(uint)SpecialType.System_Byte] = "byte";
                keywords[(uint)SpecialType.System_SByte] = "sbyte";
                keywords[(uint)SpecialType.System_Int16] = "short";
                keywords[(uint)SpecialType.System_UInt16] = "ushort";
                keywords[(uint)SpecialType.System_Int32] = "int";
                keywords[(uint)SpecialType.System_UInt32] = "uint";
                keywords[(uint)SpecialType.System_Int64] = "long";
                keywords[(uint)SpecialType.System_UInt64] = "ulong";
                keywords[(uint)SpecialType.System_Single] = "float";
                keywords[(uint)SpecialType.System_Double] = "double";
                keywords[(uint)SpecialType.System_Decimal] = "decimal";
                keywords[(uint)SpecialType.System_Boolean] = "bool";
                keywords[(uint)SpecialType.System_Char] = "char";
                keywords[(uint)SpecialType.System_String] = "string";
                return typeKeywords = keywords;
            }
        }

        public static void AppendType(this StringBuilder sb, ITypeSymbol type)
        {
            if (type.IsNativeIntegerType)
            {
                sb.Append(type.SpecialType switch
                {
                    SpecialType.System_IntPtr => "nint",
                    SpecialType.System_UIntPtr => "nuint",
                    _ => throw new($"Unknown native integer type '{type}' (SpecialType={type.SpecialType},"
                        + $" UnderlyingType={(type as INamedTypeSymbol)?.NativeIntegerUnderlyingType})"),
                });
                return;
            }
            switch (type)
            {
            case INamedTypeSymbol namedType:
                ImmutableArray<IFieldSymbol> tupleElements;
                if (IsKeywordType(namedType.SpecialType, out var keyword))
                {
                    sb.Append(keyword);
                }
                else if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    sb.AppendType(namedType.TypeArguments[0]);
                    sb.Append('?');
                    return;
                }
                else if (namedType.IsTupleType && (tupleElements = namedType.TupleElements).Length > 1)
                {
                    sb.Append('(');
                    for (int i = 0; i < tupleElements.Length; ++i)
                    {
                        if (i != 0) sb.Append(", ");
                        var element = tupleElements[i];
                        sb.AppendType(element.Type);
                        if (element.IsExplicitlyNamedTupleElement)
                        {
                            sb.Append(' ');
                            sb.Append(element.Name);
                        }
                    }
                    sb.Append(')');
                    return;
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
                sb.AppendType(pointerType.PointedAtType);
                sb.Append('*');
                return;
            case IArrayTypeSymbol arrayType:
                sb.AppendType(arrayType.ElementType);
                sb.Append('[');
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
                var callConv = fnSig.CallingConvention;
                if (callConv == SignatureCallingConvention.Unmanaged)
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
                                && Program.IsCompilerServices(ccModType.ContainingNamespace)
                                ? modName.AsSpan(8) : modName);
                        }
                        sb.Append(']');
                    }
                }
                else
                {
                    sb.Append(callConv switch
                    {
                        SignatureCallingConvention.Default => "",
                        SignatureCallingConvention.CDecl => " unmanaged[Cdecl]",
                        SignatureCallingConvention.StdCall => " unmanaged[Stdcall]",
                        SignatureCallingConvention.ThisCall => " unmanaged[Thiscall]",
                        SignatureCallingConvention.FastCall => " unmanaged[Fastcall]",
                        _ => throw new($"Unknown calling convention {callConv}"),
                    });
                }
                var fnParams = fnSig.Parameters;
                sb.AppendParameters(fnParams, false, '<', null);
                if (!fnParams.IsDefaultOrEmpty) sb.Append(", ");
                sb.AppendReturnSignature(fnSig);
                sb.Append('>');
                return;
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
        }

        public static void AppendReturnSignature(this StringBuilder sb, IMethodSymbol method)
        {
            if (method.ReturnsVoid)
            {
                sb.Append("void");
            }
            else
            {
                sb.Append(method.RefKind switch
                {
                    RefKind.Ref => "ref ",
                    RefKind.RefReadOnly => "ref readonly ",
                    RefKind.None => "",
                    _ => throw new($"Invalid ref kind for return: {method.RefKind}"),
                });
                sb.AppendType(method.ReturnType);
            }
        }

        public static void AppendParameters(this StringBuilder sb,
            ImmutableArray<IParameterSymbol> parameters,
            bool isExtension = false, char open = '(', char? close = ')')
        {
            sb.Append(open);
            for (int i = 0; i < parameters.Length; ++i)
            {
                if (i != 0) sb.Append(", ");
                var p = parameters[i];
                sb.Append(p.RefKind switch
                {
                    RefKind.Ref => "ref ",
                    RefKind.Out => "out ",
                    RefKind.In => "in ",
                    RefKind.None => "",
                    _ => throw new($"Invalid ref kind for parameter: {p.RefKind}"),
                });
                if (isExtension && i == 0) sb.Append("this ");
                else if (p.IsParams) sb.Append("params ");
                var type = p.Type;
                sb.AppendType(type);
                string name = p.Name;
                if (!string.IsNullOrEmpty(name))
                {
                    sb.Append(' ');
                    sb.Append(name);
                }
                if (p.HasExplicitDefaultValue)
                {
                    sb.Append(" = ");
                    sb.AppendConstant(p.ExplicitDefaultValue, type);
                }
            }
            if (close.HasValue) sb.Append(close.GetValueOrDefault());
        }

        private static void AppendChar(this StringBuilder sb, int c)
        {
            const string escapes = "0\0\0\0\0\0\0abtnvfr";

            if (c < escapes.Length && escapes[c] != 0)
            {
                sb.Append('\\');
                sb.Append(escapes[c]);
            }
            else if (c < 32 || c == 127)
            {
                sb.Append("\\x");
                sb.AppendHexLiteral(c, 2);
            }
            else if (c == '\\')
            {
                sb.Append("\\\\");
            }
            else if (c == '"')
            {
                sb.Append("\\\"");
            }
            else if (c < 127)
            {
                sb.Append((char)c);
            }
            else
            {
                sb.Append("\\u");
                sb.AppendHexLiteral(c, 4);
            }
        }

        // Equivalent to AppendFormat("{0:xN}", c), but avoids a box and a format string parse.
        private static void AppendHexLiteral(this StringBuilder sb, int value, int padWidth)
        {
            const string hexLiterals = "0123456789abcdef";

            if (value == 0)
            {
                sb.Append('0', padWidth);
            }
            else
            {
                int numDigits = Math.Max(padWidth, (BitOperations.Log2((uint)value) >> 2) + 1);
                for (int i = numDigits - 1; i >= 0; --i)
                {
                    sb.Append(hexLiterals[(int)((uint)value >> (i << 2)) & 0xf]);
                }
            }
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

        private static void AppendEnumValue(this StringBuilder sb, object value, INamedTypeSymbol type)
        {
            type = type.OriginalDefinition;
            ref var enumInfo = ref CollectionsMarshal.GetValueRefOrAddDefault(enumCache, type, out bool exists);
            if (!exists)
            {
                enumInfo = (type.IsFlagsEnum(), new());
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
                        sb.Append(type.Name);
                        sb.Append('.');
                        sb.Append(name);
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
                if (!started) sb.Append('0');
            }
            else if (enumInfo.Values.TryGetValue(rawValue, out var name))
            {
                sb.Append(type.Name);
                sb.Append('.');
                sb.Append(name);
            }
            else
            {
                sb.Append(value);
            }
        }

        // Note: type may be null ONLY when writing the underlying value of an enum member.
        public static void AppendConstant(this StringBuilder sb, object? value, ITypeSymbol? type)
        {
            if (value is string s)
            {
                sb.Append('"');
                foreach (int c in s) sb.AppendChar(c);
                sb.Append('"');
            }
            else if (value is char c)
            {
                sb.Append('\'');
                sb.AppendChar(c);
                sb.Append('\'');
            }
            else if (value is double d)
            {
                // double.ToString() does not produce valid C# expressions for these values.
                // Display references to the member constants instead.
                if (double.IsNaN(d)) sb.Append("double.NaN");
                else if (double.IsNegativeInfinity(d)) sb.Append("double.NegativeInfinity");
                else if (double.IsInfinity(d)) sb.Append("double.PositiveInfinity");
                else sb.Append(d);
            }
            else if (value is float f)
            {
                if (float.IsNaN(f)) sb.Append("float.NaN");
                else if (float.IsNegativeInfinity(f)) sb.Append("float.NegativeInfinity");
                else if (float.IsInfinity(f)) sb.Append("float.PositiveInfinity");
                else sb.Append(f);
            }
            else if (value is not null)
            {
                if (type is not null && type.TypeKind == TypeKind.Enum)
                {
                    sb.AppendEnumValue(value, (INamedTypeSymbol)type);
                }
                else
                {
                    sb.Append(value);
                }
            }
            else if (type is null)
            {
                sb.Append('0');
            }
            else
            {
                var typeKind = type.TypeKind;
                if (typeKind == TypeKind.Pointer || typeKind == TypeKind.FunctionPointer)
                {
                    sb.Append("null");
                }
                else if (typeKind == TypeKind.Enum || type.IsNativeIntegerType)
                {
                    sb.Append('0');
                }
                else if (type.IsReferenceType)
                {
                    // If a named type is not known to the compilation, Roslyn seems
                    // to assume that it's a class and returns true on IsReferenceType,
                    // but this may not be correct, so we use "default" for this case.
                    sb.Append(typeKind == TypeKind.Error ? "default" : "null");
                }
                else if (type is INamedTypeSymbol namedType)
                {
                    sb.Append(namedType.OriginalDefinition.SpecialType switch
                    {
                        SpecialType.System_Boolean => "false",
                        SpecialType.System_Char => "'\\0'",
                        SpecialType.System_Nullable_T => "null",
                        >= SpecialType.System_SByte and <= SpecialType.System_UIntPtr => "0",
                        _ => "default",
                    });
                }
                else
                {
                    sb.Append("default");
                }
            }
        }

        public static void AppendTypeParameters(this StringBuilder sb,
            ImmutableArray<ITypeParameterSymbol> tParams,
            out FList<(string TParamName, FList<string> Constraint)> constraints)
        {
            constraints = default;
            if (tParams.Length != 0)
            {
                sb.Append('<');
                for (int i = 0; i < tParams.Length; ++i)
                {
                    if (i != 0) sb.Append(", ");
                    var param = tParams[i];
                    string name = param.Name;
                    sb.Append(param.Variance switch
                    {
                        VarianceKind.In => "in ",
                        VarianceKind.Out => "out ",
                        VarianceKind.None => "",
                        _ => throw new($"Invalid variance kind: {param.Variance}"),
                    });
                    sb.Append(name);
                    FList<string> constraint = default;
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
                            var typeBuilder = new StringBuilder();
                            typeBuilder.AppendType(constraintTypes[j]);
                            constraint.Add(typeBuilder.ToString());
                        }
                    }
                    if (param.HasConstructorConstraint)
                    {
                        constraint.Add("new()");
                    }
                    if (constraint.Count != 0)
                    {
                        constraints.Add((name, constraint));
                    }
                }
                sb.Append('>');
            }
        }

        public static void AppendTypeConstraints(this StringBuilder sb,
            in FList<(string, FList<string>)> constraints)
        {
            foreach ((string param, var constraint) in constraints)
            {
                sb.Append(" where ");
                sb.Append(param);
                sb.Append(" : ");
                for (int i = 0; i < constraint.Count; ++i)
                {
                    if (i != 0) sb.Append(", ");
                    sb.Append(constraint[i]);
                }
            }
        }

        public static void AppendCommonModifiers(this StringBuilder sb,
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
                // Abstract by default. See comment in PrintMember about interface members.
                if (member.DeclaredAccessibility is not Accessibility.NotApplicable
                    and not Accessibility.Public)
                {
                    sb.Append("abstract ");
                }
            }
            else if (!explicitInterfaceImplementation)
            {
                // Non-abstract interface members are implicitly virtual unless declared sealed.
                // Show either way to distinguish virtual members from defaultly abstract members.
                sb.Append(member.IsVirtual ? "virtual " : "sealed ");
            }
        }
    }
}
