namespace Compiler
{
	public sealed class StringPool
	{
#if PROFILE_STRING_POOL
		static uint HIT_COUNT = 0;
		static uint MISS_COUNT = 0;
#endif

		const int SHARED_COUNT_BITS = 10;
		const uint SHARED_COUNT = 1 << SHARED_COUNT_BITS;
		const uint SHARED_COUNT_MASK = SHARED_COUNT - 1;
		const int PROBES_COUNT_BITS = 3;
		const uint PROBES_COUNT = 1 << PROBES_COUNT_BITS;
		const uint PROBES_COUNT_MASK = PROBES_COUNT - 1;
		static readonly string[] SharedHashTable = new string[SHARED_COUNT];
		uint RandState = 3451431235;

		public uint RandProbe()
		{
			uint x = RandState;
			x ^= x << 13;
			x ^= x >> 17;
			x ^= x << 5;
			RandState = x;
			return RandState & PROBES_COUNT_MASK;
		}

		public string GetString(string baseString, int start, int length)
		{
			var hash = FnvHashHelper.HashString(baseString);
			return GetString(hash, baseString, start, length);
		}

		public string GetString(FnvHashHelper.Hash hash, string baseString, int start, int length)
		{
			// Lookup with quadratic probing
			lock (SharedHashTable)
			{
				uint key = hash.Value;
				uint probe = 0;
				while (probe < PROBES_COUNT)
				{
					key = unchecked(key + probe) & SHARED_COUNT_MASK;
					var compare = SharedHashTable[key];
					if (compare == null)
					{
#if PROFILE_STRING_POOL
						MISS_COUNT++;
#endif
						break;
					}
					if (length == compare.Length)
					{
						int i;
						for (i = 0; i < length; ++i)
						{
							if (baseString[start + i] != compare[i])
#pragma warning disable S907 // "goto" statement should not be used[Reason: goto makes the code easier to read]
								goto PROBE_END_LABEL;
#pragma warning restore S907 // "goto" statement should not be used
						}
#if PROFILE_STRING_POOL
						HIT_COUNT++;
#endif
						return compare;
					}
				PROBE_END_LABEL:
					++probe;
				}

				// Insert at end if there is still space in the bucket.
				// NOTE: You could make the span completly random in range[0;probe]
				uint pos = probe == PROBES_COUNT ? RandProbe() : probe;
				key = unchecked(hash.Value + (pos * (pos + 1)) / 2) & SHARED_COUNT_MASK;
				var stringValue = start == 0 && length == baseString.Length ? baseString : baseString.Substring(start, length);
				return SharedHashTable[key] = stringValue;
			}
		}
	}
}
