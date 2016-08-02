using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;

namespace Datos
{
    public class DataPack
    {
        public long Time { get; set; }
        public long TTime { get; set; }
        public float VAngular { get; set; }
        public float AAngular { get; set; }
        public long Afr { get; set; }
        public long NivelGasolina { get; set; }
    }

    public class Cambios
    {
        public double ReduccionPrimaria { get; set; }
        public double Relacion1 { get; set; }
        public double Relacion2 { get; set; }
        public double Relacion3 { get; set; }
        public double Relacion4 { get; set; }
        public double Relacion5 { get; set; }
        public double Relacion6 { get; set; }
    }

    public class Volante
    {
        private double _radio;
        public double Radio { get { return _radio * 1000; } set { _radio = value / 1000; } }
        public double Peso { get; set; }
        public double Inercia { get {
                return 0.5 * Peso * Math.Pow(_radio, 2);
        } }
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
                for(var i = 0; i < SerieVAngular.Points.Count; i++)
                {
                    SerieVAngular.Points[i].XValue -= oldOffset - _offset;
                }
                for (var i = 0; i < SerieAAngular.Points.Count; i++)
                {
                    SerieAAngular.Points[i].XValue -= oldOffset - _offset;
                }
                for (var i = 0; i < SerieRpms.Points.Count; i++)
                {
                    SerieRpms.Points[i].XValue -= oldOffset - _offset;
                }
                for (var i = 0; i < SeriePar.Points.Count; i++)
                {
                    SeriePar.Points[i].XValue -= oldOffset - _offset;
                }
            } }
        public bool Added { get; set; }
        public Series SerieVAngular { get; set; }
        public Series SerieAAngular { get; set; }
        public Series SeriePar { get; set; }
        public Series SerieRpms { get; set; }
        public FicheroEnsayo(int indice)
        {
            SerieVAngular = new Series()
            {
                Name = "Velocidad Angular (" + indice + ")",
                ChartType = SeriesChartType.Line
            };
            SerieAAngular = new Series()
            {
                Name = "Aceleración Angular (" + indice + ")",
                ChartType = SeriesChartType.Line
            };
            SeriePar = new Series()
            {
                Name = "Par (" + indice + ")",
                ChartType = SeriesChartType.Line
            };
            SerieRpms = new Series()
            {
                Name = "RPMs (" + indice + ")",
                ChartType = SeriesChartType.Line
            };
        }
        public override string ToString()
        {
            return Nombre;
        }

        internal void ResetSeries()
        {
            SerieAAngular.Points.Clear();
            SerieVAngular.Points.Clear();
            SeriePar.Points.Clear();
            SerieRpms.Points.Clear();
        }
    }
}
