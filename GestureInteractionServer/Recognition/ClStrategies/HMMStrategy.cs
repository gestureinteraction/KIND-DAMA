using System.Linq;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Multivariate;
using Accord.Statistics.Models.Markov;
using Accord.Statistics.Models.Markov.Learning;
using Accord.Statistics.Models.Markov.Topology;
using System;
using System.Collections.Generic;
using Utilities.Logger;
using System.IO;

namespace Recognition.ClStrategies
{

    
    public class HMMStrategy: ClassifierStrategy
    {

        //multivariato in quanto le features in generale sono sempre più di una
        public HiddenMarkovClassifier<MultivariateNormalDistribution> hmm { get; set; }
        //teacher training
        public HiddenMarkovClassifierLearning<MultivariateNormalDistribution> teacher;

        //parametri del training
        public int states;
        public int iterations;
        public double tolerance;
        public bool rejection;
        public double regular;
        public bool empir;

        //serve a serializzare l'hmm
        public byte[] binhmm;

       
        
        public HMMStrategy(string dev_id, string strtype, object[] pars):base(dev_id,strtype,pars)
        {
            try
            {
                states = (int)pars[0];
                iterations = (int)pars[1];
                tolerance = (double)pars[2];
                rejection = (bool)pars[3];
                regular = (double)pars[4];
                empir = (bool)pars[5];

            }
            catch (Exception e)
            {
                Logging.Instance.Error(this.GetType(), "Eccezione:"+e);
            }
        }

        //classifica un istanza
        public override string classify(double[][] sample)
        {
            if (hmm != null)
                return index2label[hmm.Compute(sample)];
            else
                return "";
        }

        //apprende l'hmm
        public override double train(double[][][] inputs, string[] outputs) 
        {

            index2label = outputs.Distinct().ToArray();
            int nclasses = index2label.Length;


            hmm = new HiddenMarkovClassifier<MultivariateNormalDistribution>(nclasses, new Forward(states),
                new MultivariateNormalDistribution(inputs[0][0].Length));


            // Create the learning algorithm for the ensemble classifier
            teacher = new HiddenMarkovClassifierLearning<MultivariateNormalDistribution>(hmm,

                // Train each model using the selected convergence criteria
                i => new BaumWelchLearning<MultivariateNormalDistribution>(hmm.Models[i])
                {
                    Tolerance = tolerance,
                    Iterations = iterations,

                    FittingOptions = new NormalOptions()
                    {
                        Regularization = regular
                    }
                }
            );

            teacher.Empirical = empir;
            teacher.Rejection = rejection;

            int[] intoutputs=new int[outputs.Length];
            for (int i=0; i<outputs.Length; i++)
            {
                intoutputs[i] = Array.IndexOf(index2label, outputs[i]);
            }
            // Run the learning algorithm
            try {
                teacher.Run(inputs, intoutputs);
                binarizemodel();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            return teacher.ComputeError(inputs, intoutputs);
        }

        public override Dictionary<string,double> classifyProbabilities(double[][] sample)
        {
            Dictionary<string, double> output = new Dictionary<string, double>();
            
            try {
                for (int i = 0; i < hmm.Classes; i++)
                    output[index2label[i]] = Math.Exp(hmm.LogLikelihood(sample, i));
            }
            catch(Exception e)
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

            try {
                if (File.Exists("c:\\tmp.bin"))
                    File.Delete("c:\\tmp.bin");

                FileStream f = new FileStream("c:\\tmp.bin", FileMode.CreateNew);
                hmm.Save(f);
                f.Close();
                binhmm = File.ReadAllBytes("c:\\tmp.bin");
                File.Delete("c:\\tmp.bin");
            }
            catch(Exception e)
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
                System.IO.File.WriteAllBytes("c:\\tmp.bin",binhmm);
                FileStream stream2 = new FileStream("c:\\tmp.bin", FileMode.Open);
                hmm = HiddenMarkovClassifier<MultivariateNormalDistribution>.Load(stream2);
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
