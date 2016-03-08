using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;


namespace UlozenieDatDoUloziska
{
    class SpracovanieDat : IEventProcessor
    {
        private const int MAX_VELKOST_BLOKU = 4 * 1024 * 1024;
        public static string StorageConnectionString;

        private CloudBlobClient blobKlient;
        private CloudBlobContainer blobKontajner;

        private long aktualnyOffsetInitBloku;
        private MemoryStream naPridanie = new MemoryStream(MAX_VELKOST_BLOKU);

        private Stopwatch stopwatch;
        //ak sa nedosiahne MAX_BLOCK_SIZE skor, zapisu sa data do Storage kazdych 5 minut
        private TimeSpan MAX_CHECKPOINT_CAS = TimeSpan.FromMinutes (5);

        public SpracovanieDat()
        {
            var storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
            blobKlient = storageAccount.CreateCloudBlobClient();
            blobKontajner = blobKlient.GetContainerReference("mojiotarchiv");
            blobKontajner.CreateIfNotExists();
        }


        Task IEventProcessor.CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine("Procesor sa vypina. Particia '{0}', Pricina: '{1}'.", context.Lease.PartitionId, reason);
            return Task.FromResult<object>(null);
        }

        Task IEventProcessor.OpenAsync(PartitionContext context)
        {
            Console.WriteLine("StoreEventProcessor inicializovany.  Particia: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset);

            if (!long.TryParse(context.Lease.Offset, out aktualnyOffsetInitBloku))
            {
                aktualnyOffsetInitBloku = 0;
            }
            stopwatch = new Stopwatch();
            stopwatch.Start();

            return Task.FromResult<object>(null);
        }

        async Task IEventProcessor.ProcessEventsAsync(PartitionContext kontext, IEnumerable<EventData> spravy)
        {
            foreach (EventData eventData in spravy)
            {
                //do JSON objektu chceme pridat aj cas prijatia
                //preto prijatu JSON strukturu deserializujeme a pridame novu vlastnost 'cas'
                dynamic zdrojovyJson = Newtonsoft.Json.JsonConvert.DeserializeObject(System.Text.Encoding.ASCII.GetString(eventData.GetBytes()), typeof(object));
                Newtonsoft.Json.Linq.JObject zdrojovyJsonObjekt = new Newtonsoft.Json.Linq.JObject(zdrojovyJson);
                zdrojovyJsonObjekt.Add("cas", eventData.EnqueuedTimeUtc);
                var upravenyJson = Newtonsoft.Json.JsonConvert.SerializeObject( zdrojovyJsonObjekt, Newtonsoft.Json.Formatting.Indented);

                //konverzia na pole bajtov pre zapis do uloziska
                byte[] data = System.Text.Encoding.ASCII.GetBytes(upravenyJson); 
              
                if (naPridanie.Length + data.Length > MAX_VELKOST_BLOKU || stopwatch.Elapsed > MAX_CHECKPOINT_CAS)
                {
                    await PridajAVyvolajCheckpoint(kontext);
                }
                await naPridanie.WriteAsync(data, 0, data.Length);

                Console.WriteLine(string.Format("Sprava prijata.  Particia: '{0}', Data: '{1}'",
                  kontext.Lease.PartitionId, Encoding.UTF8.GetString(data)));
            }
        }

        private async Task PridajAVyvolajCheckpoint(PartitionContext kontext)
        {
            var blokIdString = String.Format("startSeq:{0}", aktualnyOffsetInitBloku.ToString("0000000000000000000000000"));
            var blokId = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(blokIdString));
            naPridanie.Seek(0, SeekOrigin.Begin);
            byte[] md5 = MD5.Create().ComputeHash(naPridanie);
            naPridanie.Seek(0, SeekOrigin.Begin);

            var nazovBlobu = String.Format("iothub_{0}", kontext.Lease.PartitionId);
            var aktualnyBlob = blobKontajner.GetBlockBlobReference(nazovBlobu);

            if (await aktualnyBlob.ExistsAsync())
            {
                await aktualnyBlob.PutBlockAsync(blokId, naPridanie, Convert.ToBase64String(md5));
                var blokList = await aktualnyBlob.DownloadBlockListAsync();
                var novyBlokList = new List<string>(blokList.Select(b => b.Name));

                if (novyBlokList.Count() > 0 && novyBlokList.Last() != blokId)
                {
                    novyBlokList.Add(blokId);
                    VypisZvyraznenuSpravu(String.Format("Pridavam blok id: {0} do blobu: {1}", blokIdString, aktualnyBlob.Name));
                }
                else
                {
                    VypisZvyraznenuSpravu(String.Format("Prepisujem blok id: {0}", blokIdString));
                }
                await aktualnyBlob.PutBlockListAsync(novyBlokList);
            }
            else
            {
                await aktualnyBlob.PutBlockAsync(blokId, naPridanie, Convert.ToBase64String(md5));
                var novyBlokList = new List<string>();
                novyBlokList.Add(blokId);
                await aktualnyBlob.PutBlockListAsync(novyBlokList);
                VypisZvyraznenuSpravu(String.Format("Vytvaram novy blob", aktualnyBlob.Name));
            }
            naPridanie.Dispose();
            naPridanie = new MemoryStream(MAX_VELKOST_BLOKU);

            // checkpoint
            await kontext.CheckpointAsync();
            VypisZvyraznenuSpravu(String.Format("Checkpoint particie: {0}", kontext.Lease.PartitionId));

            aktualnyOffsetInitBloku = long.Parse(kontext.Lease.Offset);
            stopwatch.Restart();
        }

        private void VypisZvyraznenuSpravu(string sprava)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(sprava);
            Console.ResetColor();
        }
    }
}
