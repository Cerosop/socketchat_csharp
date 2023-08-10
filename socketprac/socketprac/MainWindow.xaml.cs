using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using Newtonsoft.Json;

namespace socketprac
{
    public class Message
    {
        public string content { get; set; }
    }
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ser = false;
        }
        

        private Socket server;
        static readonly ManualResetEvent serverStarted = new ManualResetEvent(false);
        static readonly List<Socket> clients = new List<Socket>();
        private bool ser = false;
        static readonly object clientSocketsLock = new object();

        private void HandleClients()
        {
            while (ser)
            {
                Console.WriteLine("test2");
                try
                {
                    Socket client = server.Accept();

                    Console.WriteLine($"Client connected: {client.RemoteEndPoint}");
                    clients.Add(client);
                    byte[] buffer = new byte[1024];
                    int bytesRead = client.Receive(buffer);
                    string name = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    Dispatcher.Invoke(() => {
                        textblock1.Text = textblock1.Text + name + $"({client.RemoteEndPoint})connect\n";
                    });

                    Thread clientThread = new Thread(() => HandleClient(client, name));
                    clientThread.Start();
                }
                catch (SocketException)
                {
                    break;
                }
            }
        }

        private void HandleClient(Socket client, string name)
        {
            byte[] buffer = new byte[1024];
            string clientName = client.RemoteEndPoint.ToString();
            Console.WriteLine("ready");
            while (true)
            {
                try
                {
                    int bytesRead = client.Receive(buffer);
                    
                    if (bytesRead == 0) // Client has disconnected
                    {
                        Console.WriteLine($"Client {clientName} has disconnected1.");
                        break;
                    }
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Message message1 = JsonConvert.DeserializeObject<Message>(message);
                    if (message1.content == "client close")
                    {
                        clients.Remove(client);
                        client.Close();
                        
                        break;
                    }
                    Console.WriteLine($"[{clientName}]: {message1.content}");

                    Dispatcher.Invoke(() => {
                        textblock1.Text = textblock1.Text + name + $"({client.RemoteEndPoint}):{message1.content}\n";
                    });
                    BroadcastMessage(name + $":{message1.content}", client);
                }
                
                catch (SocketException)
                {
                    Console.WriteLine($"Client {clientName} has disconnected unexpectedly1.");
                    break;
                }
            }
            Console.WriteLine($"Client {clientName} disconnected1.");
            if (ser)
            {
                Dispatcher.Invoke(() => {
                    textblock1.Text = textblock1.Text + name + " disconnect\n";
                });
            }
        }
        static void BroadcastMessage(string message, Socket excludeClient)
        {
            
            Message message1 = new Message { content = message };
            string jsonString = JsonConvert.SerializeObject(message1);
            byte[] messageBytes = Encoding.ASCII.GetBytes(jsonString);

            foreach (Socket client in clients)
            {
                if (client != excludeClient)
                {
                    client.Send(messageBytes);
                }
            }
        }


        private void server_b_Click(object sender, RoutedEventArgs e)
        {
            if(ser == false)
            {
                textblock1.Text = textblock1.Text + "Server Start\n";
                ser = true;
                IPAddress ip = IPAddress.Parse(ip_t.Text);
                int port = int.Parse(port_t.Text);
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                server.Bind(new IPEndPoint(ip, port));
                server.Listen(10);
                serverStarted.Set();
                Console.WriteLine("test");

                Thread serverThread = new Thread(HandleClients);
                serverThread.Start();
            }
            else
            {
                textblock1.Text = textblock1.Text + "server already started\n";
            }
        }

        private void listen_b_Click(object sender, RoutedEventArgs e)
        {
            if(ser == true)
            {
                ser = false;
                
                foreach (Socket client in clients)
                {
                    Message message1 = new Message { content = "shutdown" };
                    string jsonString = JsonConvert.SerializeObject(message1);
                    byte[] messageBytes = Encoding.ASCII.GetBytes(jsonString);
                    client.Send(messageBytes);
                    client.Close();
                }
                clients.Clear();
                server.Close();
                Dispatcher.Invoke(() => {
                    textblock1.Text = textblock1.Text + "server close\n";
                });
            }
            else
            {
                Dispatcher.Invoke(() => {
                    textblock1.Text = textblock1.Text + "server is closed\n";
                });
            }
        }
    }
}
//test