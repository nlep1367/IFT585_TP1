using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace IFT585_TP1
{


    public class Paramètres
    {
        public uint tailleTampon { get; set; }
        public uint delaisTemporisation { get; set; }

        public string emplacementACopier { get; set; }
        public string emplacementCopie { get; set; }
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

    /*
     * La classe Pipeline implemente un pipelne sequentiel. 
     * Tous les elements composant le pipeline sont passifs.
     */
    public class Pipeline
    {

        static Signal g_signal = new Signal();

        private CoucheLLC A1;
        private CoucheLLC B1;
        private CoucheMAC A2;
        private CoucheMAC B2;
        private CouchePhysique C;

        private BlockingCollection<Trame> tramesA1_A2;
        private BlockingCollection<Trame> tramesA2_A1;
        private BlockingCollection<Trame> tramesA_C;
        private BlockingCollection<Trame> tramesC_A;
        private BlockingCollection<Trame> tramesB_C;
        private BlockingCollection<Trame> tramesC_B;
        private BlockingCollection<Trame> tramesB1_B2;
        private BlockingCollection<Trame> tramesB2_B1;

        public Pipeline() {}

        public void Run()
        {
            Paramètres param = new Paramètres();

            Console.WriteLine("Quelle est la taille du tampon à utiliser de chaque côté du support de transmission?");
            bool entreeCorrecte = false;
            while (!entreeCorrecte)
            {
                try
                {
                    param.tailleTampon = Convert.ToUInt16(Console.ReadLine());
                    entreeCorrecte = true;
                }
                catch (InvalidCastException)
                {
                    Console.WriteLine("La valeur entrée n'est pas un entier; svp réessayer :");
                }
            }

            Console.WriteLine("Quel est le délais de temporisation des trames?");
            entreeCorrecte = false;
            while (!entreeCorrecte)
            {
                try
                {
                    param.delaisTemporisation = Convert.ToUInt16(Console.ReadLine());
                    entreeCorrecte = true;
                }
                catch (InvalidCastException)
                {
                    Console.WriteLine("La valeur entrée n'est pas un entier; svp réessayer :");
                }
            }

            Console.WriteLine("Quel est l'emplacement du fichier à copier?");
            param.emplacementACopier = Console.ReadLine();

            Console.WriteLine("Quel est l'emplacemenet pour la copie du fichier?");
            param.emplacementCopie = Console.ReadLine();

            tramesA1_A2 = new BlockingCollection<Trame>();
            tramesA2_A1 = new BlockingCollection<Trame>();
            tramesA_C = new BlockingCollection<Trame>();
            tramesC_A = new BlockingCollection<Trame>();
            tramesB_C = new BlockingCollection<Trame>();
            tramesC_B = new BlockingCollection<Trame>();
            tramesB1_B2 = new BlockingCollection<Trame>();
            tramesB2_B1 = new BlockingCollection<Trame>();

            A1 = new CoucheLLC(g_signal);
            A2 = new CoucheMAC(g_signal, A1);

            B1 = new CoucheLLC(g_signal);
            B2 = new CoucheMAC(g_signal, B1);

            C = new CouchePhysique(g_signal, A2, B2);

            Thread threadA1 = new Thread(A1.Run);
            Thread threadA2 = new Thread(A2.Run);
            Thread threadB1 = new Thread(B1.Run);
            Thread threadB2 = new Thread(B2.Run);
            Thread threadC = new Thread(C.Run);

            try
            {
                threadA1.Start();
                threadA2.Start();
                threadB1.Start();
                threadB1.Start();
                threadC.Start();
            }
            finally
            {
                threadA1.Join();
                threadA2.Join();
                threadB1.Join();
                threadB1.Join();
                threadC.Join();
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
