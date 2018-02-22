using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace IFT585_TP1
{


    public struct Paramètres
    {
        uint tailleTampon { get; set; }
        uint delaisTemporisation { get; set; }

        string emplacementACopier { get; set; }
        string emplacementCopie { get; set; }
    }

    public class Signal
    {
        private volatile bool m_isComplete;
        public bool IsComplete
        {
            get { return m_isComplete; }
            set { m_isComplete = value; }
        }
    }

    static void LireFichier(Stream stream, byte[] data)
    {
        int offset = 0;
        int remaining = data.Length;
        while (remaining > 0)
        {
            int read = stream.Read(data, offset, remaining);
            if (read <= 0)
            {
                throw new EndOfStreamException(String.Format("End of stream reached with {0} bytes left to read", remaining));
            }
            remaining -= read;
            offset += read;
        }
    }

    /*
     * La classe Pipeline implemente un pipelne sequentiel. 
     * Tous les elements composant le pipeline sont passifs.
     */
    public class Pipeline
    {

        static Signal g_signal = new Signal();

        private CoucheLLC A1;
        private ThreadA2 A2;

        private BlockingCollection<Trame> tramesA1;
        private BlockingCollection<Trame> tramesA2;

        public Pipeline() {}

        public void Run()
        {
            Paramètres param = new Paramètres();

            // Reading the input file path
            Console.WriteLine("Quelle est la taille du tampon à utiliser de chaque côté du support de transmission?");
            param = Console.ReadLine();

            // Reading the input file path
            Console.WriteLine("Quel est le délais de temporisation des trames?");
            A1.Path = Console.ReadLine();

            // Reading the input file path
            Console.WriteLine("Quel est l'emplacement du fichier à copier?");
            path = Console.ReadLine();

            // Reading the input file path
            Console.WriteLine("Quel est l'emplacemenet pour la copie du fichier?");
            A1.Path = Console.ReadLine();

            tramesA1 = new BlockingCollection<Trame>();
            tramesA2 = new BlockingCollection<Trame>();

            A1 = new CoucheLLC(g_signal);
            A2 = new ThreadA2(g_signal);

            Thread threadA1 = new Thread(A1.Process);
            Thread threadA2 = new Thread(A2.Process);

            try
            {
                threadA1.Start();
                threadA2.Start();
            }
            finally
            {
                threadA1.Join();
                threadA2.Join();
            }

        }

        public void Reset()
        {
            if (g_signal.IsComplete != false)
            {
                g_signal.IsComplete = false;
            }
        }
    }
}
