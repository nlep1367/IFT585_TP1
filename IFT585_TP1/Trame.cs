using System;
using System.Windows;
namespace IFT585_TP1
{
    public class Trame
    {
        public const uint M = 32;
        public const uint N = 64;

        public uint Size { get; }

        public uint ID { get; }

        public byte[] Buffer { get; set; }

        public Trame(uint frameSize, uint frameID) 
        {
            Size = frameSize;

            ID = frameID;
            Buffer = new byte[Size];
        }
    }
}
