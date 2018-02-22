using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace IFT585_TP1
{
    // TO DO :
    //  -   Read a file byte by byte (see pr);
    //  -   Create a "M" bytes frame begining containing the data to send and the frame ID
    public class CoucheLLC
    {
        /*
         * Classe CoucheReseau
         * 
         * Prend en charge la lecture et l'écriture d'un fichier.
         * Envoie, sous forme de paquets, l'information à la sous-couche LLC.
         * 
         * TO DO :
         *  - Écrire fichier à partir du flux m_paquetsIn;
         */
        private class CoucheReseau
        {
            private Thread m_thread;
            private string m_path;
            private BlockingCollection<Paquet> m_paquetsIn;
            private BlockingCollection<Paquet> m_paquetsOut;
            private BlockingCollection<TypeEvenement> m_evenementStream;

            private volatile bool m_estActive;
            public bool EstActive
            {
                get { return m_estActive; }
                set { m_estActive = value; }
            }

            public CoucheReseau(string path, BlockingCollection<Paquet> paquetsOut, BlockingCollection<Paquet> paquetsIn, BlockingCollection<TypeEvenement> evenements, bool isReceiving)
            {
                this.m_evenementStream = evenements;
                this.m_paquetsIn = paquetsIn;
                this.m_paquetsOut = paquetsOut;
                this.m_path = path;
                this.m_estActive = false;

                m_thread = isReceiving ? null : new Thread(_lireFichier);
            }

            public void Partir()
            {
                if (this.m_thread != null)
                    this.m_thread.Start();
            }

            public void Terminer()
            {
                if (this.m_thread != null)
                    this.m_thread.Join();
            }

            private void _lireFichier()
            {
                using (FileStream fs = File.OpenRead(m_path))
                {
                    int nbOctetsALire = (int)fs.Length;

                    while (nbOctetsALire > 0)
                    {
                        if (!m_estActive)
                        {
                            Thread.Sleep(500);
                            continue;
                        }
                        else
                        {
                            Paquet paquet = new Paquet();

                            int read = fs.Read(paquet.Buffer, 0, (int)paquet.Taille);

                            m_paquetsOut.Add(paquet);
                            m_evenementStream.Add(TypeEvenement.CoucheReseauPrete);

                            if (read <= 0)
                                break;

                            nbOctetsALire -= read;
                        }
                    }
                }
                Terminer();
            }
        }

        static bool noNAK = true;      /* Pas encore reçu d'aquitement négatif*/

        private BlockingCollection<TypeEvenement> m_eventStream;
        public BlockingCollection<TypeEvenement> EvenementStream 
        {
            get { return m_eventStream; }
        }

        private BlockingCollection<Trame> m_MACStreamIn;
        public BlockingCollection<Trame> MACStreamIn
        {
            get { return m_MACStreamIn; }
        }

        private BlockingCollection<Trame> m_MACStreamOut;
        public BlockingCollection<Trame> MACStreamOut
        {
            get { return m_MACStreamOut; }
        }

        private BlockingCollection<Paquet> m_reseauStreamIn;
        private BlockingCollection<Paquet> m_reseauStreamOut;

        private CoucheReseau m_coucheReseau;

        public CoucheLLC(Signal signal)
        {
            this.m_MACStreamIn = new BlockingCollection<Trame>();
            this.m_MACStreamOut = new BlockingCollection<Trame>();

            this.m_eventStream = new BlockingCollection<TypeEvenement>();
            this.m_reseauStreamIn = new BlockingCollection<Paquet>();
            this.m_reseauStreamOut = new BlockingCollection<Paquet>();
        }

        public void InitialiserA1(string path)
        {
            this.m_coucheReseau = new CoucheReseau(path, m_reseauStreamIn, m_reseauStreamOut, m_eventStream, false);
        }

        public void InitialiserB1(string path)
        {
            this.m_coucheReseau = new CoucheReseau(path, m_reseauStreamIn, m_reseauStreamOut, m_eventStream, true);
        }

        private void _activerCoucheReseau() 
        {
            this.m_coucheReseau.EstActive = true;
        }

        private void _desactiverCoucheReseau()
        {
            this.m_coucheReseau.EstActive = false;
        }

        private void _origineCoucheReseau(out Paquet paquet)
        {
            paquet = this.m_reseauStreamIn.Take();
        }

        private void _origineCouchePhysique(out Trame trame)
        {
            trame = this.m_MACStreamIn.Take();
        }

        private void _versCoucheReseau(Paquet paquet)
        {
            m_reseauStreamOut.Add(paquet);
        }

        private void _versCouchePhysique(Trame trame) 
        {
            m_MACStreamOut.Add(trame);
        }

        /*
         * Retourne 'true' si (a <= b < c) de manière circulaire; 'false' autrement.
         */
        static bool EstAuMillieu(uint a, uint b, uint c)
        {
            return ((a <= b) && (b < c)) || ((c < a) && (a <= b)) || ((b < c) && (c < a));
        }

        /* 
         * Construction et envoie d'une trame de données ou d'une trame ACK ou NAK 
         */
        private void _envoyerTrame(TypeTrame typeTrame, uint noTrame, uint noTrameAttendue, Paquet[] tampon)
        {
            Trame s = new Trame();    /* variable scratch */

            /* Préparer la trame */
            s.Type = typeTrame;

            if (typeTrame == TypeTrame.data)
                s.Info = tampon[noTrame % Constants.NB_BUFS];

            s.NoSequence = noTrame;
            s.ACK = (noTrameAttendue + Constants.MAX_SEQ) % (Constants.MAX_SEQ + 1);

            if (typeTrame == TypeTrame.nak)
                noNAK = false;  /* Un seul NAK par trame svp */

            /* Transmission de la trame */
            _versCouchePhysique(s);

            if (typeTrame == TypeTrame.data)
                /* Armement du Timer */
                StartTimer(noTrame % Constants.NB_BUFS);

            StopACKTimer();
        }

        private TypeEvenement _attendreEvenement()
        {
            TypeEvenement evenement = m_eventStream.Take();
            return evenement;
        }

        public void Run()
        {
            uint ackAttendu;                /* Bord inf. fenêtre émetteur */
            uint prochaineTramePourEnvoie;  /* Bord sup. fenêtre émetteur + 1 */
            uint trameAttendue;             /* Bord inf. fenêtre récepteur */
            uint tropLoin;                  /* Bord sup. fenêtre récepteur + 1 */
            uint index;                     /* Index d'accès au tampon */

            Trame r = new Trame();          /* Variable temporaire */
            Paquet[] outTampon = new Paquet[Constants.NB_BUFS];   /* Tampon pour le flux de données en sortie */
            Paquet[] inTampon = new Paquet[Constants.NB_BUFS];   /* Tampon pour le flux de données en entrée */
            bool[] estArrive = new bool[Constants.NB_BUFS]; /* Tampon occupé ou non */
            uint nbTampons;                 /* Nombre de tampons sortie en cours d'utilisation */
            TypeEvenement evenement;

            /* Initialisation */
            _activerCoucheReseau();
            this.m_coucheReseau.Partir();

            ackAttendu = 0;
            prochaineTramePourEnvoie = 0;
            trameAttendue = 0;
            tropLoin = Constants.NB_BUFS;

            /* Au départ, pas de paquets en mémoire */
            nbTampons = 0;
            for (index = 0; index < Constants.NB_BUFS; index++) estArrive[index] = false;

            while (true)
            {
                evenement = _attendreEvenement();

                switch (evenement)
                {
                    case TypeEvenement.CoucheReseauPrete:  /* Accepter et transmettre la nouvelle trame */
                        /* Agrandit la fenêtre */
                        ++nbTampons;
                        /* Acquisition */
                        _origineCoucheReseau(out outTampon[prochaineTramePourEnvoie % Constants.NB_BUFS]);
                        /* Transmission */
                        _envoyerTrame(TypeTrame.data, prochaineTramePourEnvoie, trameAttendue, outTampon);
                        /* Avance bord fenêtre */
                        inc(prochaineTramePourEnvoie);
                        break;
                    case TypeEvenement.ArriveeTrame:       /* Arrivé d'une trame de données ou de contrôle */
                        /* Acquisition */
                        _origineCouchePhysique(out r);

                        if (r.Type == TypeTrame.data)
                        {
                            /* C'est une trame de données correctes */
                            if ((r.NoSequence != trameAttendue) && noNAK)
                                _envoyerTrame(TypeTrame.nak, 0, trameAttendue, outTampon);
                            else
                                StartACKTimer();

                            if (EstAuMillieu(trameAttendue, r.NoSequence, tropLoin) && (estArrive[r.NoSequence % Constants.NB_BUFS] == false))
                            {
                                /* On doit accepter les trames dans n'importe quel ordre */

                                /* Tampon remplis avec les données */
                                estArrive[r.NoSequence % Constants.NB_BUFS] = true;
                                inTampon[r.NoSequence % Constants.NB_BUFS] = r.Info;

                                while (estArrive[trameAttendue % Constants.NB_BUFS])
                                {
                                    /* Passage trames et avancée fenêtre */
                                    _versCoucheReseau(out inTampon[trameAttendue % Constants.NB_BUFS]);
                                    noNAK = true;
                                    estArrive[trameAttendue % Constants.NB_BUFS] = false;

                                    inc(trameAttendue);     /* Avance bord inf. fenêtre récepteur */
                                    inc(tropLoin);          /* Avance bord haut fenêtre récepteur */
                                    StartACKTimer();
                                }
                            }

                        }

                        if ((r.Type == TypeTrame.nak) && EstAuMillieu(ackAttendu, (r.ACK + 1) % (Constants.MAX_SEQ + 1), prochaineTramePourEnvoie))
                            _envoyerTrame(TypeTrame.data, (r.ACK + 1) % (Constants.MAX_SEQ + 1), trameAttendue, outTampon);

                        while (EstAuMillieu(ackAttendu, r.ACK, prochaineTramePourEnvoie))
                        {
                            /* Traitement ACK superposé */
                            --nbTampons;
                            /* Trame arrivée intacte */
                            StopTimer(ackAttendu % Constants.NB_BUFS);
                            /* Avance bord bas fenêtre émetteur */
                            inc(ackAttendu);
                        }
                        break;
                    case TypeEvenement.CkSumErr:
                        if (noNAK)
                            _envoyerTrame(TypeTrame.nak, 0, trameAttendue, outTampon);   /* Trame altérée */
                        break;
                    case TypeEvenement.Timeout:
                        _envoyerTrame(TypeTrame.data, plusVieilleTrame, trameAttendue, outTampon);
                        break;
                    case TypeEvenement.ACKTimeout:
                        /* Timer ACK expiré => Enovie ACK */
                        _envoyerTrame(TypeTrame.ack, 0, trameAttendue, outTampon);
                        break;
                }

                if (nbTampons < Constants.NB_BUFS)
                    _activerCoucheReseau();
                else
                    _desactiverCoucheReseau();
            }
        }
    }
}