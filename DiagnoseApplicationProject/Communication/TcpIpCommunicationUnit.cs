using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace RBC
{
    public class TcpIpCommunicationUnit
    {

        Boolean shutDown = false;

        //eventhandler
        public delegate void StatusChangedEventHandler(String statusMessage);
        public event StatusChangedEventHandler statusChangedEvent;

        public delegate void ErrorEventHandler(String errorMessage);
        public event ErrorEventHandler errorEvent;

        public delegate void MessageReceivedEventHandler(String receivedMessage);
        public event MessageReceivedEventHandler messageReceivedEvent;

        public delegate void UpdateTextCallback(string text);

        String communicationName = "Unknown";

        System.Threading.Thread listeningThread;
        System.Threading.Thread clientReceiveThread;

        System.Net.Sockets.TcpListener tcpserver;
        System.Net.Sockets.TcpClient tcpclient;

        public String configfilename = "";
        public NetworkConfig networkConfig = null;
        public RBC.Configuration dllConfiguration = null;

        public TcpIpCommunicationUnit(String pCommunicationName)
        {
            this.communicationName = pCommunicationName;

            this.configfilename = pCommunicationName + "_NetworkConfig.xml";

            this.loadNetworkConfiguration();
        }

        public void StartServer()
        {
            try
            {
                this.tcpserver = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Parse(this.networkConfig.ipAddress), this.networkConfig.port);
                this.tcpserver.Start();
                this.listeningThread = new System.Threading.Thread(new System.Threading.ThreadStart(listenForClients));
                this.listeningThread.Start();
                if (this.statusChangedEvent != null)
                    this.statusChangedEvent(this.communicationName + ": Server started. Listening for Clients at Port - " + this.networkConfig.port);

            }
            catch (System.Net.Sockets.SocketException ex)
            {
                if (this.errorEvent != null)
                    this.errorEvent(this.communicationName + ": " + ex.ToString());
            }
        }

        public void closeAllConnections()
        {
            this.shutDown = true;

            if (this.listeningThread != null)
            {
                if (this.listeningThread.ThreadState == System.Threading.ThreadState.Running)
                {
                    this.listeningThread.Abort();
                    this.tcpserver.Stop();
                }
            }

            if (this.clientReceiveThread != null)
            {
                if (this.clientReceiveThread.ThreadState == System.Threading.ThreadState.Running)
                {
                    this.clientReceiveThread.Abort();
                    this.tcpclient.Close();
                }
            }
        }

        public void listenForClients()
        {
            while (this.shutDown == false)
            {
                this.tcpclient = this.tcpserver.AcceptTcpClient();

                //if thread is created and allready running abort it before accept new client
                if (this.clientReceiveThread != null)
                {
                    if (this.clientReceiveThread.ThreadState == System.Threading.ThreadState.Running)
                    {
                        this.clientReceiveThread.Abort();
                    }
                }

                this.clientReceiveThread = new System.Threading.Thread(new System.Threading.ThreadStart(receiveFromClient));
                this.clientReceiveThread.Start();

                if (this.statusChangedEvent != null)
                    this.statusChangedEvent(this.communicationName + ": New client connected. Start receiving");
            }
        }

        //receive a message if possible and create event if succesfully received
        public void receiveFromClient()
        {
            while (this.shutDown == false)
            {
                try
                {
                    System.Net.Sockets.NetworkStream ns = this.tcpclient.GetStream();
                    byte[] buffer = new byte[8192];

                    int data = ns.Read(buffer, 0, buffer.Length);

                    if (data > 0)
                    {
                        //convert buffer into Ascii String
                        String receivedMessage = System.Text.Encoding.ASCII.GetString(buffer, 0, data);

                        String[] splitstring = null;

                        if (receivedMessage.IndexOf('\r') != (-1))
                        {
                            splitstring = receivedMessage.Split('\r');
                        }
                        else
                        {
                            splitstring = receivedMessage.Split(';');
                        }


                        foreach (var stringmessage in splitstring)
                        {
                            //check if string is empty after split
                            if (stringmessage != "")
                            {
                                if (this.statusChangedEvent != null)
                                    this.statusChangedEvent(this.communicationName + ": Message received from Client - " + receivedMessage);



                                //fire the receiveevent
                                if (this.messageReceivedEvent != null)
                                  this.messageReceivedEvent(receivedMessage);
                            }
                        }
                    }

                    System.Threading.Thread.Sleep(5);
                }
                catch(Exception ex)
                {
                    if (this.errorEvent != null)
                        this.errorEvent(this.communicationName + ": " + ex.ToString());

                    System.Threading.Thread.Sleep(5);
                }
            }
        }

        public void sendToClient(String message)
        {
            try
            {
                System.Net.Sockets.NetworkStream ns = this.tcpclient.GetStream();

                byte[] sendbuffer = System.Text.Encoding.ASCII.GetBytes(message);
                ns.Write(sendbuffer, 0, sendbuffer.Length);

                if (this.statusChangedEvent != null)
                    this.statusChangedEvent(this.communicationName + ": Message send to Client - " + message);
            }
            catch (Exception ex)
            {
                if (this.errorEvent != null)
                    this.errorEvent(this.communicationName + ": " + ex.ToString());
            }
        }

        public void saveNetworkConfiguration()
        {
            String savefilename = this.configfilename;

            System.IO.FileStream outFile = System.IO.File.Create(savefilename);
            System.Xml.Serialization.XmlSerializer formatter = new System.Xml.Serialization.XmlSerializer(typeof(NetworkConfig));
            formatter.Serialize(outFile, this.networkConfig);
            outFile.Close();
        }

        public void loadNetworkConfiguration()
        {
            String loadfilename = this.configfilename;

            //if (System.IO.File.Exists(loadfilename))
            //{
            //    System.Xml.Serialization.XmlSerializer formatter = new System.Xml.Serialization.XmlSerializer(typeof(NetworkConfig));
            //    System.IO.FileStream aFile = new System.IO.FileStream(loadfilename, System.IO.FileMode.Open);
            //    byte[] buffer = new byte[aFile.Length];
            //    aFile.Read(buffer, 0, (int)aFile.Length);
            //    System.IO.MemoryStream stream = new System.IO.MemoryStream(buffer);
            //    this.networkConfig = ((NetworkConfig)formatter.Deserialize(stream));
            //    aFile.Close();
            //    stream.Close();
            //}
            //else
            //{
                //if there is no data available clean it up
                this.networkConfig = new NetworkConfig();
                this.networkConfig.ipAddress = "0.0.0.0";
                this.networkConfig.port = 8008;
                //save the stadardconfig
                this.saveNetworkConfiguration();
                this.dllConfiguration = new RBC.Configuration();
                this.dllConfiguration.debuggingActive = false;

            //}
        }
    }
}
