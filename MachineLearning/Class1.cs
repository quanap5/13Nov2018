using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Drawing;
using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Distributions.DensityKernels;
using Accord.Math.Distances;

namespace MachineLearning
{
    public class Class1
    {
        static void Main()
        {
            Accord.Math.Random.Generator.Seed = 1232;

            // Declare some observations
            double[][] observations =
            {
                new double[] { 291.5,81.5},
                new double[] { 316,87.5},
                new double[] { 337,92.5},
                new double[] { 367,87},
                new double[] { 363.5,102},
                new double[] { 378,105},
                new double[] { 411,108.5},
                new double[] { 428.5,116.5},
                new double[] { 465.5,120},
                new double[] { 477,111.5},
                new double[] { 448.5,124.5},
                new double[] { 276,126.5},
                new double[] { 503.5,129},
                new double[] { 474,126.5},
                new double[] { 485.5,129},
                new double[] { 293.5,134.5},
                new double[] { 313,138.5},
                new double[] { 333.5,146.5},
                new double[] { 355,147.5},
                new double[] { 373.5,152.5},
                new double[] { 393,160},
                new double[] { 413.5,161},
                new double[] { 98.5,327.5},
                new double[] { 113,338.5},
                new double[] { 130.5,344.5},
                new double[] { 146.5,347.5},
                new double[] { 171,355},
                new double[] { 189,364.5},
                new double[] { 223.5,372},
                new double[] { 208,374.5},
                new double[] { 237,365.5},
                new double[] { 74,379},
                new double[] { 232,379.5},
                new double[] { 262,385.5},
                new double[] { 244,384},
                new double[] { 92,388.5},
                new double[] { 112,395},
                new double[] { 131,405},
                new double[] { 152,408.5},
                new double[] { 170.5,415.5},
                new double[] { 485,421.5},
                new double[] { 546,421.5},
                new double[] { 742,421.5},
                new double[] { 189,425},
                new double[] { 506.5,424.5},
                new double[] { 528.5,424.5},
                new double[] { 583,424.5},
                new double[] { 604,424.5},
                new double[] { 624.5,424.5},
                new double[] { 653,430},
                new double[] { 695,424.5},
                new double[] { 764.5,424.5},
                new double[] { 721,425},
                new double[] { 208,428.5},
                new double[] { 242,436.5},
                new double[] { 267,445},
                new double[] { 286.5,452},
            };

            //observations.Add(new double[][] { (double)2.8, 3.3 });

            // Create a new K-Means algorithm
            KMeans kmeans = new KMeans(k: 3);

            // Compute and retrieve the data centroids
            var clusters = kmeans.Learn(observations);

            // Use the centroids to parition all the data
            int[] labels = clusters.Decide(observations);

            Console.WriteLine("Hello World!");

            // Keep the console window open in debug mode.
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
