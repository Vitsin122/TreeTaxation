using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeTaxation
{
    public class TreeParams : INotifyPropertyChanged
    {
        public bool IsChecked { get; set; }
        public int Number {  get; set; }
        public int PointsCount {  get; set; }
        public double CrownDiameter { get; set; }
        public double MaxZ { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
