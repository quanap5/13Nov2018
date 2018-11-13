using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xisom.OCR.Preprocessor
{
    /// <summary>
    /// This is machine learning algorithm: K-mean cluster
    /// </summary>
    class K_meanCluster
    {
        public List<DataPoint> _rawDataToCluster = new List<DataPoint>();
        public List<DataPoint> _normalizedDataToCluster = new List<DataPoint>();
        public List<DataPoint> _clusters = new List<DataPoint>();
        public int _numberOfClusters = 0;
        /// <summary>
        /// This method is used to normalize input data in form [0;1]
        /// </summary>
        private void NormalizeData()
        {
            double XSum = 0.0;
            double YSum = 0.0;
            foreach (DataPoint dataPoint in _rawDataToCluster)
            {
                XSum += dataPoint.X;
                YSum += dataPoint.Y;
            }
            double XMean = XSum / _rawDataToCluster.Count;
            double YMean = YSum / _rawDataToCluster.Count;
            double sumX = 0.0;
            double sumY = 0.0;
            foreach (DataPoint dataPoint in _rawDataToCluster)
            {
                sumX += Math.Pow(dataPoint.X - XMean, 2);
                sumY += Math.Pow(dataPoint.Y - YMean, 2);
                //sumY += (dataPoint.Y - YMean) * (dataPoint.Y - YMean);

            }
            double XSD = sumX / _rawDataToCluster.Count;
            double YSD = sumY / _rawDataToCluster.Count;
            foreach (DataPoint dataPoint in _rawDataToCluster)
            {
                _normalizedDataToCluster.Add(new DataPoint()
                {
                    X = (dataPoint.X - XMean) / XSD,
                    Y = (dataPoint.Y - YMean) / YSD
                }
                    );
            }
        }
        /// <summary>
        /// This method is used to initialize centroids
        /// </summary>
        private void InitializeCentroids()
        {
            Random random = new Random(_numberOfClusters);
            for (int i = 0; i < _numberOfClusters; ++i)
            {
                _normalizedDataToCluster[i].Cluster = _rawDataToCluster[i].Cluster = i;
            }
            for (int i = _numberOfClusters; i < _normalizedDataToCluster.Count; i++)
            {
                _normalizedDataToCluster[i].Cluster = _rawDataToCluster[i].Cluster = random.Next(0, _numberOfClusters);
            }
        }
        /// <summary>
        /// This method is used to update the mean in each iteration
        /// </summary>
        /// <returns></returns>
        private bool UpdateDataPointMeans()
        {
            if (EmptyCluster(_normalizedDataToCluster)) return false;

            var groupToComputeMeans = _normalizedDataToCluster.GroupBy(s => s.Cluster).OrderBy(s => s.Key);
            int clusterIndex = 0;
            double X = 0.0;
            double Y = 0.0;
            foreach (var item in groupToComputeMeans)
            {
                foreach (var value in item)
                {
                    X += value.X;
                    Y += value.Y;
                }
                _clusters[clusterIndex].X = X / item.Count();
                _clusters[clusterIndex].Y = Y / item.Count();
                clusterIndex++;
                X = 0.0;
                Y = 0.0;
            }
            return true;
        }
        /// <summary>
        /// This method is used to reset cluster
        /// </summary>
        /// <param name="data">This is list of datapoints</param>
        /// <returns></returns>
        private bool EmptyCluster(List<DataPoint> data)
        {
            var emptyCluster =
                data.GroupBy(s => s.Cluster).OrderBy(s => s.Key).Select(g => new { Cluster = g.Key, Count = g.Count() });

            foreach (var item in emptyCluster)
            {
                if (item.Count == 0)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// This method is used to see the relationship
        /// </summary>
        /// <returns></returns>
        private bool UpdateClusterMembership()
        {
            bool changed = false;

            double[] distances = new double[_numberOfClusters];

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _normalizedDataToCluster.Count; ++i)
            {

                for (int k = 0; k < _numberOfClusters; ++k)
                    distances[k] = ElucidanDistance(_normalizedDataToCluster[i], _clusters[k]);

                int newClusterId = MinIndex(distances);
                if (newClusterId != _normalizedDataToCluster[i].Cluster)
                {
                    changed = true;
                    _normalizedDataToCluster[i].Cluster = _rawDataToCluster[i].Cluster = newClusterId;
                    sb.AppendLine("Data Point >> X: " + _rawDataToCluster[i].X + ", Y: " +
                                  _rawDataToCluster[i].Y + " moved to Cluster # " + newClusterId);
                }
                else
                {
                    sb.AppendLine("No change.");
                }
                sb.AppendLine("------------------------------");
                Debug.WriteLine(sb.ToString());

            }
            if (changed == false)
                return false;
            if (EmptyCluster(_normalizedDataToCluster)) return false;
            return true;
        }
        /// <summary>
        /// This method is used to calculate ElucidanceDistance
        /// </summary>
        /// <param name="dataPoint">This is input data</param>
        /// <param name="mean">This is mean data</param>
        /// <returns>Return the Elucian distance between the input data and mean data</returns>
        private double ElucidanDistance(DataPoint dataPoint, DataPoint mean)
        {
            double _diffs = 0.0;
            _diffs = Math.Pow(dataPoint.X - mean.X, 2);
            _diffs += Math.Pow(dataPoint.Y - mean.Y, 2);
            return Math.Sqrt(_diffs);
        }

        private int MinIndex(double[] distances)
        {
            int _indexOfMin = 0;
            double _smallDist = distances[0];
            for (int k = 0; k < distances.Length; ++k)
            {
                if (distances[k] < _smallDist)
                {
                    _smallDist = distances[k];
                    _indexOfMin = k;
                }
            }
            return _indexOfMin;
        }
        /// <summary>
        /// This method is used for clustering
        /// </summary>
        public void Cluster()
        {
            bool _changed = true;
            bool _success = true;
            InitializeCentroids();

            int maxIteration = _normalizedDataToCluster.Count * 10;
            int _threshold = 0;
            while (_success == true && _changed == true && _threshold < maxIteration)
            {
                ++_threshold;
                _success = UpdateDataPointMeans();
                _changed = UpdateClusterMembership();
            }
        }

        public List<DataPoint> runKmean()
        {
            //_rawDataToCluster.Add(new DataPoint(1, 1));
            //_rawDataToCluster.Add(new DataPoint(4, 4));
            //_rawDataToCluster.Add(new DataPoint(5, 5));
            //_rawDataToCluster.Add(new DataPoint(6, 7));
            //InitilizeRawData();// read input data
            _numberOfClusters = 3; // tu khai
            NormalizeData();

            //initialize the clusters (Setting indeces)
            for (int i = 0; i < _numberOfClusters; i++)
            {
                _clusters.Add(new DataPoint() { Cluster = i });
            }

            Cluster();
            StringBuilder sb = new StringBuilder();
            var group = _rawDataToCluster.GroupBy(s => s.Cluster).OrderBy(s => s.Key);
            foreach (var g in group)
            {
                sb.AppendLine("Cluster # " + g.Key + ":");
                foreach (var value in g)
                {
                    sb.Append("(");
                    sb.Append(value.X.ToString());
                    sb.Append(",");
                    sb.Append(value.Y.ToString());
                    sb.Append(")");
                    sb.AppendLine();
                }
                sb.AppendLine("------------------------------");
            }
            Debug.WriteLine(sb.ToString());
            return _rawDataToCluster;
        }

    }

    /// <summary>
    /// This is datapoint that is used in k-mean cluster
    /// </summary>
    internal class DataPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public int Cluster { get; set; }
        public DataPoint(double x, double y)
        {
            X = x;
            Y = y;
            Cluster = 0;
        }
        public DataPoint() { }


    }
}
