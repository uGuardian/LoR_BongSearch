using System;
using System.Linq;
using System.Collections.Generic;

namespace BongSearch {
	static class BongSearch_General_Examples {
		class Example {
			public void BongSearch_Examples(UI.UIInvenCardListScroll instance, List<DiceCardItemModel> cards) {
				// Implementations 1 and 2 should be identical
				var implementation1 = GetImplementation1(instance);
				var implementation2 = GetImplementation2(instance);
				// Implementation 3 has more complicated logic.
				var implementation3 = GetImplementation3(instance);

				// This method can accept any IEnumerable, and you should directly feed it what you're working with in most cases.
				var result1 = BongSearch.BongSearch_General(implementation1, cards);
				var result2 = BongSearch.BongSearch_General(implementation2, cards);
				var result3 = BongSearch.BongSearch_General(implementation3, cards);
			}
			BongSearch.GeneralInstructionSet<DiceCardItemModel> GetImplementation1(UI.UIInvenCardListScroll instance) {
				return new BongSearch.GeneralInstructionSet<DiceCardItemModel>(
					instance.CardFilter.CheckSearchKey,
					new Func<DiceCardItemModel, LorId>(x => x.GetID()),
					false,
					// Everything below here is fields that vanilla search words are checked against.
					// This implementation will automatically convert the letter casing to what it needs.
					new Func<DiceCardItemModel, string>(x => x.GetName()),
					new Func<DiceCardItemModel, string>(x => BattleCardAbilityDescXmlList.Instance.GetAbilityDescString(x.ClassInfo))
				);
			}
			BongSearch.GeneralInstructionSet<DiceCardItemModel> GetImplementation2(UI.UIInvenCardListScroll instance) {
				return new BongSearch.GeneralInstructionSet<DiceCardItemModel>(
					instance.CardFilter.CheckSearchKey,
					new Func<DiceCardItemModel, LorId>(x => x.GetID()),
					true,
					// Everything below here is fields that vanilla search words are checked against.
					// This implementation wants UpperInvariant inputs only.
					new Func<DiceCardItemModel, string>(x => x.GetName().ToUpperInvariant()),
					new Func<DiceCardItemModel, string>(x => BattleCardAbilityDescXmlList.Instance.GetAbilityDescString(x.ClassInfo).ToUpperInvariant())
				);
			}
			BongSearch.GeneralInstructionSet<DiceCardItemModel> GetImplementation3(UI.UIInvenCardListScroll instance) {
				// This implementation manually creates the entire vanilla search filter list, allowing for more complex behavior.
				// This implementation MUST always have been converted to Upper Invariant.
				var vanillaFieldChecks = new Func<DiceCardItemModel, IEnumerable<string>>(x => {
					var list = new List<string>();
					list.Add(x.GetName().ToUpperInvariant());
					list.AddRange(x.GetBehaviourList()
						.Select(y => BattleCardAbilityDescXmlList.Instance.GetAbilityDesc(y).ToUpperInvariant())
						.Distinct()
					);
					list.Add(BattleCardAbilityDescXmlList.Instance.GetAbilityDescString(x.ClassInfo).ToUpperInvariant());
					return list;
				});
				return new BongSearch.GeneralInstructionSet<DiceCardItemModel>(
					instance.CardFilter.CheckSearchKey,
					new Func<DiceCardItemModel, LorId>(x => x.GetID()),
					vanillaFieldChecks
				);
			}
		}
	}
}