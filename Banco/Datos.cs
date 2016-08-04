using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;

namespace Datos
{
    public class DataPack
    {
        public long? Time { get; set; }
        public long? TTime { get; set; }
        public float? VAngular { get; set; }
        public float? AAngular { get; set; }
        public long? Afr { get; set; }
        public long? NivelGasolina { get; set; }
    }

    public static class Config
    {
        public static string PathEnsayos { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Ensayos";
    }
    public static class Cambios
    {
        public static float ReduccionPrimaria { get; set; }
        public static float Relacion1 { get; set; }
        public static float Relacion2 { get; set; }
        public static float Relacion3 { get; set; }
        public static float Relacion4 { get; set; }
        public static float Relacion5 { get; set; }
        public static float Relacion6 { get; set; }
        public static int Seleccionada { get; set; }
        public static float GetRelacion()
        {
            switch (Seleccionada)
            {
                case 1:
                    return Relacion1;
                case 2:
                    return Relacion2;
                case 3:
                    return Relacion3;
                case 4:
                    return Relacion4;
                case 5:
                    return Relacion5;
                case 6:
                    return Relacion6;
                default:
                    return Relacion4;
            }
        }
    }

    public static class Volante
    {
        private static float _radio;
        public static float Radio { get { return _radio * 1000; } set { _radio = value / 1000; } }
        public static float Peso { get; set; }
        public static float Inercia { get {
                return 0.5f * Peso * (float)Math.Pow(_radio, 2);
        } }
    }

    public class DataPackProcesado
    {
        public long? Time { get; set; }
        public long? TTime { get; set; }
        public float? VAngular { get; set; }
        public float? Rpm { get { return (VAngular * Cambios.ReduccionPrimaria * Cambios.GetRelacion() * 60) / (float)(2 * Math.PI); } }
        public float? AAngular { get; set; }
        public float? Par { get { return (AAngular * Volante.Inercia / (Cambios.ReduccionPrimaria * Cambios.GetRelacion())); } }
        public float? Potencia { get { return (1.36f * Par * VAngular * Cambios.ReduccionPrimaria * Cambios.GetRelacion()) / 1000; } }
        public DataPackProcesado(string csvLine)
        {
            var txt = csvLine.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            TTime = long.Parse(txt[0]);
            VAngular = float.Parse(txt[1]);
            AAngular = float.Parse(txt[2]);
        }
        public DataPackProcesado(DataPack dato)
        {
            TTime = dato.TTime;
            Time = dato.Time;
            VAngular = dato.VAngular;
            AAngular = dato.AAngular;
        }
    }

    public class FicheroEnsayo {
        public string Nombre { get; set; }
        public string Path { get; set; }
        private long _offset { get; set; }
        public long Offset {
            get { return _offset; }
            set {
                var oldOffset = _offset;
                _offset = value;
                for(var i = 0; i < Series.Count; i++)
                {
                    Series[i].TTime -= oldOffset - _offset;
                }
            } }
        public bool Added { get; set; }

        /// Simplificación de series
        private string _nombreSerieVAngular;
        public string NombreSerieVAngular { get { return _nombreSerieVAngular; } }
        private string _nombreSerieAAngular;
        public string NombreSerieAAngular { get { return _nombreSerieAAngular; } }
        private string _nombreSeriePar;
        public string NombreSeriePar { get { return _nombreSeriePar; } }
        private string _nombreSerieRpms;
        public string NombreSerieRpms { get { return _nombreSerieRpms; } }
        private string _nombreSeriePotencia;
        public string NombreSeriePotencia { get { return _nombreSeriePotencia; } }
        public BindingList<DataPackProcesado> Series { get; set; } = new BindingList<DataPackProcesado>();
        /// <summary>
        /// Función para añadir datos a las series basado en un array de líneas CSV
        /// </summary>
        /// <param name="csvData"></param>
        public void FillData(string[] csvData)
        {
            Series.Clear();
            for (var j = 0; j < csvData.Length; j++)
            {
                Series.Add(new DataPackProcesado(csvData[j]));
            }
            //Series.Add(new DataPackProcesado(new DataPack() { TTime = null, AAngular = null, Afr = null, NivelGasolina = null, Time = null, VAngular = null }));
        }
        /// <summary>
        /// Sobrecarga para añadir datos ya parseados en un datapack
        /// </summary>
        /// <param name="dato"></param>
        public void FillData(DataPack dato)
        {
            Series.Add(new DataPackProcesado(dato));
        }
        /// <summary>
        /// Sobrecarga para añadir directamente un datapackprocesado
        /// </summary>
        /// <param name="dato"></param>
        public void FillData(DataPackProcesado dato)
        {
            Series.Add(dato);
        }
        public FicheroEnsayo(int indice)
        {
            _nombreSerieAAngular = "Aceleración Angular (" + indice + ")";
            _nombreSerieVAngular = "Velocidad Angular (" + indice + ")";
            _nombreSeriePar = "Par (" + indice + ")";
            _nombreSerieRpms = "RPMs (" + indice + ")";
            _nombreSeriePotencia = "Potencia (" + indice + ")";
        }

        public FicheroEnsayo(string nombre)
        {
            _nombreSerieAAngular = "Aceleración Angular (" + nombre + ")";
            _nombreSerieVAngular = "Velocidad Angular (" + nombre + ")";
            _nombreSeriePar = "Par (" + nombre + ")";
            _nombreSerieRpms = "RPMs (" + nombre + ")";
            _nombreSeriePotencia = "Potencia (" + nombre + ")";
        }
        public override string ToString()
        {
            return Nombre;
        }

        public void VolcarDatos()
        {
            using (var f = File.AppendText(Config.PathEnsayos + @"\" + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + ".csv"))
            {
                for(var i = 0; i < Series.Count; i++)
                {
                    f.WriteLine(Series[i].TTime + ";" + Series[i].VAngular + ";" + Series[i].AAngular);
                }
            }
        }
    }

    public class FormateaMsAs : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType)
        {
            return this;
        }
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            string s = arg.ToString();
            if (s != null && s!=string.Empty)
            {
                return (float.Parse(s) / 1000).ToString("0.##") + " s";
            }else
            {
                return null;
            }
        }
    }
}
