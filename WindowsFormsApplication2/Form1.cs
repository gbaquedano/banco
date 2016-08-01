using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;

namespace WindowsFormsApplication2{
    public struct datapack{
        public long time;
        public float velangular;
        public float acelangular;
        public long afr;
        public long nivelgasolina;
    }

    public struct datesRpmComponent {
        public DateTime previousdate;
        public DateTime currentdate;
        public double previousradseg;
        public double currentradseg;
    }


    public partial class Form1 : Form
    {
        public double I = 1.89645;
        public long rpmstart;
        //public datesRpmComponent dates;
        public TimeSpan totalTimeSpan;

        private SerialPort _serialPort;
        private CancellationTokenSource _tokenSource;
        private List<datapack> _data = new List<datapack>();

        //private Thread _readThread;

        public Form1()
        {
            InitializeComponent();
            StringComparer strCom = StringComparer.OrdinalIgnoreCase;
        }

        public void addPoint(double x, double y) {
            chart1.Series[0].Points.AddXY(x,y);
        }

        public void addLine(string str)
        {
            //richTextBox1.Text+=str+"\n";
        }

        public void setRPMLabel(string s)
        {
            labelRPM.Text = s;
        }

        public void setParLabel(string s) {
            labelPar.Text = s;
        }

        public void setAceleracionLabel(string s) {
            labelAceleracion.Text = s;
        }

        private void VolcarDatos()
        {
            using (var f = File.AppendText(DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + ".csv")) {
                
                var curvaAceleracion = MathNet.Numerics.Interpolation.CubicSpline.InterpolateAkimaSorted(_data.Select(x => (double)x.time).ToArray(), _data.Select(x => (double)x.acelangular).ToArray());
                var curvaVelocidad = MathNet.Numerics.Interpolation.CubicSpline.InterpolateAkimaSorted(_data.Select(x => (double)x.time).ToArray(), _data.Select(x => (double)x.velangular).ToArray());

                var point = (_data.Last().time - _data[0].time) / _data.Count;
                for (var i = 0; i < _data.Count*10; i++)
                {
                    //f.WriteLine(_data[i].time + ";" + _data[i].velangular + ";" + _data[i].acelangular);
                    f.WriteLine(_data[0].time + (point*i*0.1) + ";" + curvaAceleracion.Interpolate(_data[0].time + (point * i * 0.1)) + ";" + curvaVelocidad.Interpolate(_data[0].time + (point * i * 0.1)) + ";" + curvaVelocidad.Differentiate(_data[0].time + (point * i * 0.1)));
                }
                _data.Clear();
            }
        }

