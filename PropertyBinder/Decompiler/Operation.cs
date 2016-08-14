using System.Reflection.Emit;

namespace PropertyBinder.Decompiler
{
    internal struct Operation
    {
        public readonly OpCode OpCode;
        public readonly object Parameter;

        public Operation(OpCode opCode, object parameter)
        {
            OpCode = opCode;
            Parameter = parameter;
        }
    }
}