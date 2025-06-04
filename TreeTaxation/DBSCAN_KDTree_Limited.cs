using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TreeTaxation.LasReader;

namespace TreeTaxation
{
    public class DBSCAN_KDTree_Limited
    {
        public static List<List<RealLasPoint>> Cluster(List<RealLasPoint> points,
                                                     double eps,
                                                     int minPts,
                                                     int? maxClusterSize = null)
        {
            if (points == null || points.Count == 0)
                return new List<List<RealLasPoint>>();

            KDTree tree = new KDTree(points);
            List<List<RealLasPoint>> clusters = new List<List<RealLasPoint>>();
            HashSet<RealLasPoint> visited = new HashSet<RealLasPoint>();
            HashSet<RealLasPoint> noise = new HashSet<RealLasPoint>();

            foreach (var point in points)
            {
                if (visited.Contains(point))
                    continue;

                visited.Add(point);
                List<RealLasPoint> neighbors = tree.RangeSearch(point, eps);

                if (neighbors.Count < minPts)
                {
                    noise.Add(point);
                    continue;
                }

                List<RealLasPoint> cluster = new List<RealLasPoint>();
                clusters.Add(cluster);

                // Модифицированный метод расширения кластера
                ExpandClusterLimited(tree, point, neighbors, cluster, eps, minPts, visited, maxClusterSize);
            }

            return clusters;
        }

        private static void ExpandClusterLimited(KDTree tree,
                                               RealLasPoint point,
                                               List<RealLasPoint> neighbors,
                                               List<RealLasPoint> cluster,
                                               double eps,
                                               int minPts,
                                               HashSet<RealLasPoint> visited,
                                               int? maxClusterSize)
        {
            cluster.Add(point);
            Queue<RealLasPoint> queue = new Queue<RealLasPoint>(neighbors);

            while (queue.Count > 0)
            {
                // Проверяем ограничение размера кластера
                if (maxClusterSize.HasValue && cluster.Count >= maxClusterSize.Value)
                {
                    break; // Прекращаем расширение кластера
                }

                var currentPoint = queue.Dequeue();

                if (!visited.Contains(currentPoint))
                {
                    visited.Add(currentPoint);
                    var currentNeighbors = tree.RangeSearch(currentPoint, eps);

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

                if (!cluster.Contains(currentPoint))
                {
                    cluster.Add(currentPoint);
                }
            }
        }
    }
}
