﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Util;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;
using DirectCareConnect.Droid.Services.Location;

namespace DirectCareConnect.Droid
{
    public class App
    {
        protected static LocationServiceConnection locationServiceConnection;
        protected readonly string logTag = "App";

        public static App Current { get; }

        static App()
        {
            Current = new App();
        }

        protected App()
        {
            // create a new service connection so we can get a binder to the service
            locationServiceConnection = new LocationServiceConnection(null);

            // this event will fire when the Service connectin in the OnServiceConnected call 
            locationServiceConnection.ServiceConnected += (sender, e) =>
            {
                Log.Debug(logTag, "Service Connected");
                // we will use this event to notify MainActivity when to start updating the UI
                LocationServiceConnected(this, e);
            };
        }

        public LocationService LocationService
        {
            get
            {
                if (locationServiceConnection.Binder == null)
                {
                    throw new Exception("Service not bound yet");
                }

                // note that we use the ServiceConnection to get the Binder, and the Binder to get the Service here
                return locationServiceConnection.Binder.Service;
            }
        }

        // events
        public event EventHandler<ServiceConnectedEventArgs> LocationServiceConnected = delegate { };

        public static void StartLocationService()
        {
            // Starting a service like this is blocking, so we want to do it on a background thread
            new Task(() =>
            {
                // Start our main service
                Log.Debug("App", "Calling StartService");

                // Check if device is running Android 8.0 or higher and if so, use the newer StartForegroundService() method
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    Application.Context.StartForegroundService(new Intent(Application.Context, typeof(LocationService)));
                }
                else // For older versions, use the traditional StartService() method
                {
                    Application.Context.StartService(new Intent(Application.Context, typeof(LocationService)));
                }

                // bind our service (Android goes and finds the running service by type, and puts a reference
                // on the binder to that service)
                // The Intent tells the OS where to find our Service (the Context) and the Type of Service
                // we're looking for (LocationService)
                var locationServiceIntent = new Intent(Application.Context, typeof(LocationService));
                Log.Debug("App", "Calling service binding");

                // Finally, we can bind to the Service using our Intent and the ServiceConnection we
                // created in a previous step.
                Application.Context.BindService(locationServiceIntent, locationServiceConnection, Bind.AutoCreate);
            }).Start();
        }

        public static void StopLocationService()
        {
            // Check for nulls in case StartLocationService task has not yet completed.
            Log.Debug("App", "StopLocationService");

            // Unbind from the LocationService; otherwise, StopSelf (below) will not work:
            if (locationServiceConnection != null)
            {
                Log.Debug("App", "Unbinding from LocationService");
                Application.Context.UnbindService(locationServiceConnection);
            }

            // Stop the LocationService:
            if (Current.LocationService != null)
            {
                Log.Debug("App", "Stopping the LocationService");
                Current.LocationService.StopSelf();
            }
        }
    }
}