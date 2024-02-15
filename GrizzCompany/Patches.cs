using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GrizzCompany
{
	public class Patches
	{
		public static void Load()
		{
			On.StartOfRound.Awake += StartOfRound_Awake;
		}

		private static void StartOfRound_Awake(On.StartOfRound.orig_Awake orig, StartOfRound self)
		{
			orig(self);

			if (self.testRoomPrefab.GetComponent<PrefabChildInjector>())
			{
				return;
			}

			var injector = self.testRoomPrefab.AddComponent<PrefabChildInjector>();

			injector.childPrefabs.Add(Assets.Griller.testRoomAINodes);
		}
	}
}
