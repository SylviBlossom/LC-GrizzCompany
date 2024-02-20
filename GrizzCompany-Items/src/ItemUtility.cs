using LethalLib.Modules;
using System.Linq;
using UnityEngine;
using ItemsLib = LethalLib.Modules.Items;
using Object = UnityEngine.Object;

namespace GrizzCompany.Items
{
	public class ItemUtility
	{
		public static void UpdateShopPrice(Item item, int price, int defaultPrice)
		{
			if (StartOfRound.Instance != null)
			{
				ItemsLib.UpdateShopItemPrice(item, price);
			}

			var shopItem = ItemsLib.shopItems.FirstOrDefault(x => x.origItem == item || x.item == item);
			shopItem.price = price;
			shopItem.item.creditsWorth = (price == -1) ? defaultPrice : price;
		}

		public static void ToggleShopItem(Item item, bool enabled)
		{
			var shopItem = ItemsLib.shopItems.FirstOrDefault(x => x.origItem == item || x.item == item);
			var wasRemoved = shopItem.wasRemoved;

			if (wasRemoved == !enabled)
			{
				return;
			}

			shopItem.wasRemoved = !enabled;

			if (!enabled)
			{
				ItemsLib.RemoveShopItem(item);

				ItemsLib.buyableItemAssetInfos.RemoveAll(x => x.itemAsset == shopItem.item);
			}
			else
			{
				var self = Object.FindObjectOfType<Terminal>();

				if (StartOfRound.Instance == null || self == null)
				{
					return;
				}

				var itemList = self.buyableItemsList.ToList();

				var buyKeyword = self.terminalNodes.allKeywords.First(keyword => keyword.word == "buy");
				var cancelPurchaseNode = buyKeyword.compatibleNouns[0].result.terminalOptions[1].result;
				var infoKeyword = self.terminalNodes.allKeywords.First(keyword => keyword.word == "info");

				if (shopItem.price == -1)
				{
					shopItem.price = shopItem.item.creditsWorth;
				}
				else
				{
					shopItem.item.creditsWorth = shopItem.price;
				}

				var oldIndex = -1;

				if (!itemList.Any((Item x) => x == shopItem.item))
				{
					itemList.Add(shopItem.item);
				}
				else
				{
					oldIndex = itemList.IndexOf(shopItem.item);
				}

				var newIndex = oldIndex == -1 ? itemList.Count - 1 : oldIndex;

				var itemName = shopItem.item.itemName;
				var lastChar = itemName[itemName.Length - 1];
				var itemNamePlural = itemName;

				var buyNode2 = shopItem.buyNode2;

				if (buyNode2 == null)
				{
					buyNode2 = ScriptableObject.CreateInstance<TerminalNode>();

					buyNode2.name = $"{itemName.Replace(" ", "-")}BuyNode2";
					buyNode2.displayText = $"Ordered [variableAmount] {itemNamePlural}. Your new balance is [playerCredits].\n\nOur contractors enjoy fast, free shipping while on the job! Any purchased items will arrive hourly at your approximate location.\r\n\r\n";
					buyNode2.clearPreviousText = true;
					buyNode2.maxCharactersToType = 15;
				}

				buyNode2.buyItemIndex = newIndex;
				buyNode2.isConfirmationNode = false;
				buyNode2.itemCost = shopItem.price;
				buyNode2.playSyncedClip = 0;

				var buyNode1 = shopItem.buyNode1;
				if (buyNode1 == null)
				{
					buyNode1 = ScriptableObject.CreateInstance<TerminalNode>();
					buyNode1.name = $"{itemName.Replace(" ", "-")}BuyNode1";
					buyNode1.displayText = $"You have requested to order {itemNamePlural}. Amount: [variableAmount].\nTotal cost of items: [totalCost].\n\nPlease CONFIRM or DENY.\r\n\r\n";
					buyNode1.clearPreviousText = true;
					buyNode1.maxCharactersToType = 35;

				}

				buyNode1.buyItemIndex = newIndex;
				buyNode1.isConfirmationNode = true;
				buyNode1.overrideOptions = true;
				buyNode1.itemCost = shopItem.price;

				buyNode1.terminalOptions = new CompatibleNoun[2]
				{
				new CompatibleNoun()
				{
					noun = self.terminalNodes.allKeywords.First(keyword2 => keyword2.word == "confirm"),
					result = buyNode2
				},
				new CompatibleNoun()
				{
					noun = self.terminalNodes.allKeywords.First(keyword2 => keyword2.word == "deny"),
					result = cancelPurchaseNode
				}
				};

				var keyword = TerminalUtils.CreateTerminalKeyword(itemName.ToLowerInvariant().Replace(" ", "-"), defaultVerb: buyKeyword);

				var allKeywords = self.terminalNodes.allKeywords.ToList();
				allKeywords.Add(keyword);
				self.terminalNodes.allKeywords = allKeywords.ToArray();

				var nouns = buyKeyword.compatibleNouns.ToList();
				nouns.Add(new CompatibleNoun()
				{
					noun = keyword,
					result = buyNode1
				});
				buyKeyword.compatibleNouns = nouns.ToArray();


				var itemInfo = shopItem.itemInfo;
				if (itemInfo == null)
				{
					itemInfo = ScriptableObject.CreateInstance<TerminalNode>();
					itemInfo.name = $"{itemName.Replace(" ", "-")}InfoNode";
					itemInfo.displayText = $"[No information about this object was found.]\n\n";
					itemInfo.clearPreviousText = true;
					itemInfo.maxCharactersToType = 25;
				}

				self.terminalNodes.allKeywords = allKeywords.ToArray();

				var itemInfoNouns = infoKeyword.compatibleNouns.ToList();
				itemInfoNouns.Add(new CompatibleNoun()
				{
					noun = keyword,
					result = itemInfo
				});
				infoKeyword.compatibleNouns = itemInfoNouns.ToArray();

                ItemsLib.BuyableItemAssetInfo buyableItemAssetInfo = new ItemsLib.BuyableItemAssetInfo()
				{
					itemAsset = shopItem.item,
					keyword = keyword
				};

				ItemsLib.buyableItemAssetInfos.Add(buyableItemAssetInfo);

				self.buyableItemsList = itemList.ToArray();
			}
		}
	}
}
