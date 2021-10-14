// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ApiDump
{
    class MemberOrdering : IComparer<ISymbol?>
    {
        public static readonly MemberOrdering Comparer = new();

        private MemberOrdering() { }

        public int Compare(ISymbol? x, ISymbol? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            int c;
            // Special case enum values, sort by value over name.
            if (x.ContainingType?.TypeKind == TypeKind.Enum &&
                y.ContainingType?.TypeKind == TypeKind.Enum &&
                x is IFieldSymbol xf && y is IFieldSymbol yf)
            {
                object? yValue = yf.ConstantValue;
                return (c = xf.ConstantValue switch
                {
                    int xv when yValue is int yv => xv.CompareTo(yv),
                    byte xv when yValue is byte yv => xv.CompareTo(yv),
                    uint xv when yValue is uint yv => xv.CompareTo(yv),
                    sbyte xv when yValue is sbyte yv => xv.CompareTo(yv),
                    short xv when yValue is short yv => xv.CompareTo(yv),
                    ushort xv when yValue is ushort yv => xv.CompareTo(yv),
                    long xv when yValue is long yv => xv.CompareTo(yv),
                    ulong xv when yValue is ulong yv => xv.CompareTo(yv),
                    nint xv when yValue is nint yv => xv.CompareTo(yv),
                    nuint xv when yValue is nuint yv => xv.CompareTo(yv),
                    _ => 0,
                }) != 0 ? c : string.CompareOrdinal(x.Name, y.Name);
            }
            if ((c = KindOrdering(x).CompareTo(KindOrdering(y))) != 0) return c;
            if (x.Kind != SymbolKind.NamedType && (c = (!x.IsStatic).CompareTo(!y.IsStatic)) != 0) return c;
            if ((c = (!x.CanBeReferencedByName).CompareTo(!y.CanBeReferencedByName)) != 0) return c;
            if ((c = ((int)y.DeclaredAccessibility).CompareTo((int)x.DeclaredAccessibility)) != 0) return c;
            if ((c = string.CompareOrdinal(x.Name, y.Name)) != 0) return c;
            if ((c = GetArity(x).CompareTo(GetArity(y))) != 0) return c;
            var xp = GetParameters(x);
            var yp = GetParameters(y);
            if ((c = xp.Length.CompareTo(yp.Length)) != 0) return c;
            for (int i = 0; i < xp.Length; ++i)
            {
                if ((c = ((byte)xp[i].RefKind).CompareTo((byte)yp[i].RefKind)) != 0) return c;
                if ((c = CompareParameterTypes(xp[i].Type, yp[i].Type)) != 0) return c;
            }
            return 0;
        }

        private int CompareParameterTypes(ITypeSymbol x, ITypeSymbol y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            int c;
            if ((c = ((byte)x.TypeKind).CompareTo((byte)y.TypeKind)) != 0) return c;
            if ((c = ((int)x.Kind).CompareTo((int)y.Kind)) != 0) return c;
            if ((c = (!x.CanBeReferencedByName).CompareTo(!y.CanBeReferencedByName)) != 0) return c;
            if ((c = string.CompareOrdinal(x.Name, y.Name)) != 0) return c;
            if ((c = GetArity(x).CompareTo(GetArity(y))) != 0) return c;
            switch (x)
            {
            case INamedTypeSymbol ntx when ntx.Arity != 0 && y is INamedTypeSymbol nty:
                var tax = ntx.TypeArguments;
                var tay = nty.TypeArguments;
                if ((c = tax.Length.CompareTo(tay.Length)) != 0) return c;
                for (int i = 0; i < tax.Length; ++i)
                {
                    if ((c = CompareParameterTypes(tax[i], tay[i])) != 0) return c;
                }
                break;
            case IArrayTypeSymbol atx when y is IArrayTypeSymbol aty:
                if ((c = CompareParameterTypes(atx.ElementType, aty.ElementType)) != 0) return c;
                if ((c = atx.Rank.CompareTo(aty.Rank)) != 0) return c;
                break;
            case IPointerTypeSymbol ptx when y is IPointerTypeSymbol pty:
                if ((c = CompareParameterTypes(ptx.PointedAtType, pty.PointedAtType)) != 0) return c;
                break;
            case IFunctionPointerTypeSymbol fpx when y is IFunctionPointerTypeSymbol fpy:
                if ((c = Compare(fpx.Signature, fpy.Signature)) != 0) return c;
                break;
            }
            return 0;
        }

        private static int KindOrdering(ISymbol s)
        {
            return s switch
            {
                IFieldSymbol f => f.IsConst ? 0 : 1,
                IPropertySymbol p => p.IsIndexer ? 4 : 5,
                IEventSymbol => 6,
                IMethodSymbol m => m.MethodKind switch
                {
                    MethodKind.Constructor => 2,
                    MethodKind.Destructor => 3,
                    MethodKind.Ordinary => 7,
                    MethodKind.ExplicitInterfaceImplementation => 8,
                    MethodKind.UserDefinedOperator => 9,
                    MethodKind.Conversion => 10,
                    _ => 11,
                },
                INamedTypeSymbol => 12,
                _ => 13,
            };
        }

        private static int GetArity(ISymbol s)
        {
            return s switch
            {
                IMethodSymbol m => m.Arity,
                INamedTypeSymbol t => t.Arity,
                _ => 0,
            };
        }

        private static ImmutableArray<IParameterSymbol> GetParameters(ISymbol s)
        {
            return s switch
            {
                IMethodSymbol m => m.Parameters,
                IPropertySymbol p => p.Parameters,
                _ => ImmutableArray<IParameterSymbol>.Empty,
            };
        }
    }
}
