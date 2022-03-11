// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: TypeForwardedTo(typeof(DeflateStream))]
[assembly: TypeForwardedTo(typeof(CompressionMode))]

namespace TestAssembly
{
    public static class EmptyClass { }

    public class TopClass : IDisposable, IEnumerable
    {
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
        void IDisposable.Dispose() => GC.SuppressFinalize(this);

        public static void StrNonNull(string x = "hello") => throw new NotImplementedException();
        public static void StrNull(string? x = null) => throw new NotImplementedException();
        public static void CharNonZero(char x = 'Q') => throw new NotImplementedException();
        public static void CharExplicitZero(char x = '\0') => throw new NotImplementedException();
        public static void CharDefault(char x = default) => throw new NotImplementedException();
        public static void DoubleNonZero(double x = -3.14) => throw new NotImplementedException();
        public static void DoubleExplicitZero(double x = 0.0) => throw new NotImplementedException();
        public static void DoubleDefault(double x = default) => throw new NotImplementedException();
        public static void DoubleNan(double x = double.NaN) => throw new NotImplementedException();
        public static void DoublePosInf(double x = double.PositiveInfinity) => throw new NotImplementedException();
        public static void DoubleNegInf(double x = double.NegativeInfinity) => throw new NotImplementedException();
        public static void FloatNonZero(float x = -3.14f) => throw new NotImplementedException();
        public static void FloatExplicitZero(float x = 0.0f) => throw new NotImplementedException();
        public static void FloatDefault(float x = default) => throw new NotImplementedException();
        public static void FloatNan(float x = float.NaN) => throw new NotImplementedException();
        public static void FloatPosInf(float x = float.PositiveInfinity) => throw new NotImplementedException();
        public static void FloatNegInf(float x = float.NegativeInfinity) => throw new NotImplementedException();
        public static void IntNonZero(int x = 12345) => throw new NotImplementedException();
        public static void IntExplicitZero(int x = 0) => throw new NotImplementedException();
        public static void IntDefault(int x = default) => throw new NotImplementedException();
        public static void BoolTrue(bool x = true) => throw new NotImplementedException();
        public static void BoolFalse(bool x = false) => throw new NotImplementedException();
        public static void BoolDefault(bool x = default) => throw new NotImplementedException();
        public static void NullableNonZero(int? x = 12345) => throw new NotImplementedException();
        public static void NullableZero(int? x = 0) => throw new NotImplementedException();
        public static void NullableNull(int? x = null) => throw new NotImplementedException();
        public static void NullableDefault(int? x = default) => throw new NotImplementedException();
        public static void NullableUnknownNull(ArrayWithOffset? x = null)
            => throw new NotImplementedException();
        public static void NullableUnknownDefault(ArrayWithOffset? x = default)
            => throw new NotImplementedException();
        public static void NullableUnknownEnumNonZero(ZipArchiveMode? x = ZipArchiveMode.Update)
            => throw new NotImplementedException();
        public static void NullableUnknownEnumZero(ZipArchiveMode? x = 0)
            => throw new NotImplementedException();
        public static void NullableUnknownEnumNull(ZipArchiveMode? x = null)
            => throw new NotImplementedException();
        public static void NullableUnknownEnumDefault(ZipArchiveMode? x = default)
            => throw new NotImplementedException();
        public static void ClassKnown(IEnumerable? x = null) => throw new NotImplementedException();
        public static void ClassUnknown(ZipArchive? x = null) => throw new NotImplementedException();
        public static void StructKnown(ValueTuple x = default) => throw new NotImplementedException();
        public static void StructUnknown(ArrayWithOffset x = default) => throw new NotImplementedException();
        public static unsafe void PtrVoid(void* x = null) => throw new NotImplementedException();
        public static unsafe void PtrKnown(int* x = null) => throw new NotImplementedException();
        public static unsafe void PtrUnknown(ZipArchiveMode* x = null) => throw new NotImplementedException();
        public static void EnumNonZero(LayoutKind x = LayoutKind.Explicit) => throw new NotImplementedException();
        public static void EnumNamedZero(LayoutKind x = LayoutKind.Sequential)
            => throw new NotImplementedException();
        public static void EnumExplicitZero(LayoutKind x = 0) => throw new NotImplementedException();
        public static void EnumDefault(LayoutKind x = default) => throw new NotImplementedException();
        public static void EnumUnknownNonZero(ZipArchiveMode x = ZipArchiveMode.Update)
            => throw new NotImplementedException();
        public static void EnumUnknownNamedZero(ZipArchiveMode x = ZipArchiveMode.Read)
            => throw new NotImplementedException();
        public static void EnumUnknownExplicitZero(ZipArchiveMode x = 0) => throw new NotImplementedException();
        public static void EnumUnknownDefault(ZipArchiveMode x = default) => throw new NotImplementedException();
        public static void EnumFlagsSingle(AttributeTargets x = AttributeTargets.Assembly)
            => throw new NotImplementedException();
        public static void EnumFlagsMultiple(
            AttributeTargets x = AttributeTargets.Class | AttributeTargets.Struct)
            => throw new NotImplementedException();
        public static void EnumFlagsArbitrary(AttributeTargets x = (AttributeTargets)0x8000)
            => throw new NotImplementedException();
        public static void EnumFlagsMixed(AttributeTargets x = AttributeTargets.Class | (AttributeTargets)0x8000)
            => throw new NotImplementedException();
        public static void EnumFlagsExplicitZero(AttributeTargets x = 0) => throw new NotImplementedException();
        public static void EnumFlagsDefault(AttributeTargets x = default) => throw new NotImplementedException();
        public static void EnumFlagsUnknownSingle(DllImportSearchPath x = DllImportSearchPath.AssemblyDirectory)
            => throw new NotImplementedException();
        public static void EnumFlagsUnknownMultiple(
            DllImportSearchPath x = DllImportSearchPath.SafeDirectories | DllImportSearchPath.System32)
            => throw new NotImplementedException();
        public static void EnumFlagsUnknownArbitrary(DllImportSearchPath x = (DllImportSearchPath)16)
            => throw new NotImplementedException();
        public static void EnumFlagsUnknownMixed(
            DllImportSearchPath x = DllImportSearchPath.SafeDirectories | (DllImportSearchPath)16)
            => throw new NotImplementedException();
        public static void EnumFlagsUnknownNamedZero(DllImportSearchPath x = DllImportSearchPath.LegacyBehavior)
            => throw new NotImplementedException();
        public static void EnumFlagsUnknownExplicitZero(DllImportSearchPath x = 0)
            => throw new NotImplementedException();
        public static void EnumFlagsUnknownDefault(DllImportSearchPath x = default)
            => throw new NotImplementedException();

