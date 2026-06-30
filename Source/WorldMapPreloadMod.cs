using HarmonyLib;
using UnityEngine;
using Verse;

namespace WorldMapPreloadOnSaveLoad
{
	public sealed class WorldMapPreloadSettings : ModSettings
	{
		public bool preloadOnSaveLoad = true;
		public bool surfaceLayerOnly;
		public bool warmWorldUpdatePass = true;
		public bool deferWarmWorldUpdatePass;
		public bool showProgressBar = true;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref preloadOnSaveLoad, "preloadOnSaveLoad", true);
			Scribe_Values.Look(ref surfaceLayerOnly, "surfaceLayerOnly", false);
			Scribe_Values.Look(ref warmWorldUpdatePass, "warmWorldUpdatePass", true);
			Scribe_Values.Look(ref deferWarmWorldUpdatePass, "deferWarmWorldUpdatePass", false);
			Scribe_Values.Look(ref showProgressBar, "showProgressBar", true);
		}
	}

	public sealed class WorldMapPreloadMod : Mod
	{
		public const string LogPrefix = "[WorldMapPreloadOnSaveLoad]";

		public static WorldMapPreloadMod Instance { get; private set; }

		public WorldMapPreloadSettings Settings { get; private set; }

		public WorldMapPreloadMod(ModContentPack content) : base(content)
		{
			Instance = this;
			Settings = GetSettings<WorldMapPreloadSettings>();
			new Harmony("TehDav.WorldMapPreloadOnSaveLoad").PatchAll(typeof(WorldMapPreloadMod).Assembly);
			Log.Message($"{LogPrefix} initialized (preloadOnSaveLoad={Settings.preloadOnSaveLoad}).");
		}

		public override string SettingsCategory() => "World map preload on save load";

		public override void DoSettingsWindowContents(Rect inRect)
		{
			var listing = new Listing_Standard();
			listing.Begin(inRect);
			listing.Label("Regenerate world map meshes during save loading instead of on the first world-tab click.");
			listing.GapLine();
			listing.CheckboxLabeled(
				"Preload world map after save load",
				ref Settings.preloadOnSaveLoad,
				"When on, a GeneratingPlanet-style step runs at the end of loading. Total time is similar; the wait moves into the loading screen.");
			listing.CheckboxLabeled(
				"Surface planet layer only",
				ref Settings.surfaceLayerOnly,
				"When on, skip Odyssey orbit/space layers. Use if you never visit non-surface layers.");
			listing.CheckboxLabeled(
				"Warm world update pass",
				ref Settings.warmWorldUpdatePass,
				"Runs one WorldUpdate in planet mode after mesh regen to initialize expandable world-object caches.");
			if (Settings.warmWorldUpdatePass)
			{
				listing.Gap(4f);
				listing.CheckboxLabeled(
					"    Defer warm pass until after first playable tick",
					ref Settings.deferWarmWorldUpdatePass,
					"When on, mesh regen still runs during load but WorldUpdate runs after the colony is playable. Recommended on heavy mod lists.");
			}
			listing.GapLine();
			listing.CheckboxLabeled(
				"Show progress bar during preload",
				ref Settings.showProgressBar,
				"Draws a fill bar under the Generating planet message. Detail text always shows the active draw layer.");
			listing.End();
			Settings.Write();
		}
	}
}
