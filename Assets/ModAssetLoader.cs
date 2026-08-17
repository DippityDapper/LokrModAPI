using System.Collections.Generic;
using System.IO;
using LokrModAPI.Audio;
using UnityEngine;

namespace LokrModAPI.Assets
{
	/// <summary>Loads textures, sprites, and audio clips from mod folders.</summary>
	/// <remarks>
	/// Collapses the byte[] -> Texture2D -> LoadImage -> Sprite.Create block that was
	/// copy-pasted (with only the anchor/pivot math around it differing) in ~8 places across
	/// the old patches, plus the WAV-loading equivalent (docs/modapi-plan.md §4.2).
	/// </remarks>
	public sealed class ModAssetLoader
	{
		private readonly TextureAtlasPacker atlasPacker = new TextureAtlasPacker();

		/// <summary>Packs multiple part textures into one shared atlas, returning a ready-to-use Sprite per part.</summary>
		/// <remarks>
		/// Required for any multi-part ExoSkeleton rig, since ExoSkeletonRenderer only reads
		/// partSprites[0].texture for the whole mesh.
		///
		/// Uses SpriteMeshType.FullRect deliberately: ExoSkeletonRenderer hardcodes a
		/// 4-vertex/6-triangle-index quad per part (see ExoSkeletonRenderer.LateUpdate:
		/// `for (int j = 0; j &lt; 4; j++) part.vertices[j]`) and never reads the actual array
		/// lengths. The default (Tight) mesh type traces the alpha-channel silhouette instead of
		/// the plain rect, producing a variable vertex/triangle count for anything with
		/// transparent margin -- i.e. basically all real character art -- which crashes the
		/// renderer with an IndexOutOfRangeException that has no useful stack trace into game code.
		/// </remarks>
		public Dictionary<string, Sprite> PackSprites(IEnumerable<KeyValuePair<string, Texture2D>> sourceTextures, float pixelsPerUnit = 100f)
		{
			Texture2D atlas = atlasPacker.Pack(sourceTextures, out Dictionary<string, Rect> pixelRects);
			Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
			foreach (KeyValuePair<string, Rect> entry in pixelRects)
			{
				Sprite sprite = Sprite.Create(atlas, entry.Value, new Vector2(0.5f, 0.5f), pixelsPerUnit,
					0, SpriteMeshType.FullRect);
				sprite.name = entry.Key;
				sprites[entry.Key] = sprite;
			}
			return sprites;
		}

		/// <summary>Loads a PNG/JPG file from disk into a Texture2D.</summary>
		public Texture2D LoadTexture(string path, TextureFormat format = TextureFormat.ARGB32)
		{
			byte[] data = File.ReadAllBytes(path);
			Texture2D texture2D = new Texture2D(2, 2, format, false, true);
			texture2D.LoadImage(data);
			return texture2D;
		}

		/// <summary>Loads an image file from disk into a single-sprite Sprite (full texture, no atlas).</summary>
		public Sprite LoadSprite(string path, TextureFormat format = TextureFormat.ARGB32)
		{
			Texture2D texture2D = LoadTexture(path, format);
			return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f),
				100f, 0, SpriteMeshType.FullRect);
		}

		/// <summary>Loads a WAV file from disk into an AudioClip.</summary>
		public AudioClip LoadAudioClip(string path)
		{
			byte[] data = File.ReadAllBytes(path);
			return OpenWavParser.ByteArrayToAudioClip(data, "", false);
		}
	}
}
