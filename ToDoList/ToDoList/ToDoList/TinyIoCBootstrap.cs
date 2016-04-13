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
using TinyIoC;
using Contracts;
namespace ToDoList
{
    public static class TinyIoCBootstrap
    {
        public static void Register()
        {
            TinyIoCContainer.Current.Register<ILogger>(new Logger());
        }
    }
}