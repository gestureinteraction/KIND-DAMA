using Accord.IO;
using Accord.MachineLearning;
using Accord.Math.Distances;
using Accord.Statistics.Kernels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Utilities.Logger;

namespace Recognition.ClStrategies
{


    public class DTWStrategy : ClassifierStrategy
    {

        //multivariato in quanto le features in generale sono sempre più di una
        public KNearestNeighbors<double[][]> dtwclassifier;
        //teacher training
        public DynamicTimeWarping dtw;

        //parametri del training
        //il numero di vicini da utilizzare per la classificazione
        public int k;

        //serve a serializzare il classificatore
        public byte[] binclassifier;



        public DTWStrategy(string dev_id, string strtype, object[] pars) : base(dev_id, strtype, pars)
        {
            try
            {
                k = (int)pars[0];
            }
            catch (Exception e)
            {
                Logging.Instance.Error(this.GetType(), "Eccezione:" + e);
            }
        }

        //classifica un istanza
        public override string classify(double[][] sample)
        {
            if (dtwclassifier != null)
                return index2label[dtwclassifier.Compute(sample)];
            else
                return "";
        }

        //apprende il classificatore
        public override double train(double[][][] inputs, string[] outputs)
        {

            index2label = outputs.Distinct().ToArray();
           
            int nclasses = index2label.Length;
            dtw = new DynamicTimeWarping(inputs[0][0].Length);
            int[] intoutputs = new int[outputs.Length];
            for (int i = 0; i < outputs.Length; i++)
            {
                intoutputs[i] = Array.IndexOf(index2label, outputs[i]);
            }
            
            // Run the learning algorithm
            try
            {
                dtwclassifier = new KNearestNeighbors<double[][]>(k, inputs, intoutputs, dtw);
                binarizemodel();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return 0;
        }

        public override Dictionary<string, double> classifyProbabilities(double[][] sample)
        {
            Dictionary<string, double> output = new Dictionary<string, double>();

            
            double[] scores=new double[dtwclassifier.ClassCount];

            int numlabel= dtwclassifier.Compute(sample, out scores);
            try
            {
                for (int i = 0; i < dtwclassifier.ClassCount; i++) {
                    Console.WriteLine(index2label[i]);
                    output[index2label[i]] = 0;
                        }
                output[index2label[numlabel]] = 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            
            return output;
        }

        public override double test(double[][][] testset, string[] groundtruth)
        {
            double output = 0;
            for (int i = 0; i < testset.Length; i++)
            {
                if (classify(testset[i]) != groundtruth[i])
                    output += 1;
            }
            return output;
        }

        public override void binarizemodel()
        {

            try
            {
                if (File.Exists("c:\\tmp.bin"))
                    File.Delete("c:\\tmp.bin");

                FileStream f = new FileStream("c:\\tmp.bin", FileMode.CreateNew);
                Serializer.Save<KNearestNeighbors<double[][]>>(dtwclassifier, f);
                f.Close();
                binclassifier = File.ReadAllBytes("c:\\tmp.bin");
                File.Delete("c:\\tmp.bin");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
        public override void debinarizemodel()
        {

            try
            {
                if (File.Exists("c:\\tmp.bin"))
                    File.Delete("c:\\tmp.bin");
                System.IO.File.WriteAllBytes("c:\\tmp.bin", binclassifier);
                FileStream stream2 = new FileStream("c:\\tmp.bin", FileMode.Open);
                dtwclassifier = Serializer.Load<KNearestNeighbors<double[][]>>(stream2);
                stream2.Close();
                File.Delete("c:\\tmp.bin");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
    }
}
