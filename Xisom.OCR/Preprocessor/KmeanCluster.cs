﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xisom.OCR.Preprocessor
{
    class KmeanCluster
    {
        public void Test()
        {
            Console.WriteLine("\nBegin k-means clustering demo\n");

            // real data likely to come from a text file or SQL
            double[][] rawData = new double[57][];
            rawData[0] = new double[] { 291.5, 81.5 };
            rawData[1] = new double[] { 316, 87.5 };
            rawData[2] = new double[] { 337, 92.5 };
            rawData[3] = new double[] { 367, 87 };
            rawData[4] = new double[] { 363.5, 102 };
            rawData[5] = new double[] { 378, 105 };
            rawData[6] = new double[] { 411, 108.5 };
            rawData[7] = new double[] { 428.5, 116.5 };
            rawData[8] = new double[] { 465.5, 120 };
            rawData[9] = new double[] { 477, 111.5 };
            rawData[10] = new double[] { 448.5, 124.5 };
            rawData[11] = new double[] { 276, 126.5 };
            rawData[12] = new double[] { 503.5, 129 };
            rawData[13] = new double[] { 474, 126.5 };
            rawData[14] = new double[] { 485.5, 129 };
            rawData[15] = new double[] { 293.5, 134.5 };
            rawData[16] = new double[] { 313, 138.5 };
            rawData[17] = new double[] { 333.5, 146.5 };
            rawData[18] = new double[] { 355, 147.5 };
            rawData[19] = new double[] { 373.5, 152.5 };

            rawData[20] = new double[] { 393, 160 };
            rawData[21] = new double[] { 413.5, 161 };
            rawData[22] = new double[] { 98.5, 327.5 };
            rawData[23] = new double[] { 113, 338.5 };
            rawData[24] = new double[] { 130.5, 344.5 };
            rawData[25] = new double[] { 146.5, 347.5 };
            rawData[26] = new double[] { 171, 355 };
            rawData[27] = new double[] { 189, 364.5 };
            rawData[28] = new double[] { 223.5, 372 };
            rawData[29] = new double[] { 208, 374.5 };
            rawData[30] = new double[] { 237, 365.5 };
            rawData[31] = new double[] { 74, 379 };
            rawData[32] = new double[] { 232, 379.5 };
            rawData[33] = new double[] { 262, 385.5 };
            rawData[34] = new double[] { 244, 384 };
            rawData[35] = new double[] { 92, 388.5 };
            rawData[36] = new double[] { 112, 395 };
            rawData[37] = new double[] { 131, 405 };
            rawData[38] = new double[] { 152, 408.5 };
            rawData[39] = new double[] { 170.5, 415.5 };
            rawData[40] = new double[] { 485, 421.5 };
            rawData[41] = new double[] { 546, 421.5 };
            rawData[42] = new double[] { 742, 421.5 };
            rawData[43] = new double[] { 189, 425 };
            rawData[44] = new double[] { 506.5, 424.5 };
            rawData[45] = new double[] { 528.5, 424.5 };
            rawData[46] = new double[] { 583, 424.5 };
            rawData[47] = new double[] { 604, 424.5 };
            rawData[48] = new double[] { 624.5, 424.5 };

            rawData[49] = new double[] { 653, 430 };
            rawData[50] = new double[] { 695, 424.5 };
            rawData[51] = new double[] { 764.5, 424.5 };
            rawData[52] = new double[] { 721, 425 };
            rawData[53] = new double[] { 208, 428.5 };
            rawData[54] = new double[] { 242, 436.5 };
            rawData[55] = new double[] { 267, 445 };
            rawData[56] = new double[] { 286.5, 452 };


            Console.WriteLine("Raw unclustered data:\n");
            Console.WriteLine("    Height Weight");
            Console.WriteLine("-------------------");
            ShowData(rawData, 1, true, true);

            int numClusters = 3;
            Console.WriteLine("\nSetting numClusters to " + numClusters);

            int[] clustering = Cluster(rawData, numClusters); // this is it

            Console.WriteLine("\nK-means clustering complete\n");

            Console.WriteLine("Final clustering in internal form:\n");
            ShowVector(clustering, true);

            Console.WriteLine("Raw data by cluster:\n");
            ShowClustered(rawData, clustering, numClusters, 1);

            Console.WriteLine("\nEnd k-means clustering demo\n");
            Console.ReadLine();

        } // Main

        // ============================================================================

        public static int[] Cluster(double[][] rawData, int numClusters)
        {
            // k-means clustering
            // index of return is tuple ID, cell is cluster ID
            // ex: [2 1 0 0 2 2] means tuple 0 is cluster 2, tuple 1 is cluster 1, tuple 2 is cluster 0, tuple 3 is cluster 0, etc.
            // an alternative clustering DS to save space is to use the .NET BitArray class
            double[][] data = Normalized(rawData); // so large values don't dominate

            bool changed = true; // was there a change in at least one cluster assignment?
            bool success = true; // were all means able to be computed? (no zero-count clusters)

            // init clustering[] to get things started
            // an alternative is to initialize means to randomly selected tuples
            // then the processing loop is
            // loop
            //    update clustering
            //    update means
            // end loop
            int[] clustering = InitClustering(data.Length, numClusters, 0); // semi-random initialization
            double[][] means = Allocate(numClusters, data[0].Length); // small convenience

            int maxCount = data.Length * 10; // sanity check
            int ct = 0;
            while (changed == true && success == true && ct < maxCount)
            {
                ++ct; // k-means typically converges very quickly
                success = UpdateMeans(data, clustering, means); // compute new cluster means if possible. no effect if fail
                changed = UpdateClustering(data, clustering, means); // (re)assign tuples to clusters. no effect if fail
            }
            // consider adding means[][] as an out parameter - the final means could be computed
            // the final means are useful in some scenarios (e.g., discretization and RBF centroids)
            // and even though you can compute final means from final clustering, in some cases it
            // makes sense to return the means (at the expense of some method signature uglinesss)
            //
            // another alternative is to return, as an out parameter, some measure of cluster goodness
            // such as the average distance between cluster means, or the average distance between tuples in 
            // a cluster, or a weighted combination of both
            return clustering;
        }

        private static double[][] Normalized(double[][] rawData)
        {
            // normalize raw data by computing (x - mean) / stddev
            // primary alternative is min-max:
            // v' = (v - min) / (max - min)

            // make a copy of input data
            double[][] result = new double[rawData.Length][];
            for (int i = 0; i < rawData.Length; ++i)
            {
                result[i] = new double[rawData[i].Length];
                Array.Copy(rawData[i], result[i], rawData[i].Length);
            }

            for (int j = 0; j < result[0].Length; ++j) // each col
            {
                double colSum = 0.0;
                for (int i = 0; i < result.Length; ++i)
                    colSum += result[i][j];
                double mean = colSum / result.Length;
                double sum = 0.0;
                for (int i = 0; i < result.Length; ++i)
                    sum += (result[i][j] - mean) * (result[i][j] - mean);
                double sd = sum / result.Length;
                for (int i = 0; i < result.Length; ++i)
                    result[i][j] = (result[i][j] - mean) / sd;
            }
            return result;
        }

        private static int[] InitClustering(int numTuples, int numClusters, int randomSeed)
        {
            // init clustering semi-randomly (at least one tuple in each cluster)
            // consider alternatives, especially k-means++ initialization,
            // or instead of randomly assigning each tuple to a cluster, pick
            // numClusters of the tuples as initial centroids/means then use
            // those means to assign each tuple to an initial cluster.
            Random random = new Random(randomSeed);
            int[] clustering = new int[numTuples];
            for (int i = 0; i < numClusters; ++i) // make sure each cluster has at least one tuple
                clustering[i] = i;
            for (int i = numClusters; i < clustering.Length; ++i)
                clustering[i] = random.Next(0, numClusters); // other assignments random
            return clustering;
        }

        private static double[][] Allocate(int numClusters, int numColumns)
        {
            // convenience matrix allocator for Cluster()
            double[][] result = new double[numClusters][];
            for (int k = 0; k < numClusters; ++k)
                result[k] = new double[numColumns];
            return result;
        }

        private static bool UpdateMeans(double[][] data, int[] clustering, double[][] means)
        {
            // returns false if there is a cluster that has no tuples assigned to it
            // parameter means[][] is really a ref parameter

            // check existing cluster counts
            // can omit this check if InitClustering and UpdateClustering
            // both guarantee at least one tuple in each cluster (usually true)
            int numClusters = means.Length;
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false; // bad clustering. no change to means[][]

            // update, zero-out means so it can be used as scratch matrix 
            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                    means[k][j] = 0.0;

            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                for (int j = 0; j < data[i].Length; ++j)
                    means[cluster][j] += data[i][j]; // accumulate sum
            }

            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                    means[k][j] /= clusterCounts[k]; // danger of div by 0
            return true;
        }

        private static bool UpdateClustering(double[][] data, int[] clustering, double[][] means)
        {
            // (re)assign each tuple to a cluster (closest mean)
            // returns false if no tuple assignments change OR
            // if the reassignment would result in a clustering where
            // one or more clusters have no tuples.

            int numClusters = means.Length;
            bool changed = false;

            int[] newClustering = new int[clustering.Length]; // proposed result
            Array.Copy(clustering, newClustering, clustering.Length);

            double[] distances = new double[numClusters]; // distances from curr tuple to each mean

            for (int i = 0; i < data.Length; ++i) // walk thru each tuple
            {
                for (int k = 0; k < numClusters; ++k)
                    distances[k] = Distance(data[i], means[k]); // compute distances from curr tuple to all k means

                int newClusterID = MinIndex(distances); // find closest mean ID
                if (newClusterID != newClustering[i])
                {
                    changed = true;
                    newClustering[i] = newClusterID; // update
                }
            }

            if (changed == false)
                return false; // no change so bail and don't update clustering[][]

            // check proposed clustering[] cluster counts
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = newClustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false; // bad clustering. no change to clustering[][]

            Array.Copy(newClustering, clustering, newClustering.Length); // update
            return true; // good clustering and at least one change
        }

        private static double Distance(double[] tuple, double[] mean)
        {
            // Euclidean distance between two vectors for UpdateClustering()
            // consider alternatives such as Manhattan distance
            double sumSquaredDiffs = 0.0;
            for (int j = 0; j < tuple.Length; ++j)
                sumSquaredDiffs += Math.Pow((tuple[j] - mean[j]), 2);
            return Math.Sqrt(sumSquaredDiffs);
        }

        private static int MinIndex(double[] distances)
        {
            // index of smallest value in array
            // helper for UpdateClustering()
            int indexOfMin = 0;
            double smallDist = distances[0];
            for (int k = 0; k < distances.Length; ++k)
            {
                if (distances[k] < smallDist)
                {
                    smallDist = distances[k];
                    indexOfMin = k;
                }
            }
            return indexOfMin;
        }

        // ============================================================================

        // misc display helpers for demo

        static void ShowData(double[][] data, int decimals, bool indices, bool newLine)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                if (indices) Console.Write(i.ToString().PadLeft(3) + " ");
                for (int j = 0; j < data[i].Length; ++j)
                {
                    if (data[i][j] >= 0.0) Console.Write(" ");
                    Console.Write(data[i][j].ToString("F" + decimals) + " ");
                }
                Console.WriteLine("");
            }
            if (newLine) Console.WriteLine("");
        } // ShowData

        static void ShowVector(int[] vector, bool newLine)
        {
            for (int i = 0; i < vector.Length; ++i)
                Console.Write(vector[i] + " ");
            if (newLine) Console.WriteLine("\n");
        }

        static void ShowClustered(double[][] data, int[] clustering, int numClusters, int decimals)
        {
            for (int k = 0; k < numClusters; ++k)
            {
                Console.WriteLine("===================");
                for (int i = 0; i < data.Length; ++i)
                {
                    int clusterID = clustering[i];
                    if (clusterID != k) continue;
                    Console.Write(i.ToString().PadLeft(3) + " ");
                    for (int j = 0; j < data[i].Length; ++j)
                    {
                        if (data[i][j] >= 0.0) Console.Write(" ");
                        Console.Write(data[i][j].ToString("F" + decimals) + " ");
                    }
                    Console.WriteLine("");
                }
                Console.WriteLine("===================");
            } // k
        }
    }
}
