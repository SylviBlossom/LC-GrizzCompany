using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using static LethalLib.Modules.Enemies;
using static LethalLib.Modules.Levels;
using Object = UnityEngine.Object;

namespace GrizzCompany
{
	public static class Assets
	{
		public static void Load()
		{
			var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			Griller.Load(path);
		}

		public static class Griller
		{
			public static AssetBundle assetBundle { get; internal set; }

			public static EnemyType enemyType { get; internal set; }

			public static Object testRoomAINodes { get; internal set; }

			public static void Load(string path)
			{
				assetBundle = AssetBundle.LoadFromFile(Path.Combine(path, "grizzcompany-griller"));

				enemyType = assetBundle.LoadAsset<EnemyType>("Griller");
				testRoomAINodes = assetBundle.LoadAsset("TestRoomGrillerAINodes");
			}

			public static void Register()
			{
				NetworkPrefabs.RegisterNetworkPrefab(enemyType.enemyPrefab);
				RegisterEnemy(enemyType, 100, LevelTypes.None, SpawnType.Default, null, null);
			}
		}
	}
}
