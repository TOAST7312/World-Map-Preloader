namespace WorldMapPreloadOnSaveLoad
{
	public static class WorldMapPreloadProgress
	{
		public static bool IsActive { get; private set; }

		public static int CompletedLayers { get; set; }

		public static int TotalLayers { get; set; }

		public static string CurrentLayerLabel { get; set; } = string.Empty;

		public static bool IsWarmingPhase { get; set; }

		public static float FillPercent
		{
			get
			{
				if (IsWarmingPhase)
					return 1f;

				if (TotalLayers <= 0)
					return 0f;

				return CompletedLayers / (float)TotalLayers;
			}
		}

		public static string BarLabel
		{
			get
			{
				if (IsWarmingPhase)
					return "Warm pass";

				if (TotalLayers <= 0)
					return "0%";

				int percent = (int)(FillPercent * 100f);
				return $"{CompletedLayers}/{TotalLayers} ({percent}%)";
			}
		}

		public static void Begin(int totalLayers)
		{
			IsActive = true;
			CompletedLayers = 0;
			TotalLayers = totalLayers;
			CurrentLayerLabel = string.Empty;
			IsWarmingPhase = false;
		}

		public static void End()
		{
			IsActive = false;
			CompletedLayers = 0;
			TotalLayers = 0;
			CurrentLayerLabel = string.Empty;
			IsWarmingPhase = false;
		}
	}
}
