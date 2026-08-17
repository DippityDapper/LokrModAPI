using System;
using System.Collections.Generic;
using UnityEngine;

namespace LokrModAPI.Audio
{
	/// <summary>Static WAV codec, ported verbatim from the base game's own decompiled source.</summary>
	/// <remarks>Ported verbatim from ih-modded/Ironhide.Legends/OpenWavParser.cs. Stateless utility class, no changes needed for the BepInEx port.</remarks>
	public static class OpenWavParser
	{
		/// <summary>Checks whether a byte array starts with a valid RIFF/WAVE header.</summary>
		public static bool IsWAVFile(byte[] wavFile)
		{
			if (wavFile.Length > 12)
			{
				byte[] array = new byte[4];
				Buffer.BlockCopy(wavFile, 0, array, 0, array.Length);
				string a = OpenWavParser.ByteArrayToString(array);
				Buffer.BlockCopy(wavFile, 8, array, 0, array.Length);
				string a2 = OpenWavParser.ByteArrayToString(array);
				return a == "RIFF" && a2 == "WAVE";
			}
			return false;
		}

		/// <summary>Decodes a raw WAV file's bytes (16/24/32-bit PCM) into an AudioClip.</summary>
		public static AudioClip ByteArrayToAudioClip(byte[] wavFile, string name = "", bool stream = false)
		{
			if (!OpenWavParser.IsWAVFile(wavFile))
			{
				Debug.LogError("[OpenWavParser.ByteArrayToAudioClip] Format not supported.");
				return null;
			}
			int num = BitConverter.ToInt32(wavFile, 16);
			int num2 = (int)BitConverter.ToInt16(wavFile, 20);
			int num3 = (int)BitConverter.ToInt16(wavFile, 22);
			int frequency = BitConverter.ToInt32(wavFile, 24);
			OpenWavParser.Resolution resolution = (OpenWavParser.Resolution)BitConverter.ToInt16(wavFile, 34);
			if (num2 == 1)
			{
				int num4 = 20 + num;
				for (int i = num4; i < wavFile.Length; i++)
				{
					if (wavFile[i] == 100 && wavFile[i + 1] == 97 && wavFile[i + 2] == 116 && wavFile[i + 3] == 97)
					{
						num4 = i + 4;
						break;
					}
				}
				int num5 = BitConverter.ToInt32(wavFile, num4);
				num4 += 4;
				int num6 = (int)resolution / 8;
				int num7 = num5 / num6;
				float[] array = new float[num7];
				for (int j = 0; j < num7; j++)
				{
					int num8 = num4 + j * num6;
					float num9 = 0f;
					if (resolution != OpenWavParser.Resolution._16bit)
					{
						if (resolution != OpenWavParser.Resolution._24bit)
						{
							if (resolution == OpenWavParser.Resolution._32bit)
							{
								num9 = (float)BitConverter.ToInt32(wavFile, num8) / 2.1474836E+09f;
							}
						}
						else
						{
							num9 = (float)(BitConverter.ToInt32(new byte[]
							{
								0,
								wavFile[num8],
								wavFile[num8 + 1],
								wavFile[num8 + 2]
							}, 0) >> 8) / 8388607f;
						}
					}
					else
					{
						num9 = (float)BitConverter.ToInt16(wavFile, num8) / 32767f;
					}
					array[j] = num9;
				}
				AudioClip audioClip = AudioClip.Create(name, num7 / num3, num3, frequency, stream);
				audioClip.SetData(array, 0);
				return audioClip;
			}
			Debug.LogError("[OpenWavParser.ByteArrayToAudioClip] Compressed wav format not supported.");
			return null;
		}

