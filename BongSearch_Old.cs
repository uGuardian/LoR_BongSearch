using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LOR_DiceSystem;

namespace BongSearch {
	public static partial class BongSearch {
		[Obsolete("Old BongSearch Method")]
		// [HarmonyPostfix, HarmonyPatch(typeof(UI.UIInvenCardListScroll), "GetCardBySearchFilterUI")]
		public static void BongSearch_Old(UI.UIInvenCardListScroll __instance, ref List<DiceCardItemModel> __result, List<DiceCardItemModel> cards)
		// public static ParallelQuery<DiceCardItemModel> BongSearch_Old(UI.UIInvenCardListScroll __instance, ParallelQuery<DiceCardItemModel> cardsQuery)
		{
			var searchKey = __instance.CardFilter.CheckSearchKey();
			var cardsWithoutResult = cards.Except(__result);
			List<string> keyChecks = new List<string> {
				"::",
				"@",
				"$",
			};
			if (BongSearch_Config.Instance.CompatibilityMode ? keyChecks.Any(searchKey.Contains) : !string.IsNullOrWhiteSpace(searchKey))
			{
				/*
				ParallelQuery<DiceCardItemModel> cardsQuery;
				if (__result != null && __result.Count != 0) {
					cardsQuery = cards.AsParallel().Except(__result.AsParallel());
				} else {
					cardsQuery = cards.AsParallel();
				}
				*/
				searchKey = searchKey.ToUpperInvariant();
				ILookup<CardRange, DiceCardItemModel> cardsByRange = null;
				ILookup<string, DiceCardItemModel> cardsByPid = null;
				// ILookup<(CardRange range, string pid), DiceCardItemModel> cardsByBoth = null;
				bool rangeCheck = false;
				bool pidCheck = false;
				if (searchKey.Contains("@RANGE")) {
					rangeCheck = true;
				}
				if (ComparisonLists.Instance.pidChecks.Any(searchKey.Contains)) {
					pidCheck = true;
				}
				/*
				if (rangeCheck && pidCheck) {
					cardsByBoth = cards.ToLookup(card =>
						(card.GetSpec().Ranged, card.GetID().packageId?.ToUpperInvariant() ?? ""));
				*/
				// } else if (rangeCheck) {
				if (rangeCheck)
					cardsByRange = cardsWithoutResult.ToLookup(card => card.GetSpec().Ranged);
				// } else if (pidCheck) {
				if (pidCheck)
					cardsByPid = cardsWithoutResult.ToLookup(card => card.GetID().packageId?.ToUpperInvariant() ?? "");
				// }
				/*
				Dictionary<string, RangeFlags> matches = null;
				if (rangeCheck) {
					matches = new Dictionary<string, RangeFlags> {
						{"@RANGE=NEAR",			RangeFlags.Near},
						{"@RANGE=MELEE",		RangeFlags.Near},

						{"@RANGE=FAR",			RangeFlags.Far},
						{"@RANGE=RANGED",		RangeFlags.Far},

						{"@RANGE=MASS",			RangeFlags.AREA},
						{"@RANGE=FARAREA",		RangeFlags.FarArea},
						{"@RANGE=FARAREAEACH",	RangeFlags.FarAreaEach},

						{"@RANGE=SPECIAL",		RangeFlags.Special},
						{"@RANGE=GRAPPLE",		RangeFlags.Special},
						{"@RANGE=PREEMPTIVE",	RangeFlags.Special},
						{"@RANGE=FASTMELEE",	RangeFlags.Special},

						{"@RANGE=ONPLAY",		RangeFlags.Instance},
						{"@RANGE=INSTANCE",		RangeFlags.Instance},

						{"@RANGE=ALL",			RangeFlags.ALL},
					};
				}
				*/
				var search = searchKey.Split(new string[] { "::" }, StringSplitOptions.None).Select(x => x.Trim(':').Trim().ToUpper());
				var base_searchKeys = new List<string>();
				HashSet<DiceCardItemModel> blocks = new HashSet<DiceCardItemModel>(cards.Count);
				foreach (var block in search) {
					List<string> pids = new List<string>();
					RangeFlags rangeFlags = 0b_0000_0000;
					bool isVanilla = false;
					bool isWorkshop = false;
					var regexSplit = Regex.Split(block, @"(?=[@$])", RegexOptions.Compiled);
					var keys = new List<string>();
					for (int i = 0; i < regexSplit.Length; i++) {
						string regSplit = regexSplit[i];
						if (ComparisonLists.Instance.checkSymbols.Any(regSplit.StartsWith)) {
							keys.AddRange(regSplit.Split((char[])null, 2, StringSplitOptions.RemoveEmptyEntries));
						} else {
							keys.Add(regSplit);
						}
					}
					foreach (var checkKey in keys) {
						var key = checkKey.Trim();
						if (string.IsNullOrEmpty(key)) {continue;}
						if (key.StartsWith("@")) {
							if (key.StartsWith("@ORIGIN") || key.StartsWith("@VANILLA")) {
								isVanilla = true;
							} else if (key.StartsWith("@WORKSHOP")) {
								isWorkshop = true;
							} else if (rangeCheck && ComparisonLists.Instance.matches.TryGetValue(key, out var match)) {
								rangeFlags |= ComparisonLists.Instance.matches[key];
							}
						} else if (!isWorkshop && key.StartsWith("$")) {
							pids.Add(key.Remove(0, 1).TrimEnd());
						} else {
							base_searchKeys.Add(key);
						}
					}
					HashSet<DiceCardItemModel> rangeCards = null;
					if (isVanilla && isWorkshop) {
						pids.Clear();
					}
					if (rangeFlags != RangeFlags.NONE && rangeFlags != RangeFlags.ALL) {
						rangeCards = FlagsToCards(rangeFlags, cardsByRange);
						if (!(isVanilla ^ isWorkshop) && !pids.Any()) {
							blocks.UnionWith(rangeCards);
							continue;
						} else {
							goto pidComparison;
						}
					} else if (isVanilla ^ isWorkshop || pids.Any()) {
						goto pidComparison;
					}
					continue;
					pidComparison:
					var pidsDistinct = pids.Distinct();
					IEnumerable<DiceCardItemModel> pidCards = new HashSet<DiceCardItemModel>(cards.Count);
					if (!(isVanilla ^ isWorkshop)) {
						if (!pidsDistinct.Any()) {
							if (rangeCards != null) {
								blocks.UnionWith(rangeCards);
							}
							continue;
						}
					}
					if (isWorkshop) {
						rangeCards?.ExceptWith(cardsByPid[""]);
						blocks.UnionWith(rangeCards ?? cardsWithoutResult.Except(cardsByPid[""]));
						continue;
					} else if (pidsDistinct.Any()) {
						foreach (var pid in cardsByPid) {
							if (pidsDistinct.Any(pid.Key.Contains)) {
								pidCards = pidCards.Union(pid);
							}
						}
					}
					if (isVanilla) {
						pidCards = pidCards.Union(cardsByPid[""]);
					}
					rangeCards?.IntersectWith(pidCards);
					blocks.UnionWith(rangeCards ?? pidCards);
				}
				var clean_searchKeys = base_searchKeys.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct();
				/*
				if (rangeCheck || pidCheck) {
					output = blocks.AsParallel();
				} else {
					output = cards.AsParallel();
				}
				*/
				if (!BongSearch_Config.Instance.CompatibilityMode && blocks.Count <= 0) {
					__result.AddRange(!clean_searchKeys.Any()
						? cards
						: cards.Where(x => clean_searchKeys.Any(x.GetName().ToUpperInvariant().Contains)
							|| x.GetBehaviourList().Find(y => clean_searchKeys.Any(BattleCardAbilityDescXmlList.Instance.GetAbilityDesc(y).ToUpperInvariant().Contains)) != null
							|| clean_searchKeys.Any(BattleCardAbilityDescXmlList.Instance.GetAbilityDescString(x.ClassInfo).ToUpperInvariant().Contains)));
				} else {
					__result.AddRange(!clean_searchKeys.Any()
						? blocks
						: blocks.Where(x => clean_searchKeys.Any(x.GetName().ToUpperInvariant().Contains)
							|| x.GetBehaviourList().Find(y => clean_searchKeys.Any(BattleCardAbilityDescXmlList.Instance.GetAbilityDesc(y).ToUpperInvariant().Contains)) != null
							|| clean_searchKeys.Any(BattleCardAbilityDescXmlList.Instance.GetAbilityDescString(x.ClassInfo).ToUpperInvariant().Contains)));
				}
			} else if (!BongSearch_Config.Instance.CompatibilityMode) {
				__result = cards;
			}
		}
		[Flags]
		enum RangeFlags : byte {
			Near		= 0b_0000_0001,
			Far			= 0b_0000_0010,
			AREA		= 0b_0000_1100,
			FarArea		= 0b_0000_0100,
			FarAreaEach	= 0b_0000_1000,
			Special		= 0b_0001_0000,
			Instance	= 0b_0010_0000,
			NONE		= 0b_0000_0000,
			ALL			= 0b_0011_1111,
		}
		static HashSet<DiceCardItemModel> FlagsToCards(RangeFlags flags, ILookup<CardRange, DiceCardItemModel> cards) {
			HashSet<DiceCardItemModel> cardBlock = new HashSet<DiceCardItemModel>(cards.Count);
			if ((flags & RangeFlags.Near) == RangeFlags.Near) {
				cardBlock.UnionWith(cards[CardRange.Near]);
			}
			if ((flags & RangeFlags.Far) == RangeFlags.Far) {
				cardBlock.UnionWith(cards[CardRange.Far]);
			}
			if ((flags & RangeFlags.FarArea) == RangeFlags.FarArea) {
				cardBlock.UnionWith(cards[CardRange.FarArea]);
			}
			if ((flags & RangeFlags.FarAreaEach) == RangeFlags.FarAreaEach) {
				cardBlock.UnionWith(cards[CardRange.FarAreaEach]);
			}
			if ((flags & RangeFlags.Special) == RangeFlags.Special) {
				cardBlock.UnionWith(cards[CardRange.Special]);
			}
			if ((flags & RangeFlags.Instance) == RangeFlags.Instance) {
				cardBlock.UnionWith(cards[CardRange.Instance]);
			}
			return cardBlock;
		}
	}
}