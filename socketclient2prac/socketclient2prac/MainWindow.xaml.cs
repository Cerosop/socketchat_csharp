using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace socketclient2prac
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
            btn_connect.Content = "connect";
            conn = false;
        }

        private Socket client;
        private bool conn = false;

        private void connect_b_Click(object sender, RoutedEventArgs e)
        {
            if (conn == false)
            {
                try
                {
                    conn = true;
                    btn_connect.Content = "disconnect";

                    IPAddress ip = IPAddress.Parse(ip_t.Text);
                    int port = int.Parse(port_t.Text);

                    client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    client.Connect(ip, port);
                    testblock1.Text = testblock1.Text + "welcome " + name_t.Text + "\n";
                    string clientName = name_t.Text;
                    byte[] messageBytes = Encoding.ASCII.GetBytes(clientName);
                    client.Send(messageBytes);

                    Thread receiveThread = new Thread(ReceiveMessages);
                    receiveThread.Start();

                    Console.WriteLine("Connected to server. Start typing messages...");
                }
                catch (SocketException)
                {
                    conn = false;
                    btn_connect.Content = "connect";
                    testblock1.Text = testblock1.Text + "server doesn`t exist\n";
                }
                catch (FormatException)
                {
                    conn = false;
                    btn_connect.Content = "connect";
                    testblock1.Text = testblock1.Text + "ip illegal\n";
                }

            }
            else
            {
                conn = false;
                Message message1 = new Message { content = "client close" };
                string jsonString = JsonConvert.SerializeObject(message1);
                byte[] messageBytes = Encoding.ASCII.GetBytes(jsonString);
                client.Send(messageBytes);
                client.Close();
                btn_connect.Content = "connect";
                testblock1.Text = testblock1.Text + "disconnect success\n";

            }
        }
        private void send_b_Click(object sender, RoutedEventArgs e)
        {
            if (conn)
            {
                String message = message_t.Text;
                Message message1 = new Message { content = message };
                string jsonString = JsonConvert.SerializeObject(message1);
                byte[] messageBytes = Encoding.ASCII.GetBytes(jsonString);
                client.Send(messageBytes);

                Dispatcher.Invoke(() => {
                    testblock1.Text = testblock1.Text + "you send:" + message_t.Text + "\n";
                });
            }
            else
            {
                Dispatcher.Invoke(() => {
                    testblock1.Text = testblock1.Text + "send fail\n";
                });
            }

        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];

            while (true)
            {
                try
                {
                    int bytesRead = client.Receive(buffer);
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Message message1 = JsonConvert.DeserializeObject<Message>(message);

                    Console.WriteLine(message1.content);
                    if (message1.content.ToLower() == "shutdown")
                    {
                        client.Close();
                        Dispatcher.Invoke(() => {
                            btn_connect.Content = "connect";
                            testblock1.Text = testblock1.Text + "server disconnected\n";
                        });
                        conn = false;
                        break;
                    }
                    Dispatcher.Invoke(() => {
                        testblock1.Text = testblock1.Text + message1.content + "\n";
                    });

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    break;
                }
            }
        }
    }
}