        public static nint NativeInt;
        public static IntPtr NativeIntPtr;
        public static nuint NativeUInt;
        public static UIntPtr NativeUIntPtr;
    }

    public static unsafe class FunctionPointers
    {
        public static delegate*<void> MAction;
        public static delegate* managed<void> MActionExplicit;
        public static delegate*<int, void> MAction1;
        public static delegate*<int, nuint, void> MAction2;
        public static delegate*<int> MFunc;
        public static delegate* managed<int> MFuncExplicit;
        public static delegate*<int, int> MFunc1;
        public static delegate*<int, nuint, int> MFunc2;
#if NET5_0_OR_GREATER
        public static delegate* unmanaged<void> UDefAction;
        public static delegate* unmanaged<int, void> UDefAction1;
        public static delegate* unmanaged<int, nuint, void> UDefAction2;
        public static delegate* unmanaged<int> UDefFunc;
        public static delegate* unmanaged<int, int> UDefFunc1;
        public static delegate* unmanaged<int, nuint, int> UDefFunc2;
#endif
        public static delegate* unmanaged[Cdecl]<void> UCdeclAction;
        public static delegate* unmanaged[Cdecl]<int, void> UCdeclAction1;
        public static delegate* unmanaged[Cdecl]<int, nuint, void> UCdeclAction2;
        public static delegate* unmanaged[Cdecl]<int> UCdeclFunc;
        public static delegate* unmanaged[Cdecl]<int, int> UCdeclFunc1;
        public static delegate* unmanaged[Cdecl]<int, nuint, int> UCdeclFunc2;

