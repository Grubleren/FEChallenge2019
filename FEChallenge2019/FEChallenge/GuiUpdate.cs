using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace JH.Applications
{
    public partial class VirtualMachine
    {
        private void textdown(object sender, KeyEventArgs e)
        {
            //if (e.KeyValue == 13)
            //    enter = true;
            //if (e.KeyValue == 117)
            //    listing = true;
            //if (e.KeyValue == 116)
            //{
            //    listing = false;
            //    programListing = false;
            //}
            //if (e.KeyValue == 118)
            //    programListing = true;
        }

        void UpdateLabel(object sender, EventArgs e)
        {
            label1.Text = PC;
        }

        void UpdateGUI(object sender, EventArgs e)
        {
            updateGUI = true;
        }

        void ShowList(StreamWriter writer, TextBox textBox, bool all)
        {
            Invoke(new Action(() => { writer.Flush(); }));
            Stream stream = writer.BaseStream;
            long len = Math.Min(stream.Length, 10000);
            if (all)
                len = stream.Length;
            byte[] b = new byte[len];
            stream.Seek(-len, SeekOrigin.End);
            stream.Read(b, 0, b.Length);
            string s = Encoding.ASCII.GetString(b);

            Invoke(new Action(() => { textBox.Text = s; }));

            Invoke(new Action(() =>
            {
                int pos = textBox.Lines.Length; SetScrollPos(textBox.Handle, 1, pos, true);
                IntPtr msgPosition = new IntPtr((pos << 16) + 4);
                int mg = 0x115;
                SendMessage(textBox.Handle, mg, msgPosition, IntPtr.Zero);
            }));
            writer.BaseStream.Seek(0, SeekOrigin.End);
        }

        void UpdateProgramStream(string progLine)
        {
            programWriter.WriteLine(progLine);
        }

        void UpdateOutputStream(char msg)
        {
            if (msg == 0)
                return;
            if (msg == 10)
                outputWriter.WriteLine();
            else
                outputWriter.Write(msg);
        }

        void UpdateRegStream()
        {
            regStream.Seek(0, SeekOrigin.Begin);
            regStream.SetLength(0);
            for (int i = 0; i < 64; i++)
                regWriter.WriteLine(i.ToString() + ": " + REGS[i].ToString());
        }

        void UpdateMemStream(int addr, int value, bool rw)
        {
            memWriter.WriteLine("{0,2}{1,-9}{2,7}  {3,1}", (rw ? "<-" : "->"), addr, value, (value >= 32 && value < 127) ? (char)value : ' ');
        }

        private void button1_Click(object sender, EventArgs e)
        {
            waiting = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            singleStep = !singleStep;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            cjmpWait = !cjmpWait;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            toggleCond = true;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            sysWait = !sysWait;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            ShowList(memWriter, textBox2, true);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            long len = programStream.Length;
            int buflen = 10000;
            byte[] buf = new byte[buflen];
            long n = len / buflen;
            int m = (int)(len - n * buflen);

            FileStream totalProgramStream = new FileStream(userProfile + "/FE/programCopy.txt", FileMode.Create, FileAccess.Write);
            programStream.Position = 0;
            programStream.CopyTo(totalProgramStream);
            totalProgramStream.Close();
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            try
            {
                nReg = int.Parse(((TextBox)sender).Text);
                textBox7.Text = REGS[nReg].ToString();
            }
            catch
            {

            }
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            try
            {
                valReg = int.Parse(((TextBox)sender).Text);
                REGS[nReg] = valReg;
                UpdateRegStream();
                ShowList(regWriter, textBox3, false);
            }
            catch
            {

            }
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            try
            {
                nAddr = int.Parse(((TextBox)sender).Text);
                textBox9.Text = PTR(nAddr).ToString();
                textBox10.Text = PTR(nAddr).ToString("x");
            }
            catch
            {

            }

        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            try
            {
                valAddr = int.Parse(((TextBox)sender).Text);
                STOW(nAddr, valAddr);
            }
            catch
            {

            }

        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {
            try
            {
                valAddr = int.Parse(((TextBox)sender).Text, NumberStyles.AllowHexSpecifier);
                STOW(nAddr, valAddr);
            }
            catch
            {

            }

        }

        int[] binexp2Guess = new[] 
        {
            1,27 ,31 ,10 ,26 ,18 ,9,29 , 24, 15, 30, 11, 22, 34, 29, 1 , 2 , 19, 19, 35, 14, 9 , 17, 33, 7 , 5 , 20, 14, 17, 27, 31, 23, 15, 18, 20, 24, 22, 15, 16, 9 , 22, 26, 7 , 33, 15, 23, 1 , 
            29, 4 , 30, 5 , 9 , 14, 6 , 4 , 25, 29, 0 , 30, 28, 13, 15, 14, 27, 5 , 13, 2 , 26, 26, 16, 16, 9 , 32, 1 , 16, 3 , 15, 12, 0 , 9 , 18, 4 , 18, 3 , 27, 7 , 19, 33, 28, 31, 35, 26, 17, 
            8 , 30, 15, 8 , 8 , 15, 8 , 11, 17, 11, 29, 15, 12, 14, 29, 23, 5 , 27, 9 , 5 , 19, 24, 13, 15, 12, 27, 29, 2 , 25, 32, 8 , 34, 5 , 1 , 23, 18, 3 , 16, 14, 3 , 22, 34, 32, 13, 31, 7 , 
            3 , 12, 2 , 15, 25, 7 , 34, 25, 11, 11, 16, 30, 8 , 20, 24, 22, 10, 26, 2 , 0 , 15, 4 , 2 , 14, 4 , 8 , 31, 14, 24, 20, 10, 33, 33, 8 , 35, 30, 29, 18, 17, 18, 29, 24, 25, 25, 8 , 15, 
            27, 3 , 32, 30, 35, 28, 31, 3 , 4 , 7 , 14, 24, 4 , 25, 32, 6 , 33, 10, 11, 21, 35, 1 , 17, 4 , 4 , 8 , 31, 8 , 34, 13, 28, 21, 30, 17, 2 , 11, 0 , 15, 27, 13, 7 , 24, 3 , 12, 13, 28, 
            24, 0 , 20, 19, 8 , 5 , 4 , 1 , 25, 2 , 27, 25, 12, 5 , 11, 32, 32, 3 , 1 , 22, 27, 35, 5 , 35, 11, 0 , 14, 2 , 17, 17, 25, 24, 4 , 3 , 6 , 25, 26, 1 , 9 , 32, 16, 9 , 14, 34, 18, 13, 
            24, 8 , 20, 34, 8 , 23, 1 , 4 , 10, 6 , 10, 30, 25, 7 , 15, 29, 6 , 15, 0 , 21, 33, 18, 28, 6 , 14, 24, 28, 27, 5 , 0 , 8 , 29, 5 , 11, 28, 4 , 19, 13, 35, 16, 18, 9 , 34, 0 , 0 , 9 , 
            1 , 17, 23, 35, 20, 15, 28, 7 , 18, 26, 23, 3 , 24, 25, 35, 33, 22, 26, 26, 10, 20, 19, 30, 34, 13, 4 , 31, 5 , 6 , 23, 11, 2 , 3 , 18, 8 , 10, 24, 18, 23, 25, 24, 18, 0 , 14, 8 , 32, 
            34, 23, 16, 30, 11, 2 , 32, 9 , 8 , 15, 15, 1 , 4 , 32, 30, 13, 26, 23, 25, 30, 29, 17, 11, 32, 22, 2 , 18, 23, 13, 18, 8 , 10, 29, 4 , 30, 10, 31, 21, 27, 11, 23, 21, 33, 13, 21, 11, 
            6 , 19, 9 , 5 , 31, 15, 18, 17, 15, 23, 21, 8 , 28, 3 , 30, 13, 28, 5 , 32, 17, 28, 3 , 24, 0 , 11, 26, 6 , 22, 3 , 33, 2 , 23, 2 , 2 , 27, 10, 21, 11, 11, 8 , 7 , 25, 24, 5 , 26, 32, 
            10, 4 , 0 , 7 , 35, 23, 14, 7 , 13, 4 , 11, 16, 29, 20, 25, 30, 23, 30, 18, 28, 14, 15, 34, 19, 4 , 27, 19, 35, 27, 30, 3 , 0 , 31, 24, 24, 25, 2 , 20, 15, 20, 10, 3 , 35, 14, 16, 5 , 
            34, 2 , 8 , 30, 18, 31, 1 , 9 , 28, 4 , 32, 7 , 15, 22, 33, 16, 5 , 12, 20, 3 , 35, 16, 9 , 13, 1 , 12, 3 , 35, 3 , 25, 7 , 22, 28, 23, 33, 21, 1 , 9 , 28, 34, 10, 0 , 29, 14, 31, 24, 
            8 , 29, 27, 1 , 9 , 7 , 27, 14, 20, 35, 11, 12, 4 , 22, 31, 11, 34, 0 , 2 , 19, 21, 4 , 5 , 35, 23, 16, 8 , 8 , 30, 18, 35, 34, 19, 2 , 33, 1 , 34, 6 , 7 , 28, 30, 12, 5 , 25, 5 , 1 , 
            3 , 10, 29, 1 , 0 , 23, 14, 20, 29, 34, 18, 25, 30, 25, 31, 9 , 19, 17, 34, 0 , 23, 13, 31, 1 , 7 , 19, 9 , 22, 15, 9 , 6 , 9 , 35, 20, 27, 34, 18, 4 , 7 , 27, 5 , 12, 13, 30, 0 , 17, 
            0 , 32, 22, 15, 13, 29, 33         };

        [DllImport("user32.dll")]
        static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

        [DllImport("user32.dll")]
        internal static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);


    }
}
