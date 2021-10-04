using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

//we should alter to be able to accept as many arbitrary sized chunks as we like

class Program
{
    private static TcpListener listener;
    private static bool listen = true;
            
    public static void Main()
    {
        Client.Init(41, 22050, 8);

        listener = new TcpListener(IPAddress.Any, 42069);
        listener.Start();

        List<Client> deadList = new List<Client>();

        while (listen)
        {
            while(listener.Pending())
            {
                var c = new Client(listener.AcceptTcpClient().GetStream());
                Client.ClientsList.Add(c);

                Console.Write($"{c} connected!");
            }

            foreach (Client client in Client.ClientsList)
            {
                if(DateTime.UtcNow - client.MostRecentMessage > TimeSpan.FromSeconds(30))
                {
                    deadList.Add(client);
                    continue;
                }
                
                if(client.HasMessages)
                {
                    client.HandleMessages();
                }

                //if(client.NeedsConfig)
                //{
                //    client.SendMessage_Config();
                //}

                //if (client.NeedsAudio)
                //{
                //    //calculate audio for this client
                //    client.SendMessage_Audio();
                //}

                //if(client.NeedsPoseData)
                //{
                //    client.SendMessage_Pose();//not implemented
                //}
            }

            while(deadList.Count > 0)
            {
                deadList[0].Dispose();
                deadList.RemoveAt(0);
            }


            //Task.Delay(15).Wait();

            //Console.WriteLine("\n loop \n");
        }

        Console.WriteLine("Program Terminated");
    }
}