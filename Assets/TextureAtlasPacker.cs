using System.Collections.Generic;
using UnityEngine;

namespace LokrModAPI.Assets
{
	/// <summary>Simple shelf-packing atlas builder used by ModAssetLoader.PackSprites.</summary>
	/// <remarks>
	/// ExoSkeletonRenderer only reads partSprites[0].texture as the material for the whole mesh
	/// (see docs/reference notes) -- a rig with more than one part-texture needs everything packed
	/// into one shared atlas, with each part's Sprite UV rect pointing at its own sub-region. This
	/// is a plain shelf packer: sort tallest-first, lay left-to-right, wrap to a new row when a
	/// texture wouldn't fit. Good enough for mod-sized part counts; not trying to be space-optimal.
	/// </remarks>
	public sealed class TextureAtlasPacker
	{
		private sealed class Entry
		{
			/// <summary>The dictionary key this entry was packed from, used to look up its resulting pixel rect.</summary>
			public string Name;
			/// <summary>The source texture being packed into the shared atlas.</summary>
			public Texture2D Texture;
		}

		/// <summary>Packs the given textures into one shared atlas, returning each one's pixel rect within it.</summary>
		public Texture2D Pack(IEnumerable<KeyValuePair<string, Texture2D>> sources, out Dictionary<string, Rect> pixelRects, int padding = 2)
		{
			List<Entry> entries = new List<Entry>();
			foreach (KeyValuePair<string, Texture2D> source in sources)
			{
				entries.Add(new Entry { Name = source.Key, Texture = source.Value });
			}
			entries.Sort((a, b) => b.Texture.height.CompareTo(a.Texture.height));

			int totalArea = 0;
			foreach (Entry entry in entries)
			{
				totalArea += (entry.Texture.width + padding) * (entry.Texture.height + padding);
			}
			int atlasWidth = Mathf.Max(64, Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(totalArea))));

			List<Entry> orderedByPlacement = entries;
			List<Vector2Int> positions = new List<Vector2Int>(entries.Count);
			int x = 0;
			int y = 0;
			int shelfHeight = 0;
			int atlasHeight = 0;
			foreach (Entry entry in orderedByPlacement)
			{
				int width = entry.Texture.width;
				int height = entry.Texture.height;
				if (x + width > atlasWidth)
				{
					x = 0;
					y += shelfHeight + padding;
					shelfHeight = 0;
				}
				positions.Add(new Vector2Int(x, y));
				x += width + padding;
				shelfHeight = Mathf.Max(shelfHeight, height);
				atlasHeight = Mathf.Max(atlasHeight, y + height);
			}
			atlasHeight = Mathf.Max(64, Mathf.NextPowerOfTwo(atlasHeight));

			Texture2D atlas = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, false);
			Color32[] clear = new Color32[atlasWidth * atlasHeight];
			atlas.SetPixels32(clear);

			pixelRects = new Dictionary<string, Rect>();
			for (int i = 0; i < orderedByPlacement.Count; i++)
			{
				Entry entry = orderedByPlacement[i];
				Vector2Int position = positions[i];
				atlas.SetPixels(position.x, position.y, entry.Texture.width, entry.Texture.height, entry.Texture.GetPixels());
				pixelRects[entry.Name] = new Rect(position.x, position.y, entry.Texture.width, entry.Texture.height);
			}
			atlas.Apply();
			return atlas;
		}
	}
}
