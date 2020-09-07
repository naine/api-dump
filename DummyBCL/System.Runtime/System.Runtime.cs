using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: ReferenceAssembly]

namespace System
{
    public abstract class Array : IList
    {
        internal Array() { }
        public bool IsFixedSize => throw null;
        public bool IsReadOnly => throw null;
        public bool IsSynchronized => throw null;
        public int Length => throw null;
        public int Rank => throw null;
        public object SyncRoot => throw null;
        int ICollection.Count => throw null;
        object? IList.this[int index] { get => throw null; set { } }
        public void CopyTo(Array array, int index) { }
        public IEnumerator GetEnumerator() => throw null;
        public int GetLength(int dimension) => throw null;
        public int GetLowerBound(int dimension) => throw null;
        public int GetUpperBound(int dimension) => throw null;
        int IList.Add(object? value) => throw null;
        void IList.Clear() { }
        bool IList.Contains(object? value) => throw null;
        int IList.IndexOf(object? value) => throw null;
        void IList.Insert(int index, object? value) { }
        void IList.Remove(object? value) { }
        void IList.RemoveAt(int index) { }
    }
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    public abstract class Attribute { }
    [Flags]
    public enum AttributeTargets
    {
        Assembly = 0x1,
        Module = 0x2,
        Class = 0x4,
        Struct = 0x8,
        Enum = 0x10,
        Constructor = 0x20,
        Method = 0x40,
        Property = 0x80,
        Field = 0x100,
        Event = 0x200,
        Interface = 0x400,
        Parameter = 0x800,
        Delegate = 0x1000,
        ReturnValue = 0x2000,
        GenericParameter = 0x4000,
        All = 0x7FFF,
    }
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) { }
        public bool AllowMultiple { get => throw null; set { } }
        public bool Inherited { get => throw null; set { } }
        public AttributeTargets ValidOn => throw null;
    }
    public readonly struct Boolean
    {
        private readonly bool value;
    }
    public readonly struct Byte
    {
        private readonly byte value;
    }
    public readonly struct Char
    {
        private readonly char value;
    }
    public readonly struct Decimal
    {
        private readonly int dummy;
    }
    public abstract class Delegate { }
    public readonly struct Double
    {
        private readonly double value;
    }
    public abstract class Enum : ValueType { }
    public class Exception
    {
        public Exception() { }
        public Exception(string? message) { }
        public Exception(string? message, Exception? innerException) { }
        public Exception? InnerException => throw null;
        public virtual string Message => throw null;
    }
    [AttributeUsage(AttributeTargets.Enum, Inherited = false)]
    public class FlagsAttribute : Attribute { }
    public interface IDisposable
    {
        void Dispose();
    }
    public readonly struct Int16
    {
        private readonly short value;
    }
    public readonly struct Int32
    {
        private readonly int value;
    }
    public readonly struct Int64
    {
        private readonly long value;
    }
    public readonly struct IntPtr
    {
        private readonly unsafe void* value;
    }
    public abstract class MulticastDelegate : Delegate { }
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class NonSerializedAttribute : Attribute { }
    public struct Nullable<T> where T : struct
    {
        public bool HasValue { get; }
        public T Value { get; }
    }
    public class Object
    {
        ~Object() { }
        public virtual bool Equals(object? obj) => throw null;
        public virtual int GetHashCode() => throw null;
        public Type GetType() => throw null;
        public virtual string? ToString() => throw null;
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum
        | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property
        | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface
        | AttributeTargets.Delegate, Inherited = false)]
    public sealed class ObsoleteAttribute : Attribute
    {
        public ObsoleteAttribute() { }
        public ObsoleteAttribute(string? message) { }
        public ObsoleteAttribute(string? message, bool error) { }
        public bool IsError => throw null;
        public string? Message => throw null;
    }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public sealed class ParamArrayAttribute : Attribute { }
    public readonly struct SByte
    {
        private readonly sbyte value;
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct
        | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
    public sealed class SerializableAttribute : Attribute { }
    public readonly struct Single
    {
        private readonly float value;
    }
    public sealed class String : IEnumerable<char>
    {
        [IndexerName("Chars")]
        public char this[int index] => throw null;
        public int Length => throw null;
        public IEnumerator<char> GetEnumerator() => throw null;
        IEnumerator IEnumerable.GetEnumerator() => throw null;
    }
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public class ThreadStaticAttribute : Attribute { }
    public abstract class Type : MemberInfo { }
    public readonly struct UInt16
    {
        private readonly ushort value;
    }
    public readonly struct UInt32
    {
        private readonly uint value;
    }
    public readonly struct UInt64
    {
        private readonly ulong value;
    }
    public readonly struct UIntPtr
    {
        private readonly unsafe void* value;
    }
    public struct ValueTuple { }
    public struct ValueTuple<T1>
    {
        public T1 Item1;
        public ValueTuple(T1 item1) => throw null;
    }
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) => throw null;
    }
    public struct ValueTuple<T1, T2, T3>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public ValueTuple(T1 item1, T2 item2, T3 item3) => throw null;
    }
    public struct ValueTuple<T1, T2, T3, T4>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4) => throw null;
    }
    public struct ValueTuple<T1, T2, T3, T4, T5>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) => throw null;
    }
    public struct ValueTuple<T1, T2, T3, T4, T5, T6>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) => throw null;
    }
    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) => throw null;
    }
    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> where TRest : struct
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public TRest Rest;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) => throw null;
    }
    public abstract class ValueType { }
    public struct Void { }
}
namespace System.Collections
{
    public interface ICollection : IEnumerable
    {
        int Count { get; }
        bool IsSynchronized { get; }
        object SyncRoot { get; }
        void CopyTo(Array array, int index);
    }
    public interface IEnumerable
    {
        IEnumerator GetEnumerator();
    }
    public interface IEnumerator
    {
        object? Current { get; }
        bool MoveNext();
        void Reset();
    }
    public interface IList : ICollection
    {
        bool IsFixedSize { get; }
        bool IsReadOnly { get; }
        object? this[int index] { get; set; }
        int Add(object? value);
        void Clear();
        bool Contains(object? value);
        int IndexOf(object? value);
        void Insert(int index, object? value);
        void Remove(object? value);
        void RemoveAt(int index);
    }
}
namespace System.Collections.Generic
{
    public interface ICollection<T> : IEnumerable<T>
    {
        int Count { get; }
        bool IsReadOnly { get; }
        void Add(T item);
        void Clear();
        bool Contains(T item);
        void CopyTo(T[] array, int arrayIndex);
        bool Remove(T item);
    }
    public interface IEnumerable<out T> : IEnumerable
    {
        new IEnumerator<T> GetEnumerator();
    }
    public interface IEnumerator<out T> : IEnumerator, IDisposable
    {
        new T Current { get; }
    }
    public interface IList<T> : ICollection<T>
    {
        T this[int index] { get; set; }
        int IndexOf(T item);
        void Insert(int index, T item);
        void RemoveAt(int index);
    }
    public interface IReadOnlyCollection<out T> : IEnumerable<T>
    {
        int Count { get; }
    }
    public interface IReadOnlyList<out T> : IReadOnlyCollection<T>
    {
        T this[int index] { get; }
    }
}
namespace System.ComponentModel
{
    [AttributeUsage(AttributeTargets.All)]
    public class DefaultValueAttribute : Attribute
    {
        public DefaultValueAttribute(bool value) { }
        public DefaultValueAttribute(byte value) { }
        public DefaultValueAttribute(char value) { }
        public DefaultValueAttribute(double value) { }
        public DefaultValueAttribute(short value) { }
        public DefaultValueAttribute(int value) { }
        public DefaultValueAttribute(long value) { }
        public DefaultValueAttribute(object? value) { }
        public DefaultValueAttribute(sbyte value) { }
        public DefaultValueAttribute(float value) { }
        public DefaultValueAttribute(string? value) { }
        public DefaultValueAttribute(Type type, string? value) { }
        public DefaultValueAttribute(ushort value) { }
        public DefaultValueAttribute(uint value) { }
        public DefaultValueAttribute(ulong value) { }
        public virtual object? Value => throw null;
        protected void SetValue(object? value) { }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum
        | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property
        | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface
        | AttributeTargets.Delegate)]
    public sealed class EditorBrowsableAttribute : Attribute
    {
        public EditorBrowsableAttribute() { }
        public EditorBrowsableAttribute(EditorBrowsableState state) { }
        public EditorBrowsableState State => throw null;
    }
    public enum EditorBrowsableState
    {
        Always = 0,
        Never = 1,
        Advanced = 2,
    }
}
namespace System.Diagnostics
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ConditionalAttribute : Attribute
    {
        public ConditionalAttribute(string conditionString) { }
        public string ConditionString => throw null;
    }
}
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field
        | AttributeTargets.Parameter, Inherited = false)]
    public sealed class AllowNullAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field
        | AttributeTargets.Parameter, Inherited = false)]
    public sealed class DisallowNullAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class DoesNotReturnAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class DoesNotReturnIfAttribute : Attribute
    {
        public DoesNotReturnIfAttribute(bool parameterValue) { }
        public bool ParameterValue => throw null;
    }
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field
        | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
    public sealed class MaybeNullAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class MaybeNullWhenAttribute : Attribute
    {
        public MaybeNullWhenAttribute(bool returnValue) { }
        public bool ReturnValue => throw null;
    }
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field
        | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
    public sealed class NotNullAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter
        | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    public sealed class NotNullIfNotNullAttribute : Attribute
    {
        public NotNullIfNotNullAttribute(string parameterName) { }
        public string ParameterName => throw null;
    }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool returnValue) { }
        public bool ReturnValue => throw null;
    }
}
namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    public sealed class DefaultMemberAttribute : Attribute
    {
        public DefaultMemberAttribute(string memberName) { }
        public string MemberName => throw null;
    }
    public abstract class MemberInfo { }
}
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName) { }
        public string ParameterName => throw null;
    }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class CallerFilePathAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class CallerLineNumberAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class CallerMemberNameAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.All, Inherited = true)]
    public sealed class CompilerGeneratedAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)]
    public class CompilerGlobalScopeAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
    public abstract class CustomConstantAttribute : Attribute
    {
        public abstract object? Value { get; }
    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
    public sealed class DateTimeConstantAttribute : CustomConstantAttribute
    {
        public DateTimeConstantAttribute(long ticks) { }
        public override object Value => throw null;
    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
    public sealed class DecimalConstantAttribute : Attribute
    {
        public DecimalConstantAttribute(byte scale, byte sign, int hi, int mid, int low) { }
        public DecimalConstantAttribute(byte scale, byte sign, uint hi, uint mid, uint low) { }
        public decimal Value => throw null;
    }
    public class DiscardableAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class EnumeratorCancellationAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class ExtensionAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FixedAddressValueTypeAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class FixedBufferAttribute : Attribute
    {
        public FixedBufferAttribute(Type elementType, int length) { }
        public Type ElementType => throw null;
        public int Length => throw null;
    }
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public sealed class IndexerNameAttribute : Attribute
    {
        public IndexerNameAttribute(string indexerName) { }
    }
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class IsByRefLikeAttribute : Attribute { }
    public static class IsConst { }
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class IsReadOnlyAttribute : Attribute { }
    public static class IsVolatile { }
    public enum MethodCodeType
    {
        IL = 0,
        Native = 1,
        OPTIL = 2,
        Runtime = 3,
    }
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false)]
    public sealed class MethodImplAttribute : Attribute
    {
        public MethodCodeType MethodCodeType;
        public MethodImplAttribute() { }
        public MethodImplAttribute(short value) { }
        public MethodImplAttribute(MethodImplOptions methodImplOptions) { }
        public MethodImplOptions Value => throw null;
    }
    [Flags]
    public enum MethodImplOptions
    {
        Unmanaged = 0x4,
        NoInlining = 0x8,
        ForwardRef = 0x10,
        Synchronized = 0x20,
        NoOptimization = 0x40,
        PreserveSig = 0x80,
        AggressiveInlining = 0x100,
        AggressiveOptimization = 0x200,
        InternalCall = 0x1000,
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class ReferenceAssemblyAttribute : Attribute
    {
        public ReferenceAssemblyAttribute() { }
        public ReferenceAssemblyAttribute(string? description) { }
        public string? Description => throw null;
    }
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class RuntimeCompatibilityAttribute : Attribute
    {
        public bool WrapNonExceptionThrows { get => throw null; set { } }
    }
    public static class RuntimeFeature
    {
        public const string DefaultImplementationsOfInterfaces = "DefaultImplementationsOfInterfaces";
        public const string PortablePdb = "PortablePdb";
        public static bool IsDynamicCodeCompiled => throw null;
        public static bool IsDynamicCodeSupported => throw null;
        public static bool IsSupported(string feature) => throw null;
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method
        | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event)]
    public sealed class SpecialNameAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct
        | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event
        | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public sealed class TupleElementNamesAttribute : Attribute
    {
        public TupleElementNamesAttribute(string?[] transformNames) { }
        public IList<string?> TransformNames => throw null;
    }
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class TypeForwardedToAttribute : Attribute
    {
        public TypeForwardedToAttribute(Type destination) { }
        public Type Destination => throw null;
    }
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class UnsafeValueTypeAttribute : Attribute { }
}
namespace System.Runtime.InteropServices
{
    public enum CharSet
    {
        None = 1,
        Ansi = 2,
        Unicode = 3,
        Auto = 4,
    }
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct
        | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property
        | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
    public sealed class ComVisibleAttribute : Attribute
    {
        public ComVisibleAttribute(bool visibility) { }
        public bool Value => throw null;
    }
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class FieldOffsetAttribute : Attribute
    {
        public FieldOffsetAttribute(int offset) { }
        public int Value => throw null;
    }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class InAttribute : Attribute { }
    public enum LayoutKind
    {
        Sequential = 0,
        Explicit = 2,
        Auto = 3,
    }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class OutAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class StructLayoutAttribute : Attribute
    {
        public CharSet CharSet;
        public int Pack;
        public int Size;
        public StructLayoutAttribute(short layoutKind) { }
        public StructLayoutAttribute(LayoutKind layoutKind) { }
        public LayoutKind Value => throw null;
    }
}
namespace System.Runtime.Serialization
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class OptionalFieldAttribute : Attribute
    {
        public int VersionAdded { get => throw null; set { } }
    }
}
