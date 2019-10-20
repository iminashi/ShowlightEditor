﻿using System;
using System.Collections.Generic;

namespace ShowlightEditor.Core.Models
{
    internal static class ArrangementCache
    {
        private static readonly Dictionary<string, ArrangementData> cache = new Dictionary<string, ArrangementData>();

        internal static bool TryGetArrangementData(string filename, out ArrangementData arrData, DateTime timeModified)
        {
            if (cache.ContainsKey(filename) /*&& cache[filename].TryGetTarget(out ArrangementData data) && data.TimeModified == timeModified*/)
            {
                var data = cache[filename];
                if (data.TimeModified == timeModified)
                {
                    arrData = data;
                    return true;
                }
            }

            arrData = null;
            return false;
        }

        internal static void AddArrangementData(string filename, ArrangementData arrData)
            => cache[filename] = arrData;
            //=> cache[filename] = new WeakReference<ArrangementData>(arrData);
    }
}
