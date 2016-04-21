using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Text;
using Android.Webkit;
using Android.Text.Style;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ToDoDataService;
using ToDoData;
using TinyIoC;
using Contracts;
using Android.Provider;
using Java.Util;
using static Android.Provider.CalendarContract;
using System.Runtime.Serialization.Formatters.Binary;

namespace ToDoList
{
    [Activity(Label = "ToDoList", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private ToDoService _service;
        private Task _currentTask;
        private List<TaskItem> _currentItems;
        private ILogger _logger;

        EditText taskName;
        EditText taskBody;
        Spinner savedList;
        Button delete;
        Button due;
        Button alertDate;
        Button alertTime;
        EventHandler<AdapterView.ItemSelectedEventArgs> handler;
        private const string MY_ACCOUNT_NAME = "remi.muresan@outlook.com";

        enum DialogEnum
        {
            DUE_DIALOG_ID,
            ALERT_DATE_DIALOG_ID,
            ALERT_TIME_DIALOG_ID
        }

        protected override void OnCreate(Android.OS.Bundle bundle)
        {
            base.OnCreate(bundle);
            TinyIoCBootstrap.Register();
            _logger = TinyIoCContainer.Current.Resolve<ILogger>();

            try
            {
                SetContentView(Resource.Layout.Main);
                _service = new ToDoService(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "todo.db"));
                _currentTask = new Task() { Name = "New" };

                Button colorText = FindViewById<Button>(Resource.Id.colorText);
                Button save = FindViewById<Button>(Resource.Id.addList);
                Button dueReset = FindViewById<Button>(Resource.Id.dueDateReset);
                Button alertReset = FindViewById<Button>(Resource.Id.alertReset);
                due = FindViewById<Button>(Resource.Id.dueDate);
                delete = FindViewById<Button>(Resource.Id.deleteTask);
                alertDate = FindViewById<Button>(Resource.Id.alertDate);
                alertTime = FindViewById<Button>(Resource.Id.alertTime);
                delete.Enabled = false;
                taskBody = FindViewById<EditText>(Resource.Id.editText);
                taskName = FindViewById<EditText>(Resource.Id.editName);
                savedList = FindViewById<Spinner>(Resource.Id.savedList);
                LoadSavedTasks(0);

                colorText.Click += ColorText_Click;
                save.Click += Save_Click;
                delete.Click += Delete_Click;
                due.Click += (o, e) => ShowDialog((int)DialogEnum.DUE_DIALOG_ID);
                alertDate.Click += (o, e) => ShowDialog((int)DialogEnum.ALERT_DATE_DIALOG_ID);
                alertTime.Click += (o, e) => ShowDialog((int)DialogEnum.ALERT_TIME_DIALOG_ID);
                dueReset.Click += DueReset_Click;
                alertReset.Click += AlertReset_Click;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }
        }

        private async void Save_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(taskName.Text) && !string.IsNullOrEmpty(taskBody.Text))
                {
                    var text = taskBody.Text;
                    var currentItems = _service.CreateCurrentItemsFromText(text);
                    var span = new SpannableString(taskBody.TextFormatted);
                    var spans = span.GetSpans(0, taskBody.Text.Length - 1, Java.Lang.Class.FromType(typeof(ForegroundColorSpan)));
                    if (span != null)
                    {
                        foreach (var s in spans)
                        {
                            var x = span.GetSpanStart(s);
                            var line = _service.GetCurrentLine(text, x);
                            if (line != -1)
                                currentItems[line].IsDone = true;
                        }
                    }
                    PopulateCurrentTask(_currentTask);
                    if (_currentTask.Id == 0)
                    { 
                        _currentTask.DateCreated = DateTime.Now;
                        delete.Enabled = true;
                    }
                    if (_currentTask.DueDate.HasValue && _currentTask.Alert.HasValue)
                    {
                        var d = _currentTask.DueDate.Value - _currentTask.Alert.Value;
                        if (_currentTask.EventId.HasValue)
                        {
                            UpdateEvent(_currentTask.EventId.Value, _currentTask.Name, _currentTask.DueDate.Value);

                            if (_currentTask.ReminderId.HasValue)
                            {
                                UpdateReminder(_currentTask.ReminderId.Value, d.Minutes);
                            }
                            else
                            {
                                long reminderId = AddReminder(_currentTask.EventId.Value, d.Minutes);
                                _currentTask.ReminderId = reminderId;
                            }
                        }
                        else
                        {
                            long eventID = AddEvent(_currentTask.Name, _currentTask.DueDate.Value);
                            long reminderId = AddReminder(eventID, d.Minutes);
                            _currentTask.EventId = eventID;
                            _currentTask.ReminderId = reminderId;
                        }
                    }
                    else
                    {
                        _currentTask.EventId = null;
                        _currentTask.ReminderId = null;
                    }

                    var index = await _service.AddTask(_currentTask, currentItems.ToList());
                    LoadSavedTasks(index);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }
        }

        private void PopulateCurrentTask(Task t)
        {
            t.Name = taskName.Text;
            t.Text = taskBody.TextFormatted.ToString();
            t.LastChange = DateTime.Now;
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            try
            {
                _service.DeleteTask(_currentTask);
                taskName.Text = string.Empty;
                taskBody.Text = string.Empty;
                _currentTask = new Task();
                delete.Enabled = false;
                LoadSavedTasks(0);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }
        }

        private async void SavedList_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            try
            {
                var savedTasks = await _service.GetSavedTasks();
                if (_currentTask.Id != savedTasks.ElementAt(e.Position).Id)
                {
                    var t = new Task();
                    PopulateCurrentTask(t);
                    t.DueDate = _currentTask.DueDate;
                    t.Alert = _currentTask.Alert;
                    var current = savedTasks.Where(x => x.Id == _currentTask.Id).FirstOrDefault();
                    if (_currentTask.Id != savedTasks.ElementAt(e.Position).Id && _service.HasChanges(t, current))
                    {
                        var builder = new AlertDialog.Builder(this);
                        builder.SetMessage("You have unsaved changes. Do you want to continue?");
                        builder.SetPositiveButton("Yes", async (s, ev) => { await LoadSelectedItem(savedTasks, e); });
                        builder.SetNegativeButton("No", (s, ev) =>
                        {
                            savedList.SetSelection(savedTasks.IndexOf(current));
                        });
                        builder.Create().Show();
                    }
                    else
                    {
                        await LoadSelectedItem(savedTasks, e);
                    }
                }
                
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }
        }

        private async System.Threading.Tasks.Task LoadSelectedItem(List<Task> savedTasks, AdapterView.ItemSelectedEventArgs e)
        {
            _currentTask = new Task(savedTasks.ElementAt(e.Position));
            if (_currentTask != null && _currentTask.Id > 0)
            {
                taskName.Text = _currentTask.Name;
                var wordSpan = new SpannableString(_currentTask.Text);
                delete.Enabled = true;
                var items = await _service.GetTaskItems(_currentTask.Id);
                _currentItems = items;
                foreach (var item in items)
                {
                    if (item.IsDone)
                    {
                        wordSpan.SetSpan(new ForegroundColorSpan(Android.Graphics.Color.Red), _currentTask.Text.IndexOf(item.Text), _currentTask.Text.IndexOf(item.Text) + item.Text.Length, SpanTypes.ExclusiveExclusive);
                        wordSpan.SetSpan(new StrikethroughSpan(), _currentTask.Text.IndexOf(item.Text), _currentTask.Text.IndexOf(item.Text) + item.Text.Length, SpanTypes.ExclusiveExclusive);
                    }
                }
                taskBody.TextFormatted = wordSpan;
                due.Text = _currentTask.DueDate.HasValue ? _currentTask.DueDate.Value.ToShortDateString() : BaseContext.Resources.GetString(Resource.String.DueDateText);
                alertDate.Text = _currentTask.Alert.HasValue ? _currentTask.Alert.Value.ToShortDateString() : BaseContext.Resources.GetString(Resource.String.AlertDate);
                alertTime.Text = _currentTask.Alert.HasValue ? _currentTask.Alert.Value.ToShortTimeString() : BaseContext.Resources.GetString(Resource.String.AlertTime);
            }
            else
            {
                taskName.Text = string.Empty;
                taskBody.Text = string.Empty;
                AlertReset_Click(null, null);
                DueReset_Click(null, null);
                delete.Enabled = false;
            }
        }

        private void ColorText_Click(object sender, EventArgs e)
        {
            try
            {
                var text = taskBody.TextFormatted;
                SpannableString wordSpan = new SpannableString(text);
                var line = _service.GetCurrentLine(taskBody.Text, taskBody.SelectionStart);
                var textItems = taskBody.Text.Split(new string[] { "\n" }, StringSplitOptions.None);
                var beforeText = string.Join("\n", textItems.Take(line));
                if (line > 0)
                    beforeText += "\n";

                wordSpan.SetSpan(new ForegroundColorSpan(Android.Graphics.Color.Red), beforeText.Length, beforeText.Length + textItems[line].Length, SpanTypes.ExclusiveExclusive);
                wordSpan.SetSpan(new StrikethroughSpan(), beforeText.Length, beforeText.Length + textItems[line].Length, SpanTypes.ExclusiveExclusive);
                taskBody.TextFormatted = wordSpan;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }
        }

        private async void LoadSavedTasks(int index)
        {
            try
            {
                var tasks = await _service.GetSavedTasks();
                var adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, tasks);
                adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                adapter.SetNotifyOnChange(true);
                if (handler != null)
                    savedList.ItemSelected -= handler;
                handler = new EventHandler<AdapterView.ItemSelectedEventArgs>(SavedList_ItemSelected);
                savedList.ItemSelected += handler;
                savedList.Adapter = adapter;
                savedList.SetSelection(index, true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }
        }

        private void DueDatePickerCallback(object sender, DatePickerDialog.DateSetEventArgs e)
        {
            due.Text = e.Date.ToShortDateString();
            _currentTask.DueDate = e.Date;
        }
        private void AlertDatePickerCallback(object sender, DatePickerDialog.DateSetEventArgs e)
        {
            alertDate.Text = e.Date.ToShortDateString();
            var date = e.Date;

            if (_currentTask.Alert.HasValue)
            {
                _currentTask.Alert = new DateTime(date.Year, date.Month, date.Day, _currentTask.Alert.Value.Hour, _currentTask.Alert.Value.Minute, 0);
            }
            else
            {
                _currentTask.Alert = date;
            }
        }

        private void AlertTimePickerCallback(object sender, TimePickerDialog.TimeSetEventArgs e)
        {
            alertTime.Text = e.HourOfDay.ToString() + ":" + e.Minute.ToString();
            var date = _currentTask.Alert;
            if (!date.HasValue)
                date = DateTime.Now.Date;
            else
                date = date.Value.Date;

            _currentTask.Alert = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, e.HourOfDay, e.Minute, 0);

        }

        private void AlertReset_Click(object sender, EventArgs e)
        {
            _currentTask.Alert = null;
            alertDate.Text = BaseContext.Resources.GetString(Resource.String.AlertDate);
            alertTime.Text = BaseContext.Resources.GetString(Resource.String.AlertTime);

        }

        private void DueReset_Click(object sender, EventArgs e)
        {
            _currentTask.DueDate = null;
            due.Text = BaseContext.Resources.GetString(Resource.String.DueDateText);
        }

        protected override Dialog OnCreateDialog(int id)
        {
            var dialogId = (DialogEnum)id;
            switch (dialogId)
            {
                case DialogEnum.DUE_DIALOG_ID:
                    return new DatePickerDialog(this, DueDatePickerCallback, _currentTask.DueDate.HasValue ? _currentTask.DueDate.Value.Year : DateTime.Now.Year,
                    _currentTask.DueDate.HasValue ? _currentTask.DueDate.Value.Month : DateTime.Now.Month,
                    _currentTask.DueDate.HasValue ? _currentTask.DueDate.Value.Day : DateTime.Now.Day);
                case DialogEnum.ALERT_DATE_DIALOG_ID:
                    return new DatePickerDialog(this, AlertDatePickerCallback, _currentTask.Alert.HasValue ? _currentTask.Alert.Value.Year : DateTime.Now.Year,
                    _currentTask.Alert.HasValue ? _currentTask.Alert.Value.Month : DateTime.Now.Month,
                    _currentTask.Alert.HasValue ? _currentTask.Alert.Value.Day : DateTime.Now.Day);
                case DialogEnum.ALERT_TIME_DIALOG_ID:
                    return new TimePickerDialog(this, AlertTimePickerCallback, _currentTask.Alert.HasValue ? _currentTask.Alert.Value.Hour : DateTime.Now.Hour,
                    _currentTask.Alert.HasValue ? _currentTask.Alert.Value.Minute : DateTime.Now.Minute, true);
            }
            return null;
        }

        

        #region Events

        public void Remind(DateTime date, string name, DateTime dueDate)
        {
            //Intent intent = new Intent(Intent.ActionEdit);
            //intent.SetType("vnd.android.cursor.item/event");
            //intent.PutExtra("beginTime", GetDateTimeMS(date.Year, date.Month, date.Day, date.Hour, date.Minute));
            //intent.PutExtra("allDay", true);
            //intent.PutExtra("rrule", "FREQ=YEARLY");
            //intent.PutExtra("endTime", GetDateTimeMS(date.Year, date.Month, date.Day, date.Hour, date.Minute + 1));
            //intent.PutExtra("title", "A Test Event from android app");
            //StartActivity(intent);
        }
        private long AddEvent(string name, DateTime dueDate)
        {
            ContentValues eventValues = new ContentValues();
            eventValues.Put(Events.InterfaceConsts.CalendarId, GetCalendarId());
            eventValues.Put(Events.InterfaceConsts.Title, "ToDo " + name);
            eventValues.Put(Events.InterfaceConsts.Description, "Don't forget to finalize " + name + " until " + dueDate.ToShortDateString());
            eventValues.Put(Events.InterfaceConsts.AllDay, 1);
            eventValues.Put(Events.InterfaceConsts.HasAlarm, 1);
            eventValues.Put(Events.InterfaceConsts.Dtstart, GetDateTimeMS(dueDate.Year, dueDate.Month, dueDate.Day, 0, 0));
            eventValues.Put(Events.InterfaceConsts.Dtend, GetDateTimeMS(dueDate.Year, dueDate.Month, dueDate.Day, 23, 59));
            eventValues.Put(Events.InterfaceConsts.EventTimezone, "UTC");
            eventValues.Put(Events.InterfaceConsts.AccessLevel, (int)EventsAccess.Private);
            eventValues.Put(Events.InterfaceConsts.Availability, (int)EventsAvailability.Busy);
            eventValues.Put(Events.InterfaceConsts.GuestsCanModify, 1);
            eventValues.Put(Events.InterfaceConsts.GuestsCanInviteOthers, 1);
            var eventUri = ContentResolver.Insert(Events.ContentUri, eventValues);
            return long.Parse(eventUri.LastPathSegment);
        }

        private long AddReminder(long eventID, int minutes)
        {
            ContentValues remindervalues = new ContentValues();
            remindervalues.Put(CalendarContract.Reminders.InterfaceConsts.EventId, eventID);
            remindervalues.Put(CalendarContract.Reminders.InterfaceConsts.Method, (int)RemindersMethod.Alert);
            remindervalues.Put(CalendarContract.Reminders.InterfaceConsts.Minutes, minutes);
            var reminderURI = ContentResolver.Insert(CalendarContract.Reminders.ContentUri, remindervalues);
            return long.Parse(reminderURI.LastPathSegment);
        }
        long GetDateTimeMS(int yr, int month, int day, int hr, int min)
        {
            Calendar c = Calendar.GetInstance(Java.Util.TimeZone.Default);

            c.Set(CalendarField.DayOfMonth, day);
            c.Set(CalendarField.HourOfDay, hr);
            c.Set(CalendarField.Minute, min);
            c.Set(CalendarField.Month, month);
            c.Set(CalendarField.Year, yr);

            return c.TimeInMillis;
        }

        private long GetCalendarId()
        {
            string[] projection = new string[] { Calendars.InterfaceConsts.Id };
            string selection = Calendars.InterfaceConsts.AccountName + " = ? AND " + Calendars.InterfaceConsts.AccountType + " = ? ";
            // use the same values as above:
            string[] selArgs = new string[] { MY_ACCOUNT_NAME, CalendarContract.AccountTypeLocal };
            var cursor = ContentResolver.Query(
                           Calendars.ContentUri,
                           projection,
                           selection,
                           selArgs,
                           null);
            if (cursor.MoveToFirst())
            {
                return cursor.GetLong(0);
            }
            return -1;
        }

        private int DeleteEvent(long eventId)
        {
            string[] selArgs = new string[] { eventId.ToString() };
            int deleted = ContentResolver.Delete(
                           Events.ContentUri,
                           Events.InterfaceConsts.Id + " =? ",
                           selArgs);
            return deleted;
        }

        private int DeleteReminder(long reminderId)
        {
            string[] selArgs = new string[] { reminderId.ToString() };
            int deleted = ContentResolver.Delete(
                           Reminders.ContentUri,
                           Reminders.InterfaceConsts.Id + " =? ",
                           selArgs);
            return deleted;
        }

        private int UpdateEvent(long eventId, string name, DateTime dueDate)
        {
            ContentValues eventValues = new ContentValues();
            eventValues.Put(Events.InterfaceConsts.Title, "ToDo " + name);
            eventValues.Put(Events.InterfaceConsts.Description, "Don't forget to finalize " + name + " until " + dueDate.ToShortDateString());
            eventValues.Put(Events.InterfaceConsts.Dtstart, GetDateTimeMS(dueDate.Year, dueDate.Month, dueDate.Day, 0, 0));
            eventValues.Put(Events.InterfaceConsts.Dtend, GetDateTimeMS(dueDate.Year, dueDate.Month, dueDate.Day, 23, 59));
            string[] selArgs = new string[] { eventId.ToString() };
            int deleted = ContentResolver.Update(
                           Events.ContentUri,
                           eventValues,
                           Events.InterfaceConsts.Id + " =? ",
                           selArgs);
            return deleted;
        }

        private int UpdateReminder(long reminderId, int minutes)
        {
            ContentValues remindervalues = new ContentValues();
            remindervalues.Put(CalendarContract.Reminders.InterfaceConsts.Minutes, minutes);
            string[] selArgs = new string[] { reminderId.ToString() };
            int deleted = ContentResolver.Update(
                           Reminders.ContentUri,
                           remindervalues,
                           Reminders.InterfaceConsts.Id + " =? ",
                           selArgs);
            return deleted;
        }
        #endregion
    }
}

