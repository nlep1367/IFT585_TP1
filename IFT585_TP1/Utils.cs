using System;
using System.Collections.Concurrent;
using System.Threading;

namespace IFT585_TP1
{
    public static class Constants
    {
        /*
         * Numéro maximal de séquence d'une trame.
         * Les trames émises possèdent un numéro de séquence entre 0 et MAX_SEQ.
         * 
         * Note : Sous la forme (2^n - 1) => peut être codé sur n bits
         */
        public const uint MAX_SEQ = 7;

        /*
         * Taille d'une fenêtre pouvant contenir un numéro de séquence
         */
        public const uint NB_BUFS = (MAX_SEQ + 1) / 2;

        public const uint M = 32;
        public const uint N = 64;

        public const uint TIMEOUT = 1000;
    }

    public enum TypeEvenement
    {
        ArriveeTrame,
        CkSumErr,
        Timeout,
        CoucheReseauPrete,
        ACKTimeout
    }

    public enum TypeTrame
    {
        data,
        ack,
        nak
    }

    public class Trame
    {
        public Trame()
        {
            _taille = Constants.N;
        }

        private uint _taille;
        public uint Taille 
        { 
            get { return _taille; } 
            set { _taille = value; } 
        }

        private uint _ack;
        public uint ACK
        {
            get { return _ack; }
            set { _ack = value; }
        }

        private uint _noSeq;
        public uint NoSequence
        { 
            get { return _noSeq; }
            set { _noSeq = value; } 
        }

        private TypeTrame _type;
        public TypeTrame Type
        {
            get { return _type; }
            set { _type = value; }
        }

        private Paquet _information;
        public Paquet Info
        { 
            get { return _information; } 
            set { _information = value; } 
        }
    }

    public class Paquet
    {
        private byte[] _buffer;
        private uint _taille;

        public Paquet()
        {
            _taille = Constants.M;
            _buffer = new byte[_taille];
        }

        public Paquet(uint var)
        {
            _taille = var;
            _buffer = new byte[_taille];
        }

        public uint Taille => _taille;
        public byte[] Buffer => _buffer;
    }
}
