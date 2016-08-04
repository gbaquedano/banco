using Datos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
using Telerik.WinControls.UI;

namespace Banco
{
    public partial class frmPrincipal : Form
    {
        // Ejes de gráficos de tiempo real
        CategoricalAxis ejeHorizotalTRTiempo = new CategoricalAxis()
        {
            MajorTickInterval = 5,
            LabelInterval = 4,
            LabelFormatProvider = new FormateaMsAs()
        };
        LinearAxis ejeVerticalTRTiempo = new LinearAxis()
        {
            AxisType = Telerik.Charting.AxisType.Second,
            DesiredTickCount = 10
        };
        LinearAxis ejeVerticalTRTiempoPar = new LinearAxis()
        {
            AxisType = Telerik.Charting.AxisType.Second,
            DesiredTickCount = 10,
            HorizontalLocation = Telerik.Charting.AxisHorizontalLocation.Right
        };
        LinearAxis ejeHorizotalTRRpms = new LinearAxis()
        {
            DesiredTickCount = 30,
            LabelInterval = 3
        };
        LinearAxis ejeVerticalTRRpms = new LinearAxis()
        {
            AxisType = Telerik.Charting.AxisType.Second,
            DesiredTickCount = 10
        };
        LinearAxis ejeVerticalTRRpmsPar = new LinearAxis()
        {
            AxisType = Telerik.Charting.AxisType.Second,
            DesiredTickCount = 10,
            HorizontalLocation = Telerik.Charting.AxisHorizontalLocation.Right
        };

        // Ejes de gráficos de análisis
        LinearAxis ejeHorizontalAnalisisTiempo = new LinearAxis()
        {
            DesiredTickCount = 30,
            LabelInterval = 3,
            LabelFormatProvider = new FormateaMsAs()
        };
        LinearAxis ejeVerticalAnalisisTiempo = new LinearAxis()
        {
            AxisType = Telerik.Charting.AxisType.Second,
            DesiredTickCount = 10
        };
        LinearAxis ejeVerticalAnalisisTiempoPar = new LinearAxis()
        {
            AxisType = Telerik.Charting.AxisType.Second,
            DesiredTickCount = 10,
            HorizontalLocation = Telerik.Charting.AxisHorizontalLocation.Right
        };
        LinearAxis ejeHorizontalAnalisisRpms = new LinearAxis()
        {
            DesiredTickCount = 30,
            LabelInterval = 3
        };
        LinearAxis ejeVerticalAnalisisRpms = new LinearAxis()
        {
            AxisType = Telerik.Charting.AxisType.Second,
            DesiredTickCount = 10
        };
        LinearAxis ejeVerticalAnalisisRpmsPar = new LinearAxis()
        {
            AxisType = Telerik.Charting.AxisType.Second,
            DesiredTickCount = 10,
            HorizontalLocation = Telerik.Charting.AxisHorizontalLocation.Right
        };

