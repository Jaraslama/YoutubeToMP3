using MediaToolkit;
using MediaToolkit.Model;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace YoutubeToMP3
{
    public class Downloads
    {
        public static bool IsYouTubeUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return false;

            var host = uri.Host;
            return host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) ||
                   host.Equals("www.youtube.com", StringComparison.OrdinalIgnoreCase) ||
                   host.Equals("m.youtube.com", StringComparison.OrdinalIgnoreCase) ||
                   host.Equals("music.youtube.com", StringComparison.OrdinalIgnoreCase) ||
                   host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase) ||
                   host.Equals("www.youtu.be", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> DownloadToFolderAsync(
            string url,
            string destinationFolder,
            IProgress<double>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Odkaz nesmí být prázdný.", nameof(url));
            if (!Directory.Exists(destinationFolder))
                throw new DirectoryNotFoundException($"Cílová složka neexistuje: {destinationFolder}");

            if (!IsYouTubeUrl(url))
                throw new ArgumentException("Odkaz musí vést na YouTube.", nameof(url));

            var uri = new Uri(url);
            if (uri.Host.Equals("music.youtube.com", StringComparison.OrdinalIgnoreCase))
            {
                var canonicalUri = new UriBuilder(uri) { Host = "www.youtube.com" };
                url = canonicalUri.Uri.AbsoluteUri;
            }

            var youtube = new YoutubeClient();
            var videoId = YoutubeExplode.Videos.VideoId.Parse(url);
            var video = await youtube.Videos.GetAsync(videoId);
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeTitle = string.Join("_", video.Title.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries))
                .Trim()
                .TrimEnd('.');
            if (string.IsNullOrWhiteSpace(safeTitle))
                safeTitle = videoId.ToString();

            var outputPath = Path.Combine(destinationFolder, $"{safeTitle}.mp3");
            for (var suffix = 2; File.Exists(outputPath); suffix++)
                outputPath = Path.Combine(destinationFolder, $"{safeTitle} ({suffix}).mp3");

            var temporaryPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mp4");
            try
            {
                var manifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
                var audioStreams = manifest.GetAudioOnlyStreams();
                if (!audioStreams.Any())
                    throw new InvalidOperationException("Pro toto video není dostupný žádný zvukový stream.");

                var stream = audioStreams.GetWithHighestBitrate();
                temporaryPath = Path.ChangeExtension(temporaryPath, stream.Container.Name);
                await youtube.Videos.Streams.DownloadAsync(stream, temporaryPath, progress);
                if (!File.Exists(temporaryPath) || new FileInfo(temporaryPath).Length == 0)
                    throw new InvalidDataException("Stažený zvukový stream je prázdný.");

                var inputFile = new MediaFile { Filename = temporaryPath };
                var outputFile = new MediaFile { Filename = outputPath };
                await Task.Run(() =>
                {
                    using var engine = new Engine();
                    engine.GetMetadata(inputFile);
                    engine.Convert(inputFile, outputFile);
                });

                if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
                    throw new InvalidDataException("Převod se nedokončil nebo vytvořil prázdný soubor.");

                return outputPath;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
    }
}
