namespace PropertyBinder.Tests
{
    internal class UniversalStubEx : UniversalStub
    {
        public string String3 { get; set; }

        public UniversalStubEx NestedEx { get; set; }
    }
}