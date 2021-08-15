namespace Compiler
{
	public static class FnvHashHelper
	{
		public const uint FNV_1A_INITIAL = 2166136261;
		public const uint FNV_1A_STEP = 16777619;

		public static uint HashString(string str)
		{
			uint hash = FNV_1A_INITIAL;
			foreach (var c in str)
			{
				hash ^= c;
				hash = unchecked(hash * FNV_1A_STEP);
			}
			return hash;
		}
	}
}
