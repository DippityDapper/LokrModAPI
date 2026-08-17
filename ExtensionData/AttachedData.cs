using System;
using System.Runtime.CompilerServices;

namespace LokrModAPI.ExtensionData
{
	/// <summary>Generic ConditionalWeakTable-backed helper for attaching data to a type you don't own.</summary>
	/// <remarks>
	/// Generalizes the ConditionalWeakTable-based side-table pattern the old ExoSkeletonModData
	/// used to work around "Harmony can't add a field to a type it doesn't own"
	/// (docs/modapi-plan.md §4.5) into a reusable primitive any plugin can use, instead of every
	/// plugin re-deriving the same ConditionalWeakTable boilerplate.
	/// </remarks>
	public sealed class AttachedData<TKey, TValue> where TKey : class
	{
		private readonly ConditionalWeakTable<TKey, StrongBox<TValue>> table =
			new ConditionalWeakTable<TKey, StrongBox<TValue>>();

		/// <summary>Attempts to get the value attached to a key, without creating one.</summary>
		public bool TryGet(TKey key, out TValue value)
		{
			if (table.TryGetValue(key, out StrongBox<TValue> box))
			{
				value = box.Value;
				return true;
			}
			value = default;
			return false;
		}

		/// <summary>Attaches (or replaces) the value for a key.</summary>
		public void Set(TKey key, TValue value)
		{
			table.Remove(key);
			table.Add(key, new StrongBox<TValue>(value));
		}

		/// <summary>Gets the value attached to a key, creating it via the factory on first access.</summary>
		public TValue GetOrAdd(TKey key, Func<TValue> factory)
		{
			StrongBox<TValue> box = table.GetValue(key, _ => new StrongBox<TValue>(factory()));
			return box.Value;
		}
	}
}
