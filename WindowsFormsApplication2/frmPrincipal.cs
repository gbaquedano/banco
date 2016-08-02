using Datos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Banco
{
    public partial class frmPrincipal : Form
    {
        // Valores del volante y cambios
        private Cambios _cambios = new Cambios();
        private Volante _volante = new Volante();

        // Series
        private Series _serieVAngular = new Series()
        {
            Name = "Velocidad Angular",
            ChartType = SeriesChartType.Line
        };
        private Series _serieAAngular = new Series()
        {
            Name = "Aceleración Angular",
            ChartType = SeriesChartType.Line
        };
        private Series _seriePar = new Series()
        {
            Name = "Par",
            ChartType = SeriesChartType.Line
        };
        private Series _serieRpms = new Series()
        {
            Name = "RPMs",
            ChartType = SeriesChartType.Line
        };

        // Recogida y guardado de datos
        private bool Grabando {
            get { return _grabando; } // || _grabando_manual; }
            set {
                if(value != _grabando)
                {
                    _grabando = value;
                    SetGrabando(_grabando);
                }
                /*
                if ((value || _grabando_manual) != _grabando)
                {
                    _grabando = (value || _grabando_manual);
                    SetGrabando(_grabando);
                }*/
            }
        }
        private bool _init = false;
        private bool _grabando_manual = false;
        private bool _grabando = false;
        private double _rpmsInicio = 0;
        private long? _primerTime = null;
        // Máximos de ensayo actual
        private float _maxRpms = 0;
        private float _rpmsAMaxPar = 0;
        private float _maxPar = 0;
        private float _maxPotencia = 0;
        private float _rpmsAMaxPot = 0;
        private ConcurrentQueue<DataPack> _queue = new ConcurrentQueue<DataPack>();
        private Task _readTask;
        private SerialPort _serialPort;
        private CancellationTokenSource _tokenSource;

        // Ensayos de la pestaña selección
        BindingList<FicheroEnsayo> _ensayos = new BindingList<FicheroEnsayo>();
        BindingList<FicheroEnsayo> _seleccionEnsayos = new BindingList<FicheroEnsayo>();

        public frmPrincipal()
        {
            InitializeComponent();
        }

        private void lbCom_DropDown(object sender, EventArgs e)
        {
            lbCom.DataSource = SerialPort.GetPortNames();
        }

        public void UpdateConfig()
        {
            _volante.Peso = double.Parse(txtPesoVolante.Text);
            _volante.Radio = double.Parse(txtRadioVolante.Text);
            txtInerciaVolante.Text = _volante.Inercia.ToString();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            // Crear directorio de ensayos si no existe
            if (!Directory.Exists("ensayos"))
            {
                Directory.CreateDirectory("ensayos");
            }

            // COM
            lbCom.DataSource = SerialPort.GetPortNames();
            lbBaud.SelectedItem = lbBaud.Items[9];

            // Inicialización de valores
            UpdateConfig();

            // Añadir a series
            chartPrincipal.Series.Add(_serieVAngular);
            chartPrincipal.Series.Add(_serieAAngular);
            chartPrincipal.Series.Add(_seriePar);
            chartPrincipal.Series.Add(_serieRpms);

            // Ensayos
            lbEnsayos.DataSource = _ensayos;
            cbEnsayos.DataSource = _seleccionEnsayos;
            chartAnalisis.ChartAreas[0].AxisX.Minimum = 0;

            double valor = 0;
            double.TryParse(txtRpmInicio.Text, out valor);
            _rpmsInicio = valor;
        }

        private DataPack ReadData()
        {
            DataPack data = new DataPack();
            try
            {
                string lectura = _serialPort.ReadLine();

                string[] separators = { "\t" };
                string[] words = lectura.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 2)
                {
                    float vAngular, aAngular;
                    data.Time = long.Parse(words[0])/1000;

                    float.TryParse(words[1], NumberStyles.Any, CultureInfo.InvariantCulture, out vAngular);
                    float.TryParse(words[2], NumberStyles.Any, CultureInfo.InvariantCulture, out aAngular);
                    data.VAngular = vAngular;
                    data.AAngular = aAngular;

                    /// Esto es una guarrada
                    var arrancado = vAngular >= _rpmsInicio;
                    var oldGrabando = Grabando;
                    Grabando = arrancado || _grabando_manual;
                    if (Grabando) { 
                        if (Grabando != oldGrabando)
                        {
                            Invoke((MethodInvoker)delegate
                            {
                                chartPrincipal.ChartAreas[0].AxisX.Minimum = data.Time;
                                chartPrincipal.ChartAreas[0].AxisX.Maximum = data.Time + 2500;
                            });
                        }else{
                            Invoke((MethodInvoker)delegate
                            {
                                chartPrincipal.ChartAreas[0].AxisX.Maximum = data.Time + 2500;
                            });
                        }
                    }
                    /// Fin de una guarrada

                    if (Grabando)
                    {
                        if (!_primerTime.HasValue)
                        {
                            _primerTime = data.Time;
                            data.TTime = 0;
                        }else{
                            data.TTime = data.Time - _primerTime.Value;
                        }
                    }
                    else
                    {
                        data.TTime = data.Time;
                    }
                }
            }catch(InvalidOperationException ex){
                throw ex;
            }catch(TimeoutException ex){
                throw ex;
            }
            return data;
        }

        private void butConexion_Click(object sender, EventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                try
                {
                    _init = false;
                    _serieAAngular.Points.Clear();
                    _seriePar.Points.Clear();
                    _serieRpms.Points.Clear();
                    _serieVAngular.Points.Clear();

                    _tokenSource = new CancellationTokenSource();
                    _serialPort = new SerialPort(lbCom.SelectedItem.ToString(), int.Parse(lbBaud.SelectedItem.ToString()));
                    _serialPort.ReadTimeout = 500;
                    _serialPort.WriteTimeout = 500;

                    _serialPort.Open();

                    butConexion.Text = "Desconectar";

                    _readTask = TaskEx.Run(() =>
                    {
                        _serialPort.DiscardInBuffer();
                        while (!_tokenSource.Token.IsCancellationRequested)
                        {
                            var data = ReadData();

                            double rpms = (data.VAngular * 60 * 100 / (2 * Math.PI));
                            double par = (data.AAngular * _volante.Inercia);
                            double pot = (par * rpms);
                            if (rpms > _maxRpms) _maxRpms = (float)rpms;
                            if (par > _maxPar) { _maxPar = (float)par; _rpmsAMaxPar = (float)rpms; }
                            if (pot > _maxPotencia) { _maxPotencia = (float)pot;  _rpmsAMaxPot = (float)rpms; }

                            Invoke((MethodInvoker)delegate {
                                gaugeRpm.Value = (float)rpms;
                                gaugePar.Value = (float)par;
                                gaugePotencia.Value = (float)pot;

                                radialGaugeNeedle2.Value = _maxRpms;
                                radialGaugeNeedle4.Value = _maxPar;
                                radialGaugeSingleLabel6.LabelText = _rpmsAMaxPar.ToString();
                                radialGaugeNeedle6.Value = _maxPotencia;
                                radialGaugeSingleLabel10.LabelText = _rpmsAMaxPot.ToString();

                                txtRpm.Text = rpms.ToString();
                                txtPar.Text = (data.AAngular * _volante.Inercia).ToString();

                                _serieVAngular.Points.Add(new DataPoint(data.Time, data.VAngular));
                                _serieAAngular.Points.Add(new DataPoint(data.Time, data.AAngular));
                                _seriePar.Points.Add(new DataPoint(data.Time, par));
                                _serieRpms.Points.Add(new DataPoint(data.Time, rpms));
                            });

                            if (Grabando)
                            {
                                _queue.Enqueue(data);
                            }
                        }
                        Console.WriteLine("Cancelled");
                        _serialPort.Close();
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error conectando al puerto COM:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }else{
                _tokenSource.Cancel();
                butConexion.Text = "Conectar";
            }
        }

        private void VolcarDatos()
        {
            using (var f = File.AppendText("ensayos\\" + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + ".csv"))
            {
                while (_queue.Count > 0)
                {
                    DataPack dato;
                    if(_queue.TryDequeue(out dato))
                    {
                        f.WriteLine(dato.TTime + ";" + dato.VAngular + ";" + dato.AAngular);
                    }
                }
            }
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_tokenSource != null && !_tokenSource.Token.IsCancellationRequested)
            {
                _tokenSource.Cancel();
                _readTask.Wait(_tokenSource.Token);
            }
        }

        private void butRefrescar_Click(object sender, EventArgs e)
        {
            ActualizarEnsayos();
        }

        private void ActualizarEnsayos()
        {
            var files = Directory.EnumerateFiles("ensayos\\", "*.csv").Select((x, i) => new FicheroEnsayo(i)
            {
                Nombre = x.Split('\\')[1],
                Path = x
            }).ToList();

            var ensayosExistentes = _ensayos.Where(x => files.Any(y => y.Path == x.Path));
            var ensayosBorrados = _ensayos.Where(x => !files.Any(y => y.Path == x.Path));
            var ensayosNuevos = files.Where(x => !_ensayos.Any(y => y.Path == x.Path));

            while (ensayosBorrados.Count() > 0)
            {
                _ensayos.Remove(ensayosBorrados.ElementAt(0));
            }
            while (ensayosNuevos.Count() > 0)
            {
                _ensayos.Add(ensayosNuevos.ElementAt(0));
            }
        }

        private void lbEnsayos_SelectedIndexChanged(object sender, EventArgs e)
        {
            for(var i = 0; i < _ensayos.Count; i++)
            {
                if (!lbEnsayos.SelectedItems.Contains(_ensayos[i]))
                {
                    lbEnsayos.SelectedItems.Remove(_ensayos[i]);
                    _ensayos[i].Added = false;
                    chartAnalisis.Series.Remove(_ensayos[i].SerieVAngular);
                    chartAnalisis.Series.Remove(_ensayos[i].SerieAAngular);
                }
            }

            _seleccionEnsayos.Clear();
            for (var i = 0; i < lbEnsayos.SelectedItems.Count; i++)
            {
                var fichero = (FicheroEnsayo)lbEnsayos.SelectedItems[i];
                _seleccionEnsayos.Add(fichero);
                if (!fichero.Added)
                {
                    fichero.ResetSeries();
                    fichero.Added = true;
                    var dataText = File.ReadAllLines(fichero.Path);
                    for (var j = 0; j < dataText.Length; j++)
                    {
                        var txt = dataText[j].Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                        fichero.SerieVAngular.Points.Add(new DataPoint(long.Parse(txt[0]), float.Parse(txt[1])));
                        fichero.SerieAAngular.Points.Add(new DataPoint(long.Parse(txt[0]), float.Parse(txt[2])));
                    }
                    if(!chartAnalisis.Series.Any(x=>x.Name == fichero.SerieVAngular.Name)){ 
                        chartAnalisis.Series.Add(fichero.SerieVAngular);
                    }
                    if (!chartAnalisis.Series.Any(x => x.Name == fichero.SerieAAngular.Name)){
                        chartAnalisis.Series.Add(fichero.SerieAAngular);
                    }
                }
            }
        }

        private void txtOffset_TextChanged(object sender, EventArgs e)
        {
            Console.WriteLine("Changed");
            var fichero = cbEnsayos.SelectedItem as FicheroEnsayo;
            if (fichero != null)
            {
                long res = 0;
                long.TryParse(txtOffset.Text, out res);
                fichero.Offset = res;
            }
            chartAnalisis.Update();
        }

        private void txtRpmInicio_TextChanged(object sender, EventArgs e)
        {
            double valor = 0;
            double.TryParse(txtRpmInicio.Text, out valor);
            _rpmsInicio = valor;
        }

        private void SetGrabando(bool activo)
        {
            Console.WriteLine("Setting grabando to " + activo);
            if (activo)
            {
                iniciarEnsayo();
                Invoke((MethodInvoker)delegate
                {
                    butGrabar.Text = "Parar";
                });
            }else{
                Invoke((MethodInvoker)delegate
                {
                    butGrabar.Text = "Inicio Manual";
                    _primerTime = null;
                    VolcarDatos();
                });
            }
            _init = activo;
        }
        private void butGrabar_Click(object sender, EventArgs e)
        {
            _grabando_manual = !_grabando_manual;
            Grabando = _grabando_manual;
        }

        private void chartAnalisis_MouseDown(object sender, MouseEventArgs e)
        {
            HitTestResult result = chartAnalisis.HitTest(e.X, e.Y);
            if (result != null && result.Object != null)
            {
                // When user hits the LegendItem
                if (result.Object is LegendItem)
                {
                    // Legend item result
                    LegendItem legendItem = (LegendItem)result.Object;
                    var serie = chartAnalisis.Series.FindByName(legendItem.SeriesName);
                    serie.Enabled = !serie.Enabled;
                }
            }
        }

        private void iniciarEnsayo()
        {
            _maxRpms = 0;
            _maxPar = 0;
            _maxPotencia = 0;
            _rpmsAMaxPar = 0;
            _maxPotencia = 0;
            _rpmsAMaxPot = 0;

            _init = true;
        }

        private void tabBanco_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(tabBanco.SelectedTab.Name == tabEns.Name)
            {
                ActualizarEnsayos();
            }
        }

        private void gaugeRpm_Click(object sender, EventArgs e)
        {

        }
    }
}
