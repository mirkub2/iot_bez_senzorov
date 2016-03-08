using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Threading;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simulacia_zdroja
{

    class Program
    {
        static DeviceClient zdrojKlient;
        static string iotHubUri = "mojIotHub.azure-devices.net";
        static string zdrojID = "mojaWebAplikacia";
        static string zdrojKluc = "ESeU8ygRJfHlKhC3YW/cUSpOlUIt8XJM+xqLQOFbYmk=";

        static string[] geooblasti = new[] { "Zapad", "Stred", "Vychod" };
        static string[] pouzivatelia = new[] { "Pat", "Mat", "Lolek", "Bolek", "Bob", "Bobek", "Matko", "Kubko","Amalka", "Emanuel"};

        private static async void PosliSpravuDoIotHubuAsync()
        {
            Random rand = new Random();
            while (true)
            {
                //nahodny vyber pouzivatela
                string pouzivatel = pouzivatelia [rand.Next(0, pouzivatelia.Length)];
                //nahodny vyber geooblasti
                string geooblast =  geooblasti [rand.Next(0, geooblasti .Length)];

                //vytvorenie datoveho objektu, ktory sa posle do IoT Hub-u
                var telemetrickyDatovyObjekt = new
                {
                    deviceId = zdrojID,
                    idPouzivatela = pouzivatel,
                    idOblasti= geooblast 
                };
                //serializacie objektu do JSON pred odoslanim
                var spravaString = JsonConvert.SerializeObject(telemetrickyDatovyObjekt);
                var sprava = new Message(Encoding.ASCII.GetBytes(spravaString));
                //odoslanie telemetrickych dat v JSON formate na IoT Hub
                await zdrojKlient .SendEventAsync(sprava);
                //vypis odosielanych sprav na strane zdroja dat
                Console.WriteLine("{0} > Odosielanie spravy: {1}", DateTime.Now, spravaString );
                //simulovanie pozdrzania odoslania dalsej spravy 5 sekund
                Thread.Sleep(5000);
            }
        }

        static void Main(string[] args)
        {
            zdrojKlient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(zdrojID , zdrojKluc ), TransportType.Http1 );
            PosliSpravuDoIotHubuAsync();
            Console.ReadLine();
        }
    }
}
