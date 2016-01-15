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
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing.Imaging;
using System.Xml;


namespace WindowsFormsApplication6
{
    public partial class Diagnose : Form
    {
        //TODO Check algorithm to split the received string (receiveFromClient(), handleMessage())
        //TOOD Add diagram
        // Save all cycletime-data with article id

        private System.IO.StreamWriter writer = new System.IO.StreamWriter("DiagnoseDebugLog.log", true);
        private RBC.TcpIpCommunicationUnit tcpDiagnoseServer = null;
        private String dllConfigurationFileName = "";
        //private RBC.Configuration dllConfiguration = null;
        private int xAxisRate = 1;
        private Stopwatch stopWatch2 = new Stopwatch();
        private long stopWatchOld = 0;
        private int MIN_X_INCREMENT = 1;
        private int MAX_X_INCREMENT = 10;
        private bool pauseIsActive = false;
        private String machineName = "";
        private String projectName = "";
        private int MAX_CYCLES = 50;
        private int MIN_CYCLES = 1;
        private int cyclesToAcquire = 10;
        private int currentCycle = 1;
        private bool firtStart;
        private string graphName = "x";
        private NotifyIcon notifyIcon;


        public Diagnose()
        {
            InitializeComponent();
            firtStart = false;
            textBox1.Text = cyclesToAcquire.ToString();
            // label1.Text = "Bereich: " + MIN_X_INCREMENT + " - " + MAX_X_INCREMENT;
            notifyIcon = new NotifyIcon();
            state_icon.BackColor = Color.OrangeRed;
        }

        private void setTitle(string name)
        {
            this.graphName = name;
            Title title = chart1.Titles.Add("CycleTime of " + name);
            //title.Font = new System.Drawing.Font("Arial", 16, FontStyle.Bold);

            
        }

        private void startButtonClicked(object sender, EventArgs e)
        {
            cyclesToAcquire = Int32.Parse(textBox1.Text);
            currentCycle = 0;
            label_current_cycles.Text = "Cycles: " + currentCycle.ToString() + "/" + cyclesToAcquire;
            if (cyclesToAcquire > MAX_CYCLES) cyclesToAcquire = MAX_CYCLES;
            if (cyclesToAcquire < MIN_CYCLES) cyclesToAcquire = MIN_CYCLES;
            
            // Set state icon to active
            state_icon.BackColor = Color.YellowGreen;

            if (!firtStart)
            {
                firtStart = true;
                tcpDiagnoseServer = new RBC.TcpIpCommunicationUnit("DiagnoseServer");
                //register the callbackevents from tcpservers
                tcpDiagnoseServer.messageReceivedEvent += new RBC.TcpIpCommunicationUnit.MessageReceivedEventHandler(tcpDiagnoseServer_messageReceivedEvent);
                tcpDiagnoseServer.errorEvent += new RBC.TcpIpCommunicationUnit.ErrorEventHandler(tcpPLCServer_errorEvent);
                tcpDiagnoseServer.statusChangedEvent += new RBC.TcpIpCommunicationUnit.StatusChangedEventHandler(tcpDiagnoseServer_statusChangedEvent);
                tcpDiagnoseServer.clientServerInit();
            }
            // Clear graph if user press start button again
            else
            {
                foreach (var series in chart1.Series) series.Points.Clear();
                currentCycle = 1;
            }
        }

        public void setButtonText(String text)
        {
            button1.Text = text;
        }

        private void UpdateText(string text)
        {
            //textBox1.Text = text;
        }

        private void UpdateChart(string[] msg)
        {
            Debug.Write("Counter: " + currentCycle.ToString() + "\n");
            if (currentCycle <= cyclesToAcquire)
            {
                label_current_cycles.Text = "Cycles: " + currentCycle.ToString() + "/" + cyclesToAcquire;
                setDataToGraph(msg);
                currentCycle += 1;
            }
            // Set state icon to inactive
            else state_icon.BackColor = Color.OrangeRed;
        }

