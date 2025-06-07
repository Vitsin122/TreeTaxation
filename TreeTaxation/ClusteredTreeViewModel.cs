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
using System.Windows.Media;
using Aardvark.Base;
//using static TreeTaxation.LasReader;

namespace TreeTaxation
{
    public class ClusteredTreeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private List<List<RealLasPoint>> _treeClusters;
        private LasHeader? _header;
        public HelixViewport3D Viewport { get; private set; }
        public ObservableCollection<TreeParams> TreeParamsCollection { get; set; } = new();
        public Point3DCollection FindPoints { get; set; } = new();

        public ClusteredTreeViewModel(List<List<RealLasPoint>> treeClusters, LasHeader? header = null)
        {
            _treeClusters = treeClusters;
            _header = header;

            BuildHelixView(_treeClusters);
        }

        private RelayCommand _checkFixCommand;
        public RelayCommand CheckFixCommand => _checkFixCommand ??= new RelayCommand(CheckFix);
        private void CheckFix()
        {
            if (TreeParamsCollection.Any(x => x.IsChecked))
            {
                TreeParamsCollection.ForEach(x => x.IsChecked = false);
            }
            else
            {
                TreeParamsCollection.ForEach(x => x.IsChecked = true);
            }
        }

        private RelayCommand _visibilityCheckClustersCommand;
        public RelayCommand VisibilityCheckClustersCommand => _visibilityCheckClustersCommand ??= new RelayCommand(VisibilityCheckClusters);
        private void VisibilityCheckClusters()
        {
            var checkedCluistersIds = TreeParamsCollection.Where(x => x.IsChecked).Select(x => x.Number).ToList();

            var checkedClusters = new List<List<RealLasPoint>>();

            for (int i = 0; i < _treeClusters.Count; i++) 
            {
                if (checkedCluistersIds.Contains(i))
                {
                    checkedClusters.Add(_treeClusters.ElementAt(i));
                }
            }

            BuildHelixView(checkedClusters);
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

        private void BuildHelixView(List<List<RealLasPoint>> clusters)
        {
            FindPoints.Clear();
            Viewport = null;

            var allPoints = new List<Point3D>();

            foreach (var cluster in clusters)
            {
                if (_header != null)
                {
                    allPoints.AddRange(cluster.Select(x => new Point3D(x.X / _header.XScale, x.Y / _header.YScale, x.Z / _header.ZScale)));
                }
                else
                {
                    allPoints.AddRange(cluster.Select(x => new Point3D(x.X, x.Y, x.Z)));
                }
            }

            for (int i = 0; i < clusters.Count; i++)
            {
                TreeParamsCollection.Add(new TreeParams
                {
                    IsChecked = true,
                    Number = i + 1,
                    CrownDiameter = CalculateCrownDiameter(_treeClusters.ElementAt(i)),
                    PointsCount = _treeClusters.ElementAt(i).Count,
                    MaxZ = _treeClusters.ElementAt(i).Select(x => x.Z).Max(),
                });
            }

            FindPoints = new Point3DCollection(allPoints);

            Viewport = new HelixViewport3D
            {
                ZoomExtentsWhenLoaded = true,
                ShowFrameRate = true,
                ShowCoordinateSystem = true,
                ShowCameraInfo = true
            };

            Viewport.Children.Add(new DefaultLights());

            var pointsVisual = new PointsVisual3D
            {
                Points = new Point3DCollection(FindPoints), // Инициализация
                Color = Colors.Red,
                Size = 1
            };

            Viewport.Children.Add(pointsVisual);
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
