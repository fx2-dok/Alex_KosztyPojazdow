using Soneta.Business;
using Soneta.Business.UI;
using Soneta.Core;
using Soneta.Ksiega;
using Soneta.Samochodowka;
using Soneta.Tools;
using Soneta.Types;
using System;

[assembly: Worker(typeof(Alex_KosztyPojazdow.KosztyPojazdow), typeof(DokEwidencji))]

namespace Alex_KosztyPojazdow
{

    public class KosztyPojazdow
    {
        [Context]
        public DokEwidencji dokumentMain { get; set; }

        public class Params : ContextBase
        {


            public Params(Context cx) : base(cx) { }

            public Pojazd[] pojazdyWybrane;

            [Priority(1)]
            [Caption("Pojazdy")]
            [Required] // walidacja 
            public Pojazd[] Pojazdy
            {
                get
                {
                    return this.pojazdyWybrane;
                }
                set
                {
                    this.pojazdyWybrane = value;
                    OnChanged(EventArgs.Empty);
                }
            }
            public object GetListPojazdy()
            {
                Soneta.Business.View view = SamochodowkaModule.GetInstance(Session).Pojazdy.CreateView();
                return view;
            }

            public bool IsReadOnlyDokumentMain()
            {
                return false;
            }


            private string opis;
            [Priority(10)]
            [Caption("Opis")]
            [Required]
            public string Opis
            {
                get
                {
                    return this.opis;
                }
                set
                {
                    this.opis = value;
                    OnChanged();
                }
            }

            private int ilosc_butli;
            [Priority(20)]
            [Caption("Liczba butli")]
            public int IloscButli
            {
                get
                {
                    return this.ilosc_butli;
                }
                set
                {
                    this.ilosc_butli = value;
                    OnChanged();
                }
            }
        }
        static private Params param;

        [Context]
        public static Params Parametry
        {
            get { return param; }
            set { param = value; }
        }

        [Context]
        public Context Context { get; set; }
        [Action(
            "Dodaj koszty do pojazdów",
            Priority = 30,
            Icon = ActionIcon.Go,
            Mode = ActionMode.SingleSession,
            Target = ActionTarget.Menu | ActionTarget.ToolbarWithText)]

        public MessageBoxInformation MyAction()
        {
            try
            {
                KsiegaModule km = KsiegaModule.GetInstance(Context.Session);
                SamochodowkaModule sm = SamochodowkaModule.GetInstance(Context.Session);


                decimal tempWartosc = decimal.Zero;
                decimal tempWaga = decimal.Zero;

                using (ITransaction t = Context.Session.Logout(true))
                {
                    //ususwanie kosztow z pojazdow
                    foreach (KosztEP koszt in dokumentMain.KosztyEP)
                    {
                        koszt.Delete();
                    }
                    t.Commit();


                    if (param.IloscButli == 0)  //przypisywanie kosztow pojazdom
                    {

                        for (int i = 0; i < param.pojazdyWybrane.Length; i++)
                        {
                            KosztEP kep = new KosztEP(dokumentMain);

                            sm.KosztyEP.AddRow(kep);
                            kep.Pojazd = param.pojazdyWybrane[i];
                            kep.Data = Date.Today;

                            kep.Opis = param.Opis;
                            if (!kep.Pojazd.Paliwa.IsEmpty)
                                kep.IloscPaliwa = new Decimal(0.01);
                            kep.Wartosc = System.Math.Round(dokumentMain.WartoscNetto.Value / (decimal)param.pojazdyWybrane.Length, 2);
                            if (i + 1 == param.pojazdyWybrane.Length) // do osatniego pojazdu dodaje błąd zaokrągleń
                            {
                                kep.Wartosc = dokumentMain.WartoscNetto.Value - tempWartosc;
                            }

                            tempWartosc += kep.Wartosc;
                            t.Commit();
                        }
                    }
                    else // podział wagi butli gazu - tylko
                    {
                        int wagaGazu = 11 * param.IloscButli;

                        for (int i = 0; i < param.pojazdyWybrane.Length; i++)
                        {
                            KosztEP kep = new KosztEP(dokumentMain);
                            sm.KosztyEP.AddRow(kep);
                            kep.Pojazd = param.pojazdyWybrane[i];
                            kep.Data = Date.Today;
                            kep.Opis = param.Opis;
                            kep.TypPaliwa = TypPaliwa.LPG;

                            kep.Wartosc = System.Math.Round(dokumentMain.WartoscNetto.Value / (decimal)param.pojazdyWybrane.Length, 2);
                            kep.IloscPaliwaKg = System.Math.Round(wagaGazu / (decimal)param.pojazdyWybrane.Length, 2);
                            if (i + 1 == param.pojazdyWybrane.Length) // do osatniego pojazdu dodaje błąd zaokrągleń
                            {
                                kep.Wartosc = dokumentMain.WartoscNetto.Value - tempWartosc;
                                kep.IloscPaliwaKg = wagaGazu - tempWaga;
                            }

                            tempWartosc += kep.Wartosc;
                            tempWaga += kep.IloscPaliwaKg;

                            t.Commit();
                        }
                    }
                }
                return new MessageBoxInformation("Informacja" ,"Operacja zakończona pomyślnie");
            }
            catch (Exception ex)
            {
                return new MessageBoxInformation("Informacja", "Operacja zakończona niepowodzeniem!" + Environment.NewLine + ex.ToString());
            }
        }
                

        public static bool IsVisibleMyAction(DokEwidencji dok)
        {
            return dok.Stan == StanEwidencji.Wprowadzony || dok.Stan == StanEwidencji.Predekretowany ? true : false; // ### produkcja
            //return true;  //### do testów
        }
    }
}



