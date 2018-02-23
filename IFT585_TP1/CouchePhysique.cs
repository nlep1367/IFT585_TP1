using System;
using System.Collections.Concurrent;

namespace IFT585_TP1
{
    public class CouchePhysique
    {
        private BlockingCollection<Trame> m_A2StreamIn;
        private BlockingCollection<Trame> m_A2StreamOut;
        private BlockingCollection<Trame> m_B2StreamIn;
        private BlockingCollection<Trame> m_B2StreamOut;

        public CouchePhysique(Signal signal, CoucheMAC A2, CoucheMAC B2)
        {
            this.m_A2StreamIn = A2.PhysiqueStreamOut;
            this.m_A2StreamOut = A2.PhysiqueStreamIn;
            this.m_B2StreamIn = B2.PhysiqueStreamOut;
            this.m_B2StreamOut = B2.PhysiqueStreamIn;
        }

        public void Run()
        {
            while (true) 
            {
                Trame completeFrame = new Trame();
                if (m_A2StreamIn.TryTake(out completeFrame, 100))
                {
                    /* Trame provenant de A */

                    // TO DO : Faire les perturbations de la couche physique

                    m_B2StreamOut.Add(completeFrame);
                }


                if (m_B2StreamIn.TryTake(out completeFrame, 100))
                {
                    /* Trame provenant de B */

                    // TO DO : Faire les perturbations de la couche physique

                    m_A2StreamOut.Add(completeFrame);
                }
            }
        }
    }
}
