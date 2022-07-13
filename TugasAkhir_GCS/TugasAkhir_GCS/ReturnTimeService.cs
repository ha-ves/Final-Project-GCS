using FLS;
using FLS.MembershipFunctions;
using FLS.Rules;
using MavLinkNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Timers;
using Xamarin.Forms;

namespace TugasAkhir_GCS
{
    public class ReturnTimeService : IDisposable
    {
        Timer ReturnTimeChecker, ReturnTimeBlinker;
        int ReturnTimeInterval = 5000;

        public UasGlobalPositionInt UAV, Home;
        public sbyte UAVBatt;

        /* fuzzy variables */
        IFuzzyEngine Engine;
        LinguisticVariable BattPercent, Jarak, Ketinggian, WaktuKembali;

        public ReturnTimeService()
        {
            ReturnTimeBlinker = new Timer(ReturnTimeInterval);
            ReturnTimeBlinker.Elapsed += (sender, args) =>
            {
                (sender as Timer).Interval = ReturnTimeInterval;
                ((App.Current as App).MainPage as MainPage).WaktuKembaliBlinker();
            };

            //ReturnTimeChecker = new Timer(5000);
            //ReturnTimeChecker.Elapsed += (sender, args) => ReturnTimeUpdate();
            //ReturnTimeChecker.Start();
            ReturnTimeBlinker.Start();

            ((App.Current as App).MainPage as MainPage).ToggleWaktuKembali(true);
        }

