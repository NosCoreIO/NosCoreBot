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
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NosCore.ParserInputGenerator.Downloader;
using NosCore.ParserInputGenerator.Extractor;
using NosCore.Shared.Enumerations;
using NosCoreBot.Modules;

namespace NosCoreBot.Services
{
    public class TimeHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;
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
            _services = services;
            _aTimer.Elapsed += new ElapsedEventHandler(UploadInputFiles);
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
            var fileslist = _parserInputFiles.Select(o => $"NostaleData\\{o}").ToList();
            manifest.Entries = manifest.Entries.Where(s => fileslist.Contains(s.File)).ToArray();

            var request = new GetObjectRequest
            {
                BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"),
                Key = "clientmanifest.json",
            };
            ClientManifest previousManifest;
            try
            {
                using GetObjectResponse response = await client.GetObjectAsync(request);
                await using Stream responseStream = response.ResponseStream;
                using StreamReader reader = new StreamReader(responseStream);
                previousManifest = JsonConvert.DeserializeObject<ClientManifest>(await reader.ReadToEndAsync());

            }
            catch
            {
                previousManifest = new ClientManifest()
                {
                    Entries = new Entry[0]
                };
            }
            var previoussha1s = previousManifest.Entries.Select(s => s.Sha1);
            if (!manifest.Entries.Select(s => s.Sha1).All(s => previoussha1s.Contains(s)))
            {
                var emptyfile = JsonConvert.SerializeObject(manifest);
                await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(emptyfile));
                var putRequest = new PutObjectRequest
                {
                    BucketName = Environment.GetEnvironmentVariable("S3_BUCKET"),
                    Key = "clientmanifest.json",
                    ContentType = "text/json",
                    InputStream = stream
                };

                await client.PutObjectAsync(putRequest);
                if (_discord.GetChannel(719772084968095775) is SocketTextChannel channel)
                {
                    await channel.SendMessageAsync("ClientManifest Regenerated");
                }
            }
        }

        private void UploadInputFiles(object source, ElapsedEventArgs e)
        {
            Task.Run(UploadInputFilesAsync);
        }
    }
}
