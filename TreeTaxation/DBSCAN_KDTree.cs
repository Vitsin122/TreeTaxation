using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TreeTaxation.LasReader;

namespace TreeTaxation
{
    public class DBSCAN_KDTree
    {
        public static List<List<RealLasPoint>> Cluster(List<RealLasPoint> points, double eps, int minPts)
        {
            // Шаг 1: Проверка входных данных
            if (points == null || points.Count == 0)
                return new List<List<RealLasPoint>>();

            // Шаг 2: Построение KD-Tree
            KDTree tree = new KDTree(points);

            // Шаг 3: Инициализация структур данных
            List<List<RealLasPoint>> clusters = new List<List<RealLasPoint>>();
            HashSet<RealLasPoint> visited = new HashSet<RealLasPoint>();
            HashSet<RealLasPoint> noise = new HashSet<RealLasPoint>();

            // Шаг 4: Основной цикл алгоритма
            foreach (var point in points)
            {
                if (visited.Contains(point))
                    continue;

                visited.Add(point);

                // Шаг 5: Поиск соседей через KD-Tree
                List<RealLasPoint> neighbors = tree.RangeSearch(point, eps);

                // Шаг 6: Проверка на шум
                if (neighbors.Count < minPts)
                {
                    noise.Add(point);
                    continue;
                }

                // Шаг 7: Создание нового кластера
                List<RealLasPoint> cluster = new List<RealLasPoint>();
                clusters.Add(cluster);

                // Шаг 8: Расширение кластера
                ExpandCluster(tree, point, neighbors, cluster, eps, minPts, visited);
            }

            return clusters;
        }

        private static void ExpandCluster(KDTree tree, RealLasPoint point,
                                        List<RealLasPoint> neighbors, List<RealLasPoint> cluster,
                                        double eps, int minPts, HashSet<RealLasPoint> visited)
        {
            // Шаг 1: Добавляем текущую точку в кластер
            cluster.Add(point);

            // Шаг 2: Используем очередь для обработки соседей
            Queue<RealLasPoint> queue = new Queue<RealLasPoint>(neighbors);

            while (queue.Count > 0)
            {
                var currentPoint = queue.Dequeue();

                if (!visited.Contains(currentPoint))
                {
                    // Шаг 3: Помечаем как посещенную
                    visited.Add(currentPoint);

                    // Шаг 4: Ищем соседей через KD-Tree
                    var currentNeighbors = tree.RangeSearch(currentPoint, eps);

                    // Шаг 5: Если точка является ядром, добавляем ее соседей в очередь
                    if (currentNeighbors.Count >= minPts)
                    {
                        foreach (var neighbor in currentNeighbors)
                        {
                            if (!queue.Contains(neighbor))
                            {
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }

                // Шаг 6: Добавляем точку в кластер, если она еще не принадлежит другому кластеру
                if (!cluster.Contains(currentPoint))
                {
                    cluster.Add(currentPoint);
                }
            }
        }
    }
}
