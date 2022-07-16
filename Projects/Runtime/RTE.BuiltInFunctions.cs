using Runtime.IR;
using Superpower;
using System;
using System.Collections.Immutable;

namespace Runtime
{
    public sealed partial class RTE
	{
		private bool TryBuiltInCall(
			PouId callee,
			ImmutableArray<LocalVarOffset> inputs,
			ImmutableArray<LocalVarOffset> outputs)
		{
			try
			{
				return TryBuiltInCall_Unsafe(callee, inputs, outputs);
			}
			catch(ArithmeticException e)
			{
				throw Panic(e.Message);
			}
		}

        private static Func<int, bool>? GetComparer(string op) => op switch
        {
            "EQUAL" => x => x == 0,
            "NOT_EQUAL" => x => x != 0,
            "LESS" => x => x < 0,
            "LESS_EQUAL" => x => x <= 0,
            "GREATER" => x => x > 0,
            "GREATER_EQUAL" => x => x >= 0,
            _ => null,
        };
        private static Func<bool, bool>? GetEquatable(string op) => op switch
        {
            "EQUAL" => x => x,
            "NOT_EQUAL" => x => !x,
            _ => null,
        };
        private bool TryBuiltInCall_Unsafe(
			PouId callee,
			ImmutableArray<LocalVarOffset> inputs,
			ImmutableArray<LocalVarOffset> outputs)
		{
			if (outputs.Length != 1)
				return false;
			if (callee.Name.StartsWith("__SYSTEM::") && inputs.Length == 2)
			{
				var name = callee.Name["__SYSTEM::".Length..];
				var lastUnderscore = name.IndexOf('_');
				var op = name[..lastUnderscore];
				var type = name[(lastUnderscore + 1)..];
				var maybeType = RuntimeTypeParser.TryParseBuiltIn(type);
				if (maybeType != null)
				{
					if (maybeType is IR.RuntimeTypes.IComparableRuntimeType runtimeComparer && GetComparer(op) is Func<int, bool> resultComparer)
					{
						var arg0 = LoadEffectiveAddress(inputs[0]);
						var arg1 = LoadEffectiveAddress(inputs[1]);
						var compareResult = runtimeComparer.Compare(arg0, arg1, this);
						var result = resultComparer(compareResult);
						WriteBOOL(outputs[0], result);
						return true;
					}
					else if (maybeType is IR.RuntimeTypes.IEquatableRuntimeType runtimeEquatable && GetEquatable(op) is Func<bool, bool> resultEquals)
					{
						var arg0 = LoadEffectiveAddress(inputs[0]);
						var arg1 = LoadEffectiveAddress(inputs[1]);
						var equalsResult = runtimeEquatable.Equals(arg0, arg1, this);
						var result = resultEquals(equalsResult);
						WriteBOOL(outputs[0], result);
						return true;
					}
				}
            }
			var returnLocation = outputs[0];
			switch (callee.Name)
			{
				// SINT
				case "__SYSTEM::ADD_SINT":
					WriteSINT(returnLocation, checked((sbyte)(LoadSINT(inputs[0]) + LoadSINT(inputs[1]))));
					return true;
				case "__SYSTEM::SUB_SINT":
					WriteSINT(returnLocation, checked((sbyte)(LoadSINT(inputs[0]) - LoadSINT(inputs[1]))));
					return true;
				case "__SYSTEM::MUL_SINT":
					WriteSINT(returnLocation, checked((sbyte)(LoadSINT(inputs[0]) * LoadSINT(inputs[1]))));
					return true;
				case "__SYSTEM::DIV_SINT":
					WriteSINT(returnLocation, checked((sbyte)(LoadSINT(inputs[0]) / LoadSINT(inputs[1]))));
					return true;
				case "__SYSTEM::MOD_SINT":
					WriteSINT(returnLocation, checked((sbyte)(LoadSINT(inputs[0]) % LoadSINT(inputs[1]))));
					return true;
				case "__SYSTEM::NEG_SINT":
					WriteSINT(returnLocation, checked((sbyte)-LoadSINT(inputs[0])));
					return true;
				case "__SYSTEM::FOR_LOOP_INIT_SINT":
					{
						var idx = LoadSINT(inputs[0]);
						var step = LoadSINT(inputs[1]);
						var upperBound = LoadSINT(inputs[2]);
						if (step == 0)
							throw Panic("Loop step is zero.");
						bool result = step > 0 ? idx <= upperBound : idx >= upperBound;
						WriteBOOL(returnLocation, result);
						return true;
					}
				case "__SYSTEM::FOR_LOOP_NEXT_SINT":
					{
						var idxPointer = LoadPointer(inputs[0]);
						var idx = LoadSINT(idxPointer);
						var step = LoadSINT(inputs[1]);
						var upperBound = LoadSINT(inputs[2]);
						idx += step;
						bool result;
						var next = unchecked((sbyte)(idx + step));
						if (step > 0)
							result = next > idx && next <= upperBound;
						else
							result = next < idx && next >= upperBound;
						if(result)
							WriteSINT(idxPointer, next);
						WriteBOOL(returnLocation, result);
						return true;
					}
				// INT
				case "__SYSTEM::ADD_INT":
					WriteINT(returnLocation, checked((short)(LoadINT(inputs[0]) + LoadINT(inputs[1]))));
					return true;
				case "__SYSTEM::SUB_INT":
					WriteINT(returnLocation, checked((short)(LoadINT(inputs[0]) - LoadINT(inputs[1]))));
					return true;
				case "__SYSTEM::MUL_INT":
					WriteINT(returnLocation, checked((short)(LoadINT(inputs[0]) * LoadINT(inputs[1]))));
					return true;
				case "__SYSTEM::DIV_INT":
					WriteINT(returnLocation, checked((short)(LoadINT(inputs[0]) / LoadINT(inputs[1]))));
					return true;
				case "__SYSTEM::MOD_INT":
					WriteINT(returnLocation, checked((short)(LoadINT(inputs[0]) % LoadINT(inputs[1]))));
					return true;
				case "__SYSTEM::NEG_INT":
					WriteINT(returnLocation, checked((short)-LoadINT(inputs[0])));
					return true;
				case "__SYSTEM::FOR_LOOP_INIT_INT":
					{
						var idx = LoadINT(inputs[0]);
						var step = LoadINT(inputs[1]);
						var upperBound = LoadINT(inputs[2]);
						if (step == 0)
							throw Panic("Loop step is zero.");
						bool result = step > 0 ? idx <= upperBound : idx >= upperBound;
						WriteBOOL(returnLocation, result);
						return true;
					}
				case "__SYSTEM::FOR_LOOP_NEXT_INT":
					{
						var idxPointer = LoadPointer(inputs[0]);
						var idx = LoadINT(idxPointer);
						var step = LoadINT(inputs[1]);
						var upperBound = LoadINT(inputs[2]);
						idx += step;
						bool result;
						var next = unchecked((short)(idx + step));
						if (step > 0)
							result = next > idx && next <= upperBound;
						else
							result = next < idx && next >= upperBound;
						if(result)
							WriteINT(idxPointer, next);
						WriteBOOL(returnLocation, result);
						return true;
					}
				// DINT
				case "__SYSTEM::ADD_DINT":
					WriteDINT(returnLocation, checked((int)(LoadDINT(inputs[0]) + LoadDINT(inputs[1]))));
					return true;
				case "__SYSTEM::SUB_DINT":
					WriteDINT(returnLocation, checked((int)(LoadDINT(inputs[0]) - LoadDINT(inputs[1]))));
					return true;
				case "__SYSTEM::MUL_DINT":
					WriteDINT(returnLocation, checked((int)(LoadDINT(inputs[0]) * LoadDINT(inputs[1]))));
					return true;
				case "__SYSTEM::DIV_DINT":
					WriteDINT(returnLocation, checked((int)(LoadDINT(inputs[0]) / LoadDINT(inputs[1]))));
					return true;
				case "__SYSTEM::MOD_DINT":
					WriteDINT(returnLocation, checked((int)(LoadDINT(inputs[0]) % LoadDINT(inputs[1]))));
					return true;
				case "__SYSTEM::NEG_DINT":
					WriteDINT(returnLocation, checked((int)-LoadDINT(inputs[0])));
					return true;
				case "__SYSTEM::FOR_LOOP_INIT_DINT":
					{
						var idx = LoadDINT(inputs[0]);
						var step = LoadDINT(inputs[1]);
						var upperBound = LoadDINT(inputs[2]);
						if (step == 0)
							throw Panic("Loop step is zero.");
						bool result = step > 0 ? idx <= upperBound : idx >= upperBound;
						WriteBOOL(returnLocation, result);
						return true;
					}
				case "__SYSTEM::FOR_LOOP_NEXT_DINT":
					{
						var idxPointer = LoadPointer(inputs[0]);
						var idx = LoadDINT(idxPointer);
						var step = LoadDINT(inputs[1]);
						var upperBound = LoadDINT(inputs[2]);
						idx += step;
						bool result;
						var next = unchecked((int)(idx + step));
						if (step > 0)
							result = next > idx && next <= upperBound;
						else
							result = next < idx && next >= upperBound;
						if(result)
							WriteDINT(idxPointer, next);
						WriteBOOL(returnLocation, result);
						return true;
					}
				// LINT
				case "__SYSTEM::ADD_LINT":
					WriteLINT(returnLocation, checked((long)(LoadLINT(inputs[0]) + LoadLINT(inputs[1]))));
					return true;
				case "__SYSTEM::SUB_LINT":
					WriteLINT(returnLocation, checked((long)(LoadLINT(inputs[0]) - LoadLINT(inputs[1]))));
					return true;
				case "__SYSTEM::MUL_LINT":
					WriteLINT(returnLocation, checked((long)(LoadLINT(inputs[0]) * LoadLINT(inputs[1]))));
					return true;
				case "__SYSTEM::DIV_LINT":
					WriteLINT(returnLocation, checked((long)(LoadLINT(inputs[0]) / LoadLINT(inputs[1]))));
					return true;
				case "__SYSTEM::MOD_LINT":
					WriteLINT(returnLocation, checked((long)(LoadLINT(inputs[0]) % LoadLINT(inputs[1]))));
					return true;
				case "__SYSTEM::NEG_LINT":
					WriteLINT(returnLocation, checked((long)-LoadLINT(inputs[0])));
					return true;
				case "__SYSTEM::FOR_LOOP_INIT_LINT":
					{
						var idx = LoadLINT(inputs[0]);
						var step = LoadLINT(inputs[1]);
						var upperBound = LoadLINT(inputs[2]);
						if (step == 0)
							throw Panic("Loop step is zero.");
						bool result = step > 0 ? idx <= upperBound : idx >= upperBound;
						WriteBOOL(returnLocation, result);
						return true;
					}
				case "__SYSTEM::FOR_LOOP_NEXT_LINT":
					{
						var idxPointer = LoadPointer(inputs[0]);
						var idx = LoadLINT(idxPointer);
						var step = LoadLINT(inputs[1]);
						var upperBound = LoadLINT(inputs[2]);
						idx += step;
						bool result;
						var next = unchecked((int)(idx + step));
						if (step > 0)
							result = next > idx && next <= upperBound;
						else
							result = next < idx && next >= upperBound;
						if(result)
							WriteLINT(idxPointer, next);
						WriteBOOL(returnLocation, result);
						return true;
					}
				// REAL
				case "__SYSTEM::ADD_REAL":
					WriteREAL(returnLocation, checked(LoadREAL(inputs[0]) + LoadREAL(inputs[1])));
					return true;
				case "__SYSTEM::SUB_REAL":
					WriteREAL(returnLocation, checked(LoadREAL(inputs[0]) - LoadREAL(inputs[1])));
					return true;
				case "__SYSTEM::MUL_REAL":
					WriteREAL(returnLocation, checked(LoadREAL(inputs[0]) * LoadREAL(inputs[1])));
					return true;
				case "__SYSTEM::DIV_REAL":
					WriteREAL(returnLocation, checked(LoadREAL(inputs[0]) / LoadREAL(inputs[1])));
					return true;
				case "__SYSTEM::MOD_REAL":
					WriteREAL(returnLocation, checked(LoadREAL(inputs[0]) % LoadREAL(inputs[1])));
					return true;
				case "__SYSTEM::NEG_REAL":
					WriteREAL(returnLocation, checked(-LoadREAL(inputs[0])));
					return true;
				// LREAL
				case "__SYSTEM::ADD_LREAL":
					WriteLREAL(returnLocation, checked(LoadLREAL(inputs[0]) + LoadLREAL(inputs[1])));
					return true;
				case "__SYSTEM::SUB_LREAL":
					WriteLREAL(returnLocation, checked(LoadLREAL(inputs[0]) - LoadLREAL(inputs[1])));
					return true;
				case "__SYSTEM::MUL_LREAL":
					WriteLREAL(returnLocation, checked(LoadLREAL(inputs[0]) * LoadLREAL(inputs[1])));
					return true;
				case "__SYSTEM::DIV_LREAL":
					WriteLREAL(returnLocation, checked(LoadLREAL(inputs[0]) / LoadLREAL(inputs[1])));
					return true;
				case "__SYSTEM::MOD_LREAL":
					WriteLREAL(returnLocation, checked(LoadLREAL(inputs[0]) % LoadLREAL(inputs[1])));
					return true;
				case "__SYSTEM::NEG_LREAL":
					WriteLREAL(returnLocation, checked(-LoadLREAL(inputs[0])));
					return true;
				// BOOL
				case "__SYSTEM::AND_BOOL":
					WriteBOOL(returnLocation, LoadBOOL(inputs[0]) & LoadBOOL(inputs[1]));
					return true;
				case "__SYSTEM::OR_BOOL":
					WriteBOOL(returnLocation, LoadBOOL(inputs[0]) | LoadBOOL(inputs[1]));
					return true;
				case "__SYSTEM::XOR_BOOL":
					WriteBOOL(returnLocation, LoadBOOL(inputs[0]) ^ LoadBOOL(inputs[1]));
					return true;
				case "__SYSTEM::NOT_BOOL":
					WriteBOOL(returnLocation, !LoadBOOL(inputs[0]));
					return true;
				// POINTER
				case "__SYSTEM::SUB_POINTER":
					WriteDINT(returnLocation, LoadDINT(inputs[0]) - LoadDINT(inputs[1]));
					return true;
				case "__SYSTEM::ADD_POINTER":
					WriteDINT(returnLocation, LoadDINT(inputs[0]) + LoadDINT(inputs[1]));
					return true;
				default:
					return false;
			}
		}
    }
}
