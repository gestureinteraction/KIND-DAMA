using Accord.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Logger;

namespace Recognition.ClStrategies
{
    public class RecurrentNetworkStrategy: ClassifierStrategy
    {


        //da completare implementazione
        //serve a serializzare la rnn
        public byte[] binrnn;
        public RecurrentNetworkModel rnn;
        public int nstates;


        public RecurrentNetworkStrategy(string dev_id, string strtype, object[] pars):base(dev_id,strtype,pars)
        {
            try
            {
                nstates = (int)pars[0];

            }
            catch (Exception e)
            {
                Logging.Instance.Error(this.GetType(), "Eccezione:" + e);
            }
        }


        public override void binarizemodel()
        {

            try
            {
                if (File.Exists("c:\\tmp.bin"))
                    File.Delete("c:\\tmp.bin");

                FileStream f = new FileStream("c:\\tmp.bin", FileMode.CreateNew);
                Serializer.Save<RecurrentNetworkModel>(rnn, f);
                f.Close();
                binrnn = File.ReadAllBytes("c:\\tmp.bin");
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
                System.IO.File.WriteAllBytes("c:\\tmp.bin", binrnn);
                FileStream stream2 = new FileStream("c:\\tmp.bin", FileMode.Open);
                rnn = Serializer.Load<RecurrentNetworkModel>(stream2);
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
