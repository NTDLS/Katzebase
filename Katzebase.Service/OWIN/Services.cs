using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;

namespace Katzebase.Service.OWIN
{
    public class Services: IDisposable
    {
        private List<IDisposable> runningServices;

        public void Start(string baseAddress)
        {
            runningServices = new List<IDisposable>();

            runningServices.Add(WebApp.Start<Startup>(url: baseAddress));
        }

        public void Dispose()
        {
            lock (runningServices)
            {
                foreach (var obj in runningServices)
                {
                    try
                    {
                        obj.Dispose();
                    }
                    catch
                    {
                        //Discard.
                    }
                }

                runningServices = null;
            }
        }

    }
}
