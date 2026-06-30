using System;
using System.Collections;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace WorldMapPreloadOnSaveLoad
{
	public static class WorldMapPreloader
	{
		private readonly struct LayerWorkItem
		{
			public readonly WorldDrawLayerBase Layer;
			public readonly PlanetLayer PlanetLayer;
			public readonly string Label;

			public LayerWorkItem(WorldDrawLayerBase layer, PlanetLayer planetLayer, string label)
			{
				Layer = layer;
				PlanetLayer = planetLayer;
				Label = label;
			}
		}

		public static IEnumerable PreloadAsync()
		{
			var renderer = Find.World?.renderer;
			var worldGrid = Find.WorldGrid;
			if (renderer == null || worldGrid == null)
				yield break;

			var settings = WorldMapPreloadMod.Instance?.Settings;
			bool surfaceOnly = settings?.surfaceLayerOnly ?? false;
			bool warmPass = settings?.warmWorldUpdatePass ?? true;
			bool deferWarmPass = settings?.deferWarmWorldUpdatePass ?? false;

			List<LayerWorkItem> workItems = CollectDirtyLayers(worldGrid, surfaceOnly);
			int totalLayers = workItems.Count;
			int regeneratedSteps = 0;
			int completedLayers = 0;

			WorldMapPreloadProgress.Begin(totalLayers);
			ReportProgress(completedLayers, totalLayers, totalLayers > 0 ? workItems[0].Label : "Starting");

			WorldRenderMode previousMode = renderer.wantedMode;
			PlanetLayer previousLayer = PlanetLayer.Selected;

			renderer.wantedMode = WorldRenderMode.Planet;
			renderer.CheckActivateWorldCamera();

			try
			{
				foreach (LayerWorkItem workItem in workItems)
				{
					if (workItem.PlanetLayer != null)
						PlanetLayer.Selected = workItem.PlanetLayer;

					ReportProgress(completedLayers, totalLayers, workItem.Label);

					foreach (object step in RegenerateDirtyLayer(workItem.Layer))
					{
						regeneratedSteps++;
						yield return step;
					}

					completedLayers++;
					ReportProgress(completedLayers, totalLayers, workItem.Label);
				}

				if (warmPass && !deferWarmPass)
				{
					WorldMapPreloadProgress.IsWarmingPhase = true;
					ReportProgress(completedLayers, totalLayers, "Warming world caches");
					yield return null;

					RunWarmWorldUpdatePass(renderer, previousMode, previousLayer);
				}
			}
			finally
			{
				PlanetLayer.Selected = previousLayer;
				renderer.wantedMode = previousMode;
				renderer.CheckActivateWorldCamera();
				WorldMapPreloadProgress.End();
			}

			if (warmPass && deferWarmPass)
				LongEventHandler.ExecuteWhenFinished(() => RunWarmWorldUpdatePass(renderer, previousMode, previousLayer));

			Log.Message($"{WorldMapPreloadMod.LogPrefix} world map preload finished ({regeneratedSteps} layer regeneration steps, {completedLayers} layers).");
		}

		private static List<LayerWorkItem> CollectDirtyLayers(WorldGrid worldGrid, bool surfaceOnly)
		{
			var workItems = new List<LayerWorkItem>();

			foreach (WorldDrawLayerBase globalLayer in worldGrid.GlobalLayers)
			{
				if (globalLayer == null || !globalLayer.Dirty || !globalLayer.Visible)
					continue;

				workItems.Add(new LayerWorkItem(globalLayer, null, FormatLayerLabel(globalLayer, null)));
			}

			foreach (KeyValuePair<int, PlanetLayer> entry in worldGrid.PlanetLayers)
			{
				PlanetLayer planetLayer = entry.Value;
				if (surfaceOnly && !planetLayer.IsRootSurface)
					continue;

				foreach (WorldDrawLayer drawLayer in planetLayer.WorldDrawLayers)
				{
					if (drawLayer == null || !drawLayer.Dirty || !drawLayer.Visible)
						continue;

					workItems.Add(new LayerWorkItem(drawLayer, planetLayer, FormatLayerLabel(drawLayer, planetLayer)));
				}
			}

			return workItems;
		}

		private static string FormatLayerLabel(WorldDrawLayerBase layer, PlanetLayer planetLayer)
		{
			string typeName = layer.GetType().Name;
			if (typeName.StartsWith("WorldDrawLayer_", StringComparison.Ordinal))
				typeName = typeName.Substring("WorldDrawLayer_".Length);

			if (planetLayer == null)
				return typeName;

			string layerName = planetLayer.Def?.label ?? planetLayer.ToString();
			return $"{typeName} ({layerName})";
		}

		private static void ReportProgress(int completedLayers, int totalLayers, string layerLabel)
		{
			WorldMapPreloadProgress.CompletedLayers = completedLayers;
			WorldMapPreloadProgress.TotalLayers = totalLayers;
			WorldMapPreloadProgress.CurrentLayerLabel = layerLabel;

			// Keep LongEventHandler.eventText short so vanilla does not widen the status box
			// or draw the trailing animated ellipsis outside the window. Custom UI draws detail.
			LongEventHandler.SetCurrentEventText("Generating world map");
		}

		private static void RunWarmWorldUpdatePass(WorldRenderer renderer, WorldRenderMode restoreMode, PlanetLayer restoreLayer)
		{
			if (Find.World == null || renderer == null)
				return;

			WorldRenderMode previousMode = renderer.wantedMode;
			PlanetLayer previousLayer = PlanetLayer.Selected;

			try
			{
				renderer.wantedMode = WorldRenderMode.Planet;
				renderer.CheckActivateWorldCamera();
				Find.World.WorldUpdate();
			}
			catch (Exception ex)
			{
				Log.Error($"{WorldMapPreloadMod.LogPrefix} warm WorldUpdate failed: {ex}");
			}
			finally
			{
				PlanetLayer.Selected = restoreLayer ?? previousLayer;
				renderer.wantedMode = restoreMode;
				renderer.CheckActivateWorldCamera();
			}
		}

		private static IEnumerable RegenerateDirtyLayer(WorldDrawLayerBase layer)
		{
			if (layer == null || !layer.Dirty || !layer.Visible)
				yield break;

			IEnumerator enumerator = layer.Regenerate().GetEnumerator();
			try
			{
				while (true)
				{
					try
					{
						if (!enumerator.MoveNext())
							break;
					}
					catch (Exception ex)
					{
						Log.Error($"{WorldMapPreloadMod.LogPrefix} Could not regenerate WorldLayer {layer.GetType().Name}: {ex}");
						break;
					}

					yield return enumerator.Current;
				}
			}
			finally
			{
				if (enumerator is IDisposable disposable)
					disposable.Dispose();
			}

			yield return null;
		}
	}
}
