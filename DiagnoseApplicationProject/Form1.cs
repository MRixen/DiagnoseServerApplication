using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Windows.Threading;


namespace WindowsFormsApplication6
{
    public partial class Form1 : Form
    {

        private System.IO.StreamWriter writer = new System.IO.StreamWriter("DiagnoseDebugLog.log", true);
        private RBC.TcpIpCommunicationUnit tcpDiagnoseServer = null;
        private String dllConfigurationFileName = "";
        //private RBC.Configuration dllConfiguration = null;

        public Form1()
        {
            InitializeComponent();
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            tcpDiagnoseServer = new RBC.TcpIpCommunicationUnit("DiagnoseServer");
            //register the callbackevents from tcpservers
            tcpDiagnoseServer.messageReceivedEvent += new RBC.TcpIpCommunicationUnit.MessageReceivedEventHandler(tcpDiagnoseServer_messageReceivedEvent);            
            
            tcpDiagnoseServer.errorEvent += new RBC.TcpIpCommunicationUnit.ErrorEventHandler(tcpPLCServer_errorEvent);
            tcpDiagnoseServer.statusChangedEvent += new RBC.TcpIpCommunicationUnit.StatusChangedEventHandler(tcpDiagnoseServer_statusChangedEvent);
            tcpDiagnoseServer.StartServer();
        }

        private void UpdateText(string text)
        {
            textBox1.Text = text;
        }

        void tcpDiagnoseServer_statusChangedEvent(string statusMessage)
        {
            try
            {
                if (tcpDiagnoseServer.dllConfiguration.debuggingActive == true)
                {
                    this.writer.WriteLine("Statuschange - Diagnose: " + statusMessage);
                    this.writer.Flush();
                }
            }
            catch (Exception ex)
            {

            }
        }

        //public void SetConfigurationPath(String configurationpath)
        //{
        //    dllConfigurationFileName = configurationpath;

        //    this.loadConfiguration();
        //}

        void tcpPLCServer_errorEvent(string errorMessage)
        {
            System.Windows.Forms.MessageBox.Show("Error occured - " + errorMessage);
        }

        void tcpDiagnoseServer_messageReceivedEvent(string receivedMessage)
        {
            Debug.WriteLine("Message: " + receivedMessage);
            if (tcpDiagnoseServer.dllConfiguration.debuggingActive == true)
            {
                this.writer.WriteLine("Received from PLC: " + receivedMessage);
                this.writer.Flush();
            }

            //remove all data from the ; symbol to the end
            Int32 index = receivedMessage.IndexOf(';');

            if (index > 0)
            {
                receivedMessage = receivedMessage.Remove(index);
            }

            String[] splitmessage = receivedMessage.Split(':');

            //check if the message contains at least 1 element
            if (splitmessage.Length < 1)
            {
                return;
            }

            this.handleMessageDiagnose(splitmessage);
        }



        private void handleMessageDiagnose(String[] messageArray)
        {

            textBox1.Invoke(new RBC.TcpIpCommunicationUnit.UpdateTextCallback(this.UpdateText), 
            new object[]{"Text generated on non-UI thread."});

            switch (messageArray[0])
            {
                //Handle BeltInPos message from PLC
                case "BeltInPos":

                    break;
                case "DoorRequest":

                    break;
            }
        }

        //private void loadConfiguration()
        //{
        //    //if (System.IO.File.Exists(this.dllConfigurationFileName))
        //    //{
        //    //    System.Xml.Serialization.XmlSerializer formatter = new System.Xml.Serialization.XmlSerializer(typeof(RBC.Configuration));
        //    //    System.IO.FileStream aFile = new System.IO.FileStream(this.dllConfigurationFileName, System.IO.FileMode.Open);
        //    //    byte[] buffer = new byte[aFile.Length];
        //    //    aFile.Read(buffer, 0, (int)aFile.Length);
        //    //    System.IO.MemoryStream stream = new System.IO.MemoryStream(buffer);
        //    //    this.dllConfiguration = (RBC.Configuration)formatter.Deserialize(stream);
        //    //    aFile.Close();
        //    //    stream.Close();
        //    //}
        //    //else
        //    //{

        //        this.dllConfiguration = new RBC.Configuration();
        //        this.dllConfiguration.debuggingActive = false;
        //    //}
        //}

        public void SendMessageToFeedingDevice(string message)
        {
            if (tcpDiagnoseServer.dllConfiguration.debuggingActive == true)
            {
                this.writer.WriteLine("Send to PLC: " + message);
                this.writer.Flush();
            }

            this.tcpDiagnoseServer.sendToClient(message);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            tcpDiagnoseServer.closeAllConnections();
        }

    }
}
