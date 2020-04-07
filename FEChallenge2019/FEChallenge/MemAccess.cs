using System;

namespace JH.Applications
{
    public partial class VirtualMachine
    {
        int PTR(int addr)
        {
            try
            {
                byte[] ch = new byte[4];

                for (int i = 0; i < 4; i++)
                    ch[i] = Mem(addr + 3 - i);
                int value = 0;
                for (int i = 0; i < 4; i++)
                    value |= ch[i] << (i * 8);

                return value;
            }
            catch
            {
                return 0;
            }
        }

        byte Mem(int addr)
        {
            try
            {
                byte b = MEM[addr];
                return b;
            }
            catch
            {
                return 0;
            }
        }

        int GetReg(uint reg)
        {
            if (reg == 0)
                return 0;

            return REGS[reg];
        }

        void SetReg(uint reg, int value)
        {
            REGS[reg] = value;
        }

        void STOW(int addr, int value)
        {
            if (totalProgList)
                return;
            UpdateMemStream(addr, value, false);

            try
            {
                for (int i = 0; i < 4; i++)
                    MEM[addr + 3 - i] = (byte)((value >> (i * 8)) & 0xff);
            }
            catch
            {

            }
        }

        void STOH(int addr, int value)
        {
            if (totalProgList)
                return;
            UpdateMemStream(addr, value, false);

            try
            {
                for (int i = 0; i < 2; i++)
                    MEM[addr + 1 - i] = (byte)((value >> (i * 8)) & 0xff);
            }
            catch
            {

            }
        }

        void STOB(int addr, int value)
        {
            if (totalProgList)
                return;
            UpdateMemStream(addr, value, false);

            try
            {
                MEM[addr] = (byte)(value & 0xff);
            }
            catch
            {

            }
        }

    }
}
