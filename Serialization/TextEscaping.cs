using System.Text;

namespace LokrModAPI.Serialization
{
	/// <summary>Escaping helpers for hand-built JSON and KV1 text, shared by every plugin that writes either format by string concatenation instead of a real serializer.</summary>
	/// <remarks>
	/// Fixes pre-redesign audit C-01 / M-09: a user-typed string containing a quote, backslash, or
	/// control character, written unescaped into a hand-built JSON or KV1 value, can corrupt the
	/// whole surrounding file -- for KV1 specifically, a corrupted shared TextAsset (rlheroes.txt,
	/// abilities, roster) can break every other hero/enemy/ability spliced into that same file, not
	/// just the one with the bad string. Two methods, not one, because the two formats' real readers
	/// have different (and non-obvious) escaping contracts -- see each method's own remarks for why
	/// they can't share an implementation.
	/// </remarks>
	public static class TextEscaping
	{
		/// <summary>Escapes a string for embedding inside a hand-built JSON string literal (`"..."`), read back by a real JSON parser.</summary>
		/// <remarks>
		/// Standard JSON string escaping (RFC 8259 §7): backslash and the delimiting quote, plus the
		/// required control-character escapes (`\n`/`\r`/`\t`/`\b`/`\f`) and a `\u00XX` escape for any
		/// other control character (0x00-0x1F). Safe and fully lossless here specifically because
		/// every reader of this project's hand-built JSON (rig.json, rig.pivots.json,
		/// rig.animsource.json) is SimpleJSON, a real parser that already implements standard JSON
		/// unescaping -- unlike KvEscape below, there is no format-specific quirk to work around.
		/// </remarks>
		public static string JsonEscape(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return value ?? string.Empty;
			}

			StringBuilder builder = new StringBuilder(value.Length + 8);
			foreach (char c in value)
			{
				switch (c)
				{
					case '\\':
						builder.Append("\\\\");
						break;
					case '"':
						builder.Append("\\\"");
						break;
					case '\n':
						builder.Append("\\n");
						break;
					case '\r':
						builder.Append("\\r");
						break;
					case '\t':
						builder.Append("\\t");
						break;
					case '\b':
						builder.Append("\\b");
						break;
					case '\f':
						builder.Append("\\f");
						break;
					default:
						if (c < 0x20)
						{
							builder.Append("\\u").Append(((int)c).ToString("x4"));
						}
						else
						{
							builder.Append(c);
						}
						break;
				}
			}
			return builder.ToString();
		}

		/// <summary>Escapes a string for embedding inside a hand-built KV1 quoted value (`"..."`), read back by the base game's own KVLib.KeyValues.PenguinParser.</summary>
		/// <remarks>
		/// Verified against PenguinParser's real decompiled source (../lokr-modding/ih-original/Ironhide.Legends/KVLib/KeyValues/PenguinParser.cs)
		/// rather than assumed: its quoted-value scan treats a backslash as "skip the next character
		/// when looking for the closing quote" (`if (contents[num] == '\\') num++; num++;`), so
		/// escaping an embedded `"` as `\"` reliably prevents it from prematurely terminating the
		/// value or corrupting the surrounding block structure -- the actual, concrete harm C-01
		/// describes. A raw backslash needs the same treatment (escaped to `\\`) so it can never
		/// accidentally "protect" a real quote that happens to follow it in the original string.
		///
		/// This is deliberately NOT a fully lossless escape, and can't be made one without changing
		/// code this project doesn't own: PenguinParser's own scan does not strip the backslash back
		/// out when it extracts the value (`keyValue2.Set(contents.Substring(i + 1, num - (i + 1)))`
		/// keeps it verbatim), and the base game's own writer (KeyValue.ToString()) performs zero
		/// escaping at all -- values are assumed by the base game itself to never contain a quote.
		/// Escaping here trades a perfectly clean round-trip (impossible against this real parser
		/// without a game-side fix) for the much more important property: a user string can no longer
		/// corrupt the file it's written into. An embedded quote survives as the literal `\"` on
		/// reload rather than a bare `"` -- a cosmetic gap, not a data-loss or corruption one.
		///
		/// Newlines are deliberately left unescaped: PenguinParser's quoted-value scan has no special
		/// handling for `\n` at all (only whitespace *between* tokens, outside any quoted span, is
		/// skipped) -- a raw multi-line value parses back exactly as written, so escaping it here
		/// would only make legitimately multi-line text (e.g. a description) render as literal `\n`
		/// text on reload instead of round-tripping cleanly, a regression with no corresponding safety
		/// benefit against this specific parser.
		/// </remarks>
		public static string KvEscape(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return value ?? string.Empty;
			}

			return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
		}
	}
}