        // Recogida y guardado de datos
        private bool Grabando {
            get { return _grabando; }
            set {
                if(value != _grabando)
                {
                    _grabando = value;
                    SetGrabando(_grabando);
                }
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
        //private ConcurrentQueue<DataPack> _queue = new ConcurrentQueue<DataPack>();
        private Task _readTask;
        private SerialPort _serialPort;
        private CancellationTokenSource _tokenSource;

        FicheroEnsayo _ensayoActual = new FicheroEnsayo("Actual");

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
            Volante.Peso = float.Parse(txtPesoVolante.Text);
            Volante.Radio = float.Parse(txtRadioVolante.Text);
            txtInerciaVolante.Text = Volante.Inercia.ToString();
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

            // Ensayos
            lbEnsayos.DataSource = _ensayos;
            cbEnsayos.DataSource = _seleccionEnsayos;

            double valor = 0;
            double.TryParse(txtRpmInicio.Text, out valor);
            _rpmsInicio = valor;

            // Series de Tiempo Real

            radChartTiempo.Series.Add(new LineSeries()
            {
                LegendTitle = _ensayoActual.NombreSerieRpms,
                Name = _ensayoActual.NombreSerieRpms,
                DataSource = _ensayoActual.Series,
                ValueMember = "Rpm",
                CategoryMember = "TTime",
                HorizontalAxis = ejeHorizotalTRTiempo,
                VerticalAxis = ejeVerticalTRTiempo
            });

            radChartTiempo.Series.Add(new LineSeries()
            {
                LegendTitle = _ensayoActual.NombreSeriePar,
                Name = _ensayoActual.NombreSeriePar,
                DataSource = _ensayoActual.Series,
                ValueMember = "Par",
                CategoryMember = "TTime",
                HorizontalAxis = ejeHorizotalTRTiempo,
                VerticalAxis = ejeVerticalTRTiempoPar
            });

            radChartRpms.Series.Add(new ScatterLineSeries()
            {
                LegendTitle = _ensayoActual.NombreSeriePotencia,
                Name = _ensayoActual.NombreSeriePotencia,
                DataSource = _ensayoActual.Series,
                ValueMember = "Rpm",
                CategoryMember = "Potencia",
                PointSize = SizeF.Empty,
                HorizontalAxis = ejeHorizotalTRRpms,
                VerticalAxis = ejeVerticalTRRpms
            });

            radChartRpms.Series.Add(new ScatterLineSeries()
            {
                LegendTitle = _ensayoActual.NombreSeriePar,
                Name = _ensayoActual.NombreSeriePar,
                DataSource = _ensayoActual.Series,
                ValueMember = "Rpm",
                CategoryMember = "Par",
                PointSize = SizeF.Empty,
                HorizontalAxis = ejeHorizotalTRRpms,
                VerticalAxis = ejeVerticalTRRpmsPar
            });

            // Configuración de gráficos

            ChartPanZoomController panZoomControllerTiempo = new ChartPanZoomController();
            ChartPanZoomController panZoomControllerRpms = new ChartPanZoomController();
            panZoomControllerTiempo.PanZoomMode = ChartPanZoomMode.Both;
            radChartAnalisisTiempo.Controllers.Add(panZoomControllerTiempo);
            panZoomControllerRpms.PanZoomMode = ChartPanZoomMode.Both;
            radChartAnalisisRpms.Controllers.Add(panZoomControllerRpms);
            radChartAnalisisTiempo.View.Margin = new Padding(2);
            radChartAnalisisRpms.View.Margin = new Padding(2);
            radChartTiempo.View.Margin = new Padding(2);
            radChartRpms.View.Margin = new Padding(2);

            radChartAnalisisTiempo.ChartElement.LegendPosition = LegendPosition.Bottom;
            radChartAnalisisTiempo.ChartElement.LegendElement.StackElement.Orientation = Orientation.Horizontal;

            radChartAnalisisRpms.ChartElement.LegendPosition = LegendPosition.Bottom;
            radChartAnalisisRpms.ChartElement.LegendElement.StackElement.Orientation = Orientation.Horizontal;

            splitContainerTiempoReal.SplitterWidth = 2;
            splitContainerAnalisis.SplitterWidth = 2;

            CopiarGrid(radChartTiempo, radChartRpms);
            CopiarGrid(radChartAnalisisRpms, radChartAnalisisTiempo);
            CopiarGrid(radChartAnalisisTiempo, radChartAnalisisRpms);

            // Valores de cambios
            Cambios.Seleccionada = (int)nudMarcha.Value;
            Cambios.Relacion1 = float.Parse(txtRelacion1.Text);
            Cambios.Relacion2 = float.Parse(txtRelacion2.Text);
            Cambios.Relacion3 = float.Parse(txtRelacion3.Text);
            Cambios.Relacion4 = float.Parse(txtRelacion4.Text);
            Cambios.Relacion5 = float.Parse(txtRelacion5.Text);
            Cambios.Relacion6 = float.Parse(txtRelacion6.Text);
            Cambios.ReduccionPrimaria = float.Parse(txtReduccionPrimaria.Text);
        }

        private async Task<DataPackProcesado> ReadData()
        {
            Debug.WriteLine("Reading...");
            try
            {
                DataPack data = new DataPack();

                string lectura = _serialPort.ReadLine();

                // Calculamos en paralelo
                return await TaskEx.Run(() =>
                {
                    DataPackProcesado dpp = null;
                    string[] separators = { "\t" };
                    string[] words = lectura.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                    if (words.Length > 2)
                    {
                        float vAngular, aAngular;
                        data.Time = long.Parse(words[0]) / 1000;
                        float.TryParse(words[1], NumberStyles.Any, CultureInfo.InvariantCulture, out vAngular);
                        float.TryParse(words[2], NumberStyles.Any, CultureInfo.InvariantCulture, out aAngular);
                        data.VAngular = vAngular;
                        data.AAngular = aAngular;

                        dpp = new DataPackProcesado(data);

                        /// Esto es una guarrada
                        var arrancado = dpp.Rpm >= _rpmsInicio;
                        var oldGrabando = Grabando;
                        Grabando = arrancado || _grabando_manual;
                        /// Fin de una guarrada

                        if (Grabando)
                        {
                            if (!_primerTime.HasValue)
                            {
                                _primerTime = dpp.Time;
                                dpp.TTime = 0;
                            }
                            else
                            {
                                dpp.TTime = dpp.Time - _primerTime.Value;
                            }
                        }
                        else
                        {
                            dpp.TTime = dpp.Time;
                        }
                    }

                    if (dpp.Rpm > _maxRpms) _maxRpms = dpp.Rpm.Value;
                    if (dpp.Par > _maxPar) { _maxPar = dpp.Par.Value; _rpmsAMaxPar = dpp.Rpm.Value; }
                    if (dpp.Potencia > _maxPotencia) { _maxPotencia = dpp.Potencia.Value; _rpmsAMaxPot = dpp.Rpm.Value; }

                    return dpp;
                });
            }catch(Exception ex){
                throw ex;
            }
        }

        private void butConexion_Click(object sender, EventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                try
                {
                    _init = false;
                    SetConexionSerie(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error conectando al puerto COM:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetConexionSerie(false);
                }
            }else{
                SetConexionSerie(false);
            }
        }

        private void SetConexionSerie(bool activa)
        {
            if (activa)
            {
                _serialPort = new SerialPort(lbCom.SelectedItem.ToString(), int.Parse(lbBaud.SelectedItem.ToString()));
                _serialPort.ReadTimeout = 500;
                _serialPort.WriteTimeout = 500;
                _serialPort.DataReceived += _serialPort_DataReceived;
                _serialPort.ErrorReceived += _serialPort_ErrorReceived;
                _serialPort.Open();
                _serialPort.DiscardInBuffer();
                butConexion.Text = "Desconectar";
            }
            else
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                butConexion.Text = "Conectar";
            }
        }

