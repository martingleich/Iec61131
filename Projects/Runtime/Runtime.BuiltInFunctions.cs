using Runtime.IR;
using System.Collections.Immutable;

namespace Runtime
{
	public sealed partial class Runtime
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
			catch(System.ArithmeticException e)
			{
				throw Panic(e.Message);
			}
		}
		private bool TryBuiltInCall_Unsafe(
			PouId callee,
			ImmutableArray<LocalVarOffset> inputs,
			ImmutableArray<LocalVarOffset> outputs)
		{
			if (outputs.Length != 1)
				return false;
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
				case "__SYSTEM::EQUAL_SINT":
					WriteBOOL(returnLocation, LoadSINT(inputs[0]) == LoadSINT(inputs[1]));
					return true;
				case "__SYSTEM::NOT_EQUAL_SINT":
					WriteBOOL(returnLocation, LoadSINT(inputs[0]) != LoadSINT(inputs[1]));
					return true;
				case "__SYSTEM::LESS_SINT":
					WriteBOOL(returnLocation, LoadSINT(inputs[0]) < LoadSINT(inputs[1]));
					return true;
				case "__SYSTEM::LESS_EQUAL_SINT":
					WriteBOOL(returnLocation, LoadSINT(inputs[0]) <= LoadSINT(inputs[1]));
					return true;
				case "__SYSTEM::GREATER_SINT":
					WriteBOOL(returnLocation, LoadSINT(inputs[0]) > LoadSINT(inputs[1]));
					return true;
				case "__SYSTEM::GREATER_EQUAL_SINT":
					WriteBOOL(returnLocation, LoadSINT(inputs[0]) >= LoadSINT(inputs[1]));
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
				case "__SYSTEM::EQUAL_INT":
					WriteBOOL(returnLocation, LoadINT(inputs[0]) == LoadINT(inputs[1]));
					return true;
				case "__SYSTEM::NOT_EQUAL_INT":
					WriteBOOL(returnLocation, LoadINT(inputs[0]) != LoadINT(inputs[1]));
					return true;
				case "__SYSTEM::LESS_INT":
					WriteBOOL(returnLocation, LoadINT(inputs[0]) < LoadINT(inputs[1]));
					return true;
				case "__SYSTEM::LESS_EQUAL_INT":
					WriteBOOL(returnLocation, LoadINT(inputs[0]) <= LoadINT(inputs[1]));
					return true;
				case "__SYSTEM::GREATER_INT":
					WriteBOOL(returnLocation, LoadINT(inputs[0]) > LoadINT(inputs[1]));
					return true;
				case "__SYSTEM::GREATER_EQUAL_INT":
					WriteBOOL(returnLocation, LoadINT(inputs[0]) >= LoadINT(inputs[1]));
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
				case "__SYSTEM::EQUAL_DINT":
					WriteBOOL(returnLocation, LoadDINT(inputs[0]) == LoadDINT(inputs[1]));
					return true;
				case "__SYSTEM::NOT_EQUAL_DINT":
					WriteBOOL(returnLocation, LoadDINT(inputs[0]) != LoadDINT(inputs[1]));
					return true;
				case "__SYSTEM::LESS_DINT":
					WriteBOOL(returnLocation, LoadDINT(inputs[0]) < LoadDINT(inputs[1]));
					return true;
				case "__SYSTEM::LESS_EQUAL_DINT":
					WriteBOOL(returnLocation, LoadDINT(inputs[0]) <= LoadDINT(inputs[1]));
					return true;
				case "__SYSTEM::GREATER_DINT":
					WriteBOOL(returnLocation, LoadDINT(inputs[0]) > LoadDINT(inputs[1]));
					return true;
				case "__SYSTEM::GREATER_EQUAL_DINT":
					WriteBOOL(returnLocation, LoadDINT(inputs[0]) >= LoadDINT(inputs[1]));
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
				case "__SYSTEM::EQUAL_REAL":
					WriteBOOL(returnLocation, LoadREAL(inputs[0]) == LoadREAL(inputs[1]));
					return true;
				case "__SYSTEM::NOT_EQUAL_REAL":
					WriteBOOL(returnLocation, LoadREAL(inputs[0]) != LoadREAL(inputs[1]));
					return true;
				case "__SYSTEM::LESS_REAL":
					WriteBOOL(returnLocation, LoadREAL(inputs[0]) < LoadREAL(inputs[1]));
					return true;
				case "__SYSTEM::LESS_EQUAL_REAL":
					WriteBOOL(returnLocation, LoadREAL(inputs[0]) <= LoadREAL(inputs[1]));
					return true;
				case "__SYSTEM::GREATER_REAL":
					WriteBOOL(returnLocation, LoadREAL(inputs[0]) > LoadREAL(inputs[1]));
					return true;
				case "__SYSTEM::GREATER_EQUAL_REAL":
					WriteBOOL(returnLocation, LoadREAL(inputs[0]) >= LoadREAL(inputs[1]));
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
				case "__SYSTEM::EQUAL_LREAL":
					WriteBOOL(returnLocation, LoadLREAL(inputs[0]) == LoadLREAL(inputs[1]));
					return true;
				case "__SYSTEM::NOT_EQUAL_LREAL":
					WriteBOOL(returnLocation, LoadLREAL(inputs[0]) != LoadLREAL(inputs[1]));
					return true;
				case "__SYSTEM::LESS_LREAL":
					WriteBOOL(returnLocation, LoadLREAL(inputs[0]) < LoadLREAL(inputs[1]));
					return true;
				case "__SYSTEM::LESS_EQUAL_LREAL":
					WriteBOOL(returnLocation, LoadLREAL(inputs[0]) <= LoadLREAL(inputs[1]));
					return true;
				case "__SYSTEM::GREATER_LREAL":
					WriteBOOL(returnLocation, LoadLREAL(inputs[0]) > LoadLREAL(inputs[1]));
					return true;
				case "__SYSTEM::GREATER_EQUAL_LREAL":
					WriteBOOL(returnLocation, LoadLREAL(inputs[0]) >= LoadLREAL(inputs[1]));
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
				case "__SYSTEM::EQUAL_BOOL":
					WriteBOOL(returnLocation, LoadBOOL(inputs[0]) == LoadBOOL(inputs[1]));
					return true;
				case "__SYSTEM::NOT_EQUAL_BOOL":
					WriteBOOL(returnLocation, LoadBOOL(inputs[0]) != LoadBOOL(inputs[1]));
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