        void tcpDiagnoseServer_statusChangedEvent(string statusMessage)
        {
            //try
            //{
            //    if (tcpDiagnoseServer.dllConfiguration.debuggingActive == true)
            //    {
            //        this.writer.WriteLine("Statuschange - Diagnose: " + statusMessage);
            //        this.writer.Flush();
            //    }
            //}
            //catch (Exception ex)
            //{

            //}
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

        void tcpDiagnoseServer_messageReceivedEvent(string[] receivedMessage)
        {
            String command = receivedMessage[1];
            Debug.Write("message0: " + receivedMessage[0] + "\n");
            Debug.Write("message1: " + receivedMessage[1] + "\n");  
            try
            {
                switch (command)
                {

                    case "c1x":
                        //Message for graph - actual value
                        String timeStamp = "";
                        // Split message to get context and data (cycle time, timestamp)
                        String[] message = receivedMessage[0].Split(':');
                        // Add hh, mm, ss to one string
                        for (int i = 1; i <= 3; i++)
                        {
                            if (i < 3) timeStamp += message[i] + ":";
                            else timeStamp += message[i];
                        }
                        message[1] = timeStamp;
                        Debug.Write("Run UpdatChart\n");
                        chart1.Invoke(new RBC.TcpIpCommunicationUnit.UpdateChartCallback(this.UpdateChart),
                        new object[] { message });
                        break;
                    case "c2x":
                        //Message for graph - mean value
                        break;
                    case "0":
                        machineName = receivedMessage[0];
                        break;
                    case "1":
                        projectName = receivedMessage[0];
                        setTitle(machineName + " " + projectName);
                        break;
                    case "c1":
                        // Test

                        //tcpDiagnoseServer.messageForSmartphone = receivedMessage[0] + ":" + receivedMessage[1];
                        //Debug.Write("To phone: " + ":" + receivedMessage[1] + ":" + receivedMessage[0] + ";" + "\n");
                        break;
                    default:
                        //Send message to phone
                        break;
                }
            }
            catch (InvalidOperationException e)
            {

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

        private void cyclesButtonClicked(object sender, EventArgs e)
        {
            //try
            //{
            //    int xAxisRateTemp = Int32.Parse(textBox1.Text);
            //    if ((xAxisRateTemp >= 1) && (xAxisRateTemp <= 10)) xAxisRate = xAxisRateTemp;
            //}
            //catch (System.FormatException ex)
            //{

            //}
        }

        private void chart1_Click(object sender, EventArgs e)
        {
        }

        private void setDataToGraph(String[] message)
        {
            var series1 = chart1.Series[0];
            series1.ChartType = SeriesChartType.Line;
            chart1.Series[0].BorderWidth = 3;
            chart1.ChartAreas[0].AxisY.Interval = 0.5;
            chart1.ChartAreas[0].AxisX.Interval = 1;
            chart1.ChartAreas[0].AxisX.Title = "Time [hh:mm:ss]";
            chart1.ChartAreas[0].AxisY.Title = "Cycletime [s]";
            chart1.ChartAreas[0].AxisX.MajorGrid.Interval = 1;

            stopWatch2.Start();

            if ((message[1] != messageOld) && ((stopWatch2.ElapsedMilliseconds) >= (stopWatchOld + (xAxisRate * 1000))))
            {
                series1.Points.AddXY(message[1], message[0]);
                chart1.Invalidate();
                stopWatchOld = stopWatch2.ElapsedMilliseconds;
            }
            messageOld = message[1];
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (string.Equals((sender as Button).Name, @"CloseButton"))
            {
                // Do something proper to CloseButton.
            }
            else
            {
                tcpDiagnoseServer.closeAllConnections();
            }

            if(notifyIcon != null) notifyIcon.Dispose();
        }

        private void Snapshot_Click(object sender, EventArgs e)
        {
            Rectangle bounds = this.Bounds;
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
                }
                bitmap.Save("C://Users//Manuel.Rixen//Desktop//CycleTimeGraph_" + graphName + ".jpg", ImageFormat.Jpeg);
            }



            
            notifyIcon.Visible = true;

            notifyIcon.BalloonTipTitle = "Diagnose App";
            notifyIcon.Icon = SystemIcons.Application;
            notifyIcon.BalloonTipText = "Screenshot created succesfully";
            notifyIcon.ShowBalloonTip(1000);
            
        }

    }
}
