using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TreeTaxation.LasReader;

namespace TreeTaxation
{
    public class KDTree
    {
        private Node root;
        private readonly int dimensions = 3; // X, Y, Z

        private class Node
        {
            public RealLasPoint Point { get; set; }
            public Node Left { get; set; }
            public Node Right { get; set; }
            public int Axis { get; set; }

            public Node(RealLasPoint point, int axis)
            {
                Point = point;
                Axis = axis;
            }
        }

        public KDTree(List<RealLasPoint> points)
        {
            root = BuildTree(points, 0);
        }

        private Node BuildTree(List<RealLasPoint> points, int depth)
        {
            if (points == null || points.Count == 0)
                return null;

            int axis = depth % dimensions;

            // Сортируем точки по текущей оси
            var sortedPoints = axis == 0 ? points.OrderBy(p => p.X) :
                              axis == 1 ? points.OrderBy(p => p.Y) :
                                          points.OrderBy(p => p.Z);

            int median = points.Count / 2;
            var node = new Node(sortedPoints.ElementAt(median), axis);

            // Рекурсивно строим левое и правое поддеревья
            node.Left = BuildTree(sortedPoints.Take(median).ToList(), depth + 1);
            node.Right = BuildTree(sortedPoints.Skip(median + 1).ToList(), depth + 1);

            return node;
        }

        public List<RealLasPoint> RangeSearch(RealLasPoint target, double radius)
        {
            List<RealLasPoint> result = new List<RealLasPoint>();
            RangeSearch(root, target, radius, result);
            return result;
        }

        private void RangeSearch(Node node, RealLasPoint target, double radius, List<RealLasPoint> result)
        {
            if (node == null)
                return;

            double distance = CalculateDistance(node.Point, target);
            if (distance <= radius)
            {
                result.Add(node.Point);
            }

            double axisValue = node.Axis == 0 ? node.Point.X :
                             (node.Axis == 1 ? node.Point.Y : node.Point.Z);
            double targetValue = node.Axis == 0 ? target.X :
                                (node.Axis == 1 ? target.Y : target.Z);

            if (targetValue - radius <= axisValue)
            {
                RangeSearch(node.Left, target, radius, result);
            }

            if (targetValue + radius >= axisValue)
            {
                RangeSearch(node.Right, target, radius, result);
            }
        }

        private static double CalculateDistance(RealLasPoint a, RealLasPoint b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            double dz = a.Z - b.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