        public void Read()
        {
            //int cont = 0;
            _serialPort.DiscardInBuffer();
            while (!_tokenSource.Token.IsCancellationRequested)
            {
                try{
                    /*
                    int velangular = int.Parse(_serialPort.ReadLine());
                    dates.previousradseg = dates.currentradseg;
                    dates.currentradseg = velangular;
                    dates.previousdate = dates.currentdate;
                    dates.currentdate = DateTime.Now;
                    TimeSpan elapsedtime = dates.currentdate - dates.previousdate;
                    double radsegvariation = dates.currentradseg - dates.previousradseg;
                    double par = (radsegvariation / (elapsedtime.TotalMilliseconds / 1000d)) * I;


                    this.Invoke((MethodInvoker)delegate { addPoint(velangular * 60.0 / (2 * Math.PI),par); });
                    this.Invoke((MethodInvoker)delegate { addLine("RPM:"+(velangular * 60.0 / (2 * Math.PI)).ToString()+" radseg:"+dates.currentradseg.ToString()+" tiempo:"+elapsedtime.TotalMilliseconds); });
                    _serialPort.DiscardInBuffer();
                     * */
                    datapack dp = new datapack();
                    string message = _serialPort.ReadLine();
                    Console.WriteLine(message);

                    string[] separators = {"\t"};
                    string[] words=message.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length > 2)
                    {
                        dp.time = long.Parse(words[0]);

                        float.TryParse(words[1], NumberStyles.Any, CultureInfo.InvariantCulture, out dp.velangular);
                        float.TryParse(words[2], NumberStyles.Any, CultureInfo.InvariantCulture, out dp.acelangular);
                        //long.TryParse(words[3], out dp.afr);
                        //long.TryParse(words[4], out dp.nivelgasolina);
                        //dp.afr = double.Parse(words[2].Replace('.', ','));
                        //dp.nivelgasolina = double.Parse(words[3].Replace('.', ','));

                        this.Invoke((MethodInvoker)delegate { setRPMLabel((dp.velangular * 60 / (2 * Math.PI)).ToString()); });
                        this.Invoke((MethodInvoker)delegate { setAceleracionLabel((dp.acelangular).ToString()); });
                        this.Invoke((MethodInvoker)delegate { setParLabel((dp.acelangular*I).ToString()); });


                        if (dp.velangular * 60 / (2 * Math.PI) > rpmstart)
                        {
                            /*
                            dates.previousradseg = dates.currentradseg;
                            dates.currentradseg=dp.velangular;
                            dates.previousdate = dates.currentdate;
                            dates.currentdate = DateTime.Now;
                             * */
                            //long elapsedtime = dp.time - dp.previoustime;
                            //float radsegvariation = dp.velangular / 1000 - dp.previousvelangular / 1000;


                            //double par = (radsegvariation / (elapsedtime / 1000000d)) * I;
                            //totalTimeSpan += elapsedtime;

                            //this.Invoke((MethodInvoker)delegate { addPoint(dp.velangular * 60 / (2 * Math.PI * 1000), par); });
                            //this.Invoke((MethodInvoker)delegate { addLine("RPM:"+(dp.velangular * 60 / (2 * Math.PI)).ToString()+" radseg:"+dates.currentradseg.ToString()+" tiempo:"+elapsedtime.TotalMilliseconds); });
                            //Thread.Sleep(200);

                            this.Invoke((MethodInvoker)delegate { addPoint(dp.velangular, dp.acelangular * I * 60 / (2 * Math.PI)); });
                            //this.Invoke((MethodInvoker)delegate { addPoint(dp.velangular * 60 / (2 * Math.PI), dp.acelangular * I); });
                            //this.Invoke((MethodInvoker)delegate { addPoint(time.TotalMilliseconds, dp.acelangular); });
                        }
                        _data.Add(dp);
                    }
                        /*
                        for (int n = 0; n < words.Length; n++)
                        {
                            Console.WriteLine("Mensaje recibido: " + words[n]);
                            this.Invoke((MethodInvoker)delegate { addLine(n.ToString()+":"+words[n]); });
                        }
                         * */
                        //addPoint(cont++, cont);
                        //this.Invoke((MethodInvoker)delegate { addPoint(cont++, cont++); });

                        
                    
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Timeout");
                }
                catch (System.IO.IOException)
                {
                    Console.WriteLine("Cancelado");
                }
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBoxStartRPM_TextChanged(object sender, EventArgs e)
        {
            try
            {
                rpmstart=long.Parse(textBoxStartRPM.Text);
            }
            catch {
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd=new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK) {
                
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            chart1.Series[0].Points.Clear();
            //richTextBox1.Clear();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var ports = SerialPort.GetPortNames();
            lbCom.DataSource = ports;
        }

        private void butConectar_Click(object sender, EventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                try
                {
                    _tokenSource = new CancellationTokenSource();
                    _serialPort = new SerialPort(lbCom.SelectedItem.ToString(), 57600);
                    _serialPort.ReadTimeout = 500;
                    _serialPort.WriteTimeout = 500;

                    _serialPort.Open();

                    if (_serialPort.IsOpen)
                    {
                        TaskEx.Run(() =>
                        {
                            Read();
                        }, _tokenSource.Token);
                        //_readThread = new Thread(Read);
                        //_readThread.Start();
                        butConectar.Text = "Desconectar puerto COM";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error conectando al puerto COM:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                _tokenSource.Cancel();
                _serialPort.Close();
                chart1.Series[0].Points.Clear();
                VolcarDatos();
                //_readThread.Abort();
                //_readThread.Join();
                butConectar.Text = "Conectar puerto COM";
            }
        }
    }
}
