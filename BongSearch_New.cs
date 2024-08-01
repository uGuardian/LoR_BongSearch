using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LOR_DiceSystem;

namespace BongSearch {
	public static partial class BongSearch {
		class ComparisonLists {
			public Dictionary<string, RangeFlags> matches = new Dictionary<string, RangeFlags> {
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
				{"@RANGE=RAPIDMELEE",	RangeFlags.Special},
				{"@RANGE=RAPID",		RangeFlags.Special},

				{"@RANGE=ONPLAY",		RangeFlags.Instance},
				{"@RANGE=INSTANCE",		RangeFlags.Instance},

				{"@RANGE=ALL",			RangeFlags.ALL},
			};
			public List<string> pidChecks = new List<string> {
				"$",
				"@ORIGIN",
				"@VANILLA",
				"@WORKSHOP",
			};
			public List<string> checkSymbols = new List<string> {
				"$",
				"@",
			};

			public static ComparisonLists Instance {
				get {
					if (instance == null) {
						instance = new ComparisonLists();
					}
					return instance;
				}
				set => instance = value;
			}
			static ComparisonLists instance;
		}
		#if NoSequential
		#warning NoSequential
		public static ParallelQuery<DiceCardItemModel> BongSearch_New(UI.UIInvenCardListScroll instance, ParallelQuery<DiceCardItemModel> cardsQueryTemp) {
			var cardsQuery = cardsQueryTemp.AsSequential();
		#else
		public static ParallelQuery<DiceCardItemModel> BongSearch_New(UI.UIInvenCardListScroll instance, ParallelQuery<DiceCardItemModel> cardsQuery) {
		#endif
			var searchKey = instance.CardFilter.CheckSearchKey();
			if (!string.IsNullOrWhiteSpace(searchKey)) {
				searchKey = searchKey.ToUpperInvariant();
				bool rangeCheck = false;
				bool pidCheck = false;
				if (searchKey.Contains("@RANGE")) {
					rangeCheck = true;
				}
				if (ComparisonLists.Instance.pidChecks.Any(searchKey.Contains)) {
					pidCheck = true;
				}
				var search = searchKey.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim(':'));
				var base_searchKeys = new List<string>();
				var filters = new List<FilterStruct>();
				foreach (var block in search) {
					var pids = Enumerable.Empty<string>();
					var filter = new FilterStruct(false, false, 0b_0000_0000);
					var regexSplit = Regex.Split(block, @"(?=[@$])", RegexOptions.Compiled);
					// var keys = Regex.Split(block, @"(?=[@$])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
					var keys = new List<string>();
					for (int i = 0; i < regexSplit.Length; i++) {
						string regSplit = regexSplit[i];
						if (ComparisonLists.Instance.checkSymbols.Any(regSplit.StartsWith)) {
							keys.AddRange(regSplit.Split((char[])null, 2, StringSplitOptions.RemoveEmptyEntries));
						} else {
							keys.Add(regSplit);
						}
					}
					for (int i = 0; i < keys.Count; i++) {
						string key = keys[i].Trim();
						if (string.IsNullOrEmpty(key)) {continue;}
						if (key.StartsWith("@", StringComparison.Ordinal)) {
							if (key.StartsWith("@ORIGIN", StringComparison.Ordinal) || key.StartsWith("@VANILLA", StringComparison.Ordinal)) {
								filter.isVanilla = true;
							} else if (key.StartsWith("@WORKSHOP", StringComparison.Ordinal)) {
								filter.isWorkshop = true;
							} else if (rangeCheck && ComparisonLists.Instance.matches.TryGetValue(key, out var match)) {
								filter.rangeFlags |= match;
							}
						} else if (!filter.isWorkshop && key.StartsWith("$", StringComparison.Ordinal)) {
							pids = pids.Append(key.Remove(0, 1));
						} else {
							base_searchKeys.Add(key);
						}
					}
					if (filter.isWorkshop) {
						pids = Enumerable.Empty<string>();
					}
					filter.pids.AddRange(pids.Distinct());
					filters.Add(filter);
				}
				bool filterBlock(DiceCardItemModel card, FilterStruct block) {
					bool cardRangeCheck() {
						if (block.rangeFlags != RangeFlags.NONE && block.rangeFlags != RangeFlags.ALL) {
							return IsCardFlag(block.rangeFlags, card);
						}
						return true;
					}
					bool cardPidCheck() {
						if (!(block.isVanilla ^ block.isWorkshop) && !block.pids.Any()) {
							return true;
						}
						var pid = card.GetID().packageId;
						var isVanillaPid = string.IsNullOrEmpty(pid);
						if (block.isVanilla) {
							if (isVanillaPid) {
								return true;
							}
						}
						if (block.isWorkshop) {
							if (!isVanillaPid) {
								return true;
							}
						} else if (block.pids.Count > 0) {
							if (!isVanillaPid && block.pids.Any(pid.ToUpperInvariant().Contains)) {
								return true;
							}
						}
						return false;
					}
					return (!rangeCheck || cardRangeCheck()) && (!pidCheck || cardPidCheck());
				}
				if (rangeCheck || pidCheck) {
					cardsQuery = cardsQuery.Where(c => filters.Any(b => filterBlock(c, b)));
				}
				var clean_searchKeys = base_searchKeys.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct();
				return !clean_searchKeys.Any()
					? cardsQuery
						#if NoSequential
						.AsParallel()
						#endif
					: cardsQuery.Where(x => {
						var name = x.GetName().ToUpperInvariant();
						var diceAbilityDescs = x.GetBehaviourList()
							.Select(y => BattleCardAbilityDescXmlList.Instance.GetAbilityDesc(y).ToUpperInvariant())
							.Distinct()
							.ToList();
						var abilityDesc = BattleCardAbilityDescXmlList.Instance.GetAbilityDescString(x.ClassInfo).ToUpperInvariant();
						return clean_searchKeys.Any(name.Contains)
							|| diceAbilityDescs.Exists(y => clean_searchKeys.Any(y.Contains))
							|| clean_searchKeys.Any(abilityDesc.Contains);
						})
						#if NoSequential
						.AsParallel();
						#else
						;
						#endif
			} else {
				#if NoSequential
				return cardsQuery.AsParallel();
				#else
				return cardsQuery;
				#endif
			}
		}
		public static ParallelQuery<DiceCardItemModel> BongSearch_New_CultureSensitive(UI.UIInvenCardListScroll instance, ParallelQuery<DiceCardItemModel> cardsQuery) {
			// TODO Culture Sensitive Mode
			var ex = new NotImplementedException("Culture Sensitive Mode not ready yet");
			#if DEBUG
			throw ex;
			#else
			UnityEngine.Debug.LogException(ex);
			return BongSearch_New(instance, cardsQuery); // Fall back to working operation.
			#endif
		}

