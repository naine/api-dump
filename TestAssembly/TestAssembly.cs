// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace TestAssembly
{
    public static class EmptyClass { }

    public class TopClass : IDisposable, IEnumerable
    {
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
        void IDisposable.Dispose() { }

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
    }

    public abstract class AbstractClass : IEnumerable
    {
        public abstract IEnumerator GetEnumerator();
        public abstract bool Foo1(double x);
        public abstract ref int Foo2(in decimal x);
        public virtual ref readonly object Foo3(ref string x) => throw new NotImplementedException();
        public virtual dynamic Foo4(out (int a, double b) x) => throw new NotImplementedException();
        public void Foo5(dynamic x) { }
        public static void Foo6(GenContainer<int>.GenEnum x) { }
        public static void Foo7((int a, int, int c, int, int e, int, int g, int, int? i, int) x) { }
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
    }

    public sealed class SealedClass : ConcreteClass
    {
        private SealedClass() { }
        public override IEnumerator GetEnumerator() => throw new NotImplementedException();
    }

    public unsafe struct NormalStruct
    {
        public fixed int Stinx[16];

        public static void Foo1() { }
        public void Foo2() { }
        public readonly void Foo3() { }
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

    public interface IFooInterface : IEnumerable
    {
        int Prop1 { get; }
        int Prop2 { get; set; }
        ref int Prop3 { get; }
        ref readonly int Prop4 { get; }
        event VoidDelegate Event1;
        event ValDelegate Event2;
        void Method();

#if INTERFACE_DEFAULTS
        virtual void Method1() { }
        public void Method2() { }
        public sealed void Method3(int x) { }
        protected void Method4(int x) { }
    }

    public interface ISubInterface : IFooInterface
    {
        void IFooInterface.Method() { }
        abstract void IFooInterface.Method1();
        void IFooInterface.Method2() { }
#endif
    }

    public class FooImpl : IFooInterface
    {
        public int Prop1 => throw new NotImplementedException();
        public int Prop2
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public ref int Prop3 => throw new NotImplementedException();
        public ref readonly int Prop4 => throw new NotImplementedException();
        public event VoidDelegate? Event1;
        public event ValDelegate Event2 { add { } remove { } }
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
        public void Method() { }
        public void Method2() { }
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
