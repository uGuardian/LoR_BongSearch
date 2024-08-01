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
using System.Runtime.CompilerServices;

namespace BongSearch
{
	public static class Globals {
		public const string version = "1.0.0";
	}
	#if BepInEx
	[BepInPlugin("LoR.uGuardian.BongSearch", "BongSearch", "2.0")]
	public class BongSearch_BepInEx : BaseUnityPlugin {
		void Awake() {
			// new Harmony("LoR.uGuardian.BongSearch").PatchAll();
			new BongSearch_Patcher.Patch();
		}
	}
	#else
	public class BongSearch_Vanilla : ModInitializer {
		public override void OnInitializeMod() {
			// new Harmony("LoR.uGuardian.BongSearch").PatchAll();
			new BongSearch_Patcher().Patch();
			base.OnInitializeMod();
		}
	}
	#endif
	public class BongSearch_Patcher {
		public readonly Version version = new Version(Globals.version);
		public readonly Harmony harmony = new Harmony("LoR.uGuardian.BongSearch");
		public void Patch() {
			ConfigInitializer.OnInitializeMod();
			if (!BongSearch_Config.Instance.CompatibilityMode) {
			harmony.Patch(typeof(UI.UIInvenCardListScroll).GetMethod(nameof(UI.UIInvenCardListScroll.ApplyFilterAll)),
				prefix: new HarmonyMethod(typeof(BongSearch).GetMethod(nameof(BongSearch.UIInvenCardListScroll_ApplyFilterAll_Prefix)),
					priority: Priority.Last));
			} else {
			harmony.Patch(typeof(UI.UIInvenCardListScroll).GetMethod(nameof(UI.UIInvenCardListScroll.GetCardBySearchFilterUI)),
				postfix: new HarmonyMethod(typeof(BongSearch).GetMethod(nameof(BongSearch.UIInvenCardListScroll_GetCardBySearchFilterUI_Postfix)),
					priority: Priority.LowerThanNormal));
			}
		}
	}
	[HarmonyPatch]
	public static partial class BongSearch {
		public class CardItemCompByCost : Comparer<DiceCardItemModel> {
			[Obsolete("Use constructor (EquipRangeType, HashSet<LorId>)")]
			public CardItemCompByCost() {
				range = EquipRangeType.Hybrid;
				onlyCardsList = new HashSet<LorId>();
			}
			public CardItemCompByCost(EquipRangeType range, HashSet<LorId> onlyCardsList) {
				this.range = range;
				if (onlyCardsList != null) {
					this.onlyCardsList = onlyCardsList;
				} else {
					this.onlyCardsList = new HashSet<LorId>();
				}
			}
			readonly EquipRangeType range;
			readonly HashSet<LorId> onlyCardsList;

			public override int Compare(DiceCardItemModel a, DiceCardItemModel b) {
				int num = onlyCardsList.Contains(b.GetID()) ? 1 : 0;
				int num2 = onlyCardsList.Contains(a.GetID()) ? 1 : 0;
				num = b.ClassInfo.isError ? -1 : num;
				num2 = a.ClassInfo.isError ? -1 : num2;
				int num3 = num - num2;
				if (num3 != 0) {
					return num3;
				}

				int numRanged;
				int numRanged2;
				switch (range) {
					case EquipRangeType.Melee:
						numRanged = b.GetSpec().Ranged == CardRange.Far ? -1 : 0;
						numRanged2 = a.GetSpec().Ranged == CardRange.Far ? -1 : 0;
						break;
					case EquipRangeType.Range:
						numRanged = b.GetSpec().Ranged == CardRange.Near ? -1 : 0;
						numRanged2 = a.GetSpec().Ranged == CardRange.Near ? -1 : 0;
						break;
					case EquipRangeType.Hybrid:
					default:
						goto skipRanged;
				}
				int numRangedResult = numRanged - numRanged2;
				if (numRangedResult != 0) {
					return numRangedResult;
				}

				skipRanged:
				int num4 = a.GetSpec().Cost - b.GetSpec().Cost;
				if (num4 != 0) {
					return num4;
				}

				int num5 = StringCompare(a.GetID().packageId, b.GetID().packageId);
				if (num5 != 0) {
					return num5;
				}

				return a.GetID().id - b.GetID().id;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static int StringCompare(string x, string y) {
				bool check1 = string.IsNullOrEmpty(x);
				bool check2 = string.IsNullOrEmpty(y);
				if (check1 && check2) {
					return 0;
				}
				if (check1) {
					return 1;
				}
				if (check2) {
					return -1;
				}
				return string.CompareOrdinal(x, y);
			}
		}
		public class CardItemCompByCost_Custom : Comparer<DiceCardItemModel> {
			[Obsolete("Use constructor (EquipRangeType, HashSet<LorId>)")]
			public CardItemCompByCost_Custom() {}
			public CardItemCompByCost_Custom(EquipRangeType range, HashSet<LorId> onlyCardsList) {
				this.range = range;
				this.onlyCardsList = onlyCardsList;
			}
			readonly EquipRangeType range = EquipRangeType.Hybrid;
			readonly HashSet<LorId> onlyCardsList;