        private void _serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            SetConexionSerie(false);
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            SetConexionSerie(false);
        }

        private async void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //Debug.WriteLine("Data received");
            var data = await ReadData();
            try
            {
                Invoke((MethodInvoker)delegate
                {
                    if (Grabando)
                    {
                        _ensayoActual.FillData(data);
                    }

                    gaugeRpm.Value = data.Rpm.Value;
                    gaugePar.Value = data.Par.Value;
                    gaugePotencia.Value = data.Potencia.Value;

                    radialGaugeNeedle2.Value = _maxRpms;
                    radialGaugeNeedle4.Value = _maxPar;
                    radialGaugeSingleLabelRpmsMaxPar.LabelText = _rpmsAMaxPar.ToString("0");
                    radialGaugeNeedle6.Value = _maxPotencia;
                    radialGaugeSingleLabel10.LabelText = _rpmsAMaxPot.ToString("0");

                    txtRpm.Text = data.Rpm.ToString();
                    txtPar.Text = (data.AAngular * Volante.Inercia).ToString();
                });
            }catch( Exception ex)
            {
                Debug.WriteLine(ex.Message);
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

                    radChartAnalisisTiempo.Series.Remove(radChartAnalisisTiempo.Series.FirstOrDefault(x => x.Name == _ensayos[i].NombreSerieRpms));
                    radChartAnalisisTiempo.Series.Remove(radChartAnalisisTiempo.Series.FirstOrDefault(x => x.Name == _ensayos[i].NombreSeriePar));

                    radChartAnalisisRpms.Series.Remove(radChartAnalisisRpms.Series.FirstOrDefault(x => x.Name == _ensayos[i].NombreSeriePar));
                    radChartAnalisisRpms.Series.Remove(radChartAnalisisRpms.Series.FirstOrDefault(x => x.Name == _ensayos[i].NombreSeriePotencia));
                }
            }

            _seleccionEnsayos.Clear();
            for (var i = 0; i < lbEnsayos.SelectedItems.Count; i++)
            {
                var fichero = (FicheroEnsayo)lbEnsayos.SelectedItems[i];
                _seleccionEnsayos.Add(fichero);
                if (!fichero.Added)
                {
                    fichero.Added = true;

                    var dataText = File.ReadAllLines(fichero.Path);

                    fichero.FillData(dataText);

                    if (!radChartAnalisisTiempo.Series.Any(x => x.Name == fichero.NombreSerieRpms))
                    {
                        var serie = new ScatterLineSeries()
                        {
                            LegendTitle = fichero.NombreSerieRpms,
                            Name = fichero.NombreSerieRpms,
                            DataSource = fichero.Series,
                            ValueMember = "TTime",
                            CategoryMember = "Rpm",
                            PointSize = SizeF.Empty,
                            HorizontalAxis = ejeHorizontalAnalisisTiempo,
                            VerticalAxis = ejeVerticalAnalisisTiempo
                        };
                        radChartAnalisisTiempo.Series.Add(serie);
                    }

                    if (!radChartAnalisisTiempo.Series.Any(x => x.Name == fichero.NombreSeriePar))
                    {
                        var serie = new ScatterLineSeries()
                        {
                            LegendTitle = fichero.NombreSeriePar,
                            Name = fichero.NombreSeriePar,
                            DataSource = fichero.Series,
                            ValueMember = "TTime",
                            CategoryMember = "Par",
                            PointSize = SizeF.Empty,
                            HorizontalAxis = ejeHorizontalAnalisisTiempo,
                            VerticalAxis = ejeVerticalAnalisisTiempoPar
                        };
                        radChartAnalisisTiempo.Series.Add(serie);
                    }

                    if (!radChartAnalisisRpms.Series.Any(x => x.Name == fichero.NombreSeriePotencia))
                    {
                        radChartAnalisisRpms.Series.Add(new ScatterLineSeries()
                        {
                            LegendTitle = fichero.NombreSeriePotencia,
                            Name = fichero.NombreSeriePotencia,
                            DataSource = fichero.Series,
                            ValueMember = "Rpm",
                            CategoryMember = "Potencia",
                            PointSize = SizeF.Empty,
                            HorizontalAxis = ejeHorizontalAnalisisRpms,
                            VerticalAxis = ejeVerticalAnalisisRpms
                        });
                    }

                    if (!radChartAnalisisRpms.Series.Any(x => x.Name == fichero.NombreSeriePar))
                    {
                        radChartAnalisisRpms.Series.Add(new ScatterLineSeries()
                        {
                            LegendTitle = fichero.NombreSeriePar,
                            Name = fichero.NombreSeriePar,
                            DataSource = fichero.Series,
                            ValueMember = "Rpm",
                            CategoryMember = "Par",
                            PointSize = SizeF.Empty,
                            HorizontalAxis = ejeHorizontalAnalisisRpms,
                            VerticalAxis = ejeVerticalAnalisisRpmsPar
                        });
                    }
                }
            }
            CopiarGrid(radChartAnalisisTiempo, radChartAnalisisRpms);
        }

        private void CopiarGrid(RadChartView src, RadChartView dst)
        {
            /// Copiar estilos de primera a segunda gráfica de análisis
            var gridSrc = src.GetArea<CartesianArea>().GetGrid<CartesianGrid>();
            var gridDst = dst.GetArea<CartesianArea>().GetGrid<CartesianGrid>();
            gridDst.AlternatingBackColor = gridSrc.AlternatingBackColor;
            gridDst.AlternatingBackColor2 = gridSrc.AlternatingBackColor2;
            gridDst.AlternatingHorizontalColor = gridSrc.AlternatingHorizontalColor;
            gridDst.AlternatingVerticalColor = gridSrc.AlternatingVerticalColor;
            gridDst.BackColor = gridSrc.BackColor;
            gridDst.BackColor2 = gridSrc.BackColor2;
            gridDst.BackColor3 = gridSrc.BackColor3;
            gridDst.BackColor4 = gridSrc.BackColor4;
            gridDst.BorderDashStyle = gridSrc.BorderDashStyle;
            gridDst.BorderDashPattern = gridSrc.BorderDashPattern;
            gridDst.BorderCornerRadius = gridSrc.BorderCornerRadius;
            gridDst.BorderDrawMode = gridSrc.BorderDrawMode;
            gridDst.BorderGradientAngle = gridSrc.BorderGradientAngle;
            gridDst.BorderGradientStyle = gridSrc.BorderGradientStyle;
            gridDst.BorderInnerColor = gridSrc.BorderInnerColor;
            gridDst.BorderInnerColor2 = gridSrc.BorderInnerColor2;
            gridDst.BorderInnerColor3 = gridSrc.BorderInnerColor3;
            gridDst.BorderInnerColor4 = gridSrc.BorderInnerColor4;
            gridDst.BorderLeftColor = gridSrc.BorderLeftColor;
            gridDst.BorderLeftShadowColor = gridSrc.BorderLeftShadowColor;
            gridDst.BorderLeftWidth = gridSrc.BorderLeftWidth;
            gridDst.BorderRightColor = gridSrc.BorderRightColor;
            gridDst.BorderRightShadowColor = gridSrc.BorderRightShadowColor;
            gridDst.BorderRightWidth = gridSrc.BorderRightWidth;
            gridDst.BorderTopColor = gridSrc.BorderTopColor;
            gridDst.BorderTopShadowColor = gridSrc.BorderTopShadowColor;
            gridDst.BorderBottomColor = gridSrc.BorderBottomColor;
            gridDst.BorderBottomShadowColor = gridSrc.BorderBottomShadowColor;
            gridDst.BorderBoxStyle = gridSrc.BorderBoxStyle;
            gridDst.BorderColor = gridSrc.BorderColor;
            gridDst.BorderColor2 = gridSrc.BorderColor2;
            gridDst.BorderColor3 = gridSrc.BorderColor3;
            gridDst.BorderColor4 = gridSrc.BorderColor4;
            gridDst.BorderTopWidth = gridSrc.BorderTopWidth;
            gridDst.BorderWidth = gridSrc.BorderWidth;
            gridDst.ForeColor = gridSrc.ForeColor;
            gridDst.DrawVerticalFills = false;
            gridDst.Style = gridSrc.Style;
            gridDst.NumberOfColors = gridSrc.NumberOfColors;
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
                    _ensayoActual.VolcarDatos();
                });
            }
            _init = activo;
        }
        private void butGrabar_Click(object sender, EventArgs e)
        {
            _grabando_manual = !_grabando_manual;
            Grabando = _grabando_manual;
        }

        private void iniciarEnsayo()
        {
            _maxRpms = 0;
            _maxPar = 0;
            _maxPotencia = 0;
            _rpmsAMaxPar = 0;
            _maxPotencia = 0;
            _rpmsAMaxPot = 0;

            _ensayoActual.Series.Clear();

            _init = true;
        }

        private void tabBanco_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(tabBanco.SelectedTab.Name == tabEns.Name)
            {
                ActualizarEnsayos();
            }
        }

        private void nudMarcha_ValueChanged(object sender, EventArgs e)
        {
            Cambios.Seleccionada = (int)nudMarcha.Value;
        }

        private void txtRelacion1_TextChanged(object sender, EventArgs e)
        {
            Cambios.Relacion1 = float.Parse(txtRelacion1.Text);
        }
        private void txtRelacion2_TextChanged(object sender, EventArgs e)
        {
            Cambios.Relacion2 = float.Parse(txtRelacion2.Text);
        }
        private void txtRelacion3_TextChanged(object sender, EventArgs e)
        {
            Cambios.Relacion3 = float.Parse(txtRelacion3.Text);
        }
        private void txtRelacion4_TextChanged(object sender, EventArgs e)
        {
            Cambios.Relacion4 = float.Parse(txtRelacion4.Text);
        }
        private void txtRelacion5_TextChanged(object sender, EventArgs e)
        {
            Cambios.Relacion5 = float.Parse(txtRelacion5.Text);
        }
        private void txtRelacion6_TextChanged(object sender, EventArgs e)
        {
            Cambios.Relacion6 = float.Parse(txtRelacion6.Text);
        }
        private void txtReduccionPrimaria_TextChanged(object sender, EventArgs e)
        {
            Cambios.ReduccionPrimaria = float.Parse(txtReduccionPrimaria.Text);
        }

        private void butAbrirEnsayos_Click(object sender, EventArgs e)
        {
            Process.Start(@"ensayos");
        }

        private void nudOffset_ValueChanged(object sender, EventArgs e)
        {
            _seleccionEnsayos[cbEnsayos.SelectedIndex].Offset = (long)nudOffset.Value;
            var series = radChartAnalisisTiempo.Series.ToArray();
            radChartAnalisisTiempo.Series.Clear();
            radChartAnalisisTiempo.Series.AddRange(series);
        }

        private void cbEnsayos_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbEnsayos.SelectedIndex >= 0)
            {
                nudOffset.Value = _seleccionEnsayos[cbEnsayos.SelectedIndex].Offset;
            }
        }
    }
}
