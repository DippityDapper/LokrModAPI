using UnityEngine;

namespace LokrModAPI.Input
{
	/// <summary>Keyboard chord checked via UnityEngine.Input (KeyDown + modifier hold state).</summary>
	public readonly struct KeyBinding
	{
		public KeyBinding(KeyCode key, bool control = false, bool shift = false, bool alt = false)
		{
			Key = key;
			Control = control;
			Shift = shift;
			Alt = alt;
		}

		public KeyCode Key { get; }
		public bool Control { get; }
		public bool Shift { get; }
		public bool Alt { get; }

		/// <summary>True on the frame this chord was pressed.</summary>
		public bool IsDown()
		{
			if (!UnityEngine.Input.GetKeyDown(Key))
			{
				return false;
			}

			bool controlHeld = UnityEngine.Input.GetKey(KeyCode.LeftControl)
				|| UnityEngine.Input.GetKey(KeyCode.RightControl);
			bool shiftHeld = UnityEngine.Input.GetKey(KeyCode.LeftShift)
				|| UnityEngine.Input.GetKey(KeyCode.RightShift);
			bool altHeld = UnityEngine.Input.GetKey(KeyCode.LeftAlt)
				|| UnityEngine.Input.GetKey(KeyCode.RightAlt);

			return controlHeld == Control && shiftHeld == Shift && altHeld == Alt;
		}

		public bool Equals(KeyBinding other)
		{
			return Key == other.Key
				&& Control == other.Control
				&& Shift == other.Shift
				&& Alt == other.Alt;
		}

		public override bool Equals(object obj)
		{
			return obj is KeyBinding other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = (int)Key;
				hash = (hash * 397) ^ Control.GetHashCode();
				hash = (hash * 397) ^ Shift.GetHashCode();
				hash = (hash * 397) ^ Alt.GetHashCode();
				return hash;
			}
		}
		public override string ToString()
		{
			string prefix = string.Empty;
			if (Control)
			{
				prefix += "Ctrl+";
			}
			if (Shift)
			{
				prefix += "Shift+";
			}
			if (Alt)
			{
				prefix += "Alt+";
			}
			return prefix + Key;
		}
	}
}
