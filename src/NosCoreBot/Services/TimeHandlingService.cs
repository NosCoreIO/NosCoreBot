using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ICSharpCode.SharpZipLib.BZip2;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NosCore.ParserInputGenerator.Downloader;
using NosCore.ParserInputGenerator.Extractor;
using NosCore.Shared.Enumerations;
using NosCoreBot.Modules;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;

namespace NosCoreBot.Services
{
    public class TimeHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly Timer _aTimer = new Timer(TimeSpan.FromHours(1).TotalMilliseconds);
        private readonly IClientDownloader _client;
        private readonly IExtractor _extractor;

        private readonly string[] _parserInputFiles = {
            "NScliData_CZ.NOS",
            "NScliData_DE.NOS",
            "NScliData_ES.NOS",
            "NScliData_FR.NOS",
            "NScliData_IT.NOS",
            "NScliData_PL.NOS",
            "NScliData_RU.NOS",
            "NScliData_TR.NOS",
            "NScliData_UK.NOS",
            "NSlangData_CZ.NOS",
            "NSlangData_DE.NOS",
            "NSlangData_ES.NOS",
            "NSlangData_FR.NOS",
            "NSlangData_IT.NOS",
            "NSlangData_PL.NOS",
            "NSlangData_RU.NOS",
            "NSlangData_TR.NOS",
            "NSlangData_UK.NOS",
            "NStcData.NOS",
            "NSgtdData.NOS"
        };

        public TimeHandlingService(IServiceProvider services, IClientDownloader client, IExtractor extractor)
        {
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _aTimer.Elapsed += UploadInputFiles;
            _aTimer.Start();
            _client = client;
            _extractor = extractor;
        }

        public async Task UploadInputFilesAsync()
        {
            using var client = new AmazonS3Client(new BasicAWSCredentials(
                Environment.GetEnvironmentVariable("S3_ACCESS_KEY"),
                Environment.GetEnvironmentVariable("S3_SECRET_KEY")), RegionEndpoint.USWest2);

            var manifest = await _client.DownloadManifest();
            manifest.Entries = manifest.Entries.Select(s =>
            {
                s.File = s.File.Replace('\\', Path.DirectorySeparatorChar);
                return s;
            }).ToArray();
            var fileslist = _parserInputFiles.Select(o => $"NostaleData{Path.DirectorySeparatorChar}{o}").ToList();
            manifest.Entries = manifest.Entries.Where(s => fileslist.Contains(s.File)).ToArray();

            var request = new GetObjectRequest
            {
                BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"),
                Key = "clientmanifest.json",
            };
            ClientManifest previousManifest;
            try
            {
                {
                    using var response = await client.GetObjectAsync(request);
                    await using var responseStream = response.ResponseStream;
                    using var reader = new StreamReader(responseStream);
                    previousManifest = JsonConvert.DeserializeObject<ClientManifest>(await reader.ReadToEndAsync());
                }
            }
            catch
            {
                previousManifest = new ClientManifest()
                {
                    Entries = new Entry[0]
                };
            }

            var previoussha1s = previousManifest.Entries.Select(s => s.Sha1);
            if (true || !manifest.Entries.Select(s => s.Sha1).All(s => previoussha1s.Contains(s)))
            {
                var archiveName =
                    $".{Path.DirectorySeparatorChar}output{Path.DirectorySeparatorChar}parser-input-files.tar.bz2";

                await _client.DownloadClientAsync(manifest);
                await Task.WhenAll(fileslist.Select(file =>
                {
                    var rename = file.Contains("NScliData");
                    var dest = file.Contains("NStcData")
                        ? $".{Path.DirectorySeparatorChar}output{Path.DirectorySeparatorChar}parser{Path.DirectorySeparatorChar}maps{Path.DirectorySeparatorChar}"
                        : $".{Path.DirectorySeparatorChar}output{Path.DirectorySeparatorChar}parser{Path.DirectorySeparatorChar}";
                    var fileInfo =
                        new FileInfo($".{Path.DirectorySeparatorChar}output{Path.DirectorySeparatorChar}{file}");
                    return _extractor.ExtractAsync(fileInfo, dest, rename);
                }));

                var directoryOfFilesToBeTarred =
                    $".{Path.DirectorySeparatorChar}output{Path.DirectorySeparatorChar}parser";
                var filesInDirectory = Directory.GetFiles(directoryOfFilesToBeTarred, "*.*", SearchOption.AllDirectories);

                if (File.Exists(archiveName))
                {
                    File.Delete(archiveName);
                }

                await Task.Delay(10000);
                {
                    await using var targetStream = new BZip2OutputStream(File.Create(archiveName));
                    using var tarArchive =
                        TarArchive.CreateOutputTarArchive(targetStream, TarBuffer.DefaultBlockFactor);
                    foreach (var file in filesInDirectory)
                    {
                        var entry = TarEntry.CreateEntryFromFile(file);
                        tarArchive.WriteEntry(entry, true);
                    }
                }

                var emptyfile = JsonConvert.SerializeObject(manifest);
                {
                    await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(emptyfile));
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"),
                        Key = "clientmanifest.json",
                        ContentType = "text/json",
                        InputStream = stream
                    };
                    await client.PutObjectAsync(putRequest);
                }

                if (_discord.GetChannel(719772084968095775) is SocketTextChannel channel)
                {
                    var file = new FileInfo(archiveName);
                    if (file.Length > 8388119)
                    {
                        var send = await channel.SendMessageAsync($"<:altz:699420721088168036><:altz:699420721088168036><:altz:699420721088168036>Parser Too Heavy<:altz:699420721088168036><:altz:699420721088168036><:altz:699420721088168036>\n - Size : {file.Length}");
                    }
                    else
                    {
                        var alq = string.Concat(Enumerable.Repeat("<:altq:699420721130242159>", 20));
                        var send = await channel.SendFileAsync(archiveName, $"{alq}\n<:altp:699420720819732651><:altp:699420720819732651><:altp:699420720819732651><:altp:699420720819732651><:altp:699420720819732651><:altp:699420720819732651>PARSER FILES GENERATED<:altp:699420720819732651><:altp:699420720819732651><:altp:699420720819732651><:altp:699420720819732651><:altp:699420720819732651><:altp:699420720819732651>\n{alq}");
                    }
                }
            }
        }

        private void UploadInputFiles(object source, ElapsedEventArgs e)
        {
            Task.Run(UploadInputFilesAsync);
        }
    }
}