			public override int Compare(DiceCardItemModel a, DiceCardItemModel b) {
				var step0 = CustomComparer.Step0_internal;
				if (step0 != null) {
					Delegate[] array = step0.GetInvocationList();
					for (int i = 0; i < array.Length; i++) {
						CustomComparerDelegate del = (CustomComparerDelegate)array[i];
						var result = del.Invoke(a, b);
						if (result != 0) {
							return result;
						}
					}
				}

				int num = onlyCardsList.Contains(b.GetID()) ? 1 : 0;
				int num2 = onlyCardsList.Contains(a.GetID()) ? 1 : 0;
				num = b.ClassInfo.isError ? -1 : num;
				num2 = a.ClassInfo.isError ? -1 : num2;
				int num3 = num - num2;
				if (num3 != 0) {
					return num3;
				}

				var stepRanged = CustomComparer.StepRanged_internal;
				if (stepRanged != null) {
					Delegate[] array = stepRanged.GetInvocationList();
					for (int i = 0; i < array.Length; i++) {
						CustomComparerDelegate del = (CustomComparerDelegate)array[i];
						var result = del.Invoke(a, b);
						if (result != 0) {
							return result;
						}
					}
				}

				int numRanged;
				int numRanged2;
				switch (range) {
					case EquipRangeType.Melee:
						numRanged = b.GetSpec().Ranged == CardRange.Far ? -1 : 0;
						numRanged2 = a.GetSpec().Ranged == CardRange.Far ? -1 : 0;
						break;
					case EquipRangeType.Range:
						numRanged = b.GetSpec().Ranged == CardRange.Near ? -1 : 0;
						numRanged2 = a.GetSpec().Ranged == CardRange.Near ? -1 : 0;
						break;
					case EquipRangeType.Hybrid:
					default:
						goto skipRanged;
				}
				int numRangedResult = numRanged - numRanged2;
				if (numRangedResult != 0) {
					return numRangedResult;
				}

				skipRanged:
				var step1 = CustomComparer.Step1_internal;
				if (step1 != null) {
					Delegate[] array = step1.GetInvocationList();
					for (int i = 0; i < array.Length; i++) {
						CustomComparerDelegate del = (CustomComparerDelegate)array[i];
						var result = del.Invoke(a, b);
						if (result != 0) {
							return result;
						}
					}
				}
				int num4 = a.GetSpec().Cost - b.GetSpec().Cost;
				if (num4 != 0) {
					return num4;
				}

				var step2 = CustomComparer.Step2_internal;
				if (step2 != null) {
					Delegate[] array = step2.GetInvocationList();
					for (int i = 0; i < array.Length; i++) {
						CustomComparerDelegate del = (CustomComparerDelegate)array[i];
						var result = del.Invoke(a, b);
						if (result != 0) {
							return result;
						}
					}
				}

				int num5 = CardItemCompByCost.StringCompare(a.GetID().packageId, b.GetID().packageId);
				if (num5 != 0) {
					return num5;
				}

				var step3 = CustomComparer.Step3_internal;
				if (step3 != null) {
					Delegate[] array = step3.GetInvocationList();
					for (int i = 0; i < array.Length; i++) {
						CustomComparerDelegate del = (CustomComparerDelegate)array[i];
						var result = del.Invoke(a, b);
						if (result != 0) {
							return result;
						}
					}
				}
				return a.GetID().id - b.GetID().id;
			}
		}
		private static bool overrideDetailFilter = false;
		private static bool overrideSearchFilter = false;
		private static bool overrideCostFilter = false;
		private static bool overrideGradeFilter = false;
		public static bool OverrideDetailFilter {
			get => overrideDetailFilter;
			set {
				UnityEngine.Debug.Log($"BongSearch: OverrideDetailFilter = {value}");
				if (value != overrideDetailFilter) {
					switch (value) {
						case true:
							DetailFilter -= GetCardsByDetailFilterUI;
							break;
						case false:
							DetailFilter += GetCardsByDetailFilterUI;
							break;
					}
					overrideDetailFilter = value;
				}
			}
		}
		public static bool OverrideSearchFilter {
			get => overrideSearchFilter;
			set {
				UnityEngine.Debug.Log($"BongSearch: OverrideSearchFilter = {value}");
				if (value != overrideSearchFilter) {
					switch (value) {
						case true:
							SearchFilter -= GetCardsBySearchFilterUI;
							break;
						case false:
							SearchFilter += GetCardsBySearchFilterUI;
							break;
					}
					overrideSearchFilter = value;
				}
			}
		}
		public static bool OverrideCostFilter {
			get => overrideCostFilter;
			set {
				UnityEngine.Debug.Log($"BongSearch: OverrideCostFilter = {value}");
				if (value != overrideCostFilter) {
					switch (value) {
						case true:
							CostFilter -= GetCardsByCostFilterUI_NoSort;
							break;
						case false:
							CostFilter += GetCardsByCostFilterUI_NoSort;
							break;
					}
					overrideCostFilter = value;
				}
			}
		}
		public static bool OverrideGradeFilter {
			get => overrideGradeFilter;
			set {
				UnityEngine.Debug.Log($"BongSearch: OverrideGradeFilter = {value}");
				if (value != overrideGradeFilter) {
					switch (value) {
						case true:
							GradeFilter -= GetCardsByGradeFilterUI_NoSort;
							break;
						case false:
							GradeFilter += GetCardsByGradeFilterUI_NoSort;
							break;
					}
					overrideGradeFilter = value;
				}
			}
		}
		public static bool OverrideAutoRangeFilter {
			get => BongSearch_Config.Instance.overrideAutoRangeFilter;
			set {
				UnityEngine.Debug.Log($"BongSearch: OverrideAutoRangeFilter = {value}");
				BongSearch_Config.Instance.overrideAutoRangeFilter = value;
			}
		}
		public static bool OverrideAutoExclusiveFilter {
			get => BongSearch_Config.Instance.overrideAutoExclusiveFilter;
			set {
				UnityEngine.Debug.Log($"BongSearch: OverrideAutoExclusiveFilter = {value}");
				BongSearch_Config.Instance.overrideAutoExclusiveFilter = value;
			}
		}

