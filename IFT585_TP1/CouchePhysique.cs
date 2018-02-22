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
    }
}
