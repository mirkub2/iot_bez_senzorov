using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace registracia_zdroja
{
    class Program
    {
        static RegistryManager registryManager;
        static string connectionString = "HostName=mojIotHub.azure-devices.net;SharedAccessKeyName=registryReadWrite;SharedAccessKey=/xi2M1tyCcV5SNx04nP8i16Xb/vW2iQ8xztEoewP0M8=";


        private async static Task PridajZdrojDatAsync()
        {
            //zdroj dat bude v IoT Hub-e nasledujucou hodnotou
            string zdrojId = "mojaWebAplikacia";
            Device zdroj;
            try
            {
                //zaregistrovanie noveho zdroja dat 
                zdroj = await registryManager.AddDeviceAsync(new Device(zdrojId));
            }
            catch (DeviceAlreadyExistsException)
            {
                //nacitanie objektu, ak je uz zdroj v IoT Hub-e zaregistrovany 
                zdroj = await registryManager.GetDeviceAsync(zdrojId);
            }
            Console.WriteLine("Vygenerovany kluc zdroja dat: {0}", zdroj.Authentication.SymmetricKey.PrimaryKey);
        }

        static void Main(string[] args)
        {
            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            PridajZdrojDatAsync().Wait();
            Console.ReadLine();
        }
    }
}
