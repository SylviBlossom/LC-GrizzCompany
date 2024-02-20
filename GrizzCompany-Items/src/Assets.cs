using LethalLib.Modules;
using System.IO;
using System.Reflection;
using UnityEngine;
using ItemsLib = LethalLib.Modules.Items;

namespace GrizzCompany.Items
{
	public static class Assets
	{
		public static AssetBundle bundle { get; internal set; }

		public static void Load()
		{
			var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			bundle = AssetBundle.LoadFromFile(Path.Combine(path, "grizzcompany-items.bundle"));

			ScrapCannon.Load();
		}

		public static class ScrapCannon
		{
			public const int price = 300;

			public static Item item { get; internal set; }

			public static void Load()
			{
				item = bundle.LoadAsset<Item>("ScrapCannon");

				NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
				ItemsLib.RegisterShopItem(item, price);
			}
		}
	}
}
