using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Media3D;
using DbscanImplementation;
using HelixToolkit.Wpf;
using static TreeTaxation.LasReader;
//using LazToLasEasy.Common;

namespace TreeTaxation
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private KDTree _tree;
        private LasHeader _header;
        private List<LasPoint> _allPoints;
        private List<RealLasPoint> _treePoints = new List<RealLasPoint>();

        // Коллекция точек для визуализации
        private Point3DCollection _points;
        public Point3DCollection Points
        {
            get => _points;
            set
            {
                _points = value;
                OnPropertyChanged();
            }
        }
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

        public string Eps { get; set; }
        public string MinPts { get; set; }
        public string MaxClstPts { get; set; }
        public string MinTreePoints { get; set; }
        public bool ClearSmallClusters { get; set; }
        public string MinHeight { get; set; }

        public MainWindowViewModel()
        {
            
            Points = new Point3DCollection();

            var reader = new LasReader("C:\\Users\\Vitsin\\source\\repos\\Las_Converter_Console\\01_ALS.las");

            _header = reader.Header;

            _allPoints = reader.ReadPoints().OrderBy(x => x.X).ThenBy(x => x.Y).ThenBy(x => x.Z).ToList();

            int counter = 0;

            foreach(var point in _allPoints)
            {

                if ( point.Classification >= 3 && point.Classification <= 5 && (point.ReturnNumber == 1 || point.ReturnNumber == 2))
                {
                    var x = point.X;
                    var y = point.Y;
                    var z = point.Z;

                    _treePoints.Add(new RealLasPoint
                    {
                        X = point.X * reader.Header.XScale + reader.Header.XOffset,
                        Y = point.Y * reader.Header.YScale + reader.Header.YOffset,
                        Z = point.Z * reader.Header.ZScale + reader.Header.ZOffset,
                        Classification = point.Classification,
                        ReturnNumber = point.ReturnNumber,
                        Intensity = point.Intensity,
                    });

                    Points.Add(new Point3D(x, y, z));
                }

                counter++; 
            }

            /*var a = FindOptimalEps(_treePoints);

            List<List<RealLasPoint>> treeClusters = DBSCAN_KDTree.Cluster(_treePoints, eps: 0.75000000000000025, minPts: 7);
            List<List<RealLasPoint>> treeClusters = DBSCAN_KDTree_Limited.Cluster(_treePoints, eps: 0.75000000000000025, minPts: 7, maxClusterSize: 2500);
            var filteredClusters = treeClusters.Where(x => x.Count > 1000).ToList();

            //var filteredCluster = FilterSmallClusters(treeClusters);

            counter = 0;

            foreach (var cluster in filteredClusters)
            {
                //if (cluster.Count > 1000)
                //{
                //    foreach (var point in cluster)
                //    {
                //        FindPoints.Add(new Point3D(point.X / 0.01, point.Y / 0.01, point.Z / 0.01));
                //    }
                //}

                foreach (var point in cluster)
                {
                    FindPoints.Add(new Point3D(point.X / 0.01, point.Y / 0.01, point.Z / 0.01));
                }

                counter++;
            }*/

        }

        public static double FindOptimalEps(List<RealLasPoint> points, double startEps = 0.5, double endEps = 1.0, double step = 0.02, int minPts = 7)
        {
            double bestEps = startEps;
            double bestScore = 0;

            for (double eps = startEps; eps <= endEps; eps += step)
            {
                var clusters = DBSCAN_KDTree_Limited.Cluster(points, eps: 0.75000000000000025, minPts: 7, maxClusterSize: 2500);
                int clusterCount = clusters.Count;
                double avgSize = clusters.Average(c => c.Count);

                // Критерий качества: баланс между количеством кластеров и их размером
                double score = clusterCount * Math.Log(avgSize);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestEps = eps;
                }
            }

            return bestEps;
        }

        //public static List<List<RealLasPoint>> FilterSmallClusters( List<List<RealLasPoint>> clusters, int minTreePoints = 50, double minHeight = 2.0)
        //{
        //    return clusters.Where(c =>
        //    {
        //        if (c.Count < minTreePoints) return false;

        //        double height = c.Max(p => p.Z) - c.Min(p => p.Z);
        //        return height >= minHeight;
        //    }).ToList();
        //}

        public static List<List<RealLasPoint>> FilterSmallClusters(List<List<RealLasPoint>> clusters, int minTreePoints = 50)
        {
            return clusters.Where(c =>
            {
                if (c.Count < minTreePoints) return false;

                return true;
            }).ToList();
        }

        private RelayCommand _dbSCANCommand;
        public RelayCommand DBSCANCommand => _dbSCANCommand ??= new RelayCommand(DBSCANExecute);

        private async void DBSCANExecute()
        {

            List<List<RealLasPoint>> treeClusters = new();

            await Task.Run(() =>
            {
                treeClusters = DBSCAN_KDTree_Limited.Cluster(_treePoints, eps: double.Parse(Eps), minPts: int.Parse(MinPts), maxClusterSize:int.Parse(MaxClstPts));

                if (ClearSmallClusters)
                {
                    treeClusters = FilterVegetation(treeClusters, _allPoints, _header, double.Parse(MinHeight), int.Parse(MinTreePoints));
                }
            });

            var cluteredView = new CluteredTreeView(treeClusters);

            if (cluteredView.ShowDialog() ?? false)
            {
                
            }

            cluteredView.Close();
        }

        public List<List<RealLasPoint>> FilterVegetation(List<List<RealLasPoint>> clusters, List<LasPoint> allPoints, LasHeader header, double minHeight, int minPoints)
        {
            var filteredClusters = new List<List<RealLasPoint>>();

            foreach (var cluster in clusters)
            {
                // Пропускаем кластеры с малым количеством точек
                if (cluster.Count < minPoints)
                    continue;

                // Находим самую высокую точку в кластере (вершину кроны)
                var topPoint = cluster.OrderByDescending(p => p.Z).First();

                // Находим все точки под этой вершиной (в радиусе 1м по X,Y)
                var pointsBelow = allPoints
                    .Where(p => Math.Abs((double)(p.X * header.XScale + header.XOffset) - topPoint.X) < 1.0 &&
                                Math.Abs((double)(p.Y * header.YScale + header.YOffset) - topPoint.Y) < 1.0 &&
                                ((double)(p.Z * header.ZScale + header.ZOffset)) < topPoint.Z)
                    .ToList();

                // Если нет точек под кроной, пропускаем этот кластер
                if (!pointsBelow.Any())
                    continue;

                // Находим самую низкую точку под вершиной
                var bottomPoint = pointsBelow.OrderBy(p => p.Z)
                    .Select(p => new RealLasPoint { X = p.X, Y = p.Y, Z = ((double)(p.Z * header.ZScale + header.ZOffset)) })
                    .First();

                // Вычисляем высоту дерева
                double height = topPoint.Z - bottomPoint.Z;

                // Фильтруем по минимальной высоте
                if (height >= minHeight)
                {
                    filteredClusters.Add(cluster);
                }
            }

            return filteredClusters;
        }

        // Метод для расчета диаметра кроны
        private static double CalculateCrownDiameter(List<RealLasPoint> treePoints)
        {
            if (treePoints.Count == 0) return 0;

            double maxDistance = 0;

            // Ищем максимальное расстояние между любыми двумя точками в XY-плоскости
            for (int i = 0; i < treePoints.Count; i++)
            {
                for (int j = i + 1; j < treePoints.Count; j++)
                {
                    double dx = treePoints[i].X - treePoints[j].X;
                    double dy = treePoints[i].Y - treePoints[j].Y;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                    }
                }
            }

            return maxDistance;
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

    }
}
