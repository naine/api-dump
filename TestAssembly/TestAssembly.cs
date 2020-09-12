// Copyright (c) 2020 Nathan Williams. All rights reserved.
// Licensed under the MIT license. See LICENCE file in the project root
// for full license information.

using System;
using System.Collections;
using System.IO;

namespace TestAssembly
{
    public class TopClass : IDisposable, IEnumerable
    {
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
        void IDisposable.Dispose() { }
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
        public fixed byte Stinx[16];

        public static void Foo1() { }
        public void Foo2() { }
        public readonly void Foo3() { }
        public readonly int Foo4() => throw new NotImplementedException();
        public readonly ref int Foo5() => throw new NotImplementedException();
        public ref readonly int Foo6() => throw new NotImplementedException();
        public readonly ref readonly int Foo7() => throw new NotImplementedException();
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
        public void Method2() { }
        public void Method3(int x) { }
        protected void Method4(int x) { }
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

    public enum Enum1 { A, B, C }
    public enum Enum2 : byte { A, B, C }
    public enum Enum3 : ulong { A, B, C }
    public enum Enum4 : int { A, B, C }

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
