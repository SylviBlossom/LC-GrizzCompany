using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
using HarmonyLib;
using System;

namespace GrizzCompany.Items
{
	public class Config : SyncedConfig2<Config>
	{
		[SyncedEntryField] public SyncedEntry<bool> ScrapCannonEnabled;
		[SyncedEntryField] public SyncedEntry<int> ScrapCannonPrice;
		[SyncedEntryField] public SyncedEntry<bool> ScrapCannonGuideLines;
		[SyncedEntryField] public SyncedEntry<bool> ScrapCannonHinderedMovement;

		public Config(ConfigFile cfg) : base(Plugin.GUID)
		{
			ScrapCannonEnabled = cfg.BindSyncedEntry("ScrapCannon", "Enabled", true, "Whether the Scrap cannon is added to the store.");
			ScrapCannonPrice = cfg.BindSyncedEntry("ScrapCannon", "Price", -1, $"The cost of the Scrap cannon in the store. -1 means default (currently {Assets.ScrapCannon.price}).");
			ScrapCannonGuideLines = cfg.BindSyncedEntry("ScrapCannon", "ShowGuideLines", false, "Displays lines on the screen while holding the Scrap cannon indicating target angles (blue is ship, yellow is main entrance, red is fire exit).");
			ScrapCannonHinderedMovement = cfg.BindSyncedEntry("ScrapCannon", "HinderedMovement", false, "Prevents the player from jumping or sprinting while carrying the Scrap cannon.");

			ScrapCannonEnabled.Changed += (_, _) => UpdateScrapCannonShop();
			ScrapCannonPrice.Changed += (_, _) => UpdateScrapCannonShop();
			ScrapCannonHinderedMovement.Changed += (_, _) => UpdateScrapCannonHinderedMovement();

			InitialSyncCompleted += (_, _) => UpdateValues();

			ConfigManager.Register(this);
		}

		public void UpdateValues()
		{
			UpdateScrapCannonShop();
			UpdateScrapCannonHinderedMovement();
		}

		private void UpdateScrapCannonShop()
		{
			ItemUtility.ToggleShopItem(Assets.ScrapCannon.item, ScrapCannonEnabled.Value);
			ItemUtility.UpdateShopPrice(Assets.ScrapCannon.item, ScrapCannonPrice.Value, Assets.ScrapCannon.price);
		}

		private void UpdateScrapCannonHinderedMovement()
		{
			if (Assets.ScrapCannon.item == null)
			{
				return;
			}
			Assets.ScrapCannon.item.weight = ScrapCannonHinderedMovement.Value ? 1f : ScrapCannonItem.weight;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Terminal), "Awake")]
		private static void TerminalAwakePatch()
		{
			Plugin.Config.UpdateValues();
		}
	}
}
