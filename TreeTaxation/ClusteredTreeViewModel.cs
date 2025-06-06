using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using LazToLasEasy;
using LazToLasEasy.Common;
//using static TreeTaxation.LasReader;

namespace TreeTaxation
{
    public class ClusteredTreeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public HelixViewport3D viewport { get; set; }

        private List<List<RealLasPoint>> _treeClusters;

        public ObservableCollection<TreeParams> TreeParamsCollection { get; set; } = new();

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

            for (int i = 0; i < treeClusters.Count; i++)
            {
                TreeParamsCollection.Add(new TreeParams
                {
                    Number = i + 1,
                    CrownDiameter = CalculateCrownDiameter(treeClusters.ElementAt(i)),
                    PointsCount = treeClusters.ElementAt(i).Count
                });
            }

            FindPoints = new Point3DCollection(allPoints);
        }

        // Расчет диаметра кроны в XY-плоскости
        private double CalculateCrownDiameter(List<RealLasPoint> cluster)
        {
            if (cluster.Count == 0) return 0;

            double minX = cluster.Min(p => p.X);
            double maxX = cluster.Max(p => p.X);
            double minY = cluster.Min(p => p.Y);
            double maxY = cluster.Max(p => p.Y);

            return Math.Max(maxX - minX, maxY - minY);
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
