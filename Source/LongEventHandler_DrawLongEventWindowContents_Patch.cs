using HarmonyLib;
using UnityEngine;
using Verse;

namespace WorldMapPreloadOnSaveLoad
{
	[HarmonyPatch(typeof(LongEventHandler), "DrawLongEventWindowContents")]
	internal static class LongEventHandler_DrawLongEventWindowContents_Patch
	{
		private const float BarHeight = 22f;
		private const float BarBottomPadding = 6f;
		private const float BarHorizontalPadding = 4f;
		private const float TitleHeight = 20f;
		private const float DetailHeight = 20f;
		private const int MaxDetailChars = 52;

		[HarmonyPrefix]
		private static bool Prefix(Rect rect)
		{
			if (!WorldMapPreloadProgress.IsActive)
				return true;

			DrawPreloadContents(rect);
			return false;
		}

		private static void DrawPreloadContents(Rect rect)
		{
			if (Event.current.type == EventType.Repaint)
				MarkCurrentEventDisplayed();

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperCenter;

			float y = rect.y + 4f;
			Widgets.Label(new Rect(rect.x, y, rect.width, TitleHeight), "Generating world map");
			y += TitleHeight + 2f;

			Text.Anchor = TextAnchor.UpperCenter;
			Widgets.Label(new Rect(rect.x + 8f, y, rect.width - 16f, DetailHeight), BuildDetailLine());

			if (WorldMapPreloadMod.Instance?.Settings.showProgressBar == true)
			{
				Rect barRect = new Rect(
					rect.x + BarHorizontalPadding,
					rect.yMax - BarHeight - BarBottomPadding,
					rect.width - BarHorizontalPadding * 2f,
					BarHeight);

				DrawFillableBarWithCenteredLabel(barRect, WorldMapPreloadProgress.FillPercent, WorldMapPreloadProgress.BarLabel);
			}

			Text.Anchor = TextAnchor.UpperLeft;
		}

		private static void DrawFillableBarWithCenteredLabel(Rect barRect, float fillPercent, string label)
		{
			Widgets.FillableBar(barRect, fillPercent);

			GameFont previousFont = Text.Font;
			TextAnchor previousAnchor = Text.Anchor;
			Color previousColor = GUI.color;

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = Color.white;

			// Slight shadow so the label stays readable over the fill color.
			Rect shadowRect = barRect;
			shadowRect.x += 1f;
			shadowRect.y += 1f;
			GUI.color = new Color(0f, 0f, 0f, 0.75f);
			Widgets.Label(shadowRect, label);

			GUI.color = Color.white;
			Widgets.Label(barRect, label);

			Text.Font = previousFont;
			Text.Anchor = previousAnchor;
			GUI.color = previousColor;
		}

		private static string BuildDetailLine()
		{
			if (WorldMapPreloadProgress.IsWarmingPhase)
				return "Warming world caches";

			if (WorldMapPreloadProgress.TotalLayers <= 0)
				return "Preparing layers";

			string layer = WorldMapPreloadProgress.CurrentLayerLabel ?? string.Empty;
			if (layer.Length > MaxDetailChars)
				layer = layer.Substring(0, MaxDetailChars - 3) + "...";

			int index = WorldMapPreloadProgress.CompletedLayers + 1;
			if (index < 1)
				index = 1;
			else if (index > WorldMapPreloadProgress.TotalLayers)
				index = WorldMapPreloadProgress.TotalLayers;

			return $"{index}/{WorldMapPreloadProgress.TotalLayers}: {layer}";
		}

		private static void MarkCurrentEventDisplayed()
		{
			object currentEvent = Traverse.Create(typeof(LongEventHandler)).Field("currentEvent").GetValue();
			if (currentEvent == null)
				return;

			Traverse.Create(currentEvent).Field("alreadyDisplayed").SetValue(true);
		}
	}
}
