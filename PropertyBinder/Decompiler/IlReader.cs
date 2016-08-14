using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace PropertyBinder.Decompiler
{
    internal class IlReader
    {
        private static readonly OpCode[] OpCodes = new OpCode[256];
        private static readonly OpCode[] OpCodesExt = new OpCode[256];

        static IlReader()
        {
            foreach (var opcode in typeof(OpCodes).GetFields()
                .Where(x => x.FieldType == typeof(OpCode))
                .Select(x => x.GetValue(null))
                .Cast<OpCode>())
            {
                if (opcode.Size == 1)
                {
                    OpCodes[opcode.Value] = opcode;
                }
                else
                {
                    OpCodesExt[(ushort)opcode.Value % 0x100] = opcode;
                }
            }

        }

        private readonly MethodInfo _method;
        private readonly byte[] _data;
        private int _position;

        public IlReader(MethodInfo method)
        {
            _method = method;
            _data = method.GetMethodBody().GetILAsByteArray();
        }

        public IEnumerable<Operation> ReadToEnd()
        {
            while (_position < _data.Length)
            {
                var opCode = ReadOpCode();
                yield return new Operation(opCode, ReadOperand(opCode.OperandType));
            }
        }

        private OpCode ReadOpCode()
        {
            if (_data[_position] == 0xFE)
            {
                var opcode = OpCodesExt[_data[_position + 1]];
                _position += 2;
                return opcode;
            }

            return OpCodes[_data[_position++]];
        }

        private object ReadOperand(OperandType operandType)
        {
            if (_position >= _data.Length)
            {
                return null;
            }

            object res = null;
            switch (operandType)
            {
                case OperandType.InlineBrTarget:
                case OperandType.InlineI:
                {
                    res = BitConverter.ToInt32(_data, _position);
                    _position += 4;
                    break;
                }

                case OperandType.InlineField:
                {
                    res = _method.Module.ResolveField(BitConverter.ToInt32(_data, _position));
                    _position += 4;
                    break;
                }

                case OperandType.InlineI8:
                {
                    res = BitConverter.ToInt64(_data, _position);
                    _position += 8;
                    break;
                }

                case OperandType.InlineMethod:
                {
                    res = _method.Module.ResolveMethod(BitConverter.ToInt32(_data, _position));
                    _position += 4;
                    break;
                }

                case OperandType.InlineR:
                {
                    res = BitConverter.ToDouble(_data, _position);
                    _position += 8;
                    break;
                }

                case OperandType.InlineSig:
                {
                    res = _method.Module.ResolveSignature(BitConverter.ToInt32(_data, _position));
                    _position += 4;
                    break;
                }

                case OperandType.InlineString:
                {
                    res = _method.Module.ResolveString(BitConverter.ToInt32(_data, _position));
                    _position += 4;
                    break;
                }

                case OperandType.InlineType:
                {
                    res = _method.Module.ResolveType(BitConverter.ToInt32(_data, _position));
                    _position += 4;
                    break;
                }

                case OperandType.InlineVar:
                {
                    res = (int) BitConverter.ToInt16(_data, _position);
                    _position += 2;
                    break;
                }

                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                {
                    res = (int) _data[_position++];
                    break;
                }

                case OperandType.ShortInlineR:
                {
                    res = (double) BitConverter.ToSingle(_data, _position);
                    _position += 4;
                    break;
                }
            }

            return res;
        }
    }
}