using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using NI = NationalInstruments.NI4882;

namespace Lab6FormsApplication
{
    public partial class Form1 : Form
    {
        DMM dmm1 = new DMM(1); 

        public Form1()
        {
            InitializeComponent();

            for (int i = 0; i < 100; i++)
            {
                this.dataGridView1.Rows.Add(); //create table in the form
            }   
        }

        private void incButton_Click(object sender, EventArgs e)
        {
            List<double> dmm1Res = new List<double>();

                    for (int i = 0; i < 100; i++) 
                    {
                        dmm1Res.Add(dmm1.measureResistance()); 
                        this.dataGridView1.Rows[i].Cells[0].Value = dmm1Res[i]; 
                        serialPort2.WriteLine("i");
                        Thread.Sleep(500); 
                    }
            
        }
       
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            serialPort2.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            serialPort2.Open();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            serialPort2.WriteLine("d");
        }

        private void uploadButton_Click(object sender, EventArgs e)
        {
            var boardId = textBox1.Text;
            char groupId = Convert.ToChar(textBox2.Text);
            var cap = getCap();
            /* Grab data from spreadsheet and place in lists */
            for (int i = 0; i < 100; i++)
            {

                var resistance = Convert.ToSingle(dataGridView1.Rows[i].Cells[0].Value);
                var onTime = Convert.ToSingle(dataGridView1.Rows[i].Cells[1].Value);

                var res = new EET321_Lab6DataContext();
                var data = new EET321_Lab6_Table();


                data.GroupID = groupId;
                data.BoardID = boardId;
                data.DateTime = DateTime.Now;
                data.Resistance = resistance;
                data.OnTime = onTime;
                data.Capacitance = cap;

                res.EET321_Lab6_Tables.InsertOnSubmit(data);//insert data to server
                res.SubmitChanges(); //submit changes
            }

       
        }

        private float getCap()
        {
            var capStr = capValue.Text;
            var capacitance = 0F;
            switch (capStr)
            {
                case "1uF":
                    capacitance = 1e-6F;
                    break;
                case "2.2uF":
                    capacitance = 2.2e-6F;
                    break;
                case "3.3uF":
                    capacitance = 3.3e-6F;
                    break;
                case "4.7uF":
                    capacitance = 4.7e-6F;
                    break;
                case "10uF":
                    capacitance = 10e-6F;
                    break;
            }
            return capacitance;
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }
    }

 
}

    /* Thanks Jonny */
    class DMM
    {
        NI.Device device;
        //Initialize the DMM with an address
        public DMM(byte addr)
        {
            device = new NI.Device(0, addr);
        }

        //Does exactly what you think it should
        public double measureResistance()
        {
            device.Write("MEAS:RES?"); 
            var str = device.ReadString(); //reads value from DMM
            double d = 0.0;

            /* If it cannot parse the str then try and measure again */
            try
            {
                d = double.Parse(str);
            }
            catch
            {
                //weird error look at dmm
                d = this.measureResistance();
            }
            return d;
        }
    }


