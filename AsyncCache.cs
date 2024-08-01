using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace BongSearch {
	public static partial class BongSearch {
		public class AsyncCache {
			// public static readonly ConcurrentDictionary<string, object> lockDic = new ConcurrentDictionary<string, object>();
			static public readonly object lockObj = new object();
			// static public System.Threading.SpinLock dicLock = new System.Threading.SpinLock();
			static public bool GetCacheDic(string key, out List<string> item) {
				return BattleCardAbilityDescXmlList.Instance._dictionaryKeywordCache.TryGetValue(key, out item);
			}
			/*
			static public List<string> SetCacheDic(string key, Lazy<List<string>> value) {
				if (!lazyDic.TryAdd(key, value)) {
					return lazyDic[key].Value;
				}
				List<string> result;
				lock (value) {
					result = value.Value;
				}
				var xmlList = BattleCardAbilityDescXmlList.Instance;
				bool gotLock = false;
                try {
                    dicLock.Enter(ref gotLock);
                    xmlList._dictionaryKeywordCache.TryAdd(key, result);
                } finally {
                    if (gotLock) dicLock.Exit();
                }
				return result;
			}
			*/
			static public List<string> SetCacheDic(string key, Lazy<List<string>> value) {
				if (!lazyDic.TryAdd(key, value)) {
					return lazyDic[key].Value;
				}
				List<string> result;
				// lock (lockDic.GetOrAdd(key, new object())) {
				lock (value) {
					result = value.Value;
				}
				var xmlList = BattleCardAbilityDescXmlList.Instance;
				lock (lockObj) {
					xmlList._dictionaryKeywordCache[key] = result;
				}
				return result;
			}
			public static readonly ConcurrentDictionary<string, Lazy<List<string>>> lazyDic =
				new ConcurrentDictionary<string, Lazy<List<string>>>(StringComparer.Ordinal);
			// public static readonly ConcurrentDictionary<string, Lazy<List<string>>> dic = new ConcurrentDictionary<string, Lazy<List<string>>>();
		}
	}
}