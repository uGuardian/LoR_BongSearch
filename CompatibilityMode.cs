using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HarmonyLib;
using LOR_DiceSystem;
using BepInEx;
using BepInEx.Harmony;
using Unity.Collections;
using Unity.Jobs;

namespace BongSearch {
	public static partial class BongSearch {
		public static void UIInvenCardListScroll_GetCardBySearchFilterUI_Postfix(UI.UIInvenCardListScroll __instance, ref List<DiceCardItemModel> __result, List<DiceCardItemModel> cards) =>
			CompatibilityMode(__instance, ref __result, cards);
		public static void CompatibilityMode(UI.UIInvenCardListScroll __instance, ref List<DiceCardItemModel> __result, List<DiceCardItemModel> cards) {
			if (!BongSearch_Config.Instance.LegacyMode) {
				if (!BongSearch_Config.Instance.CultureSensitive) {
					__result.AddRange(BongSearch_New(__instance, cards.AsParallel().Except(__result.AsParallel())));
				} else {
					// TODO Culture Sensitive Mode
					__result.AddRange(BongSearch_New_CultureSensitive(__instance, cards.AsParallel().Except(__result.AsParallel())));
				}
			} else {
				BongSearch_Old(__instance, ref __result, cards);
			}
		}
	}
}