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
using System.Text.RegularExpressions;

namespace Lab6FormsApplication
{
    public partial class Form1 : Form
    {
        DMM dmm1 = new DMM(1);
        DSO dso = new DSO(2);            //declares dso from dso class
        List<float> dsodata;            //declares float list
        NI.Device device;

        public Form1()
        {
            InitializeComponent();

            for (int i = 0; i < 100; i++)
            {
                this.dataGridView1.Rows.Add(); //create table in the form
            }
            for (int i = 0; i < 100; i++)
            {
                this.dataGridView2.Rows.Add(); //create table in the form
            }
        }

        private void incButton_Click(object sender, EventArgs e)
        {

            for (int i = 0; i < 100; i++){
                this.dataGridView2.Rows[i].Cells[0].Value = dmm1.measureResistance();//place resistance on chart
                serialPort2.WriteLine("i");
                Thread.Sleep(500); 
             }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                this.dataGridView1.Rows[i].Cells[1].Value = getTime();
                serialPort2.WriteLine("i");
                Thread.Sleep(500);
            }
        }
        private float getTime()
        {
            dso.clearOffset();
            dso.setTimeScale(1.0f / (500.0f));
            dso.setScale(20.0F);
            dso.setTrigerSlopePos();
            dso.setTrigerLevel(-0.3F);
            dso.setCoupling(DSO.Coupling.DC);
            dso.clearMeasure();
            Thread.Sleep(500);
            var data = dso.getdata();
            data = data.Skip(data.Length / 2).ToArray();
            var xinc = dso.getXInc();
            var time = data.Where((d) => Math.Abs(d) < 1).ToArray().Length * xinc; //gets on time

            return time;
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
            System.Windows.Forms.MessageBox.Show("Resitor Reseting!");
            Thread.Sleep(12000); 
        }

        private void uploadButton_Click(object sender, EventArgs e)
        {
            var boardId = textBox2.Text;
            char groupId = Convert.ToChar(textBox1.Text);
            var cap = getCap();
            /* Grab data from spreadsheet and place in lists */
            for (int i = 0; i < 100; i++)
            {

                var onTime = Convert.ToSingle(dataGridView1.Rows[i].Cells[1].Value);

                var res = new EET321_Lab6DataContext();
                var data = new Table_1();


                data.GroupID = groupId;
                data.BoardID = boardId;
                data.DateTime = DateTime.Now;
                data.OnTime = onTime;
                data.Step = i;
                data.Capacitance = cap;

                res.Table_1s.InsertOnSubmit(data);//insert data to server
                res.SubmitChanges(); //submit changes
                
            }
            System.Windows.Forms.MessageBox.Show("Upload complete!");
        }

        private void uploadResistanceButton_Click(object sender, EventArgs e)
        {
            var boardId = textBox2.Text;
            char groupId = Convert.ToChar(textBox1.Text);

                        /* Grab data from spreadsheet and place in lists */
            for (int i = 0; i < 100; i++)
            {

                var resistance = Convert.ToSingle(dataGridView2.Rows[i].Cells[0].Value);

                var res = new EET321_Lab6DataContext();
                var data = new Table_Resistance();


                data.GroupID = groupId;
                data.BoardID = boardId;
                data.DateTime = DateTime.Now;
                data.Resistance = resistance;

                res.Table_Resistances.InsertOnSubmit(data);
                res.SubmitChanges(); //submit changes
                
            }
            System.Windows.Forms.MessageBox.Show("Upload complete!");
        }
        private void uploadManual_Click(object sender, EventArgs e)
        {
            var boardId = textBox2.Text;
            char groupId = Convert.ToChar(textBox1.Text);

            var res = new EET321_Lab6DataContext();
            var data = new Table_2();

            data.GroupID = groupId;
            data.BoardID = boardId;
            data.DateTime = DateTime.Now;
            data.MaxOnTime = Convert.ToSingle(textBox6.Text);
            data.MinOnTime = Convert.ToSingle(textBox4.Text);
            data.MaxTimeResistance = Convert.ToSingle(textBox5.Text);
            data.MinTimeResistance = Convert.ToSingle(textBox3.Text);
           
            res.Table_2s.InsertOnSubmit(data);
            res.SubmitChanges(); //submit changes
            System.Windows.Forms.MessageBox.Show("Upload complete!");
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
    //Helper class for the DSO
    class DSO
    {
        public enum Coupling
        {
            DC,
            AC,
            GND
        }
        NI.Device device;
        //Initialize the DSO with an address
        public DSO(byte addr)
        {
            device = new NI.Device(0, addr);        //dso address and board number 0
        }

        public void clearOffset()
        {
            device.Write(":CHAN1:OFFS 0");      //writes 0 to chan1 of the dso
        }

        public void setTimeScale(float scale)
        {
            device.Write(":TIM:SCAL " + scale.ToString("E"));           //sends command to set time scale of dso
            this.isDone();
        }

        public void setScale(float scale)
        {
            device.Write(":CHAN1:SCAL " + scale.ToString("E"));     //sends command to set voltage scale of dso
        }

        //Not really sure what this is for...
        //the documentation said to use it every once in awhile
        //I guess it tells when the last command was finished or something.
        public int isDone()
        {
            device.Write("*OPC?");              //asks if operation is complete in query
            var data = device.ReadString();        //reads the string sent from
            return int.Parse(data);
        }

        public void setTrigerSlopePos()
        {
            device.Write(":TRIG:SLOP: POS");
        }

        public void setTrigerLevel(float level)
        {
            device.Write(":TRIG:EDGE:LEV " + level.ToString("f"));
        }

        public void setCoupling(Coupling coupling)
        {
            string coup;
            switch (coupling)
            {
                case Coupling.AC:
                    coup = "AC";
                    break;
                case Coupling.DC:
                    coup = "DC";
                    break;
                default:
                    coup = "GND";
                    break;
            }
            device.Write(":CHAN1:COUP " + coup);
        }

        //Clear the data from the DSO
        //And collect new data.
        public void clearMeasure()
        {
            device.Write(":MEAS:CLE");      //clears the measure input of the device
            this.isDone();                   //dso is done
            device.Write(":DISP:CLE");      //clears the display of the device
            this.isDone();                      //dso is done
            device.Write(":KEY:SINGLE");        //command virtually presses single button on the dso
            this.isDone();                        //dso is done      
        }
        public float getYInc()
        {
            device.Write(":WAV:YINC?");
            string YINC = device.ReadString();
            return float.Parse(YINC);
        }
        public float getXInc()
        {
            device.Write(":WAV:XINC?");
            string XINC = device.ReadString();
            return float.Parse(XINC);
        }
        public float getYor()
        {
            device.Write(":WAV:YOR?");
            var YOR = device.ReadString();
            return float.Parse(YOR);
        }
        public float[] getdata()
        {
            string DATA = "";
            device.Write(":WAV:SCREENDATA?");
            do
            {
                DATA += device.ReadString();
            } while (!Regex.IsMatch(DATA, @"\n"));
            DATA = DATA.Replace(" \n", "");
            this.isDone();

            string[] data_str = DATA.Split();
            var data_int = data_str.Select((s) => Convert.ToInt32(s, 16)).ToArray();
            var yinc = 0F;
            try
            {
                yinc = this.getYInc();
            }catch{
                yinc = this.getYInc();
            }
            this.isDone();
            var yor = 0F;
            try
            {
                yor = this.getYor();
            }
            catch
            {
                yor = this.getYor();
            }
            this.isDone();
            var data = data_int.Select((d) => (125 - d) * yinc - yor).ToArray();
            return data;
        }

    }


