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

        private void AlertReset_Click(object sender, EventArgs e)
        {
            _currentTask.Alert = null;
            alertDate.Text = Resource.String.AlertDate.ToString();
            alertTime.Text = Resource.String.AlertTime.ToString();

        }

        private void DueReset_Click(object sender, EventArgs e)
        {
            _currentTask.DueDate = null;
            due.Text = Resource.String.DueDateText.ToString();
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
                    if (_currentTask != null && _currentTask.Id > 0)
                    {
                        _currentTask.Name = taskName.Text;
                        _currentTask.Text = taskBody.TextFormatted.ToString();
                        _currentTask.LastChange = DateTime.Now;
                    }
                    else
                    {
                        _currentTask = new Task();
                        _currentTask.DateCreated = DateTime.Now;
                        _currentTask.LastChange = DateTime.Now;
                        _currentTask.Text = taskBody.TextFormatted.ToString();
                        _currentTask.Name = taskName.Text;
                        delete.Enabled = true;
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
                _currentTask = savedTasks.ElementAt(e.Position);
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
                }
                else
                {
                    taskName.Text = string.Empty;
                    taskBody.Text = string.Empty;
                    delete.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
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
            _currentTask.DueDate = e.Date;
            _currentTask.Alert = e.Date;
        }

        private void AlertTimePickerCallback(object sender, TimePickerDialog.TimeSetEventArgs e)
        {
            alertTime.Text = e.HourOfDay.ToString() + ":" + e.Minute.ToString();
            var date = _currentTask.Alert;
            if (!date.HasValue)
                date = DateTime.Now.Date;
            else
                date = date.Value.Date;
            _currentTask.Alert = date.Value.AddHours(e.HourOfDay);
            _currentTask.Alert = date.Value.AddMinutes(e.Minute);

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
    }
}

