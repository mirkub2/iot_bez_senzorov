using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ServiceBus.Messaging;

namespace UlozenieDatDoUloziska
{
    class Program
    {
        static void Main(string[] args)
        {
            string iotHubConnectionString = "HostName=mojIotHub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=ARb0jyaGZQax2kBMD7rw6rMIFBZ86et7cKHrrdRp6KE=";
            string iotHubEndpoint = "messages/events";
            SpracovanieDat.StorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=iotarchiv;AccountKey=H9v0iRl3p/TT7qDDw69WCYQDjmjNT8oYvV/nWQ4kvBLod9epKnHKWXd5TKmkuWB6evQR+NWH8bA1aP4h4jVoPg==;BlobEndpoint=https://iotarchiv.blob.core.windows.net/;TableEndpoint=https://iotarchiv.table.core.windows.net/;QueueEndpoint=https://iotarchiv.queue.core.windows.net/;FileEndpoint=https://iotarchiv.file.core.windows.net/";
            
            string eventProcessorHostName = Guid.NewGuid().ToString();
            EventProcessorHost eventProcessorHost = new EventProcessorHost(eventProcessorHostName, iotHubEndpoint, EventHubConsumerGroup.DefaultGroupName, iotHubConnectionString, SpracovanieDat.StorageConnectionString, "messages-events");
            Console.WriteLine("Registrovanie EventProcessor-a...");
            eventProcessorHost.RegisterEventProcessorAsync<SpracovanieDat>().Wait();
            
            Console.WriteLine("Prijimam. Potvrdte klavesu Enter na zastavenie.");
            Console.ReadLine();
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();
        }
    }
}
