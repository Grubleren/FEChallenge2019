using System;
using System.Globalization;
using System.IO;

namespace JH.Applications
{
    public partial class VirtualMachine
    {
        void CheckRegisters(int pc)
        {
            int[] registers = ParseRegisterValues(pc);
            CompareRegisters(REGS, registers);
        }

        int[] ParseRegisterValues(int pc)
        {
            int[] registers = new int[64];

            StreamReader reader = new StreamReader(userProfile + "/fe/" + pc.ToString());

            while (!reader.EndOfStream)
            {
                string s = reader.ReadLine();
                if (s.StartsWith("      ______"))
                    break;

            }
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();
            reader.ReadLine();

            for (int i = 0; i < 10; i++)
            {
                string s = reader.ReadLine();
                string[] split = s.Split(new char[] { ' ', '|' });
                int count = 0;
                for (int j = 0; j < split.Length; j++)
                {
                    string s1 = split[j];
                    if (s1 == "")
                        continue;
                    try
                    {
                        if ((count % 2) == 1)
                        {
                            int r = (int)long.Parse(s1, NumberStyles.AllowHexSpecifier);
                            if (r == -1)
                                r = 0;
                            if (r > 0xfefe000 && r < 0x1f000000)
                                r = 200000 - (0xfeff000 - r);
                            registers[(count - 1) / 2 * 10 + i] = r;
                        }
                    }
                    catch
                    {

                    }
                    count++;

                }
            }

            return registers;
        }

        void CompareRegisters(int[] regs, int[] registers)
        {
            for (int i = 3; i < 64; i++)
                if (regs[i] != registers[i] && registers[i] != 0)
                    Console.WriteLine("{2}   {0,9:x}    {1,9:x}", regs[i], registers[i], i);
        }

    }
}
