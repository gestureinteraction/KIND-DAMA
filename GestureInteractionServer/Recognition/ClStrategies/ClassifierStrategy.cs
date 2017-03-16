using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recognition.ClStrategies
{
   
    abstract public class ClassifierStrategy
    {
        //id del dispositivo a cui si può applicare la strategia
        public string dev_id;
        //stream al quale si può applicare la strategia
        public string strtype;
        //converte gli indici di classe in label originaria
        public string[] index2label;
        public ClassifierStrategy(string dev_id, string strtype, object[] pars) { this.dev_id = dev_id;this.strtype = strtype; }
        public virtual double test(double[][][] testset, string[] outputs) { return -1; }
        public virtual double train(double[][][] inputs, string[] outputs) { return -1; }

        //ritorna la stringa della classe più probabile
        public virtual string classify(double[][] sample) { return ""; }
        //calcola le probabilità che il campione appartenga a ciascuna delle classi
        public virtual Dictionary<string, double> classifyProbabilities(double[][] sample) { return null; }

        //converte in un array di bytes
        public abstract void binarizemodel();
        public abstract void debinarizemodel();
        
    }
}
