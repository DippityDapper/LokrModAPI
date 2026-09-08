using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LokrModAPI.Diagnostics
{
	/// <summary>Dev-only diagnostic that writes a scene's full GameObject/Component tree to a text file.</summary>
	/// <remarks>
	/// Reference tool, not a runtime dependency for any patch. Ctrl+Shift+F9 always writes a
	/// timestamped file so two atlas states can be compared. Auto-dump after each scene load is
	/// still gated behind ModAPI.Config.DumpSceneHierarchies.
	/// </remarks>
	public static class SceneHierarchyDumper
	{
		/// <summary>Writes the scene tree to <c>&lt;scene&gt;-&lt;yyyyMMdd-HHmmss&gt;.txt</c> under outputDirectory.</summary>
		public static void Dump(Scene scene, string outputDirectory)
		{
			StringBuilder text = new StringBuilder();
			text.Append("Scene: ").Append(scene.name).Append(" (path: ").Append(scene.path).AppendLine(")");
			text.Append("Dumped at: ").AppendLine(DateTime.Now.ToString("u"));
			text.AppendLine();

			foreach (GameObject root in scene.GetRootGameObjects())
			{
				AppendGameObject(text, root, 0);
			}

			try
			{
				Directory.CreateDirectory(outputDirectory);
				string safeName = string.IsNullOrEmpty(scene.name) ? "unnamed" : scene.name;
				string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
				string path = Path.Combine(outputDirectory, safeName + "-" + stamp + ".txt");
				File.WriteAllText(path, text.ToString());
				LokrModAPIPlugin.Log.LogInfo("SceneHierarchyDumper: wrote " + path);
			}
			catch (Exception ex)
			{
				LokrModAPIPlugin.Log.LogError("SceneHierarchyDumper: failed writing to " + outputDirectory + " — " + ex.Message);
			}
		}

		/// <summary>Recursively appends a GameObject, its components, and its children to the dump text.</summary>
		private static void AppendGameObject(StringBuilder text, GameObject gameObject, int depth)
		{
			string indent = new string(' ', depth * 2);
			text.Append(indent).Append("- ").Append(gameObject.name)
				.Append(" [active=").Append(gameObject.activeSelf)
				.Append(", layer=").Append(LayerMask.LayerToName(gameObject.layer))
				.Append(", tag=").Append(gameObject.tag)
				.AppendLine("]");

			foreach (Component component in gameObject.GetComponents<Component>())
			{
				if (component == null)
				{
					text.Append(indent).AppendLine("    * <missing script>");
					continue;
				}
				text.Append(indent).Append("    * ").Append(component.GetType().FullName);
				AppendComponentDetails(text, component);
				text.AppendLine();
			}

			foreach (Transform child in gameObject.transform)
			{
				AppendGameObject(text, child.gameObject, depth + 1);
			}
		}

		/// <summary>Appends type-specific detail (rect/text/camera/canvas info) for a handful of common component types.</summary>
		private static void AppendComponentDetails(StringBuilder text, Component component)
		{
			RectTransform rectTransform = component as RectTransform;
			if (rectTransform != null)
			{
				text.Append(" anchoredPos=").Append(rectTransform.anchoredPosition)
					.Append(" sizeDelta=").Append(rectTransform.sizeDelta)
					.Append(" anchorMin=").Append(rectTransform.anchorMin)
					.Append(" anchorMax=").Append(rectTransform.anchorMax);
				return;
			}
			Text uiText = component as Text;
			if (uiText != null)
			{
				text.Append(" text=\"").Append(uiText.text).Append("\"");
				return;
			}
			Camera camera = component as Camera;
			if (camera != null)
			{
				text.Append(" depth=").Append(camera.depth).Append(" enabled=").Append(camera.enabled);
				return;
			}
			Canvas canvas = component as Canvas;
			if (canvas != null)
			{
				text.Append(" renderMode=").Append(canvas.renderMode).Append(" sortingOrder=").Append(canvas.sortingOrder);
			}
		}
	}
}
