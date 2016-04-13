using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Contracts;
using Android.Util;

namespace ToDoList
{
    public class Logger : ILogger
    {
        public void Error(string msg)
        {
            Log.Error("ERROR", msg);
        }

        public void Info(string msg)
        {
            Log.Info("INFO", msg);
        }
    }
}