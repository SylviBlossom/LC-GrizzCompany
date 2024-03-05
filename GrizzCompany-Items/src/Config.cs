using BepInEx.Configuration;
using CSync.Lib;
using CSync.Util;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Unity.Collections;
using Unity.Netcode;

namespace GrizzCompany.Items
{
	[DataContract]
	public class Config : SyncedConfig<Config>
	{
		[DataMember] public SyncedEntry<bool> ScrapCannonEnabled { get; internal set; }
		[DataMember] public SyncedEntry<int> ScrapCannonPrice { get; internal set; }

		public Config(ConfigFile cfg) : base(Plugin.GUID)
		{
			ConfigManager.Register(this);

			ScrapCannonEnabled = cfg.BindSyncedEntry("ScrapCannon", "Enabled", true, "Whether the Scrap cannon is added to the store.");
			ScrapCannonPrice = cfg.BindSyncedEntry("ScrapCannon", "Price", -1, $"The cost of the Scrap cannon in the store. -1 means default (currently {Assets.ScrapCannon.price}).");

			ScrapCannonEnabled.SettingChanged += SettingChanged;
			ScrapCannonPrice.SettingChanged += SettingChanged;

			SyncComplete += SettingSynced;
			SyncReverted += SettingSynced;
		}

		private static void ApplyValues()
		{
			ItemUtility.ToggleShopItem(Assets.ScrapCannon.item, Instance.ScrapCannonEnabled.Value);
			ItemUtility.UpdateShopPrice(Assets.ScrapCannon.item, Instance.ScrapCannonPrice.Value, Assets.ScrapCannon.price);
		}

		private void SettingChanged(object sender, EventArgs e)
		{
			if (!IsHost) return;

			ApplyValues();
			SendSync();
		}

		private void SettingSynced(object sender, EventArgs e)
		{
			ApplyValues();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Terminal), "Awake")]
		private static void TerminalAwakePatch()
		{
			ApplyValues();
		}

		internal void SendSync()
		{
			if (!IsHost) return;

			foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
			{
				if (NetworkManager.Singleton.LocalClientId == clientId)
				{
					continue;
				}

				OnRequestSync(clientId, default);
			}
		}
	}
}
