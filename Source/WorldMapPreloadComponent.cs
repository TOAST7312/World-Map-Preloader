using Verse;

namespace WorldMapPreloadOnSaveLoad
{
	public sealed class WorldMapPreloadComponent : GameComponent
	{
		public WorldMapPreloadComponent(Game game)
		{
		}

		public override void LoadedGame()
		{
			if (WorldMapPreloadMod.Instance != null && !WorldMapPreloadMod.Instance.Settings.preloadOnSaveLoad)
				return;

			if (Find.World?.renderer == null || Find.WorldGrid == null)
				return;

			LongEventHandler.QueueLongEvent(
				WorldMapPreloader.PreloadAsync(),
				"GeneratingPlanet",
				exceptionHandler: ex =>
				{
					WorldMapPreloadProgress.End();
					if (ex != null)
						Log.Error($"{WorldMapPreloadMod.LogPrefix} preload failed: {ex}");
				},
				showExtraUIInfo: true);
		}
	}
}
