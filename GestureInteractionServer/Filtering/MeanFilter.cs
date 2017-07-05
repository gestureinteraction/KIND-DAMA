using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureInteractionServer.Filtering
{
    public class MeanFilter:Filter
    {
        //da completare e testare
        //dimensione buffer= numero di punti precedenti per il filtraggio
        int buffer_size;
        
        //dimensionalita' del punto in input: N, il punto verrà convertito in un double[N]
        int point_length;

        double[,] buffer;

        public override void init(Dictionary<string, object> init_pars){
            point_length = (int) init_pars["point_length"];
            buffer_size = (int)init_pars["buffer_size"];

            buffer = new double[buffer_size,point_length];
            
        }

        public override Array filter(Array point_to_filter)
        {
            
            double[] output = new double[point_length];
            double[] punto = new double[point_length];
            for (int i = 0; i < punto.Length; i++)
            {
                punto[i] = (double)point_to_filter.GetValue(i);
            }

            for (int i = 0; i < point_length; i++)
            {
                for (int j = 0; j < buffer_size; j++)
                    output[i] = output[i] + buffer[j, i];
                output[i] = output[i] / buffer_size;
            }
            for (int i = 0; i < point_length; i++)
            {
                for (int j = 0; j < buffer_size; j++)
                    buffer[j, i] = buffer[j + 1, i];
            }
            for(int i = 0; i < point_length; i++)
            {
                buffer[buffer_size - 1, i] = punto[i];
            }



            return (Array)output;
        }
    }
}