        public double ReturnTimeUpdate(/*double batt, double km, double alt*/)
        {
            //if (UAV == null || Home == null)
            //    return 0;

            BattPercent = new LinguisticVariable("BattPercent");
            var kritikal = BattPercent.MembershipFunctions.AddZShaped("kritikal", 6.25, 3.125, 0, 12.5);
            var low = BattPercent.MembershipFunctions.AddGaussian("low", 25, 8, 0, 60);
            var normal = BattPercent.MembershipFunctions.AddSShaped("normal", 37.5, 5.5, 25, 100);

            Jarak = new LinguisticVariable("Jarak");
            var dekat = Jarak.MembershipFunctions.AddZShaped("dekat", 0.75, 0.325, 0, 1.5);
            var jauh = Jarak.MembershipFunctions.AddGaussian("jauh", 2, 0.75, 0, 5);
            var sangat_jauh = Jarak.MembershipFunctions.AddSShaped("sangat_jauh", 2.75, 0.325, 2, 5);

            Ketinggian = new LinguisticVariable("Ketinggian");
            var rendah = Ketinggian.MembershipFunctions.AddZShaped("rendah", 37.5, 16, 0, 75);
            var sedang = Ketinggian.MembershipFunctions.AddGaussian("sedang", 100, 35, 0, 250);
            var tinggi = Ketinggian.MembershipFunctions.AddSShaped("tinggi", 150, 20, 100, 200);

            WaktuKembali = new LinguisticVariable("WaktuKembali");
            var nominal = WaktuKembali.MembershipFunctions.AddZShaped("nominal", 3, 0.9, 0, 5);
            var mendekati_batas = WaktuKembali.MembershipFunctions.AddGaussian("mendekati_batas", 5, 1.5, 0, 10);
            var waktunya_kembali = WaktuKembali.MembershipFunctions.AddSShaped("waktunya_kembali", 7, 0.9, 5, 10);

            FuzzyRule[] rules =
            {
                    Rule.If(BattPercent.Is(kritikal)).Then(WaktuKembali.Is(waktunya_kembali)),

                    Rule.If(BattPercent.Is(low).And(Jarak.Is(dekat)).And(Ketinggian.Is(rendah))).Then(WaktuKembali.Is(nominal)),
                    Rule.If(BattPercent.Is(low).And(Jarak.Is(dekat)).And(Ketinggian.Is(sedang))).Then(WaktuKembali.Is(nominal)),
                    Rule.If(BattPercent.Is(low).And(Jarak.Is(dekat)).And(Ketinggian.Is(tinggi))).Then(WaktuKembali.Is(mendekati_batas)),

                    Rule.If(BattPercent.Is(low).And(Jarak.Is(jauh)).And(Ketinggian.Is(rendah))).Then(WaktuKembali.Is(nominal)),
                    Rule.If(BattPercent.Is(low).And(Jarak.Is(jauh)).And(Ketinggian.Is(sedang))).Then(WaktuKembali.Is(mendekati_batas)),
                    Rule.If(BattPercent.Is(low).And(Jarak.Is(jauh)).And(Ketinggian.Is(tinggi))).Then(WaktuKembali.Is(waktunya_kembali)),

                    Rule.If(BattPercent.Is(low).And(Jarak.Is(sangat_jauh)).And(Ketinggian.Is(rendah))).Then(WaktuKembali.Is(mendekati_batas)),
                    Rule.If(BattPercent.Is(low).And(Jarak.Is(sangat_jauh)).And(Ketinggian.Is(sedang))).Then(WaktuKembali.Is(waktunya_kembali)),
                    Rule.If(BattPercent.Is(low).And(Jarak.Is(sangat_jauh)).And(Ketinggian.Is(tinggi))).Then(WaktuKembali.Is(waktunya_kembali)),

                    Rule.If(BattPercent.Is(normal).And(Jarak.Is(dekat)).And(Ketinggian.Is(rendah))).Then(WaktuKembali.Is(nominal)),
                    Rule.If(BattPercent.Is(normal).And(Jarak.Is(dekat)).And(Ketinggian.Is(sedang))).Then(WaktuKembali.Is(nominal)),
                    Rule.If(BattPercent.Is(normal).And(Jarak.Is(dekat)).And(Ketinggian.Is(tinggi))).Then(WaktuKembali.Is(nominal)),

                    Rule.If(BattPercent.Is(normal).And(Jarak.Is(jauh)).And(Ketinggian.Is(rendah))).Then(WaktuKembali.Is(nominal)),
                    Rule.If(BattPercent.Is(normal).And(Jarak.Is(jauh)).And(Ketinggian.Is(sedang))).Then(WaktuKembali.Is(nominal)),
                    Rule.If(BattPercent.Is(normal).And(Jarak.Is(jauh)).And(Ketinggian.Is(tinggi))).Then(WaktuKembali.Is(nominal)),

                    Rule.If(BattPercent.Is(normal).And(Jarak.Is(sangat_jauh)).And(Ketinggian.Is(rendah))).Then(WaktuKembali.Is(nominal)),
                    Rule.If(BattPercent.Is(normal).And(Jarak.Is(sangat_jauh)).And(Ketinggian.Is(sedang))).Then(WaktuKembali.Is(nominal)),
                    Rule.If(BattPercent.Is(normal).And(Jarak.Is(sangat_jauh)).And(Ketinggian.Is(tinggi))).Then(WaktuKembali.Is(mendekati_batas)),
                };

            Engine = new FuzzyEngineFactory().Default();
            Engine.Rules.Add(rules);

            var batt = /*UAVBatt + */((App.Current as App).MainPage as MainPage).GetBattMod();
            //var km = (GetJaraknya(UAV.Lat, UAV.Lon, Home.Lat, Home.Lon) + ((App.Current as App).MainPage as MainPage).GetJarakMod()) * 0.001;
            var km = ((App.Current as App).MainPage as MainPage).GetJarakMod() * 0.001;
            //var alt = (Math.Abs(UAV.RelativeAlt - Home.RelativeAlt) / 304.8) + ((App.Current as App).MainPage as MainPage).GetTinggiMod();
            var alt = ((App.Current as App).MainPage as MainPage).GetTinggiMod();

            var res = Engine.Defuzzify(new
            {
                BattPercent = (double)batt,
                Jarak = km,
                Ketinggian = alt
            });

            var adj = res.Map(1.3, 8.5, 0, 10);

            //Debug.WriteLine($"FIS:" +
            //    $"{batt}[%] " +
            //    $"{km}[km] " +
            //    $"{alt}[ft] | " +
            //    $"res = {res} | " +
            //    $"adj = {adj}");

            ReturnTimeInterval = (int)(15000 - (Math.Log10(1 + adj) / Math.Log10(11) * 14000));
            //ReturnTimeInterval = 500;

            //Debug.WriteLine($"blinker = {ReturnTimeInterval} ms");

            ((App.Current as App).MainPage as MainPage).UpdateWaktuKembali(adj);

            return res;
        }

        private double GetJaraknya(int UAVlat, int UAVlon, int Homelat, int Homelon)
        {
            // radius bumi (meter)
            int R = 6371000;

            double lat1 = Homelat * 0.0000001 * Math.PI / 180.0;
            double lon1 = Homelon * 0.0000001 * Math.PI / 180.0;

            double lat2 = UAVlat * 0.0000001 * Math.PI / 180.0;
            double lon2 = UAVlon * 0.0000001 * Math.PI / 180.0;

            double deltaLat = lat2 - lat1;
            double deltaLon = lon2 - lon1;

            /* Jarak (Haversine) */
            double A = Math.Pow(Math.Sin(deltaLat * 0.5), 2)
                            + (Math.Cos(lat1) * Math.Cos(lat2)
                            * Math.Pow(Math.Sin(deltaLon * 0.5), 2));

            double B = 2 * Math.Atan2(Math.Sqrt(A), Math.Sqrt(1 - A));

            return (double)(R * B); // meter
        }

        public void Dispose()
        {
            ((App.Current as App).MainPage as MainPage).ToggleWaktuKembali(false);

            if(ReturnTimeChecker != null)
            {
                ReturnTimeChecker.Stop();
                ReturnTimeChecker.Dispose();
                ReturnTimeChecker = null;
            }

            Engine = null;
            BattPercent = null;
            Jarak = null;
            Ketinggian = null;
        }
    }
}
