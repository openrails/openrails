// COPYRIGHT 2014 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using ORTS.Common;
using Xunit;

namespace Tests.Orts.Common
{
    public static class Conversions
    {
        static readonly IEqualityComparer<double> RequestedAccuracy = DynamicPrecisionEqualityComparer.Float;

        [Fact]
        public static void ConversionValues()
        {
            Assert.Equal(0, C.FromF(32f), RequestedAccuracy);
            Assert.Equal(-20, C.FromF(-4f), RequestedAccuracy);
        }

        [Fact]
        public static void InverseRelations()
        {
            Assert.Equal(1.2f, Me.FromMi(Me.ToMi(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Me.FromKiloM(Me.ToKiloM(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Me.FromYd(Me.ToYd(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Me.FromFt(Me.ToFt(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Me.FromIn(Me.ToIn(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, Me2.FromFt2(Me2.ToFt2(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Me2.FromIn2(Me2.ToIn2(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, Me3.FromFt3(Me3.ToFt3(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Me3.FromIn3(Me3.ToIn3(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, MpS.FromMpH(MpS.ToMpH(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, MpS.FromKpH(MpS.ToKpH(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, Kg.FromLb(Kg.ToLb(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Kg.FromTUS(Kg.ToTUS(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Kg.FromTUK(Kg.ToTUK(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Kg.FromTonne(Kg.ToTonne(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, N.FromLbf(N.ToLbf(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, KgpS.FromLbpH(KgpS.ToLbpH(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, W.FromKW(W.ToKW(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, W.FromHp(W.ToHp(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, W.FromBTUpS(W.ToBTUpS(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, KPa.FromPSI(KPa.ToPSI(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.FromInHg(KPa.ToInHg(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.FromBar(KPa.ToBar(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.FromKgfpCm2(KPa.ToKgfpCm2(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, Bar.FromKPa(Bar.ToKPa(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Bar.FromPSI(Bar.ToPSI(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Bar.FromInHg(Bar.ToInHg(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Bar.FromKgfpCm2(Bar.ToKgfpCm2(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, BarpS.FromPSIpS(BarpS.ToPSIpS(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, KJpKg.FromBTUpLb(KJpKg.ToBTUpLb(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, L.FromGUK(L.ToGUK(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, L.FromGUS(L.ToGUS(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, pS.FrompM(pS.TopM(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, pS.FrompH(pS.TopH(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, S.FromM(S.ToM(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, S.FromH(S.ToH(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, C.FromF(C.ToF(1.2f)), RequestedAccuracy);
            Assert.Equal(12f, C.FromK(C.ToK(12f)), RequestedAccuracy); // we loose accuracy because of the large 273.15
        }

        [Fact]
        public static void RelatedConversions()
        {
            Assert.Equal(1.44f, Me2.FromFt2((float)Math.Pow(Me.ToFt(1.2f), 2)), RequestedAccuracy);
            Assert.Equal(1.44f, Me2.ToFt2((float)Math.Pow(Me.FromFt(1.2f), 2)), RequestedAccuracy);
            Assert.Equal(1.44f, Me2.FromIn2((float)Math.Pow(Me.ToIn(1.2f), 2)), RequestedAccuracy);
            Assert.Equal(1.44f, Me2.ToIn2((float)Math.Pow(Me.FromIn(1.2f), 2)), RequestedAccuracy);

            Assert.Equal(1.728f, Me3.FromFt3((float)Math.Pow(Me.ToFt(1.2f), 3)), RequestedAccuracy);
            Assert.Equal(1.728f, Me3.ToFt3((float)Math.Pow(Me.FromFt(1.2f), 3)), RequestedAccuracy);
            Assert.Equal(1.728f, Me3.FromIn3((float)Math.Pow(Me.ToIn(1.2f), 3)), RequestedAccuracy);
            Assert.Equal(1.728f, Me3.ToIn3((float)Math.Pow(Me.FromIn(1.2f), 3)), RequestedAccuracy);

            Assert.Equal(1.2f, KPa.FromBar(Bar.FromKPa(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.ToBar(Bar.ToKPa(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.FromPSI(Bar.ToPSI(KPa.ToBar(1.2f))), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.ToPSI(KPa.FromBar(Bar.FromPSI(1.2f))), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.FromInHg(Bar.ToInHg(KPa.ToBar(1.2f))), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.ToInHg(KPa.FromBar(Bar.FromInHg(1.2f))), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.FromKgfpCm2(Bar.ToKgfpCm2(KPa.ToBar(1.2f))), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.ToKgfpCm2(KPa.FromBar(Bar.FromKgfpCm2(1.2f))), RequestedAccuracy);
        }

        [Fact]
        public static void RelatedConversionNonPhysical()
        {
            // Note: Related conversions that should hold mathematically, but perhaps not physically because of unit mismatch.

            Assert.Equal(1.2f, BarpS.FromPSIpS(Bar.ToPSI(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Bar.FromPSI(BarpS.ToPSIpS(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, BarpS.ToPSIpS(Bar.FromPSI(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, Bar.ToPSI(BarpS.FromPSIpS(1.2f)), RequestedAccuracy);

            Assert.Equal(1.2f, pS.FrompM(S.FromM(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, pS.TopM(S.ToM(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, pS.FrompH(S.FromH(1.2f)), RequestedAccuracy);
            Assert.Equal(1.2f, pS.TopH(S.ToH(1.2f)), RequestedAccuracy);
        }

        [Fact]
        public static void MultiUnitConversions()
        {
            Assert.Equal(1.2f, MpS.FromMpS(MpS.FromKpH(1.2f), true), RequestedAccuracy);
            Assert.Equal(1.2f, MpS.FromMpS(MpS.FromMpH(1.2f), false), RequestedAccuracy);
            Assert.Equal(1.2f, MpS.ToMpS(MpS.ToKpH(1.2f), true), RequestedAccuracy);
            Assert.Equal(1.2f, MpS.ToMpS(MpS.ToMpH(1.2f), false), RequestedAccuracy);

            Assert.Equal(1.2f, KPa.FromKPa(1.2f, PressureUnit.KPa), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.FromBar(KPa.FromKPa(1.2f, PressureUnit.Bar)), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.FromInHg(KPa.FromKPa(1.2f, PressureUnit.InHg)), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.FromKgfpCm2(KPa.FromKPa(1.2f, PressureUnit.KgfpCm2)), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.FromPSI(KPa.FromKPa(1.2f, PressureUnit.PSI)), RequestedAccuracy);
            Assert.Throws<ArgumentOutOfRangeException>(() => KPa.FromKPa(1.2f, PressureUnit.None));

            Assert.Equal(1.2f, KPa.ToKPa(1.2f, PressureUnit.KPa), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.ToBar(KPa.ToKPa(1.2f, PressureUnit.Bar)), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.ToInHg(KPa.ToKPa(1.2f, PressureUnit.InHg)), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.ToKgfpCm2(KPa.ToKPa(1.2f, PressureUnit.KgfpCm2)), RequestedAccuracy);
            Assert.Equal(1.2f, KPa.ToPSI(KPa.ToKPa(1.2f, PressureUnit.PSI)), RequestedAccuracy);
            Assert.Throws<ArgumentOutOfRangeException>(() => KPa.ToKPa(1.2f, PressureUnit.None));
        }

        [Fact]
        public static void CompareTime()
        {
            var time4 = 4 * 3600;
            var time7 = 7 * 3600;
            var time8 = 8 * 3600;
            var time12 = 12 * 3600;
            var time15 = 15 * 3600;
            var time16 = 16 * 3600;
            var time20 = 20 * 3600;

            // Simple cases
            Assert.Equal(time7, CompareTimes.LatestTime(time4, time7));
            Assert.Equal(time12, CompareTimes.LatestTime(time4, time12));
            Assert.Equal(time15, CompareTimes.LatestTime(time4, time15));
            Assert.Equal(time4, CompareTimes.LatestTime(time4, time20));

            Assert.Equal(time12, CompareTimes.LatestTime(time7, time12));
            Assert.Equal(time15, CompareTimes.LatestTime(time7, time15));
            Assert.Equal(time7, CompareTimes.LatestTime(time7, time20));

            Assert.Equal(time15, CompareTimes.LatestTime(time12, time15));
            Assert.Equal(time20, CompareTimes.LatestTime(time12, time20));

            Assert.Equal(time20, CompareTimes.LatestTime(time15, time20));

            Assert.Equal(time4, CompareTimes.EarliestTime(time4, time7));
            Assert.Equal(time4, CompareTimes.EarliestTime(time4, time12));
            Assert.Equal(time4, CompareTimes.EarliestTime(time4, time15));
            Assert.Equal(time20, CompareTimes.EarliestTime(time4, time20));

            Assert.Equal(time7, CompareTimes.EarliestTime(time7, time12));
            Assert.Equal(time7, CompareTimes.EarliestTime(time7, time15));
            Assert.Equal(time20, CompareTimes.EarliestTime(time7, time20));

            Assert.Equal(time12, CompareTimes.EarliestTime(time12, time15));
            Assert.Equal(time12, CompareTimes.EarliestTime(time12, time20));

            Assert.Equal(time15, CompareTimes.EarliestTime(time15, time20));

            // Boundary cases
            Assert.Equal(time4, CompareTimes.EarliestTime(time8, time4));
            Assert.Equal(time8, CompareTimes.EarliestTime(time8, time12));
            Assert.Equal(time8, CompareTimes.EarliestTime(time8, time20));

            Assert.Equal(time8, CompareTimes.LatestTime(time8, time4));
            Assert.Equal(time12, CompareTimes.LatestTime(time8, time12));
            Assert.Equal(time20, CompareTimes.LatestTime(time8, time20));

            Assert.Equal(time4, CompareTimes.EarliestTime(time16, time4));
            Assert.Equal(time12, CompareTimes.EarliestTime(time16, time12));
            Assert.Equal(time16, CompareTimes.EarliestTime(time16, time20));

            Assert.Equal(time16, CompareTimes.LatestTime(time16, time4));
            Assert.Equal(time16, CompareTimes.LatestTime(time16, time12));
            Assert.Equal(time20, CompareTimes.LatestTime(time16, time20));
        }

        [Fact]
        public static void FormattedStrings()
        {
            // Note: Only pressure is tested at the moment, mainly because of its complexity.

            Assert.Equal(String.Empty, FormatStrings.FormatPressure(1.2f, PressureUnit.None, PressureUnit.KPa, true));
            Assert.Equal(String.Empty, FormatStrings.FormatPressure(1.2f, PressureUnit.KPa, PressureUnit.None, true));

            Assert.Equal("1 kPa", FormatStrings.FormatPressure(1.2f, PressureUnit.KPa, PressureUnit.KPa, true));
            Assert.Equal("1 kPa", FormatStrings.FormatPressure(KPa.ToBar(1.2f), PressureUnit.Bar, PressureUnit.KPa, true));
            Assert.Equal("1 kPa", FormatStrings.FormatPressure(KPa.ToInHg(1.2f), PressureUnit.InHg, PressureUnit.KPa, true));
            Assert.Equal("1 kPa", FormatStrings.FormatPressure(KPa.ToKgfpCm2(1.2f), PressureUnit.KgfpCm2, PressureUnit.KPa, true));
            Assert.Equal("1 kPa", FormatStrings.FormatPressure(KPa.ToPSI(1.2f), PressureUnit.PSI, PressureUnit.KPa, true));

            var barResult = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:F1} bar", 1.2f);
            Assert.Equal(barResult, FormatStrings.FormatPressure(Bar.ToKPa(1.2f), PressureUnit.KPa, PressureUnit.Bar, true));
            Assert.Equal(barResult, FormatStrings.FormatPressure(1.2f, PressureUnit.Bar, PressureUnit.Bar, true));
            Assert.Equal(barResult, FormatStrings.FormatPressure(Bar.ToInHg(1.2f), PressureUnit.InHg, PressureUnit.Bar, true));
            Assert.Equal(barResult, FormatStrings.FormatPressure(Bar.ToKgfpCm2(1.2f), PressureUnit.KgfpCm2, PressureUnit.Bar, true));
            Assert.Equal(barResult, FormatStrings.FormatPressure(Bar.ToPSI(1.2f), PressureUnit.PSI, PressureUnit.Bar, true));

            var psiResult = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:F0} psi", 1.2f);
            Assert.Equal(psiResult, FormatStrings.FormatPressure(KPa.FromPSI(1.2f), PressureUnit.KPa, PressureUnit.PSI, true));
            Assert.Equal(psiResult, FormatStrings.FormatPressure(KPa.ToBar(KPa.FromPSI(1.2f)), PressureUnit.Bar, PressureUnit.PSI, true));
            Assert.Equal(psiResult, FormatStrings.FormatPressure(KPa.ToInHg(KPa.FromPSI(1.2f)), PressureUnit.InHg, PressureUnit.PSI, true));
            Assert.Equal(psiResult, FormatStrings.FormatPressure(KPa.ToKgfpCm2(KPa.FromPSI(1.2f)), PressureUnit.KgfpCm2, PressureUnit.PSI, true));
            Assert.Equal(psiResult, FormatStrings.FormatPressure(KPa.ToPSI(KPa.FromPSI(1.2f)), PressureUnit.PSI, PressureUnit.PSI, true));

            var inhgResult = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:F0} inHg", 1.2f);
            Assert.Equal(inhgResult, FormatStrings.FormatPressure(KPa.FromInHg(1.2f), PressureUnit.KPa, PressureUnit.InHg, true));
            Assert.Equal(inhgResult, FormatStrings.FormatPressure(KPa.ToBar(KPa.FromInHg(1.2f)), PressureUnit.Bar, PressureUnit.InHg, true));
            Assert.Equal(inhgResult, FormatStrings.FormatPressure(KPa.ToInHg(KPa.FromInHg(1.2f)), PressureUnit.InHg, PressureUnit.InHg, true));
            Assert.Equal(inhgResult, FormatStrings.FormatPressure(KPa.ToKgfpCm2(KPa.FromInHg(1.2f)), PressureUnit.KgfpCm2, PressureUnit.InHg, true));
            Assert.Equal(inhgResult, FormatStrings.FormatPressure(KPa.ToPSI(KPa.FromInHg(1.2f)), PressureUnit.PSI, PressureUnit.InHg, true));

            var kgfResult = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:F1} kgf/cm²", 1.2f);
            Assert.Equal(kgfResult, FormatStrings.FormatPressure(KPa.FromKgfpCm2(1.2f), PressureUnit.KPa, PressureUnit.KgfpCm2, true));
            Assert.Equal(kgfResult, FormatStrings.FormatPressure(KPa.ToBar(KPa.FromKgfpCm2(1.2f)), PressureUnit.Bar, PressureUnit.KgfpCm2, true));
            Assert.Equal(kgfResult, FormatStrings.FormatPressure(KPa.ToInHg(KPa.FromKgfpCm2(1.2f)), PressureUnit.InHg, PressureUnit.KgfpCm2, true));
            Assert.Equal(kgfResult, FormatStrings.FormatPressure(KPa.ToKgfpCm2(KPa.FromKgfpCm2(1.2f)), PressureUnit.KgfpCm2, PressureUnit.KgfpCm2, true));
            Assert.Equal(kgfResult, FormatStrings.FormatPressure(KPa.ToPSI(KPa.FromKgfpCm2(1.2f)), PressureUnit.PSI, PressureUnit.KgfpCm2, true));
        }
    }
}