		public static bool Contains(this string str, string match, StringComparison options) =>
			str.IndexOf(match, options) >= 0;
		struct FilterStruct {
			public bool isVanilla;
			public bool isWorkshop;
			public RangeFlags rangeFlags;
			public readonly List<string> pids;

			public FilterStruct(bool isVanilla, bool isWorkshop, RangeFlags rangeFlags) {
				this.isVanilla = isVanilla;
				this.isWorkshop = isWorkshop;
				this.rangeFlags = rangeFlags;
				this.pids = new List<string>();
			}
		}
		static bool IsCardFlag(RangeFlags flags, DiceCardItemModel card) {
			switch (card.GetSpec().Ranged) {
				case CardRange.Near:
					return (flags & RangeFlags.Near) == RangeFlags.Near;
				case CardRange.Far:
					return (flags & RangeFlags.Far) == RangeFlags.Far;
				case CardRange.FarArea:
					return (flags & RangeFlags.FarArea) == RangeFlags.FarArea;
				case CardRange.FarAreaEach:
					return (flags & RangeFlags.FarAreaEach) == RangeFlags.FarAreaEach;
				case CardRange.Special:
					return (flags & RangeFlags.Special) == RangeFlags.Special;
				case CardRange.Instance:
					return (flags & RangeFlags.Instance) == RangeFlags.Instance;
				default:
					return false;
			}
		}
	}
}