		/// <summary>Encodes an AudioClip's samples into a raw WAV file's bytes at the given bit resolution.</summary>
		public static byte[] AudioClipToByteArray(AudioClip clip, OpenWavParser.Resolution res = OpenWavParser.Resolution._16bit)
		{
			float[] array = new float[clip.samples * clip.channels];
			clip.GetData(array, 0);
			List<byte> list = new List<byte>();
			int num = (int)res / 8;
			list.AddRange(new byte[] { 82, 73, 70, 70 });
			list.AddRange(BitConverter.GetBytes(array.Length * num + 44 - 8));
			list.AddRange(new byte[] { 87, 65, 86, 69 });
			list.AddRange(new byte[] { 102, 109, 116, 32 });
			list.AddRange(BitConverter.GetBytes(16));
			list.AddRange(BitConverter.GetBytes(1));
			list.AddRange(BitConverter.GetBytes((ushort)clip.channels));
			list.AddRange(BitConverter.GetBytes(clip.frequency));
			list.AddRange(BitConverter.GetBytes(clip.frequency * clip.channels * num));
			list.AddRange(BitConverter.GetBytes((ushort)(clip.channels * num)));
			list.AddRange(BitConverter.GetBytes((ushort)res));
			list.AddRange(new byte[] { 100, 97, 116, 97 });
			list.AddRange(BitConverter.GetBytes(array.Length * num));
			for (int i = 0; i < array.Length; i++)
			{
				if (res != OpenWavParser.Resolution._16bit)
				{
					if (res != OpenWavParser.Resolution._24bit)
					{
						if (res == OpenWavParser.Resolution._32bit)
						{
							int value = (int)(array[i] * 2.1474836E+09f);
							list.AddRange(BitConverter.GetBytes(value));
						}
					}
					else
					{
						byte[] bytes = BitConverter.GetBytes(Mathf.FloorToInt(array[i] * 8388607f));
						list.AddRange(new byte[] { bytes[0], bytes[1], bytes[2] });
					}
				}
				else
				{
					short value2 = (short)(array[i] * 32767f);
					list.AddRange(BitConverter.GetBytes(value2));
				}
			}
			return list.ToArray();
		}

		private static string ByteArrayToString(byte[] content)
		{
			char[] array = new char[content.Length];
			content.CopyTo(array, 0);
			return new string(array);
		}

		/// <summary>Concatenates multiple clips' sample data into a single stereo AudioClip.</summary>
		public static AudioClip Combine(AudioClip[] clips)
		{
			if (clips == null || clips.Length == 0)
			{
				return null;
			}
			int num = 0;
			for (int i = 0; i < clips.Length; i++)
			{
				if (clips[i] != null)
				{
					num += clips[i].samples * clips[i].channels;
				}
			}
			float[] array = new float[num];
			num = 0;
			for (int j = 0; j < clips.Length; j++)
			{
				if (clips[j] != null)
				{
					float[] array2 = new float[clips[j].samples * clips[j].channels];
					clips[j].GetData(array2, 0);
					array2.CopyTo(array, num);
					num += array2.Length;
				}
			}
			AudioClip audioClip = null;
			if (num > 0)
			{
				audioClip = AudioClip.Create("AudioClip", num / 2, 2, 44100, false);
				audioClip.SetData(array, 0);
			}
			return audioClip;
		}

		/// <summary>Downmixes a stereo clip to mono by averaging the two channels.</summary>
		public static AudioClip StereoToMono(AudioClip stereoClip, bool stream = false)
		{
			float[] array = new float[stereoClip.samples * stereoClip.channels];
			stereoClip.GetData(array, 0);
			float[] array2 = new float[stereoClip.samples];
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = (float)((double)array[i * 2] + (double)array[i * 2 + 1]) / 2f;
			}
			AudioClip audioClip = AudioClip.Create(stereoClip.name, array2.Length, 1, stereoClip.frequency, stream);
			audioClip.SetData(array2, 0);
			return audioClip;
		}

		/// <summary>Upmixes a mono clip to stereo by duplicating the single channel.</summary>
		public static AudioClip MonoToStereo(AudioClip monoClip, bool stream = false)
		{
			float[] array = new float[monoClip.samples];
			monoClip.GetData(array, 0);
			float[] array2 = new float[monoClip.samples * 2];
			for (int i = 0; i < array2.Length; i += 2)
			{
				array2[i] = array[i / 2];
				array2[i + 1] = array[i / 2];
			}
			AudioClip audioClip = AudioClip.Create(monoClip.name, monoClip.samples, 2, monoClip.frequency, stream);
			audioClip.SetData(array2, 0);
			return audioClip;
		}

		/// <summary>The PCM bit depth used when encoding or decoding a WAV file's raw sample data.</summary>
		public enum Resolution
		{
			_16bit = 16,
			_24bit = 24,
			_32bit = 32
		}
	}
}
