﻿using System;
using Alpha;
using Beta;
using Gamma;
using Windows.Devices.Geolocation;

namespace TestEmbeddedLibrary
{
    class MyAlpha : IAlpha
    {
        public int Five() { return 5; }
    }

    class MyBeta : IBeta
    {
        public int CallFive(Alpha.IAlpha a) { return a.Five(); }
    }

    class MyGreek : IAlpha, IGamma
    {
        public int Five() { return 5; }
        public int Six() { return 6; }
    }

    public class TestLib 
    {
        public TestLib()
        {
        }

        internal int Test1_Helper(IAlpha alpha) { return alpha.Five(); }

        public int Test1() 
        {
            MyAlpha a = new();
            MyGreek g = new();
            return Test1_Helper(a) + Test1_Helper(g);
        }

        public int Test2()
        {
            MyGreek g = new();
            MyBeta b = new();
            return b.CallFive(g);
        }

        public (bool, bool) Test3()
        {
            IAlpha myAlpha = new MyAlpha();
            MyGreek myGreek = new();
            QIAgent qiAgent = new();
            return (qiAgent.CheckForIGamma(myAlpha), qiAgent.CheckForIGamma(myGreek));
        }

        internal QIAgent GetQIAgent() { return new QIAgent(); }

        public int Test4()
        {
            QIAgent qiAgent = GetQIAgent();
            var x = qiAgent.IdentityAlpha(new MyAlpha());
            return qiAgent.Run(x);
        }

        async System.Threading.Tasks.Task CallGeoAsyncApi()
        {
            Console.WriteLine("Making a Microsoft.Devices.Geolocation.Geolocator object...");
            Geolocator g = new();
            Console.WriteLine("Setting the Desired Accuracy to Default on the Geolocator object...");
            g.DesiredAccuracy = PositionAccuracy.Default;
            Console.WriteLine("Accessing the Desired Accuracy, shows: " + g.DesiredAccuracy);
            Console.WriteLine("Calling GetGeopositionAsync...");
            Geoposition pos = await g.GetGeopositionAsync();
        }

        public void Test5()
        {
            CallGeoAsyncApi().Wait(1000);
        }
    }

}
