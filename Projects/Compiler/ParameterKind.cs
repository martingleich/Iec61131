using System;

namespace Compiler
{
	public sealed class ParameterKind : IEquatable<ParameterKind>
	{
		public readonly static ParameterKind Input = new(VarInputToken.DefaultGenerating, AssignToken.DefaultGenerating);
		public readonly static ParameterKind Output = new(VarOutToken.DefaultGenerating, DoubleArrowToken.DefaultGenerating);
		public readonly static ParameterKind InOut = new(VarInOutToken.DefaultGenerating, AssignToken.DefaultGenerating);

		private ParameterKind(string code, string assignCode)
		{
			Code = code ?? throw new ArgumentNullException(nameof(code));
			AssignCode = assignCode ?? throw new ArgumentNullException(nameof(assignCode));
		}

		public string Code { get; }
		public string AssignCode { get; }

		public bool Equals(ParameterKind? other) => other != null && other.Code == Code;
		public override bool Equals(object? obj) => throw new NotImplementedException();
		public override int GetHashCode() => Code.GetHashCode();
		public override string ToString() => Code;

		public static ParameterKind? TryMapDecl(IVarDeclKindToken token) => token.Accept(ParameterKindMapper.Instance);
		private sealed class ParameterKindMapper : IVarDeclKindToken.IVisitor<ParameterKind?>
		{
			public static readonly ParameterKindMapper Instance = new();
			public ParameterKind? Visit(VarToken varToken) => null;
			public ParameterKind? Visit(VarInputToken varInputToken) => Input;
			public ParameterKind? Visit(VarGlobalToken varGlobalToken) => null;
			public ParameterKind? Visit(VarOutToken varOutToken) => Output;
			public ParameterKind? Visit(VarInOutToken varInOutToken) => InOut;
			public ParameterKind? Visit(VarTempToken varTempToken) => null;
		}

		internal bool MatchesAssignKind(IParameterKindToken parameterKind) => parameterKind.Accept(ParameterKindChecker.Instance, this);
		private sealed class ParameterKindChecker : IParameterKindToken.IVisitor<bool, ParameterKind>
		{
			public static readonly ParameterKindChecker Instance = new();

			public bool Visit(AssignToken assignToken, ParameterKind context) => context.Equals(Input) || context.Equals(InOut);
			public bool Visit(DoubleArrowToken doubleArrowToken, ParameterKind context) => context.Equals(Output);
		}
	}
}