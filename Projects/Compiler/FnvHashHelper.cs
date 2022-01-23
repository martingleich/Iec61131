namespace Compiler
{
	public static class FnvHashHelper
	{
		public const uint FNV_1A_INITIAL = 2166136261;
		public const uint FNV_1A_STEP = 16777619;

		public static FnvHashHelper.Hash HashString(string str)
		{
			var hash = Hash.Initial;
			foreach (var c in str)
				hash.Add(c);
			return hash;
		}

		public readonly struct Hash
		{
			private readonly uint _hash;
			public static readonly Hash Initial = new (FNV_1A_INITIAL);
			private Hash(uint hash)
			{
				_hash = hash;
			}

			public Hash Add(char c) => new(unchecked((_hash ^ c) * FNV_1A_STEP));
			public uint Value => _hash;
		}
	}
}
