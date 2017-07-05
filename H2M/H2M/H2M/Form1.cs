using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace H2M
{
    public partial class Form1 : Form
    {
        
        private DeviceConnector device;


        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            device = new DeviceConnector(this);
        }


        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            base.OnPaint(e);
            this.Location = new Point(0, 0);
            System.Drawing.SolidBrush myBrush;
            if (device.device_connected)
            {
                myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Green);
                label1.Text = "device is tracking....";
            }
            else
            {
                myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);
                label1.Text = "device not connected";
            }
            System.Windows.Forms.Cursor.Position = new Point(600, 400);
            System.Drawing.Graphics formGraphics;
            formGraphics = this.CreateGraphics();
            formGraphics.FillEllipse(myBrush, new Rectangle(0, 0, 20, 20));
            myBrush.Dispose();
            formGraphics.Dispose();

        }

    
    }
}
