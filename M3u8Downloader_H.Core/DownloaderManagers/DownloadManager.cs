﻿using M3u8Downloader_H.Core.DownloaderSources;
using M3u8Downloader_H.Core.Utils.Extensions;
using M3u8Downloader_H.M3U8.Infos;
using M3u8Downloader_H.M3U8.Readers.Services;
using M3u8Downloader_H.Plugin;
using M3u8Downloader_H.Plugin.PluginManagers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace M3u8Downloader_H.Core.DownloaderManagers
{
    public class DownloadManager : IDownloadManager
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _url;
        private IEnumerable<KeyValuePair<string, string>>? _headers;
        private readonly IPluginManager? _pluginManager = default!;
        private readonly IM3u8FileInfoSource _m3U8FileInfoSource = default!;
        private IDownloaderSource? _downloaderSource;
        private M3UFileInfo? _m3UFileInfo;
        private M3UKeyInfo? keyInfo;
        private string? m3uContent;
        private IProgress<double> _vodProgress = default!;
        private IProgress<double> _liveProgress = default!;
        private Action<int> _setStatusDelegate = default!;

        public string VideoFullPath { get; } = default!;
        public string VideoFullName { get; private set; } = default!;

        public DownloadManager(HttpClient httpClient, Uri url, IEnumerable<KeyValuePair<string, string>>? Headers, string videoFullPath, IPluginBuilder? pluginBuilder)
        {
            _httpClient = httpClient;
            _url = url;
            _headers = Headers;
            VideoFullPath = videoFullPath;
            _pluginManager = PluginManger.CreatePluginMangaer(pluginBuilder);
            _m3U8FileInfoSource = M3u8FileInfoSourceFactory.CreateM3u8FileInfoSource(_pluginManager?.M3U8FileInfoService, httpClient, _pluginManager?.AttributeReaders);
        }

        public IDownloadManager WithM3u8FileInfo(M3UFileInfo fileinfo)
        {
            _m3UFileInfo = fileinfo;
            return this;
        }

        public IDownloadManager WithM3u8Content(string content)
        {
            m3uContent = content;
            return this;
        }

        public IDownloadManager WithKeyInfo(M3UKeyInfo m3UKeyInfo)
        {
            keyInfo = m3UKeyInfo;
            return this;
        }

        public IDownloadManager WithVodProgress(IProgress<double> action)
        {
            _vodProgress = action;
            return this;
        }
        public IDownloadManager WithLiveProgress(IProgress<double> action)
        {
            _liveProgress = action;
            return this;
        }

        public IDownloadManager WithStatusAction(Action<int> action)
        {
            _setStatusDelegate = action;
            return this;
        }

        public IDownloadManager WithHeaders(IEnumerable<KeyValuePair<string, string>>? headers)
        {
            _headers ??= headers;
            return this;
        }

        public async ValueTask GetM3U8FileInfo(CancellationToken cancellationToken)
        {
            if (_m3UFileInfo is not null)
                return;

            M3UFileInfo m3UFileInfo = await _m3U8FileInfoSource.GetM3u8FileInfo(_url, m3uContent, _headers, cancellationToken);

            if (keyInfo is not null)
                m3UFileInfo.Key = keyInfo;

            VideoFullName = VideoFullPath + (m3UFileInfo.Map is not null ? Path.GetExtension(m3UFileInfo.Map?.Title) : ".ts");

            _m3UFileInfo = m3UFileInfo;
        }


        private static DownloaderSource CreateDownloadSource(M3UFileInfo m3UFileInfo)
        {
            bool isFile = m3UFileInfo.MediaFiles.Any(m => m.Uri.IsFile);
            if (isFile)
                return new OnlyMergeSource();
            else if (m3UFileInfo.IsVod())
                return new DownloadVodSource();
            else
                return new DownloadLiveSource();
        }


        public IDownloaderSource Build()
        {
            if (_downloaderSource is not null)
                return _downloaderSource;
  
            DownloaderSource downloaderSource = CreateDownloadSource(_m3UFileInfo!);

            downloaderSource.Url = _url;
            downloaderSource.Headers = _headers;
            downloaderSource.HttpClient = _httpClient;
            downloaderSource.M3UFileInfo = _m3UFileInfo!;
            downloaderSource.VideoFullName = VideoFullName;
            downloaderSource.VideoFullPath = VideoFullPath;
            downloaderSource.LiveProgress = _liveProgress;
            downloaderSource.VodProgress = _vodProgress;
            downloaderSource.SetStatusDelegate = _setStatusDelegate;
            downloaderSource.downloadService = _pluginManager?.PluginService;
            downloaderSource.M3uReader = _m3U8FileInfoSource;

            downloaderSource.ChangeVideoNameDelegate = videoname => VideoFullName = videoname;
            return _downloaderSource = downloaderSource;
        }

    }
}