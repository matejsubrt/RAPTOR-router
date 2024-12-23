using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.Structures.Configuration;

namespace UnitTests
{
    [TestClass]
    public class SettingsTests
    {
        private Settings settings = Settings.DEFAULT;

        void ResetSettings()
        {
            settings = Settings.DEFAULT;
        }

        [TestMethod]
        public void ValidateTest()
        {
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.ComfortBalance = ComfortBalance.ShortestTimeAbsolute;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.ComfortBalance = ComfortBalance.LeastTransfers;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.ComfortBalance++;
            Assert.IsFalse(settings.ValidateParameterValues());

            ResetSettings();



            settings.WalkingPreference = WalkingPreference.High;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.WalkingPreference = WalkingPreference.Low;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.WalkingPreference++;
            Assert.IsFalse(settings.ValidateParameterValues());

            ResetSettings();




            settings.TransferBuffer = TransferBuffer.None;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.TransferBuffer = TransferBuffer.Long;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.TransferBuffer++;
            Assert.IsFalse(settings.ValidateParameterValues());

            ResetSettings();




            settings.BikeTripBuffer = BikeTripBuffer.None;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.BikeTripBuffer = BikeTripBuffer.Long;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.BikeTripBuffer++;
            Assert.IsFalse(settings.ValidateParameterValues());

            ResetSettings();




            settings.WalkingPace = 2;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.WalkingPace = 12;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.WalkingPace = 60;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.WalkingPace = 61;
            Assert.IsFalse(settings.ValidateParameterValues());

            settings.WalkingPace = 1;
            Assert.IsFalse(settings.ValidateParameterValues());

            settings.WalkingPace = 0;
            Assert.IsFalse(settings.ValidateParameterValues());

            settings.WalkingPace = -1;
            Assert.IsFalse(settings.ValidateParameterValues());

            ResetSettings();




            settings.CyclingPace = 1;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.CyclingPace = 12;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.CyclingPace = 60;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.CyclingPace = 61;
            Assert.IsFalse(settings.ValidateParameterValues());

            settings.CyclingPace = 0;
            Assert.IsFalse(settings.ValidateParameterValues());

            settings.CyclingPace = -1;
            Assert.IsFalse(settings.ValidateParameterValues());

            ResetSettings();




            settings.BikeLockTime = 0;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.BikeLockTime = 12;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.BikeLockTime = 120;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.BikeLockTime = 121;
            Assert.IsFalse(settings.ValidateParameterValues());

            settings.BikeLockTime = -1;
            Assert.IsFalse(settings.ValidateParameterValues());

            ResetSettings();




            settings.BikeUnlockTime = 0;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.BikeUnlockTime = 12;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.BikeUnlockTime = 120;
            Assert.IsTrue(settings.ValidateParameterValues());

            settings.BikeUnlockTime = 121;
            Assert.IsFalse(settings.ValidateParameterValues());

            settings.BikeUnlockTime = -1;
            Assert.IsFalse(settings.ValidateParameterValues());

            ResetSettings();
        }
    }
}
