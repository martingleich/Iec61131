using Compiler.Messages;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Compiler
{
	public sealed class FlowAnalyzer : IBoundStatement.IVisitor<FlowAnalyzer.FlowState, FlowAnalyzer.FlowState>
	{
		private readonly MessageBag _messages;

		private readonly Reader _reader;
		private readonly AddressTaker _writeAddressTaker;
		private readonly AddressTaker _readWriteAddressTaker;
		private readonly ImmutableDictionary<IVariableSymbol, int> _variableTable;

		private FlowAnalyzer(ImmutableDictionary<IVariableSymbol, int> variableTable, MessageBag messages)
		{
			_messages = messages ?? throw new ArgumentNullException(nameof(messages));
			_reader = new Reader(this);
			_writeAddressTaker = AddressTaker.CreateWrite(this);
			_readWriteAddressTaker = AddressTaker.CreateReadWrite(this);
			_variableTable = variableTable ?? throw new ArgumentNullException(nameof(variableTable));
		}

		private static ImmutableDictionary<IVariableSymbol, int> CreateVariableTable(ImmutableArray<TrackedVariable> trackedVariables)
		{
			var table = ImmutableDictionary.CreateBuilder<IVariableSymbol, int>(ReferenceEqualityComparer<IVariableSymbol>.Instance);
			int id = 0;
			foreach (var trackedVariable in trackedVariables)
			{
				table.Add(trackedVariable.Variable, id);
				++id;
			}
			return table.ToImmutable();
		}

		public enum VariableKind
		{
			Unrequired = 0,
			Inline = 1,
			Required = 2,
		}

		public readonly struct TrackedVariable
		{
			public readonly IVariableSymbol Variable;
			public readonly VariableKind Kind;

			public TrackedVariable(IVariableSymbol variable, VariableKind kind)
			{
				Variable = variable ?? throw new ArgumentNullException(nameof(variable));
				Kind = kind;
			}

			public static TrackedVariable Required(IVariableSymbol variable) => new (variable, VariableKind.Required);
			public static TrackedVariable Inline(IVariableSymbol variable) => new (variable, VariableKind.Inline);
			public static TrackedVariable Unrequired(IVariableSymbol variable) => new (variable, VariableKind.Unrequired);
			public void Deconstruct(out IVariableSymbol variable, out VariableKind kind)
			{
				variable = Variable;
				kind = Kind;
			}
		}
		public static void Analyse(
			ImmutableArray<IBoundExpression> initialExpressions,
			IBoundStatement statement,
			ImmutableArray<TrackedVariable> trackedVariables,
			MessageBag messages)
		{
			var variableTable = CreateVariableTable(trackedVariables);
			var analyzer = new FlowAnalyzer(variableTable, messages);
			analyzer.Analyze(initialExpressions, statement, messages, trackedVariables);
		}

		private void Analyze(
			ImmutableArray<IBoundExpression> initialExpressions,
			IBoundStatement statement,
			MessageBag messages,
			ImmutableArray<TrackedVariable> trackedVariables)
		{
			var state = FlowState.Empty(_variableTable.Count);
			foreach (var (variable, kind) in trackedVariables)
				if(kind == VariableKind.Inline) // Mark all inline variables as readable until they are declared, so we don't report duplicate errors for using a variable before it is declared.
					state = state.MarkReadable(_variableTable, variable);
			foreach (var initial in initialExpressions)
				state = initial.Accept(_reader, state);
			var result = statement.Accept(this, state);
			foreach (var (variable, kind) in trackedVariables)
			{
				if (kind == VariableKind.Required && !result.CanRead(_variableTable, variable))
					messages.Add(new VariableMustBeAssignedBeforeEndOfFunctionMessage(variable, variable.DeclaringSpan));
			}
		}

		private void AddMessage(IMessage message)
		{
			_messages.Add(message);
		}

		FlowState IBoundStatement.IVisitor<FlowState, FlowState>.Visit(SequenceBoundStatement sequenceBoundStatement, FlowState context)
		{
			FlowState end = context;
			foreach (var st in sequenceBoundStatement.Statements)
			{
				if (!end.Reaches)
				{
					AddMessage(new UnreachableCodeMessage(st.OriginalNode.SourceSpan));
					break;
				}
				else
				{
					end = st.Accept(this, end);
				}
			}
			return end;
		}

		FlowState IBoundStatement.IVisitor<FlowState, FlowState>.Visit(ExpressionBoundStatement expressionBoundStatement, FlowState context)
			=> expressionBoundStatement.Expression.Accept(_reader, context);

		FlowState IBoundStatement.IVisitor<FlowState, FlowState>.Visit(AssignBoundStatement assignToExpressionBoundStatement, FlowState context)
		{
			var (afterTakeAddress, assigner) = assignToExpressionBoundStatement.LeftSide.Accept(_writeAddressTaker, context);
			var afterRead = assignToExpressionBoundStatement.RightSide.Accept(_reader, afterTakeAddress);
			var afterAssign = assigner.PerformAssign(_variableTable, afterRead);
			return afterAssign;
		}

		FlowState IBoundStatement.IVisitor<FlowState, FlowState>.Visit(IfBoundStatement ifBoundStatement, FlowState context)
		{
			FlowState preCondition = context;
			FlowState? end = null;
			bool isComplete = false;
			foreach (var branch in ifBoundStatement.Branches)
			{
				if (branch.Condition != null)
					preCondition = branch.Condition.Accept(_reader, preCondition);
				else
					isComplete = true;
				var afterBody = branch.Body.Accept(this, preCondition);
				end = FlowState.Merge(end, afterBody);
			}
			if (!isComplete)
				end = FlowState.Merge(end, preCondition);

#pragma warning disable CS8629 // Nullable value type may be null. 
			// There is always at least one branch.
			return end.Value;
#pragma warning restore CS8629 // Nullable value type may be null.
		}

		FlowState IBoundStatement.IVisitor<FlowState, FlowState>.Visit(WhileBoundStatement whileBoundStatement, FlowState context)
		{
			var afterCondition = whileBoundStatement.Condition.Accept(_reader, context);
			var afterBody = whileBoundStatement.Body.Accept(this, afterCondition);
			var end = FlowState.Merge(afterCondition, afterBody);
			return end.PopLoopControl();
		}

		FlowState IBoundStatement.IVisitor<FlowState, FlowState>.Visit(ForLoopBoundStatement forLoopBoundStatement, FlowState context)
		{
			var (afterIndexAddress, indexAssigner) = forLoopBoundStatement.Index.Accept(_writeAddressTaker, context);
			var afterInitialRead = forLoopBoundStatement.Initial.Accept(_reader, afterIndexAddress);
			var afterIndexAssign = indexAssigner.PerformAssign(_variableTable, afterInitialRead);
			var afterConditionRead = forLoopBoundStatement.UpperBound.Accept(_reader, afterIndexAssign);
			var afterStepRead = forLoopBoundStatement.Step.Accept(_reader, afterConditionRead);
			var afterBody = forLoopBoundStatement.Body.Accept(this, afterStepRead);
			var end = FlowState.Merge(afterStepRead, afterBody);
			return end.PopLoopControl();
		}

		FlowState IBoundStatement.IVisitor<FlowState, FlowState>.Visit(ExitBoundStatement exitBoundStatement, FlowState context)
			=> context.AddExit();

		FlowState IBoundStatement.IVisitor<FlowState, FlowState>.Visit(ContinueBoundStatement continueBoundStatement, FlowState context)
			=> context.AddContinue();

		FlowState IBoundStatement.IVisitor<FlowState, FlowState>.Visit(ReturnBoundStatement returnBoundStatement, FlowState context)
			=> context.AddReturn();
		FlowState IBoundStatement.IVisitor<FlowState, FlowState>.Visit(InitVariableBoundStatement initVariableBoundStatement, FlowState context)
		{
			if (initVariableBoundStatement.RightSide is IBoundExpression initial)
			{
				var afterReadInitial = initial.Accept(_reader, context);
				var assigner = Assigner.ForVariable(initVariableBoundStatement.LeftSide);
				return assigner.PerformAssign(_variableTable, afterReadInitial);
			}
			else
			{
				return context.MarkUnreadable(_variableTable, initVariableBoundStatement.LeftSide);
			}
		}

		private readonly struct Assigner
		{
			public static readonly Assigner Null = new(null);
			public static Assigner ForVariable(IVariableSymbol variable)
			{
				if (variable is null)
					throw new ArgumentNullException(nameof(variable));
				return new(variable);
			}
			private readonly IVariableSymbol? _variable;
			private Assigner(IVariableSymbol? variable)
			{
				_variable = variable;
			}
			public FlowState PerformAssign(ImmutableDictionary<IVariableSymbol, int> table, FlowState context) => _variable is IVariableSymbol variable ? context.MarkReadable(table, variable) : context;
		}

		private sealed class AddressTaker : IBoundExpression.IVisitor<(FlowState, Assigner), FlowState>
		{
			private readonly FlowAnalyzer _owner;
			private readonly bool _readWrite;
			private Reader Reader => _owner._reader;
			private AddressTaker ReadWriteAddressTaker => _owner._readWriteAddressTaker;

			private AddressTaker(bool readWrite, FlowAnalyzer owner)
			{
				_readWrite = readWrite;
				_owner = owner ?? throw new ArgumentNullException(nameof(owner));
			}
			public static AddressTaker CreateReadWrite(FlowAnalyzer owner) => new(true, owner);
			public static AddressTaker CreateWrite(FlowAnalyzer owner) => new(false, owner);

			public (FlowState, Assigner) Visit(ArrayIndexAccessBoundExpression arrayIndexAccessBoundExpression, FlowState context)
			{
				var afterRead = Reader.Visit(arrayIndexAccessBoundExpression, context);
				return (afterRead, Assigner.Null);
			}

			public (FlowState, Assigner) Visit(PointerIndexAccessBoundExpression pointerIndexAccessBoundExpression, FlowState context)
			{
				var afterRead = Reader.Visit(pointerIndexAccessBoundExpression, context);
				return (afterRead, Assigner.Null);
			}
			public (FlowState, Assigner) Visit(FieldAccessBoundExpression fieldAccessBoundExpression, FlowState context)
			{
				var baseAddress = fieldAccessBoundExpression.BaseExpression.Accept(ReadWriteAddressTaker, context);
				return (baseAddress.Item1, Assigner.Null);
			}
			public (FlowState, Assigner) Visit(DerefBoundExpression derefBoundExpression, FlowState context)
			{
				var afterRead = derefBoundExpression.Value.Accept(Reader, context);
				return (afterRead, Assigner.Null);
			}
			public (FlowState, Assigner) Visit(VariableBoundExpression variableBoundExpression, FlowState context)
			{
				if (_readWrite && !context.CanRead(_owner._variableTable, variableBoundExpression.Variable, out context))
					_owner.AddMessage(new UseOfUnassignedVariableMessage(variableBoundExpression.Variable, variableBoundExpression.OriginalNode.SourceSpan));
				return (context, Assigner.ForVariable(variableBoundExpression.Variable));
			}

			#region NonLValue
			public (FlowState, Assigner) Visit(LiteralBoundExpression literalBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(SizeOfTypeBoundExpression sizeOfTypeBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(ImplicitEnumToBaseTypeCastBoundExpression implicitEnumCastBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(BinaryOperatorBoundExpression binaryOperatorBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(ImplicitPointerTypeCastBoundExpression implicitPointerTypeCaseBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(ImplicitCastBoundExpression implicitArithmeticCaseBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(UnaryOperatorBoundExpression unaryOperatorBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(PointerDiffrenceBoundExpression pointerDiffrenceBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(PointerOffsetBoundExpression pointerOffsetBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(ImplicitAliasToBaseTypeCastBoundExpression aliasToBaseTypeCastBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(ImplicitErrorCastBoundExpression implicitErrorCastBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(ImplicitAliasFromBaseTypeCastBoundExpression implicitAliasFromBaseTypeCastBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(ImplicitDiscardBoundExpression implicitDiscardBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(CallBoundExpression callBoundExpression, FlowState context) => NonLValue(context);
			public (FlowState, Assigner) Visit(InitializerBoundExpression initializerBoundExpression, FlowState context) => NonLValue(context);
			#endregion NonLValue
			private static (FlowState, Assigner) NonLValue(FlowState context) => (context, Assigner.Null);
		}
		private sealed class Reader : IBoundExpression.IVisitor<FlowState, FlowState>
		{
			private readonly FlowAnalyzer _owner;
			private AddressTaker WriteAddressTaker => _owner._writeAddressTaker;
			private AddressTaker ReadWriteAddressTaker => _owner._readWriteAddressTaker;

			public Reader(FlowAnalyzer owner)
			{
				_owner = owner ?? throw new ArgumentNullException(nameof(owner));
			}

			public FlowState Visit(VariableBoundExpression variableBoundExpression, FlowState context)
			{
				if (!context.CanRead(_owner._variableTable, variableBoundExpression.Variable, out context))
					_owner.AddMessage(new UseOfUnassignedVariableMessage(variableBoundExpression.Variable, variableBoundExpression.OriginalNode.SourceSpan));
				return context;
			}

			private readonly List<Assigner> _sharedAssignerStack = new(); // Use to reduce allocation count. Use like a stack.
			public FlowState Visit(CallBoundExpression callBoundExpression, FlowState context)
			{
				var afterCallee = callBoundExpression.Callee.Accept(this, context);
				var afterCall = afterCallee;
				var assignerStart = _sharedAssignerStack.Count;
				foreach (var arg in callBoundExpression.Arguments)
				{
					// arg.Parameter is always a parameter variable from the function and not relevant to control flow
					// Maybe containing a non-relevant cast, but never real flow/assign logic.
					var kind = arg.ParameterSymbol.Kind;
					if (kind.Equals(ParameterKind.Input))
					{
						afterCall = arg.Value.Accept(this, afterCall);
					}
					else if (kind.Equals(ParameterKind.Output))
					{
						var (afterTakeAddress, assigner) = arg.Value.Accept(WriteAddressTaker, afterCall);
						afterCall = afterTakeAddress;
						_sharedAssignerStack.Add(assigner);
					}
					else if (kind.Equals(ParameterKind.InOut))
					{
						var (afterTakeAddress, assigner) = arg.Value.Accept(ReadWriteAddressTaker, afterCall);
						afterCall = afterTakeAddress;
						_sharedAssignerStack.Add(assigner);
					}
					else
					{
						throw new InvalidOperationException($"Unknown parameter kind '{kind}'.");
					}
				}

				for (var i = assignerStart; i < _sharedAssignerStack.Count; ++i)
					afterCall = _sharedAssignerStack[i].PerformAssign(_owner._variableTable, afterCall);
				_sharedAssignerStack.ShrinkToSize(assignerStart);

				return afterCall;
			}

			#region Trivial
			public FlowState Visit(FieldAccessBoundExpression fieldAccessBoundExpression, FlowState context)
				=> fieldAccessBoundExpression.BaseExpression.Accept(this, context);
			public FlowState Visit(LiteralBoundExpression literalBoundExpression, FlowState context) => context;
			public FlowState Visit(SizeOfTypeBoundExpression sizeOfTypeBoundExpression, FlowState context) => context;
			public FlowState Visit(ImplicitEnumToBaseTypeCastBoundExpression implicitEnumCastBoundExpression, FlowState context)
				=> implicitEnumCastBoundExpression.Value.Accept(this, context);
			public FlowState Visit(BinaryOperatorBoundExpression binaryOperatorBoundExpression, FlowState context)
				=> ReadMany(binaryOperatorBoundExpression.Left, binaryOperatorBoundExpression.Right, context);
			public FlowState Visit(ImplicitPointerTypeCastBoundExpression implicitPointerTypeCaseBoundExpression, FlowState context)
				=> implicitPointerTypeCaseBoundExpression.Value.Accept(this, context);
			public FlowState Visit(ImplicitCastBoundExpression implicitArithmeticCaseBoundExpression, FlowState context)
				=> implicitArithmeticCaseBoundExpression.Value.Accept(this, context);
			public FlowState Visit(UnaryOperatorBoundExpression unaryOperatorBoundExpression, FlowState context)
				=> unaryOperatorBoundExpression.Value.Accept(this, context);
			public FlowState Visit(PointerDiffrenceBoundExpression pointerDiffrenceBoundExpression, FlowState context)
				=> ReadMany(pointerDiffrenceBoundExpression.Left, pointerDiffrenceBoundExpression.Right, context);
			public FlowState Visit(PointerOffsetBoundExpression pointerOffsetBoundExpression, FlowState context)
				=> ReadMany(pointerOffsetBoundExpression.Left, pointerOffsetBoundExpression.Right, context);
			public FlowState Visit(DerefBoundExpression derefBoundExpression, FlowState context)
				=> derefBoundExpression.Value.Accept(this, context);
			public FlowState Visit(ImplicitAliasToBaseTypeCastBoundExpression aliasToBaseTypeCastBoundExpression, FlowState context)
				=> aliasToBaseTypeCastBoundExpression.Value.Accept(this, context);
			public FlowState Visit(ImplicitErrorCastBoundExpression implicitErrorCastBoundExpression, FlowState context)
				=> implicitErrorCastBoundExpression.Value.Accept(this, context);
			public FlowState Visit(ImplicitAliasFromBaseTypeCastBoundExpression implicitAliasFromBaseTypeCastBoundExpression, FlowState context)
				=> implicitAliasFromBaseTypeCastBoundExpression.Value.Accept(this, context);
			public FlowState Visit(ArrayIndexAccessBoundExpression arrayIndexAccessBoundExpression, FlowState context)
			{
				var afterBase = arrayIndexAccessBoundExpression.Base.Accept(this, context);
				var end = ReadMany(arrayIndexAccessBoundExpression.Indices, afterBase);
				return end;
			}
			public FlowState Visit(PointerIndexAccessBoundExpression pointerIndexAccessBoundExpression, FlowState context)
			{
				var afterBase = pointerIndexAccessBoundExpression.Base.Accept(this, context);
				var end = ReadMany(pointerIndexAccessBoundExpression.Indices, afterBase);
				return end;
			}
			public FlowState Visit(ImplicitDiscardBoundExpression implicitDiscardBoundExpression, FlowState context)
				=> implicitDiscardBoundExpression.Value.Accept(this, context);
			public FlowState Visit(InitializerBoundExpression initializerBoundExpression, FlowState context)
			{
				var end = context;
				foreach (var elem in initializerBoundExpression.Elements)
					end = elem.Value.Accept(this, end); // The left side is always compile time constant.
				return end;
			}
			#endregion Trivial

			private FlowState ReadMany(IBoundExpression a, IBoundExpression b, FlowState context)
			{
				var afterA = a.Accept(this, context);
				var afterB = b.Accept(this, afterA);
				return afterB;
			}
			private FlowState ReadMany<T>(ImmutableArray<T> xs, FlowState context) where T : IBoundExpression
			{
				var end = context;
				foreach (var x in xs)
					end = x.Accept(this, end);
				return end;
			}
		}

		[Flags]
		private enum ReachState
		{
			None = 0,
			DefReturn = 1,
			DefExit = 2,
			DefContinue = 4,
		}

		private readonly struct BitSet
		{
			private readonly ulong[] _bits;
			public BitSet(int size)
			{
				int blocks = size >> 6;
				if (blocks * 32 < size)
					blocks += 1;
				_bits = new ulong[blocks];
			}
			private BitSet(ulong[] bits)
			{
				_bits = bits;
			}

			public bool Contains(int i) => (_bits[i >> 6] & (1ul << (i & 0x3F))) != 0;
			public BitSet Add(int i)
			{
				var copy = new ulong[_bits.Length];
				Array.Copy(_bits, copy, _bits.Length);
				copy[i >> 6] |= 1ul << (i & 0x3F);
				return new BitSet(copy);
			}
			public BitSet Remove(int i)
			{
				var copy = new ulong[_bits.Length];
				Array.Copy(_bits, copy, _bits.Length);
				copy[i >> 6] &= ~(1ul << (i & 0x3F));
				return new BitSet(copy);
			}
			public BitSet Intersect(BitSet other)
			{
				var copy = new ulong[_bits.Length];
				Array.Copy(_bits, copy, _bits.Length);
				for (int i = 0; i < _bits.Length; ++i)
					_bits[i] &= other._bits[i];
				return new BitSet(_bits);
			}
		}

		private readonly struct FlowState
		{
			public bool Reaches => _state == ReachState.None;
			private readonly BitSet ReadableVariables;
			private readonly ReachState _state;

			private FlowState(ReachState state, BitSet readableVariables)
			{
				_state = state;
				ReadableVariables = readableVariables;
			}

			public static FlowState Empty(int size) => new(ReachState.None, new BitSet(size));

			public FlowState AddReturn() => new(_state | ReachState.DefReturn, ReadableVariables);
			public FlowState AddContinue() => new(_state | ReachState.DefContinue, ReadableVariables);
			public FlowState AddExit() => new(_state | ReachState.DefExit, ReadableVariables);
			public FlowState PopLoopControl() => new(_state & ReachState.DefReturn, ReadableVariables);

			public static FlowState Merge(FlowState? maybeA, FlowState b) => maybeA is FlowState a ? Merge(a, b) : b;
			public static FlowState Merge(FlowState a, FlowState b) => new(
					a._state & b._state,
					a.ReadableVariables.Intersect(b.ReadableVariables));
			public FlowState MarkReadable(ImmutableDictionary<IVariableSymbol, int> table, IVariableSymbol variable)
			{
				if (table.TryGetValue(variable, out int id) && !ReadableVariables.Contains(id))
					return new FlowState(_state, ReadableVariables.Add(id));
				else
					return this;
			}
			public FlowState MarkUnreadable(ImmutableDictionary<IVariableSymbol, int> table, IVariableSymbol variable)
			{
				if (table.TryGetValue(variable, out int id) && ReadableVariables.Contains(id))
					return new FlowState(_state, ReadableVariables.Remove(id));
				else
					return this;
			}
			public bool CanRead(ImmutableDictionary<IVariableSymbol, int> table, IVariableSymbol variable)
				=> !table.TryGetValue(variable, out var id) || ReadableVariables.Contains(id);
			public bool CanRead(ImmutableDictionary<IVariableSymbol, int> table, IVariableSymbol variable, out FlowState forcedRead)
			{
				if (CanRead(table, variable))
				{
					forcedRead = this;
					return true;
				}
				else
				{
					forcedRead = MarkReadable(table, variable);
					return false;
				}
			}
		}
	}
}
