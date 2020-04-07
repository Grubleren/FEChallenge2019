using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace JH.Applications
{
    public partial class VirtualMachine : Form
    {

        int[] REGS;
        byte[] MEM;
        byte[] DISK;
        int diskSize;
        int memSize;
        int memOffsetRoData;
        int memOffsetText;
        int memOffsetData;
        int memOffsetBss;
        bool singleStep = false;
        bool waiting = true;
        bool cjmpWait = false;
        bool sysWait = true;
        bool toggleCond = false;
        bool ending;
        string PC;
        int nReg;
        int valReg;
        int nAddr;
        int valAddr;
        bool updateGUI;
        int gameNumber = 0;
        bool totalProgList = false;
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        TextWriter streamWriter;
        FileStream memStream;
        FileStream regStream;
        FileStream outputStream;
        FileStream programStream;
        TextWriter textWriter = null;
        StreamWriter memWriter;
        StreamWriter regWriter;
        StreamWriter outputWriter;
        StreamWriter programWriter;

        public VirtualMachine()
        {
            InitializeComponent();
            streamWriter = new StreamWriter(userProfile + "/FE/feTrace.txt");
            memStream = new FileStream(userProfile + "/FE/memStream", FileMode.Create, FileAccess.ReadWrite);
            regStream = new FileStream(userProfile + "/FE/regStream", FileMode.Create, FileAccess.ReadWrite);
            outputStream = new FileStream(userProfile + "/FE/outputStream", FileMode.Create, FileAccess.ReadWrite);
            programStream = new FileStream(userProfile + "/FE/programStream", FileMode.Create, FileAccess.ReadWrite);

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!totalProgList)
            {
                timer1.Interval = 1000;
                timer1.Tick += new EventHandler(UpdateGUI);
                timer1.Start();
                timer2.Interval = 10;
                timer2.Tick += new EventHandler(UpdateLabel);
                timer2.Start();
            }

            Thread thread = new Thread(new ThreadStart(Go));
            thread.Start();
        }

        void Go()
        {

            textBox5.Text = "";
            textBox4.Text = "";
            textBox3.Text = "";
            textBox2.Text = "";
            memWriter = new StreamWriter(memStream);
            regWriter = new StreamWriter(regStream);
            outputWriter = new StreamWriter(outputStream);
            programWriter = new StreamWriter(programStream);
            if (totalProgList)
                textWriter = streamWriter;
            ending = false;

            REGS = new int[64];
            for (int i = 0; i < 64; i++)
                REGS[i] = 0;

            string image;
            int diskOffsetRoData = -1;
            int diskOffsetText = -1;
            int diskOffsetData = -1;
            int diskOffsetBss = -1;
            int sizeRoData = -1;
            int sizeText = -1;
            int sizeData = -1;
            int sizeBss = -1;
            int stackOffset;
            int imageOffset;

            int testNumber = 2;
            switch (testNumber)
            {
                case 0:
                    image = userProfile + "/fe/testhello";
                    diskOffsetRoData = 224;
                    sizeRoData = 3180;
                    diskOffsetText = 0x1000;
                    sizeText = 81968;
                    diskOffsetData = 90112;
                    sizeData = 308;
                    diskOffsetBss = 90420;
                    sizeBss = 3444;
                    imageOffset = 0x400000;
                    memOffsetRoData = diskOffsetRoData + imageOffset;
                    memOffsetText = diskOffsetText + imageOffset;
                    memOffsetData = diskOffsetData + imageOffset;
                    memOffsetBss = diskOffsetBss + imageOffset;
                    stackOffset = 200000;
                    break;
                case 1:
                    image = userProfile + "/fe/test";
                    diskOffsetRoData = 0x0;
                    sizeRoData = 0;
                    diskOffsetText = 0x1000;
                    sizeText = 5080;
                    diskOffsetData = 0x0;
                    sizeData = 0;
                    diskOffsetBss = 0x0;
                    sizeBss = 0;
                    imageOffset = 0x400000;
                    memOffsetRoData = diskOffsetRoData + imageOffset;
                    memOffsetText = diskOffsetText + imageOffset;
                    memOffsetData = diskOffsetData + imageOffset;
                    memOffsetBss = diskOffsetBss + imageOffset;
                    stackOffset = 200000;
                    break;
                case 2:
                    image = userProfile + "/fe/binexp2";
                    diskOffsetRoData = 0xe0;
                    sizeRoData = 4016;
                    diskOffsetText = 0x2000;
                    sizeText = 89596;
                    diskOffsetData = 0x18000;
                    sizeData = 316;
                    diskOffsetBss = 0x1813c;
                    sizeBss = 3532;
                    imageOffset = 0x400000;
                    memOffsetRoData = 224 + imageOffset;
                    memOffsetText = 8192 + imageOffset;
                    memOffsetData = 98304 + imageOffset;
                    memOffsetBss = 98620 + imageOffset;
                    stackOffset = 200000;
                    break;
                default:
                    image = userProfile + "/fe/img.dat";
                    diskOffsetRoData = 0xe0;
                    diskOffsetText = 0x2000;
                    diskOffsetData = 0x18000;
                    sizeRoData = 4016;
                    sizeText = 89596;
                    sizeData = 316;
                    sizeBss = 3532;
                    imageOffset = 4194304;
                    memOffsetRoData = 224;
                    memOffsetText = 8192;
                    memOffsetData = 98304;
                    memOffsetBss = 98620;
                    stackOffset = 200000;
                    break;
            }

            FileStream fp = new FileStream(image, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(fp);
            long fileLength = fp.Length;

            diskSize = (int)fp.Length;
            DISK = new byte[diskSize + 512];
            for (int i = 0; i < diskSize; i++)
                DISK[i] = 0xff;

            for (int i = 0; i < diskSize; i++)
                DISK[i] = reader.ReadByte();
            reader.Close();

            memSize = 6000000;
            MEM = new byte[memSize];

            for (int i = 0; i < MEM.Length; i++)
                MEM[i] = 0x00;

            for (int i = 0; i < sizeRoData; i++)
                MEM[i + memOffsetRoData] = DISK[i + diskOffsetRoData];

            for (int i = 0; i < sizeText; i++)
                MEM[i + memOffsetText] = DISK[i + diskOffsetText];

            for (int i = 0; i < sizeData; i++)
                MEM[i + memOffsetData] = DISK[i + diskOffsetData];

            for (int i = 0; i < sizeBss; i++)
                MEM[i + memOffsetBss] = 0x0;

            REGS[60] = stackOffset;
            REGS[62] = stackOffset;
            REGS[63] = memOffsetText;

            Machine(testNumber);
        }

        void Machine(int testNumber)
        {
            uint instr = 0;
            uint opcode = 0;
            int pc = 0;
            uint r0 = 0;
            uint r1 = 0;
            uint r2 = 0;
            int reg0;
            int reg1;
            int reg2;
            int reg63;
            int last63 = memOffsetText;
            uint extension = 0;
            int offset = 0;
            int resultAddress;
            int cJumpLength;
            int signNegationMode;
            ushort imediateParam1;
            ushort imediateParem2;
            int imediateValueLow;
            int imediateValueHigh;
            int blendMode;
            int shiftMode;
            int shiftVal;
            int reserved;
            string actionString = "";
            int instructionResult = 0;
            string[] opcodes = new string[] { "LB", "LH", "LW", "NP", "SB", "SH", "SW", "NP", "AD", "ML", "DV", "NO", "MSK", "NP", "NP", "NP", "MI", "ADI", "CM", "CMP", "NP", "NP", "NP", "CJMP", "IN", "OU", "RE", "WR", "NP", "SYS", "IRET", "HT" };


            for (; ; )
            {
                if (totalProgList)
                    SetReg(63, last63);
                int rg = GetReg(63);

                pc = rg - memOffsetText;
                PC = pc.ToString();

                instr = (uint)PTR(rg);
                SetReg(63, GetReg(63) + 4);
                last63 = GetReg(63);

                opcode = (instr >> 27) & 0x1f;

                r0 = (instr >> 21) & 0x3f;
                r1 = (instr >> 15) & 0x3f;
                r2 = (instr >> 9) & 0x3f;

                reg0 = GetReg(r0);
                reg1 = GetReg(r1);
                reg2 = GetReg(r2);
                reg63 = GetReg(63);

                extension = (instr >> 8) & 1;
                offset = ((int)(instr & 0xff) << 24) >> 24;
                resultAddress = reg1 + reg2 + offset;

                imediateParam1 = (ushort)((instr >> 5) & 0xffff);
                imediateParem2 = (ushort)(instr & 0x1f);
                int imediateValue = imediateParam1 << imediateParem2;
                imediateValueLow = imediateValue;
                imediateValueHigh = (reg0 & 0x0000ffff) | imediateValue;

                signNegationMode = (int)(instr & 0xf);
                cJumpLength = (int)((instr >> 5) & 0x3ff);
                cJumpLength = ((cJumpLength << 22) >> 22) * 4;
                bool cond = false;

                blendMode = (int)((instr >> 7) & 3);
                shiftMode = (int)((instr >> 5) & 3);
                shiftVal = (int)(instr & 0x1f);

                reserved = (int)((instr >> 4) & 1);

                switch (opcodes[opcode])
                {
                    case "LB":  //OP_LOAD_B:
                        SetReg(r0, Mem(reg1 + reg2 + offset));
                        if (extension == 1)
                            SetReg(r0, (GetReg(r0) << 24) >> 24);

                        UpdateMemStream(reg1 + reg2 + offset, REGS[r0], true);

                        actionString = string.Format(" r{0:00} = [r{1:00} + r{2:00} + {3,6}]", new object[] { r0, r1, r2, offset });
                        instructionResult = GetReg(r0);
                        break;
                    case "LH":  //OP_LOAD_H;
                        SetReg(r0, (Mem(reg1 + reg2 + offset) << 8) | Mem(reg1 + reg2 + offset + 1));
                        if (extension == 1)
                            SetReg(r0, (GetReg(r0) << 16) >> 16);

                        UpdateMemStream(reg1 + reg2 + offset, REGS[r0], true);

                        actionString = string.Format(" r{0:00} = [r{1:00} ++ r{2:00} + {3,6}]", new object[] { r0, r1, r2, offset });
                        instructionResult = GetReg(r0);
                        break;
                    case "LW":  //OP_LOAD_W;
                        int it = PTR(reg1 + reg2 + offset);
                        SetReg(r0, it);

                        UpdateMemStream(reg1 + reg2 + offset, it, true);

                        actionString = string.Format(" r{0:00} = [r{1:00} ++++ r{2:00} + {3,6}]", new object[] { r0, r1, r2, offset });
                        instructionResult = GetReg(r0);
                        break;
                    case "SB":  //OP_STORE_B;
                        STOB(reg1 + reg2 + offset, reg0);

                        actionString = string.Format("[r{0:00} + r{1:00} +{2,6}] = r{3:00}", new object[] { r1, r2, offset, r0 });
                        instructionResult = GetReg(r0);
                        break;
                    case "SH":  //OP_STORE_H;
                        STOH(reg1 + reg2 + offset, reg0);

                        actionString = string.Format("[r{0:00} ++ r{1:00} + {2,6}] = r{3:00}", new object[] { r1, r2, offset, r0 });
                        instructionResult = GetReg(r0);
                        break;
                    case "SW":  //OP_STORE_W;
                        STOW(reg1 + reg2 + offset, reg0);

                        actionString = string.Format("[r{0:00} ++++ r{1:00} + {2,6}] = r{3:00}", new object[] { r1, r2, offset, r0 });
                        instructionResult = GetReg(r0);
                        break;
                    case "AD":  //OP_ADD;

                        SetReg(r0, reg1 + reg2 + offset);

                        actionString = string.Format(" r{0:00} = r{1:00} + r{2:00} +     {3,6}", new object[] { r0, r1, r2, offset });
                        instructionResult = GetReg(r0);
                        break;
                    case "ML":  //OP_MUL;
                        SetReg(r0, reg1 * (reg2 + offset));

                        actionString = string.Format(" r{0:00} = r{1:00} * (r{2:00} +     {3,6})", new object[] { r0, r1, r2, offset });
                        instructionResult = GetReg(r0);
                        break;
                    case "DV":  //OP_DIV;
                        try
                        {
                            int denominator = reg2 + offset;
                            if (denominator != 0)
                            {
                                int div = reg1 / denominator;
                                if (reg1 > 0 && denominator < 0 || reg1 < 0 && denominator > 0)
                                    div--;
                                SetReg(r0, div);

                            }
                            else
                                throw new DivideByZeroException();
                        }
                        catch
                        {

                        }
                        actionString = string.Format(" r{0:00} = r{1:00} / (r{2:00} + {3,6})", new object[] { r0, r1, r2, offset });
                        instructionResult = GetReg(r0);
                        break;
                    case "NO":  //OP_NOR;
                        SetReg(r0, ~(reg1 | reg2 | offset));

                        actionString = string.Format(" r{0:00} = ~(r{1:00} | r{2:00} |   {3,6})", new object[] { r0, r1, r2, offset });
                        instructionResult = GetReg(r0);
                        break;
                    case "MI":  //OP_MOVI;
                        SetReg(r0, imediateValueLow);

                        actionString = string.Format(" r{0:00}            {1,9:0}  {2,2:0}  {3,9:0}", new object[] { r0, imediateParam1, imediateParem2, imediateValue });
                        instructionResult = GetReg(r0);
                        break;
                    case "ADI":
                        SetReg(r0, imediateValueHigh);

                        actionString = string.Format(" r{0:00}            {1,9:0}  {2,2:0}  {3,9:0}", new object[] { r0, imediateParam1, imediateParem2, imediateValue });
                        instructionResult = GetReg(r0);
                        break;
                    case "CM":  //OP_CMOV;
                        {
                            string condString;
                            cond = Cond(signNegationMode, r1, r2, reg1, reg2, out condString);

                            if (cond)
                                SetReg(r0, reg1);

                            actionString = string.Format(" r{0:00}  r{1:00}  r{2:00}  {3,-3:00}", new object[] { r0, r1, r2, condString });
                            instructionResult = GetReg(r0);
                        }
                        break;
                    case "CMP":
                        {
                            string condString;

                            cond = Cond(signNegationMode, r1, r2, reg1, reg2, out condString);

                            if (cond)
                                SetReg(r0, 1);
                            else
                                SetReg(r0, 0);

                            actionString = string.Format(" r{0:00}  r{1:00}  r{2:00}  {3,-3:00}", new object[] { r0, r1, r2, condString });
                            instructionResult = GetReg(r0);
                        }
                        break;
                    case "CJMP":
                        {
                            string condString;

                            cond = Cond(signNegationMode, r0, r1, reg0, reg1, out condString);

                            if (cond)
                                SetReg(63, GetReg(63) + cJumpLength - 4);

                            actionString = string.Format(" r{0:00}   r{1:00}      {2,-3:00}     {3,9:0}", new object[] { r0, r1, condString, cJumpLength });
                            instructionResult = GetReg(r0);

                            if (cjmpWait)
                                waiting = true;
                        }
                        break;
                    case "MSK":
                        uint mask;
                        SetReg(r0, Mask(blendMode, shiftMode, shiftVal, reg0, reg1, reg2, out mask));

                        actionString = string.Format(" r{0:00}   r{1:00}   r{2:00} {3,2:0} {4,2:0}  {5,2:0}  0x{6,7:x8}  ", new object[] { r0, r1, r2, blendMode, shiftMode, shiftVal, mask });
                        instructionResult = GetReg(r0);
                        break;
                    case "SYS":
                    case "IRET":
                        if (testNumber == 0)
                        {
                            if (GetReg(3) == 4054)
                            {
                                for (int i = 0; i < GetReg(30); i++)
                                    UpdateOutputStream((char)Mem(GetReg(31) + i));
                            }
                            else if (GetReg(3) == 4146)
                            {
                                int idx = 0;
                                char c = (char)Mem(PTR(GetReg(5)) + idx);
                                while (c != 0)
                                {
                                    UpdateOutputStream((char)c);
                                    idx++;
                                    c = (char)Mem(PTR(GetReg(5)) + idx);
                                }
                            }
                            else if (GetReg(3) == 4003)
                            {
                                int addr = GetReg(5);
                                STOW(addr, 0x31353038);
                                STOB(addr + 4, 0x0a);
                                STOB(addr + 5, 0x00);
                            }
                        }

                        if (testNumber == 1 || testNumber == 2)
                        {
                            int sysType = GetReg(3);
                            switch (sysType)
                            {
                                case 4252:
                                    break;
                                case 4049:
                                    break;
                                case 4195:
                                    break;
                                case 4070:
                                    break;
                                case 4140:
                                    SetReg(3, 4094);
                                    break;
                                case 4004:
                                    for (int i = 0; i < GetReg(6); i++)
                                        UpdateOutputStream((char)Mem(GetReg(5) + i));
                                    break;
                                case 4054:
                                    for (int i = 0; i < GetReg(30); i++)
                                        UpdateOutputStream((char)Mem(GetReg(31) + i));
                                    break;
                                case 4146:
                                    int idx = 0;
                                    char c = (char)Mem(PTR(GetReg(5)) + idx);
                                    while (idx < GetReg(15))
                                    {
                                        UpdateOutputStream((char)c);
                                        idx++;
                                        c = (char)Mem(PTR(GetReg(5)) + idx);
                                    }
                                    if (idx == 0)
                                        idx = GetReg(10);
                                    SetReg(3, idx);
                                    break;
                                case 4003:
                                    int data;
                                    if ((gameNumber % 2) == 0)
                                        data = 1;
                                    else
                                        data = binexp2Guess[(gameNumber - 1) / 2];
                                    string s = data.ToString();
                                    int addr = GetReg(5);
                                    foreach (char ch in s)
                                    {
                                        STOB(addr, ch);
                                        addr++;

                                    }
                                    STOB(addr++, 10);
                                    STOB(addr++, 0);
                                    SetReg(3, s.Length + 1);
                                    gameNumber++;
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (sysWait)
                            waiting = true;

                        actionString = string.Format(" r{0:00}   r{1:00}   r{2:00}       {3,6}", new object[] { r0, r1, r2, offset });
                        instructionResult = GetReg(r0);
                        break;
                    case "NP":
                        MessageBox.Show("NP");
                        if (textWriter != null)
                            textWriter.Flush();
                        ending = true;
                        break;
                    case "HT":  //OP_HALT;
                        MessageBox.Show("HALT");
                        if (textWriter != null)
                            textWriter.Flush();
                        ending = true;
                        break;
                    case "IN":  //OP_IN;
                        throw new NotImplementedException();
                    case "OU":  //OP_OUT;
                        throw new NotImplementedException();
                    case "RE":  //OP_READ;
                        throw new NotImplementedException();
                    case "WR":  //OP_WRITE;
                        throw new NotImplementedException();
                    default:
                        ending = true;
                        break;
                }

                if (ending)
                    break;

                ProgramTrace(pc, opcodes[opcode], actionString, instructionResult, resultAddress, reg0, reg1, reg2);

                UpdateRegStream();

                if (updateGUI || waiting & !totalProgList)
                {
                    updateGUI = false;
                    ShowList(programWriter, textBox5, false);
                    ShowList(outputWriter, textBox4, true);
                    ShowList(regWriter, textBox3, true);
                    ShowList(memWriter, textBox2, false);
                }

                while (waiting && !totalProgList)
                {
                    Thread.Sleep(100);
                }

                if (opcodes[opcode] == "CJMP")
                {
                    if (toggleCond)
                    {
                        if (cond)
                            SetReg(63, GetReg(63) - cJumpLength + 4);
                        else
                            SetReg(63, GetReg(63) + cJumpLength - 4);
                        toggleCond = false;
                    }
                }

                if (singleStep)
                    waiting = true;

            }
        }

        void ProgramTrace(int pc, string opcodeString, string actionString, int result, int resultAddress, int reg0, int reg1, int reg2)
        {
            string s = string.Format("{0:00000}  {1,-4}  {2,-40}  {3,9:0}  {4,9:00000000}  {5,9:00000000}  {6,9:00000000}  {7,9:00000000}", pc, opcodeString, actionString, result, resultAddress, reg0, reg1, reg2);

            UpdateProgramStream(s);

            if (textWriter != null)
                textWriter.WriteLine(s);
        }

        bool Cond(int signNegationMode, uint r1, uint r2, int reg1, int reg2, out string condString)
        {
            bool cond = false;
            condString = "";

            switch (signNegationMode)
            {
                case 0:
                    cond = reg2 != 0;
                    condString = "NZ";
                    break;
                case 1:
                    cond = Math.Abs(reg1) <= Math.Abs(reg2);
                    condString = "LE";
                    break;
                case 2:
                    cond = Math.Abs(reg1) < Math.Abs(reg2);
                    condString = "LT";
                    break;
                case 3:
                    cond = (reg1 == reg2);
                    condString = "EQ";
                    break;
                case 4:
                    cond = reg2 == 0;
                    condString = "EZ";
                    break;
                case 5:
                    cond = Math.Abs(reg1) > Math.Abs(reg2);
                    condString = "GT";
                    break;
                case 6:
                    cond = Math.Abs(reg1) >= Math.Abs(reg2);
                    condString = "GE";
                    break;
                case 7:
                    cond = (reg1 != reg2);
                    condString = "NE";
                    break;
                case 8:
                    throw new InvalidProgramException();
                case 9:
                    cond = reg1 <= reg2;
                    condString = "SLE";
                    break;
                case 10:
                    cond = reg1 < reg2;
                    condString = "SLT";
                    break;
                case 11:
                    throw new InvalidProgramException();
                case 12:
                    throw new InvalidProgramException();
                case 13:
                    cond = reg1 > reg2;
                    condString = "SGT";
                    break;
                case 14:
                    cond = reg1 >= reg2;
                    condString = "SGE";
                    break;
                case 15:
                    throw new InvalidProgramException();
            }
            return cond;
        }

        int Mask(int blendMode, int shiftMode, int shiftVal, int reg0, int reg1, int reg2, out uint umask)
        {
            int result = 0;
            int mask = 0;

            shiftVal += reg2;

            switch (shiftMode)
            {
                case 0:
                    mask = reg1 << shiftVal;
                    break;
                case 1:
                    mask = (int)((uint)reg1 >> shiftVal);
                    break;
                case 2:
                    mask = reg1 >> shiftVal;
                    break;
                case 3:
                    throw new InvalidProgramException();
            }
            switch (blendMode)
            {
                case 0:
                    result = mask;
                    break;
                case 1:
                    result = reg0 & mask;
                    break;
                case 2:
                    result = reg0 | mask;
                    break;
                case 3:
                    result = reg0 ^ mask;
                    break;
            }
            umask = (uint)mask;
            return result;
        }

    }
}
