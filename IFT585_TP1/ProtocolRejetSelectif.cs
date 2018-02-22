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

    public class ProtocoleRejetSelectif
    {
        static bool noNAK = true;      /* Pas encore reçu d'aquitement négatif*/

        private BlockingCollection<TypeEvenement> m_eventStream;
        private BlockingCollection<Paquet> m_reseauStream;

        public ProtocoleRejetSelectif() 
        {
            m_eventStream = new BlockingCollection<TypeEvenement>();
            m_reseauStream = new BlockingCollection<TypeEvenement>();
        }

        // TO DO 
        static void OrigineCoucheReseau(out Paquet paquet) 
        {
            paquet = 
        }

        // TO DO 
        private void ActiverCoucheReseau() {}


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
        static void EnvoyerTrame(TypeTrame typeTrame, uint noTrame, uint noTrameAttendue, Paquet[] tampon)
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
            VersCouchePhysique(s);

            if (typeTrame == TypeTrame.data)
                /* Armement du Timer */
                StartTimer(noTrame % Constants.NB_BUFS);

            StopACKTimer();
        }

        private TypeEvenement _attendreEvenement() { 
            TypeEvenement evenement = m_eventStream.Take();
            return evenement; 
        }

        public void Protocole()
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
            ActiverCoucheReseau();

            ackAttendu = 0;
            prochaineTramePourEnvoie = 0;
            trameAttendue = 0;
            tropLoin = Constants.NB_BUFS;

            /* Au départ, pas de paquets en mémoire */
            nbTampons = 0;
            for (index = 0; index < Constants.NB_BUFS; index++) estArrive[index] = false;

            while(true) 
            {
                evenement = _attendreEvenement();

                switch(evenement)
                {
                    case TypeEvenement.CoucheReseauPrete :  /* Accepter et transmettre la nouvelle trame */
                        /* Agrandit la fenêtre */
                        ++nbTampons;
                        /* Acquisition */
                        OrigineCoucheReseau(out outTampon[prochaineTramePourEnvoie % Constants.NB_BUFS]);
                        /* Transmission */
                        EnvoyerTrame(TypeTrame.data, prochaineTramePourEnvoie, trameAttendue, outTampon);
                        /* Avance bord fenêtre */
                        inc(prochaineTramePourEnvoie);
                        break;
                    case TypeEvenement.ArriveeTrame :       /* Arrivé d'une trame de données ou de contrôle */
                        /* Acquisition */
                        OrigineCouchePhysique(out r);

                        if (r.Type == TypeTrame.data)
                        {
                            /* C'est une trame de données correctes */
                            if ((r.NoSequence != trameAttendue) && noNAK)
                                EnvoyerTrame(TypeTrame.nak, 0, trameAttendue, outTampon);
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
                                    VersCoucheReseau(out inTampon[trameAttendue % Constants.NB_BUFS]);
                                    noNAK = true;
                                    estArrive[trameAttendue % Constants.NB_BUFS] = false;

                                    inc(trameAttendue);     /* Avance bord inf. fenêtre récepteur */
                                    inc(tropLoin);          /* Avance bord haut fenêtre récepteur */
                                    StartACKTimer();
                                }
                            }

                        }

                        if ((r.Type == TypeTrame.nak) && EstAuMillieu(ackAttendu, (r.ACK + 1) % (Constants.MAX_SEQ + 1), prochaineTramePourEnvoie))
                            EnvoyerTrame(TypeTrame.data, (r.ACK + 1) % (Constants.MAX_SEQ + 1), trameAttendue, outTampon);

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
                    case TypeEvenement.CkSumErr :
                        if (noNAK)
                            EnvoyerTrame(TypeTrame.nak, 0, trameAttendue, outTampon);   /* Trame altérée */
                        break;
                    case TypeEvenement.Timeout :
                        EnvoyerTrame(TypeTrame.data, plusVieilleTrame, trameAttendue, outTampon);
                        break;
                    case TypeEvenement.ACKTimeout :
                        /* Timer ACK expiré => Enovie ACK */
                        EnvoyerTrame(TypeTrame.ack, 0, trameAttendue, outTampon);
                        break;
                }

                if (nbTampons < Constants.NB_BUFS)
                    ActiverCoucherReseau();
                else
                    DesactiverCoucheReseau();
            }
        }
    }
}
