using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LOR_DiceSystem;

namespace BongSearch {
	public static partial class BongSearch {
		public class GeneralInstructionSet<T> {
			public readonly Func<string> searchKey;
			public readonly Func<T, LorId> lorIdCheck;
			public readonly Func<T, IEnumerable<string>> vanillaFieldChecks;
			protected GeneralInstructionSet(Func<string> searchKey, Func<T, LorId> lorIdCheck) {
				this.searchKey = searchKey;
				this.lorIdCheck = lorIdCheck;
            }
			public GeneralInstructionSet(Func<string> searchKey, Func<T, LorId> lorIdCheck, Func<T, IEnumerable<string>> vanillaFieldChecks) : this(searchKey, lorIdCheck) {
				this.vanillaFieldChecks = vanillaFieldChecks;
			}

			public GeneralInstructionSet(Func<string> searchKey, Func<T, LorId> lorIdCheck, bool isUpperInvariant, params Func<T, string>[] vanillaFieldChecks) : this(searchKey, lorIdCheck) {
				this.vanillaFieldChecks = item => AllFieldChecks(item, isUpperInvariant, vanillaFieldChecks);
			}
            private static HashSet<string> AllFieldChecks(T item, bool isUpperInvariant, Func<T, string>[] vanillaFieldChecks)  {
                HashSet<string> fieldCheckHashset = new HashSet<string>(vanillaFieldChecks.Length);
                for (int i = 0; i < vanillaFieldChecks.Length; i++) {
                    var fieldCheckCurrent = vanillaFieldChecks[i];
                    string fieldCheckResult = fieldCheckCurrent.Invoke(item);
                    if (!isUpperInvariant) {
                        fieldCheckResult = fieldCheckResult.ToUpperInvariant();
                    }
                    fieldCheckHashset.Add(fieldCheckResult);
                }
                return fieldCheckHashset;
            }
		}
		#if NoSequential
		#warning NoSequential
		public static ParallelQuery<T> BongSearch_General<T>(GeneralInstructionSet<T> instructions, ParallelQuery<T> queryTemp) {
			var query = queryTemp.AsSequential();
		#else
		public static ParallelQuery<T> BongSearch_General<T>(GeneralInstructionSet<T> instructions, ParallelQuery<T> query) {
		#endif
			var searchKey = instructions.searchKey?.Invoke();
			if (!string.IsNullOrWhiteSpace(searchKey)) {
				searchKey = searchKey.ToUpperInvariant();
				bool pidCheck = false;
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
				bool filterBlock(T card, FilterStruct block) {
					bool cardPidCheck() {
						if (!(block.isVanilla ^ block.isWorkshop) && !block.pids.Any()) {
							return true;
						}
						var pid = instructions.lorIdCheck(card).packageId;
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
					return !pidCheck || cardPidCheck();
				}
				if (pidCheck) {
					query = query.Where(c => filters.Any(b => filterBlock(c, b)));
				}
				var clean_searchKeys = base_searchKeys.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct();
				return !clean_searchKeys.Any()
					? query
						#if NoSequential
						.AsParallel()
						#endif
					: query.Where(x => {
                        return instructions.vanillaFieldChecks.Invoke(x).Any(y => clean_searchKeys.Any(y.Contains));
					})
						#if NoSequential
						.AsParallel();
						#else
						;
						#endif
			} else {
				#if NoSequential
				return query.AsParallel();
				#else
				return query;
				#endif
			}
		}
		public static ParallelQuery<T> BongSearch_General<T>(GeneralInstructionSet<T> instructions, IEnumerable<T> cards)
			=> BongSearch_General(instructions, cards.AsParallel());
	}
}