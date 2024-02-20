using LethalLib.Modules;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using EnemiesLib = LethalLib.Modules.Enemies;

namespace GrizzCompany.Enemies
{
	public static class Assets
	{
		public static AssetBundle bundle { get; internal set; }

		public static void Load()
		{
			var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			bundle = AssetBundle.LoadFromFile(Path.Combine(path, "grizzcompany-enemies.bundle"));

			Griller.Load();
		}

		public static class Griller
		{
			public static EnemyType enemyType { get; internal set; }
			public static Object testRoomAINodes { get; internal set; }

			public static void Load()
			{
				enemyType = bundle.LoadAsset<EnemyType>("Griller");
				testRoomAINodes = bundle.LoadAsset("TestRoomGrillerAINodes");

				NetworkPrefabs.RegisterNetworkPrefab(enemyType.enemyPrefab);
				EnemiesLib.RegisterEnemy(enemyType, 100, Levels.LevelTypes.None, EnemiesLib.SpawnType.Default, null, null);
			}
		}
	}
}