		public delegate void FilterDelegate(UI.UIInvenCardListScroll instance, ref ParallelQuery<DiceCardItemModel> query);
		public static event FilterDelegate DetailFilter = GetCardsByDetailFilterUI;
		public static event FilterDelegate SearchFilter = GetCardsBySearchFilterUI;
		public static event FilterDelegate CostFilter = GetCardsByCostFilterUI_NoSort;
		public static event FilterDelegate GradeFilter = GetCardsByGradeFilterUI_NoSort;
		public static event FilterDelegate PostRangeExclusiveFilter;
		public delegate int CustomComparerDelegate(DiceCardItemModel a, DiceCardItemModel b);
		public static class CustomComparer {
			public static event CustomComparerDelegate Step0;
			internal static CustomComparerDelegate Step0_internal => Step0;
			public static event CustomComparerDelegate StepRanged;
			internal static CustomComparerDelegate StepRanged_internal => StepRanged;
			public static event CustomComparerDelegate Step1;
			internal static CustomComparerDelegate Step1_internal => Step1;
			public static event CustomComparerDelegate Step2;
			internal static CustomComparerDelegate Step2_internal => Step2;
			public static event CustomComparerDelegate Step3;
			internal static CustomComparerDelegate Step3_internal => Step3;
		}

		public static bool UIInvenCardListScroll_ApplyFilterAll_Prefix(this UI.UIInvenCardListScroll __instance) =>
			BongSearch_ApplyFilterAll(__instance);

