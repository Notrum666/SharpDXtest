using Engine.BaseAssets.Components;

namespace SharpDXtest.Assets.Components
{
    public struct TestStruct2
    {
        public int ImOutOfIdeas = 142;
        public int ImOutOfIdeas2 = 142;

        public TestStruct2() { }
    }
    public struct TestStruct
    {
        public int StructIntValue = 142;
        public double StructDoubleValue = 0.142;
        public TestStruct2 StructSubStruct = new TestStruct2();
        public string StructStringValue = "Number 142";

        public TestStruct() { }
    }
    public class InspectorTestComponent : Component
    {
        public bool BoolValue = true;
        public byte ByteValue = 42;
        public sbyte SignedByteValue = 42;
        public char CharValue = 'a';
        public TestStruct TestStructValue = new TestStruct();
        public decimal DecimalValue = 42;
        public double DoubleValue = 0.42;
        public float FloatValue = 0.42f;
        public int IntValue = 42;
        public uint UnsignedIntValue = 42;
        public long LongValue = 42;
        public ulong UnsignedLongValue = 42;
        public short ShortValue = 42;
        public ushort UnsignedShortValue = 42;
    }
}