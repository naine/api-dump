// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ApiDump
{
    class MemberOrdering : IComparer<ISymbol>
    {
        public static readonly MemberOrdering Comparer = new MemberOrdering();

        private MemberOrdering() { }

        public int Compare(ISymbol? x, ISymbol? y)
        {
            if (x == null) return y == null ? 0 : -1;
            if (y == null) return 1;
            int c;
            // Special case enum values, sort by value over name.
            if (x.ContainingType?.TypeKind == TypeKind.Enum &&
                y.ContainingType?.TypeKind == TypeKind.Enum &&
                x is IFieldSymbol xf && y is IFieldSymbol yf)
            {
                return (c = xf.ConstantValue switch
                {
                    int xv when yf.ConstantValue is int yv => xv.CompareTo(yv),
                    byte xv when yf.ConstantValue is byte yv => xv.CompareTo(yv),
                    ushort xv when yf.ConstantValue is ushort yv => xv.CompareTo(yv),
                    short xv when yf.ConstantValue is short yv => xv.CompareTo(yv),
                    sbyte xv when yf.ConstantValue is sbyte yv => xv.CompareTo(yv),
                    uint xv when yf.ConstantValue is uint yv => xv.CompareTo(yv),
                    long xv when yf.ConstantValue is long yv => xv.CompareTo(yv),
                    ulong xv when yf.ConstantValue is ulong yv => xv.CompareTo(yv),
                    _ => 0,
                }) != 0 ? c : x.Name.CompareTo(y.Name);
            }
            if ((c = KindOrdering(x).CompareTo(KindOrdering(y))) != 0) return c;
            if (x.Kind != SymbolKind.NamedType && (c = (!x.IsStatic).CompareTo(!y.IsStatic)) != 0) return c;
            if ((c = ((int)y.DeclaredAccessibility).CompareTo((int)x.DeclaredAccessibility)) != 0) return c;
            if ((c = x.Name.CompareTo(y.Name)) != 0) return c;
            if ((c = GetArity(x).CompareTo(GetArity(y))) != 0) return c;
            var xp = GetParameters(x);
            var yp = GetParameters(y);
            if ((c = xp.Length.CompareTo(yp.Length)) != 0) return c;
            for (int i = 0; i < xp.Length; ++i)
            {
                if ((c = ((byte)xp[i].RefKind).CompareTo((byte)yp[i].RefKind)) != 0) return c;
                if ((c = xp[i].Type.ToDisplayString().CompareTo(yp[i].Type.ToDisplayString())) != 0) return c;
            }
            return 0;
        }

        private int KindOrdering(ISymbol s)
        {
            return s switch
            {
                IFieldSymbol f => f.IsConst ? 0 : 1,
                IPropertySymbol p => p.IsIndexer ? 4 : 5,
                IEventSymbol _ => 6,
                IMethodSymbol m => m.MethodKind switch
                {
                    MethodKind.Constructor => 2,
                    MethodKind.Destructor => 3,
                    MethodKind.Ordinary => 7,
                    MethodKind.UserDefinedOperator => 8,
                    MethodKind.Conversion => 9,
                    _ => 10,
                },
                INamedTypeSymbol _ => 11,
                _ => 12,
            };
        }

        private int GetArity(ISymbol s)
        {
            return s switch
            {
                IMethodSymbol m => m.Arity,
                INamedTypeSymbol t => t.Arity,
                _ => 0,
            };
        }

        private ImmutableArray<IParameterSymbol> GetParameters(ISymbol s)
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
