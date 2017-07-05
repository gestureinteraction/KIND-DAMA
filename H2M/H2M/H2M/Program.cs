using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace H2M
{
    static class Program
    {
        /// <summary>
        /// Punto di ingresso principale dell'applicazione.
        /// </summary>
        [STAThread]
        static void Main()
        {


            float maxY = Screen.PrimaryScreen.WorkingArea.Height;
            float maxX = Screen.PrimaryScreen.WorkingArea.Width;
            float x = 800;
            float y = 600;
           
            VirtualMouse.MoveTo(x, y,maxX,maxY);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form1 f = new Form1();
            Application.Run(f);
        }
    }



}
