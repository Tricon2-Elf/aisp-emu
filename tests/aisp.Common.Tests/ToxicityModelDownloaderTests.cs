using System.Net;
using System.Text;
using aisp.Common.Services.Toxicity;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class ToxicityModelDownloaderTests
{
    sealed class FakeHandler : HttpMessageHandler
    {
        public Dictionary<string, byte[]> Responses { get; } = new(StringComparer.Ordinal);
        public List<string> Requests { get; } = [];
        public bool Fail { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var url = request.RequestUri?.ToString() ?? "";
            Requests.Add(url);
            if (Fail)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            if (!Responses.TryGetValue(url, out var body))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) }
            );
        }
    }

    [Fact]
    public async Task EnsureModelsAsync_SkipsExistingNonEmptyFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "tox-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            foreach (var (_, relative) in ToxicityModelDownloader.RequiredFiles)
            {
                var dest = Path.Combine(root, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                await File.WriteAllTextAsync(
                    dest,
                    "existing",
                    TestContext.Current.CancellationToken
                );
            }

            var handler = new FakeHandler();
            using var http = new HttpClient(handler);
            var downloader = new ToxicityModelDownloader(
                http,
                NullLogger<ToxicityModelDownloader>.Instance
            );
            await downloader.EnsureModelsAsync(root, TestContext.Current.CancellationToken);
            Assert.Empty(handler.Requests);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task EnsureModelsAsync_DownloadsMissingFilesAtomically()
    {
        var root = Path.Combine(Path.GetTempPath(), "tox-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var handler = new FakeHandler();
            foreach (var (url, _) in ToxicityModelDownloader.RequiredFiles)
                handler.Responses[url] = Encoding.UTF8.GetBytes("model-bytes");

            using var http = new HttpClient(handler);
            var downloader = new ToxicityModelDownloader(
                http,
                NullLogger<ToxicityModelDownloader>.Instance
            );
            await downloader.EnsureModelsAsync(root, TestContext.Current.CancellationToken);

            Assert.Equal(ToxicityModelDownloader.RequiredFiles.Count, handler.Requests.Count);
            foreach (var (_, relative) in ToxicityModelDownloader.RequiredFiles)
            {
                var dest = Path.Combine(root, relative);
                Assert.True(File.Exists(dest));
                Assert.False(File.Exists(dest + ".tmp"));
                Assert.Equal(
                    "model-bytes",
                    await File.ReadAllTextAsync(dest, TestContext.Current.CancellationToken)
                );
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RequiredFiles_PinsLid176OnnxExport()
    {
        Assert.Equal(9, ToxicityModelDownloader.RequiredFiles.Count);
        var lid = ToxicityModelDownloader.RequiredFiles.Where(f =>
            f.RelativePath.Replace('\\', '/').StartsWith("lid176/", StringComparison.Ordinal)
        );
        Assert.Equal(5, lid.Count());
        Assert.All(
            lid,
            f =>
                Assert.Contains(
                    $"huggingface.co/{FastTextLanguageId.HfRepo}/resolve/{FastTextLanguageId.HfSha}/",
                    f.Url,
                    StringComparison.Ordinal
                )
        );
        Assert.Contains(
            ToxicityModelDownloader.RequiredFiles,
            f => f.RelativePath.Replace('\\', '/') == "lid176/onnx/lid176.int8.onnx"
        );
    }

    [Fact]
    public async Task EnsureModelsAsync_PropagatesHttpFailure()
    {
        var root = Path.Combine(Path.GetTempPath(), "tox-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var handler = new FakeHandler { Fail = true };
            using var http = new HttpClient(handler);
            var downloader = new ToxicityModelDownloader(
                http,
                NullLogger<ToxicityModelDownloader>.Instance
            );
            await Assert.ThrowsAnyAsync<Exception>(() =>
                downloader.EnsureModelsAsync(root, TestContext.Current.CancellationToken)
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
