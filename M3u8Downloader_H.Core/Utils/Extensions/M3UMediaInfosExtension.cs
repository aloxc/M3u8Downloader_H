﻿using System;
using System.Collections.Generic;
using System.Linq;
using M3u8Downloader_H.Common.Extensions;
using M3u8Downloader_H.Common.M3u8Infos;

namespace M3u8Downloader_H.Core.Utils.Extensions
{
    internal static class M3UMediaInfosExtension
    {
        public static IList<M3UMediaInfo> GenerateTitle(this IEnumerable<M3UMediaInfo> m3UMediaInfos)
        {
            var m3uMediaInfoList = m3UMediaInfos.ToList();
            for (int i = 0; i < m3uMediaInfoList.Count; i++)
            {
                var videoName = m3uMediaInfoList[i].Uri.Segments.Last();
                m3uMediaInfoList[i].Title = videoName.EndsWith(".ts", StringComparison.CurrentCulture) ? videoName : videoName.GetMd5();
            }
            return m3uMediaInfoList;
        }
    }
}