		// public static bool BongSearch_ApplyFilterAll(this UI.UIInvenCardListScroll __instance, List<DiceCardItemModel> cards) {
		public static bool BongSearch_ApplyFilterAll(this UI.UIInvenCardListScroll __instance) {
			var cards = __instance._originCardList;
			if (cards.Count <= 0) {return false;}
			try {
				#if DEBUG
				#warning DEBUG
				var stopwatch = System.Diagnostics.Stopwatch.StartNew();
				#endif
				__instance._currentCardListForFilter.Clear();
				ParallelQuery<DiceCardItemModel> query = cards.AsParallel();

				#region filter
				DetailFilter?.Invoke(__instance, ref query);
				#if DEBUG
				stopwatch.Stop();
				var temp = query.ToList();
				stopwatch.Start();
				#endif
				SearchFilter?.Invoke(__instance, ref query);
				#if DEBUG
				stopwatch.Stop();
				var temp2 = query.ToList();
				stopwatch.Start();
				#endif
				CostFilter?.Invoke(__instance, ref query);
				#if DEBUG
				stopwatch.Stop();
				var temp3 = query.ToList();
				stopwatch.Start();
				#endif
				GradeFilter?.Invoke(__instance, ref query);
				#if DEBUG
				stopwatch.Stop();
				var temp4 = query.ToList();
				stopwatch.Start();
				#endif
				#endregion

				#region conditions
				EquipRangeType curUnitRange = EquipRangeType.Hybrid;
				HashSet<LorId> onlyCardsList = null;
				if (__instance._unitdata != null) {
					Predicate<DiceCardItemModel> cond1 = null;
					curUnitRange = __instance._unitdata.bookItem.ClassInfo.RangeType;
					if (!OverrideAutoRangeFilter) {
						switch (__instance._unitdata.bookItem.ClassInfo.RangeType) {
							case EquipRangeType.Melee:
								cond1 = x => x.GetSpec().Ranged != CardRange.Far;
								break;
							case EquipRangeType.Range:
								cond1 = x => x.GetSpec().Ranged != CardRange.Near;
								break;
							case EquipRangeType.Hybrid:
							default:
								cond1 = x => true;
								break;
						}
					}
					onlyCardsList = __instance._unitdata.bookItem.GetOnlyCards()
						.Where(x => x.optionList.Contains(CardOption.OnlyPage)) // Oversight check for bad patches.
						.Select(x => x.id).ToHashSet();
					query = query.Where(x =>
						(!x.ClassInfo.optionList.Contains(CardOption.OnlyPage) && (OverrideAutoRangeFilter || cond1(x))) ||
						(x.ClassInfo.optionList.Contains(CardOption.OnlyPage) && (OverrideAutoExclusiveFilter || onlyCardsList.Contains(x.GetID())))
					);
					PostRangeExclusiveFilter?.Invoke(__instance, ref query);
				}
				#endregion

				#region sorting
				#if DEBUG
				stopwatch.Stop();
				var temp5 = query.ToList();
				stopwatch.Start();
				#endif
				var orderedQuery = CustomComparer.Step0_internal == null && CustomComparer.Step1_internal == null &&
					CustomComparer.Step2_internal == null && CustomComparer.Step3_internal == null
						? query.OrderBy(c => c, new CardItemCompByCost(curUnitRange, onlyCardsList))
						: query.OrderBy(c => c, new CardItemCompByCost_Custom(curUnitRange, onlyCardsList));
				#if DEBUG
				stopwatch.Stop();
				var temp6 = orderedQuery.ToList();
				stopwatch.Start();
				#endif
				/*
				if (__instance._unitdata != null) {
					orderedQuery = orderedQuery.ThenBy(c => c.ClassInfo.optionList.Contains(CardOption.OnlyPage) && cond2(c));
				}
				*/
				#endregion

				#region finalize
				__instance._currentCardListForFilter.AddRange(orderedQuery);
				var xmlList = BattleCardAbilityDescXmlList.Instance;
				AsyncCache.lazyDic.Clear();
				ComparisonLists.Instance = null;
				#if DEBUG
				// xmlList._dictionaryKeywordCache.Clear();
				#endif
				#endregion

				#region UI
				__instance.scrollBar.SetScrollRectSize(__instance.column * __instance.slotWidth, (__instance.GetMaxRow() + __instance.row - 1) * __instance.slotHeight);
				__instance.scrollBar.SetWindowPosition(0.0f, 0.0f);
				__instance.selectablePanel.ChildSelectable = __instance.slotList[0].selectable;
				__instance.SetCardsData(__instance.GetCurrentPageList());
				#endregion
				#if DEBUG
				stopwatch.Stop();
				UnityEngine.Debug.LogWarning(stopwatch.ElapsedTicks);
				UnityEngine.Debug.LogWarning(stopwatch.Elapsed);
				#endif
				return false;
			} catch (Exception ex) {
				UnityEngine.Debug.LogException(ex);
				return true;
			}
		}
		public static void GetCardsByDetailFilterUI(this UI.UIInvenCardListScroll instance, ref ParallelQuery<DiceCardItemModel> query) {
			var detailFilter = instance.CardFilter.GetDetailFilter();
			List<string> rarityFilter = detailFilter.CheckRarityDetailFilter();
			List<string> diceFilter = detailFilter.CheckDiceDetailFilter();
			List<string> bufFilter = detailFilter.CheckBufDetailFilter();
			List<string> abilityFilter = detailFilter.CheckAbilityDetailFilter();
			List<int> diceCountFilter = detailFilter.CheckDiceCountDetailFilter();

			var xmlList = Singleton<BattleCardAbilityDescXmlList>.Instance;
			bool bufFilterExists = bufFilter.Count > 0;
			bool abilityFilterExists = abilityFilter.Count > 0;

			if (rarityFilter.Count > 0) {
				query = query.Where(x => rarityFilter.Contains(x.GetRarity().ToString()));
			}
			if (diceFilter.Count > 0) {
				query = query.Where(x =>
					diceFilter.Any(g => x.GetBehaviourList().Exists(y => y.Detail.ToString().Equals(g) || y.Type.ToString().Equals(g)))
				);
			}

			bool shared(DiceCardItemModel x, string g) => x.GetBehaviourList().Exists(y =>
				xmlList.GetAbilityKeywords_byScript_Async(y.Script).Exists(z => z.Equals(g))) ||
				xmlList.GetAbilityKeywords_Async(x.ClassInfo).Exists(z => z.Equals(g)
			);
			bool bufAbilityFilter(DiceCardItemModel x) {
				bool buf = !bufFilterExists;
				if (bufFilterExists) {
					buf = bufFilter.Any(g => shared(x, g));
				}
				bool ability = !abilityFilterExists;
				if (abilityFilterExists) {
					ability = abilityFilter.Any(g => shared(x, g));
				}
				return buf && ability;
			}
			if (bufFilterExists || abilityFilterExists) {
				query = query.Where(x => bufAbilityFilter(x));
			}
			if (diceCountFilter.Count > 0) {
				query = query.Where(x => diceCountFilter.Contains(x.GetBehaviourList().Count));
			}
			return;
		}
		public static List<string> GetAbilityKeywords_Async(this BattleCardAbilityDescXmlList xmlList, DiceCardXmlInfo card) {
			List<string> abilityKeywords = new List<string>();
			if (card.Spec.Ranged == CardRange.FarArea) {
				abilityKeywords.Add("AreaCard_Keyword");
				abilityKeywords.Add("AreaDiceAll_Keyword");
			}
			else if (card.Spec.Ranged == CardRange.FarAreaEach) {
				abilityKeywords.Add("AreaCard_Keyword");
				abilityKeywords.Add("AreaDiceEach_Keyword");
			}
			else if (card.Spec.Ranged == CardRange.Instance)
				abilityKeywords.Add("Instant_Keyword");
			if (card.IsExhaustOnUse())
				abilityKeywords.Add("ExhaustOnUse_Keyword");
			string script = card.Script;
			if (script != "")
				abilityKeywords.AddRange(xmlList.GetAbilityKeywords_byScript_Async(script));
			return abilityKeywords;
		}
		public static List<string> GetAbilityKeywords_byScript_Async(this BattleCardAbilityDescXmlList xmlList, string scriptName) {
			// if (xmlList._dictionaryKeywordCache.TryGetValue(scriptName, out List<string> list)) {return list;}
			if (AsyncCache.GetCacheDic(scriptName, out var output)) {
				if (output.Count > 0 || AsyncCache.lazyDic.ContainsKey(scriptName)) {
					return output;
				}
			}
			return AsyncCache.SetCacheDic(scriptName, new Lazy<List<string>>(() => LazyDelegate(scriptName)));
		}
		static List<string> LazyDelegate(string scriptName) {
			#if DEBUG
			// UnityEngine.Debug.LogWarning("Getting Card Script : " + scriptName);
			#endif
			List<string> keywordsByScript2 = new List<string>();
			if (!string.IsNullOrEmpty(scriptName)) {
				DiceCardAbilityBase instanceDiceCardAbility = Singleton<AssemblyManager>.Instance.CreateInstance_DiceCardAbility(scriptName);
				if (instanceDiceCardAbility == null) {
					var instanceDiceCardSelfAbility = Singleton<AssemblyManager>.Instance.CreateInstance_DiceCardSelfAbility(scriptName);
					if (instanceDiceCardSelfAbility == null) {
						UnityEngine.Debug.LogError("card or dice ability not found : " + scriptName);
						return keywordsByScript2;
						/*
						var typeName = $"DiceCardSelfAbility_{scriptName.Trim()}, {typeof(BattleCardAbilityDescXmlList).Assembly.FullName}";
						Type type = Type.GetType(typeName);
						if (type != null) {
							if (Activator.CreateInstance(type) is DiceCardSelfAbilityBase instance)
								keywordsByScript2.AddRange(instance.Keywords);
						} else {
							UnityEngine.Debug.LogError("card or dice ability not found : " + scriptName);
							return keywordsByScript2;
						}
						*/
					} else {
						keywordsByScript2.AddRange(instanceDiceCardSelfAbility.Keywords);
					}
					/*
					Type type = Type.GetType("DiceCardSelfAbility_" + scriptName.Trim());
					if (type != null) {
						if (Activator.CreateInstance(type) is DiceCardSelfAbilityBase instance)
							keywordsByScript2.AddRange(instance.Keywords);
					} else {
						#if DEBUG
						// UnityEngine.Debug.LogWarning("card or dice ability not found : " + scriptName);
						#endif
						return keywordsByScript2;
					}
					*/
				} else {
					keywordsByScript2.AddRange(instanceDiceCardAbility.Keywords);
				}
			}
			return keywordsByScript2;
		}
		public static void GetCardsBySearchFilterUI(this UI.UIInvenCardListScroll instance, ref ParallelQuery<DiceCardItemModel> query) {
			if (!BongSearch_Config.Instance.LegacyMode) {
				if (!BongSearch_Config.Instance.CultureSensitive) {
					query = BongSearch_New(instance, query);
					return;
				} else {
					// TODO Culture Sensitive Mode
					UnityEngine.Debug.LogError("Culture Sensitive Mode not ready yet");
					query = BongSearch_New_CultureSensitive(instance, query);
					return;
				}
			} else {
				var __result = new List<DiceCardItemModel>();
				BongSearch_Old(instance, ref __result, query.ToList());
				query = __result.AsParallel();
				return;
			}
		}
		public static void GetCardsByCostFilterUI_NoSort(this UI.UIInvenCardListScroll instance, ref ParallelQuery<DiceCardItemModel> query) => GetCardsByCostFilterUI(instance, ref query, false);
		public static void GetCardsByCostFilterUI(this UI.UIInvenCardListScroll instance, ref ParallelQuery<DiceCardItemModel> query, bool sort = true) {
			var filter = instance.CardFilter.CheckCardCostFilter();
			if (filter.Count > 0)
				query = query.Where(c => filter.Contains(c.GetSpec().Cost));
			if (sort)
				query = query.OrderBy(c => c.GetSpec().Cost);
			return;
		}
		public static ParallelQuery<IGrouping<int, DiceCardItemModel>> GroupByCostFilterUI(this UI.UIInvenCardListScroll _, ParallelQuery<DiceCardItemModel> query) {
			return query.GroupBy(c => c.GetSpec().Cost);
		}
		public static void GetCardsByGradeFilterUI_NoSort(this UI.UIInvenCardListScroll instance, ref ParallelQuery<DiceCardItemModel> query) => GetCardsByGradeFilterUI(instance, ref query, false);
		public static void GetCardsByGradeFilterUI(this UI.UIInvenCardListScroll instance, ref ParallelQuery<DiceCardItemModel> query, bool sort = true) {
			var filter = instance.GradeFilter.GetStoryGradeFilter();
			if (filter.Count > 0)
				query = query.Where(c => filter.Contains((Grade)c.ClassInfo.Chapter));
			if (sort)
				query = query.OrderBy(c => c.ClassInfo.Chapter);
			return;
		}
		public static ParallelQuery<IGrouping<Grade, DiceCardItemModel>> GroupByGradeFilterUI(this UI.UIInvenCardListScroll _, ParallelQuery<DiceCardItemModel> query) {
			return query.GroupBy(c => (Grade)c.ClassInfo.Chapter);
		}
	}
}