        public static delegate* unmanaged[Stdcall]<void> UStdcallAction;
        public static delegate* unmanaged[Stdcall]<int, void> UStdcallAction1;
        public static delegate* unmanaged[Stdcall]<int, nuint, void> UStdcallAction2;
        public static delegate* unmanaged[Stdcall]<int> UStdcallFunc;
        public static delegate* unmanaged[Stdcall]<int, int> UStdcallFunc1;
        public static delegate* unmanaged[Stdcall]<int, nuint, int> UStdcallFunc2;

        public static delegate* unmanaged[Thiscall]<void> UThiscallAction;
        public static delegate* unmanaged[Thiscall]<int, void> UThiscallAction1;
        public static delegate* unmanaged[Thiscall]<int, nuint, void> UThiscallAction2;
        public static delegate* unmanaged[Thiscall]<int> UThiscallFunc;
        public static delegate* unmanaged[Thiscall]<int, int> UThiscallFunc1;
        public static delegate* unmanaged[Thiscall]<int, nuint, int> UThiscallFunc2;

        public static delegate* unmanaged[Fastcall]<void> UFastcallAction;
        public static delegate* unmanaged[Fastcall]<int, void> UFastcallAction1;
        public static delegate* unmanaged[Fastcall]<int, nuint, void> UFastcallAction2;
        public static delegate* unmanaged[Fastcall]<int> UFastcallFunc;
        public static delegate* unmanaged[Fastcall]<int, int> UFastcallFunc1;
        public static delegate* unmanaged[Fastcall]<int, nuint, int> UFastcallFunc2;
    }

#if NET5_0_OR_GREATER
    public static class UnmanagedFuncs
    {
        public static int FuncManaged(nuint x, nuint y) => (int)(x + y);

        [UnmanagedCallersOnly]
        public static int FuncUnmanaged(nuint x, nuint y) => (int)(x + y);

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static int FuncCdecl(nuint x, nuint y) => (int)(x + y);

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        public static int FuncStdcall(nuint x, nuint y) => (int)(x + y);

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvThiscall) })]
        public static int FuncThiscall(nuint x, nuint y) => (int)(x + y);

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
        public static int FuncFastcall(nuint x, nuint y) => (int)(x + y);
    }
