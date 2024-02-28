using BepInEx.Configuration;
using CSync.Lib;
using CSync.Util;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Runtime.Serialization;
using Unity.Collections;
using Unity.Netcode;

namespace GrizzCompany.Items
{
	[DataContract]
	public class Config : SyncedInstance<Config>
	{
		[DataMember] public SyncedEntry<bool> ScrapCannonEnabled { get; internal set; }
		[DataMember] public SyncedEntry<int> ScrapCannonPrice { get; internal set; }

		public Config(ConfigFile cfg)
		{
			InitInstance(this);

			ScrapCannonEnabled = cfg.BindSyncedEntry("ScrapCannon", "Enabled", true, "Whether the Scrap cannon is added to the store.");
			ScrapCannonPrice = cfg.BindSyncedEntry("ScrapCannon", "Price", -1, $"The cost of the Scrap cannon in the store. -1 means default (currently {Assets.ScrapCannon.price}).");

			ScrapCannonEnabled.SettingChanged += SettingChanged;
			ScrapCannonPrice.SettingChanged += SettingChanged;
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

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Terminal), "Awake")]
		private static void TerminalAwakePatch()
		{
			ApplyValues();
		}

		#region Synchronization Methods
		internal static void RequestSync()
		{
			if (!IsClient) return;

			using FastBufferWriter stream = new(IntSize, Allocator.Temp);

			// Method `OnRequestSync` will then get called on host.
			stream.SendMessage($"{Plugin.GUID}_OnRequestConfigSync");
		}

		internal static void OnRequestSync(ulong clientId, FastBufferReader _)
		{
			if (!IsHost) return;

			SendSyncTo(clientId);
		}

		internal static void SendSync()
		{
			if (!IsHost) return;

			foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
			{
				if (NetworkManager.Singleton.LocalClientId == clientId)
				{
					continue;
				}

				SendSyncTo(clientId);
			}
		}

		internal static void SendSyncTo(ulong clientId)
		{
			if (!IsHost) return;

			byte[] array = SerializeToBytes(Instance);
			int value = array.Length;

			using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

			try
			{
				stream.WriteValueSafe(in value, default);
				stream.WriteBytesSafe(array);

				stream.SendMessage($"{Plugin.GUID}_OnReceiveConfigSync", clientId);
			}
			catch (Exception e)
			{
				Plugin.Logger.LogError($"Error occurred syncing config with client: {clientId}\n{e}");
			}
		}

		internal static void OnReceiveSync(ulong _, FastBufferReader reader)
		{
			if (!reader.TryBeginRead(IntSize))
			{
				Plugin.Logger.LogError("Config sync error: Could not begin reading buffer.");
				return;
			}

			reader.ReadValueSafe(out int val, default);
			if (!reader.TryBeginRead(val))
			{
				Plugin.Logger.LogError("Config sync error: Host could not sync.");
				return;
			}

			byte[] data = new byte[val];
			reader.ReadBytesSafe(ref data, val);

			try
			{
				Instance.SyncInstance(data);
				ApplyValues();
			}
			catch (Exception e)
			{
				Plugin.Logger.LogError($"Error syncing config instance!\n{e}");
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
		private static void InitializeLocalPlayer()
		{
			if (IsHost)
			{
				MessageManager.RegisterNamedMessageHandler($"{Plugin.GUID}_OnRequestConfigSync", OnRequestSync);
				Synced = true;

				return;
			}

			Synced = false;
			MessageManager.RegisterNamedMessageHandler($"{Plugin.GUID}_OnReceiveConfigSync", OnReceiveSync);
			RequestSync();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
		private static void PlayerLeave()
		{
			Instance.RevertSync();
			ApplyValues();
		}
		#endregion
	}
}
