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
using Android.Database;
using Android.Telephony;
using Android.Provider;

namespace ToDoList
{
    [BroadcastReceiver]
    [IntentFilter(new [] { "android.provider.Telephony.SMS_RECEIVED" })]
    public class SmsListener : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (Telephony.Sms.Intents.SmsReceivedAction.Equals(intent.Action))
            {
                foreach (SmsMessage smsMessage in Telephony.Sms.Intents.GetMessagesFromIntent(intent))
                {
                    string messageBody = smsMessage.MessageBody;
                }
            }
        }
    }
}