#endif

    public abstract class AbstractClass : IEnumerable
    {
        public abstract IEnumerator GetEnumerator();
        public abstract bool Foo1(double x);
        public abstract ref int Foo2(in decimal x);
        public virtual ref readonly object Foo3(ref string x) => throw new NotImplementedException();
        public virtual dynamic Foo4(out (int a, double b) x) => throw new NotImplementedException();
        public void Foo5(dynamic x) => throw new NotImplementedException();
        public static void Foo6(GenContainer<int>.GenEnum x) { }
        public static void Foo7((int a, int, int c, int, int e, int, int g, int, int? i, int) x) { }
        public virtual AbstractClass CovReturn() => throw new NotImplementedException();
    }

    public static class GenContainer<T>
    {
        public enum GenEnum { A, B, C }
    }

    public class ConcreteClass : AbstractClass
    {
        protected ConcreteClass() { }
        public ConcreteClass(string s) { }
        public override IEnumerator GetEnumerator() => throw new NotImplementedException();
        public sealed override bool Foo1(double x) => throw new NotImplementedException();
        public override ref int Foo2(in decimal x) => throw new NotImplementedException();
        public sealed override ref readonly object Foo3(ref string x) => throw new NotImplementedException();
        public override dynamic Foo4(out (int a, double b) x) => throw new NotImplementedException();
#if NET5_0_OR_GREATER
        public override ConcreteClass CovReturn() => throw new NotImplementedException();
#endif
    }

    public sealed class SealedClass : ConcreteClass
    {
        private SealedClass() { }
        public override IEnumerator GetEnumerator() => throw new NotImplementedException();
    }

    public unsafe struct NormalStruct
    {
        public fixed int Stinx[16];

        public readonly int Prop1 { get; }
        public int Prop2 { readonly get; set; }
        public readonly int Prop3 { get => throw new NotImplementedException(); }
        public int Prop4
        {
            readonly get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public int Prop5
        {
            get => throw new NotImplementedException();
            readonly set => throw new NotImplementedException();
        }
        public readonly int Prop6
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
#if NET5_0_OR_GREATER
        public int InitProp1
        {
            get => throw new NotImplementedException();
            init => throw new NotImplementedException();
        }
        public int InitProp2 { get; init; }
        public int InitProp3 { readonly get; init; }
        public int InitProp4
        {
            readonly get => throw new NotImplementedException();
            init => throw new NotImplementedException();
        }
        public readonly int InitProp5
        {
            get => throw new NotImplementedException();
            init => throw new NotImplementedException();
        }
#endif

        public static void Foo1() { }
        public void Foo2() => throw new NotImplementedException();
        public readonly void Foo3() => throw new NotImplementedException();
        public readonly int Foo4() => throw new NotImplementedException();
        public readonly ref int Foo5() => throw new NotImplementedException();
        public ref readonly int Foo6() => throw new NotImplementedException();
        public readonly ref readonly int Foo7() => throw new NotImplementedException();

        public readonly event Action Event1 { add { } remove { } }
        public event Action Event2 { add { } remove { } }
        public event Action Event3;
    }

    public ref struct RefStruct
    {
        public byte Foo;

        public RefStruct(byte b) { Foo = b; }
    }

    public readonly struct ReadStruct
    {
        public readonly byte Foo;
    }

    public readonly ref struct RefReadStruct
    {
        public readonly byte Foo;
    }

    public interface IFooInterface<T> : IEnumerable
        where T : IFooInterface<T>
    {
        int Prop1 { get; }
        int Prop1B { get; }
        int Prop2 { get; set; }
        int Prop2B { get; set; }
        ref int Prop3 { get; }
        ref readonly int Prop4 { get; }
#if NET5_0_OR_GREATER
        int Prop5 { get; init; }
        int Prop6 { get; init; }
#endif
        event VoidDelegate Event1;
        event ValDelegate Event2;
        event Action Event3;
        void Method();
        void MethodB();

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        void Method0() { }
        virtual void Method1() { }
        public void Method2() { }
        public sealed void Method3() { }
        protected void Method4() { }
        protected sealed void Method5() { }
        protected void Method6();

#if NET6_0_OR_GREATER
        static abstract int StaticAbstractMethod1();
        static abstract int StaticAbstractMethod2();
        static abstract int StaticAbstractProp1 { get; set; }
        static abstract int StaticAbstractProp2 { get; set; }
        static abstract event Action StaticAbstractEvent1;
        static abstract event Action StaticAbstractEvent2;
        static abstract int operator +(IFooInterface<T> l, IFooInterface<T> r);
        static abstract int operator -(T l, T r);
        static abstract int operator *(T l, T r);
        static abstract explicit operator int(T x);
        static abstract explicit operator long(T x);
        static abstract implicit operator uint(T x);
        static abstract implicit operator ulong(T x);
        static abstract explicit operator T(int x);
        static abstract explicit operator T(long x);
        static abstract implicit operator T(uint x);
        static abstract implicit operator T(ulong x);
#endif
    }

    public interface ISubInterface<T> : IFooInterface<T>
        where T : IFooInterface<T>
    {
        void IFooInterface<T>.Method() { }
        abstract void IFooInterface<T>.Method1();
        void IFooInterface<T>.Method2() { }
#endif
    }

    public class FooImpl : IFooInterface<FooImpl>
    {
        public int Prop1 => 0;
        int IFooInterface<FooImpl>.Prop1B => 0;
        public int Prop2 { get => 0; set { } }
        int IFooInterface<FooImpl>.Prop2B { get => 0; set { } }
        public ref int Prop3 => throw new NotImplementedException();
        public ref readonly int Prop4 => throw new NotImplementedException();
#if NET5_0_OR_GREATER
        public int Prop5
        {
            get => throw new NotImplementedException();
            init => throw new NotImplementedException();
        }
        public int Prop6 { get; init; }
#endif
        public event VoidDelegate? Event1;
        public event ValDelegate Event2 { add { } remove { } }
        event Action IFooInterface<FooImpl>.Event3 { add { } remove { } }
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
        public void Method() => throw new NotImplementedException();
        void IFooInterface<FooImpl>.MethodB() => throw new NotImplementedException();
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        void IFooInterface<FooImpl>.Method0() { }
        public void Method2() => throw new NotImplementedException();
        void IFooInterface<FooImpl>.Method4() { }
        void IFooInterface<FooImpl>.Method6() { }
#if NET6_0_OR_GREATER
        public static int StaticAbstractMethod1() => 0;
        static int IFooInterface<FooImpl>.StaticAbstractMethod2() => 0;
        public static int StaticAbstractProp1 { get => 0; set { } }
        static int IFooInterface<FooImpl>.StaticAbstractProp2 { get => 0; set { } }
        public static event Action StaticAbstractEvent1 { add { } remove { } }
        static event Action IFooInterface<FooImpl>.StaticAbstractEvent2 { add { } remove { } }
        static int IFooInterface<FooImpl>.operator +(IFooInterface<FooImpl> l, IFooInterface<FooImpl> r) => 0;
        static int IFooInterface<FooImpl>.operator -(FooImpl l, FooImpl r) => 0;
        public static int operator *(FooImpl l, FooImpl r) => 0;
        public static explicit operator int(FooImpl x) => 0;
        public static implicit operator uint(FooImpl x) => 0;
        public static explicit operator FooImpl(int x) => null!;
        public static implicit operator FooImpl(uint x) => null!;
        static explicit IFooInterface<FooImpl>.operator long(FooImpl x) => 0;
        static implicit IFooInterface<FooImpl>.operator ulong(FooImpl x) => 0;
        static explicit IFooInterface<FooImpl>.operator FooImpl(long x) => null!;
        static implicit IFooInterface<FooImpl>.operator FooImpl(ulong x) => null!;
#endif
#endif
    }

    public enum Enum1 { A = 1, B = 2, C = 4, D = B | C }
    public enum Enum2 : int { A = 0, B = 1, C = 2, D = B | C }
    [Flags] public enum Enum3 { A = 1, B = 2, C = 4, D = B | C }
    [Flags] public enum Enum4 : int { A = 0, B = 1, C = 2, D = B | C }
    public enum Enum5 : ulong { A, B, C, D }
    public enum Enum6 : byte { A, B, C, D }

    public static class EnumConsts
    {
        public const Enum1 ConstC1 = Enum1.C;
        public const Enum2 ConstC2 = Enum2.C;
        public const Enum3 ConstC3 = Enum3.C;
        public const Enum4 ConstC4 = Enum4.C;
        public const Enum1 ConstDef1 = 0;
        public const Enum2 ConstDef2 = 0;
        public const Enum3 ConstDef3 = 0;
        public const Enum4 ConstDef4 = 0;
        public const Enum1 ConstMix1 = Enum1.B | Enum1.C;
        public const Enum2 ConstMix2 = Enum2.B | Enum2.C;
        public const Enum3 ConstMix3 = Enum3.B | Enum3.C;
        public const Enum4 ConstMix4 = Enum4.B | Enum4.C;
        public static void DefaultC(Enum1 x = Enum1.C) { }
        public static void DefaultC(Enum2 x = Enum2.C) { }
        public static void DefaultC(Enum3 x = Enum3.C) { }
        public static void DefaultC(Enum4 x = Enum4.C) { }
        public static void DefaultMix(Enum1 x = Enum1.D) { }
        public static void DefaultMix(Enum2 x = Enum2.D) { }
        public static void DefaultMix(Enum3 x = Enum3.D) { }
        public static void DefaultMix(Enum4 x = Enum4.D) { }
        public static void DefaultZero(Enum1 x = 0) { }
        public static void DefaultZero(Enum2 x = 0) { }
        public static void DefaultZero(Enum3 x = 0) { }
        public static void DefaultZero(Enum4 x = 0) { }
    }

    public static class Extensions
    {
        public static int IntExtension(this int foo) => throw new NotImplementedException();
    }

    public delegate void VoidDelegate();
    public delegate int ValDelegate(int x);
    public delegate ref int RefDelegate(ref int x);
    public delegate ref readonly int InDelegate(in int x);
    public delegate void OutDelegate(out int x);
    public delegate T GenDelegate<T>(T x);
    public delegate void GenInDelegate<in T>(T x);
    public delegate T GenOutDelegate<out T>();

    public delegate T ConstrainedDelegate01<T>(T x) where T : struct;
    public delegate T ConstrainedDelegate02<T>(T x) where T : struct, Enum;
    public delegate T ConstrainedDelegate03<T>(T x) where T : struct, IComparable;
    public delegate T ConstrainedDelegate04<T>(T x) where T : struct, IComparable<T>;
    public delegate T ConstrainedDelegate05<T>(T x) where T : struct, Enum, IComparable;
    public unsafe delegate T ConstrainedDelegate06<T>(T* x) where T : unmanaged;
    public unsafe delegate T ConstrainedDelegate07<T>(T* x) where T : unmanaged, Enum;
    public unsafe delegate T ConstrainedDelegate08<T>(T* x) where T : unmanaged, IComparable;
    public unsafe delegate T ConstrainedDelegate09<T>(T* x) where T : unmanaged, IComparable<T>;
    public unsafe delegate T ConstrainedDelegate10<T>(T* x) where T : unmanaged, Enum, IComparable;
    public delegate T ConstrainedDelegate11<T>(T x) where T : class;
    public delegate T ConstrainedDelegate12<T>(T x) where T : class?;
    public delegate T ConstrainedDelegate13<T>(T x) where T : notnull;
    public delegate T ConstrainedDelegate14<T>(T x) where T : Stream;
    public delegate T ConstrainedDelegate15<T>(T x) where T : Stream?;
    public delegate T ConstrainedDelegate16<T>(T x) where T : IEnumerable;
    public delegate T ConstrainedDelegate17<T>(T x) where T : IEnumerable?;
    public delegate T ConstrainedDelegate18<T>(T x) where T : Stream, IEnumerable;
    public delegate T ConstrainedDelegate19<T>(T x) where T : Stream, IEnumerable?;
    public delegate T ConstrainedDelegate20<T>(T x) where T : Stream?, IEnumerable;
    public delegate T ConstrainedDelegate21<T>(T x) where T : Stream?, IEnumerable?;
    public delegate T ConstrainedDelegate22<T>(T x) where T : new();
    public delegate T ConstrainedDelegate23<T>(T x) where T : class, new();
    public delegate T ConstrainedDelegate24<T>(T x) where T : class?, new();
    public delegate T ConstrainedDelegate25<T>(T x) where T : notnull, new();
    public delegate T ConstrainedDelegate26<T>(T x) where T : Stream, new();
    public delegate T ConstrainedDelegate27<T>(T x) where T : Stream?, new();
    public delegate T ConstrainedDelegate28<T>(T x) where T : IEnumerable, new();
    public delegate T ConstrainedDelegate29<T>(T x) where T : IEnumerable?, new();
    public delegate T ConstrainedDelegate30<T>(T x) where T : Stream, IEnumerable, new();
    public delegate T ConstrainedDelegate31<T>(T x) where T : Stream, IEnumerable?, new();
    public delegate T ConstrainedDelegate32<T>(T x) where T : Stream?, IEnumerable, new();
    public delegate T ConstrainedDelegate33<T>(T x) where T : Stream?, IEnumerable?, new();
    public delegate T ConstrainedDelegate34<T, U>(T x, U y) where T : U;
    public delegate T ConstrainedDelegate35<T, U>(T x, U y) where T : U, new();
    public delegate T ConstrainedDelegate36<T, U>(T x, U y) where T : U, IEnumerable;
    public delegate T ConstrainedDelegate37<T, U>(T x, U y) where T : U, IEnumerable, new();

    public class ConstrainedClass01<T> where T : struct { }
    public class ConstrainedClass02<T> where T : struct, Enum { }
    public class ConstrainedClass03<T> where T : struct, IComparable { }
    public class ConstrainedClass04<T> where T : struct, IComparable<T> { }
    public class ConstrainedClass05<T> where T : struct, Enum, IComparable { }
    public class ConstrainedClass06<T> where T : unmanaged { }
    public class ConstrainedClass07<T> where T : unmanaged, Enum { }
    public class ConstrainedClass08<T> where T : unmanaged, IComparable { }
    public class ConstrainedClass09<T> where T : unmanaged, IComparable<T> { }
    public class ConstrainedClass10<T> where T : unmanaged, Enum, IComparable { }
    public class ConstrainedClass11<T> where T : class { }
    public class ConstrainedClass12<T> where T : class? { }
    public class ConstrainedClass13<T> where T : notnull { }
    public class ConstrainedClass14<T> where T : Stream { }
    public class ConstrainedClass15<T> where T : Stream? { }
    public class ConstrainedClass16<T> where T : IEnumerable { }
    public class ConstrainedClass17<T> where T : IEnumerable? { }
    public class ConstrainedClass18<T> where T : Stream, IEnumerable { }
    public class ConstrainedClass19<T> where T : Stream, IEnumerable? { }
    public class ConstrainedClass20<T> where T : Stream?, IEnumerable { }
    public class ConstrainedClass21<T> where T : Stream?, IEnumerable? { }
    public class ConstrainedClass22<T> where T : new() { }
    public class ConstrainedClass23<T> where T : class, new() { }
    public class ConstrainedClass24<T> where T : class?, new() { }
    public class ConstrainedClass25<T> where T : notnull, new() { }
    public class ConstrainedClass26<T> where T : Stream, new() { }
    public class ConstrainedClass27<T> where T : Stream?, new() { }
    public class ConstrainedClass28<T> where T : IEnumerable, new() { }
    public class ConstrainedClass29<T> where T : IEnumerable?, new() { }
    public class ConstrainedClass30<T> where T : Stream, IEnumerable, new() { }
    public class ConstrainedClass31<T> where T : Stream, IEnumerable?, new() { }
    public class ConstrainedClass32<T> where T : Stream?, IEnumerable, new() { }
    public class ConstrainedClass33<T> where T : Stream?, IEnumerable?, new() { }
    public class ConstrainedClass34<T, U> where T : U { }
    public class ConstrainedClass35<T, U> where T : U, new() { }
    public class ConstrainedClass36<T, U> where T : U, IEnumerable { }
    public class ConstrainedClass37<T, U> where T : U, IEnumerable, new() { }
}
