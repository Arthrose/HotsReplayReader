using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace HotsReplayReader
{
    internal class hotsImage
    {
        public Bitmap Bitmap { get; set; }
        public string Name { get; set; }

        public hotsImage()
        {
        }

        public void setBitmap()
        {
            if (Name == "Abathur") Bitmap = ByteToImage(heroesIcon.Abathur);
            else if (Name == "Alarak") Bitmap = ByteToImage(heroesIcon.Alarak);
            else if (Name == "Alexstrasza") Bitmap = ByteToImage(heroesIcon.Alexstrasza);
            else if (Name == "Ana") Bitmap = ByteToImage(heroesIcon.Ana);
            else if (Name == "Anduin") Bitmap = ByteToImage(heroesIcon.Anduin);
            else if (Name == "Anub'arak") Bitmap = ByteToImage(heroesIcon.Anub_arak);
            else if (Name == "Artanis") Bitmap = ByteToImage(heroesIcon.Artanis);
            else if (Name == "Arthas") Bitmap = ByteToImage(heroesIcon.Arthas);
            else if (Name == "Auriel") Bitmap = ByteToImage(heroesIcon.Auriel);
            else if (Name == "Azmodan") Bitmap = ByteToImage(heroesIcon.Azmodan);
            else if (Name == "Blaze") Bitmap = ByteToImage(heroesIcon.Blaze);
            else if (Name == "Brightwing") Bitmap = ByteToImage(heroesIcon.Brightwing);
            else if (Name == "Cassia") Bitmap = ByteToImage(heroesIcon.Cassia);
            else if (Name == "Chen") Bitmap = ByteToImage(heroesIcon.Chen);
            else if (Name == "Cho") Bitmap = ByteToImage(heroesIcon.Cho);
            else if (Name == "Chromie") Bitmap = ByteToImage(heroesIcon.Chromie);
            else if (Name == "D.Va") Bitmap = ByteToImage(heroesIcon.D_Va);
            else if (Name == "Deathwing") Bitmap = ByteToImage(heroesIcon.Deathwing);
            else if (Name == "Deckard") Bitmap = ByteToImage(heroesIcon.Deckard);
            else if (Name == "Dehaka") Bitmap = ByteToImage(heroesIcon.Dehaka);
            else if (Name == "Diablo") Bitmap = ByteToImage(heroesIcon.Diablo);
            else if (Name == "E.T.C.") Bitmap = ByteToImage(heroesIcon.E_T_C_);
            else if (Name == "Falstad") Bitmap = ByteToImage(heroesIcon.Falstad);
            else if (Name == "Fenix") Bitmap = ByteToImage(heroesIcon.Fenix);
            else if (Name == "Gall") Bitmap = ByteToImage(heroesIcon.Gall);
            else if (Name == "Garrosh") Bitmap = ByteToImage(heroesIcon.Garrosh);
            else if (Name == "Gazlowe") Bitmap = ByteToImage(heroesIcon.Gazlowe);
            else if (Name == "Genji") Bitmap = ByteToImage(heroesIcon.Genji);
            else if (Name == "Greymane") Bitmap = ByteToImage(heroesIcon.Greymane);
            else if (Name == "Gul'dan") Bitmap = ByteToImage(heroesIcon.Gul_dan);
            else if (Name == "Hanzo") Bitmap = ByteToImage(heroesIcon.Hanzo);
            else if (Name == "Hogger") Bitmap = ByteToImage(heroesIcon.Hogger);
            else if (Name == "Illidan") Bitmap = ByteToImage(heroesIcon.Illidan);
            else if (Name == "Imperius") Bitmap = ByteToImage(heroesIcon.Imperius);
            else if (Name == "Jaina") Bitmap = ByteToImage(heroesIcon.Jaina);
            else if (Name == "Johanna") Bitmap = ByteToImage(heroesIcon.Johanna);
            else if (Name == "Junkrat") Bitmap = ByteToImage(heroesIcon.Junkrat);
            else if (Name == "Kael'thas") Bitmap = ByteToImage(heroesIcon.Kael_thas);
            else if (Name == "Kel'Thuzad") Bitmap = ByteToImage(heroesIcon.Kel_Thuzad);
            else if (Name == "Kerrigan") Bitmap = ByteToImage(heroesIcon.Kerrigan);
            else if (Name == "Kharazim") Bitmap = ByteToImage(heroesIcon.Kharazim);
            else if (Name == "Leoric") Bitmap = ByteToImage(heroesIcon.Leoric);
            else if (Name == "Li Li") Bitmap = ByteToImage(heroesIcon.Li_Li);
            else if (Name == "Li-Ming") Bitmap = ByteToImage(heroesIcon.Li_Ming);
            else if (Name == "Lt. Morales") Bitmap = ByteToImage(heroesIcon.Lt__Morales);
            else if (Name == "Lunara") Bitmap = ByteToImage(heroesIcon.Lunara);
            else if (Name == "Lúcio") Bitmap = ByteToImage(heroesIcon.Lúcio);
            else if (Name == "Maiev") Bitmap = ByteToImage(heroesIcon.Maiev);
            else if (Name == "Mal'Ganis") Bitmap = ByteToImage(heroesIcon.Mal_Ganis);
            else if (Name == "Malfurion") Bitmap = ByteToImage(heroesIcon.Malfurion);
            else if (Name == "Malthael") Bitmap = ByteToImage(heroesIcon.Malthael);
            else if (Name == "Medivh") Bitmap = ByteToImage(heroesIcon.Medivh);
            else if (Name == "Mei") Bitmap = ByteToImage(heroesIcon.Mei);
            else if (Name == "Mephisto") Bitmap = ByteToImage(heroesIcon.Mephisto);
            else if (Name == "Muradin") Bitmap = ByteToImage(heroesIcon.Muradin);
            else if (Name == "Murky") Bitmap = ByteToImage(heroesIcon.Murky);
            else if (Name == "Nazeebo") Bitmap = ByteToImage(heroesIcon.Nazeebo);
            else if (Name == "Nova") Bitmap = ByteToImage(heroesIcon.Nova);
            else if (Name == "Orphea") Bitmap = ByteToImage(heroesIcon.Orphea);
            else if (Name == "Probius") Bitmap = ByteToImage(heroesIcon.Probius);
            else if (Name == "Qhira") Bitmap = ByteToImage(heroesIcon.Qhira);
            else if (Name == "Ragnaros") Bitmap = ByteToImage(heroesIcon.Ragnaros);
            else if (Name == "Raynor") Bitmap = ByteToImage(heroesIcon.Raynor);
            else if (Name == "Rehgar") Bitmap = ByteToImage(heroesIcon.Rehgar);
            else if (Name == "Rexxar") Bitmap = ByteToImage(heroesIcon.Rexxar);
            else if (Name == "Samuro") Bitmap = ByteToImage(heroesIcon.Samuro);
            else if (Name == "Sgt. Hammer") Bitmap = ByteToImage(heroesIcon.Sgt__Hammer);
            else if (Name == "Sonya") Bitmap = ByteToImage(heroesIcon.Sonya);
            else if (Name == "Stitches") Bitmap = ByteToImage(heroesIcon.Stitches);
            else if (Name == "Stukov") Bitmap = ByteToImage(heroesIcon.Stukov);
            else if (Name == "Sylvanas") Bitmap = ByteToImage(heroesIcon.Sylvanas);
            else if (Name == "Tassadar") Bitmap = ByteToImage(heroesIcon.Tassadar);
            else if (Name == "The Butcher") Bitmap = ByteToImage(heroesIcon.The_Butcher);
            else if (Name == "The Lost Vikings") Bitmap = ByteToImage(heroesIcon.The_Lost_Vikings);
            else if (Name == "Thrall") Bitmap = ByteToImage(heroesIcon.Thrall);
            else if (Name == "Tracer") Bitmap = ByteToImage(heroesIcon.Tracer);
            else if (Name == "Tychus") Bitmap = ByteToImage(heroesIcon.Tychus);
            else if (Name == "Tyrael") Bitmap = ByteToImage(heroesIcon.Tyrael);
            else if (Name == "Tyrande") Bitmap = ByteToImage(heroesIcon.Tyrande);
            else if (Name == "Uther") Bitmap = ByteToImage(heroesIcon.Uther);
            else if (Name == "Valeera") Bitmap = ByteToImage(heroesIcon.Valeera);
            else if (Name == "Valla") Bitmap = ByteToImage(heroesIcon.Valla);
            else if (Name == "Varian") Bitmap = ByteToImage(heroesIcon.Varian);
            else if (Name == "Whitemane") Bitmap = ByteToImage(heroesIcon.Whitemane);
            else if (Name == "Xul") Bitmap = ByteToImage(heroesIcon.Xul);
            else if (Name == "Yrel") Bitmap = ByteToImage(heroesIcon.Yrel);
            else if (Name == "Zagara") Bitmap = ByteToImage(heroesIcon.Zagara);
            else if (Name == "Zarya") Bitmap = ByteToImage(heroesIcon.Zarya);
            else if (Name == "Zeratul") Bitmap = ByteToImage(heroesIcon.Zeratul);
            else if (Name == "Zul'jin") Bitmap = ByteToImage(heroesIcon.Zul_jin);
            else Bitmap = ByteToImage(heroesIcon._Null);
        }

        public Bitmap getBitmap(string name)
        {
            Name = name;
            setBitmap();
            return Bitmap;
        }

        private static Bitmap ByteToImage(byte[] blob)
        {
            MemoryStream mStream = new MemoryStream();
            byte[] pData = blob;
            mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
            Bitmap bm = new Bitmap(mStream, false);
            mStream.Dispose();
            return bm;
        }
    }
}
