using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public class TypeSolver
    {
        public static Type[] getTypeByName(string className)
        {

            //ritorna la lista di tutti i tipi che hanno come classname quello specificato
            List<Type> returnVal = new List<Type>();

            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] assemblyTypes = a.GetTypes();
                for (int j = 0; j < assemblyTypes.Length; j++)
                {
                    if (assemblyTypes[j].Name == className)
                    {
                        returnVal.Add(assemblyTypes[j]);
                    }
                }
            }

            return returnVal.ToArray();
        }
    }
}
