using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using static TreeTaxation.LasReader;

namespace TreeTaxation
{
    public class ClusteredTreeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private List<List<RealLasPoint>> _treeClusters;

        private Point3DCollection _findPoints;
        public Point3DCollection FindPoints
        {
            get => _findPoints;
            set
            {
                _findPoints = value;
                OnPropertyChanged();
            }
        }

        public ClusteredTreeViewModel(List<List<RealLasPoint>> treeClusters)
        {
            _treeClusters = treeClusters;

            var allPoints = new List<Point3D>();

            foreach (var cluster in _treeClusters)
            {
                allPoints.AddRange(cluster.Select(x => new Point3D(x.X / 0.01, x.Y / 0.01, x.Z / 0.01)));
            }

            FindPoints = new Point3DCollection(allPoints);
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
