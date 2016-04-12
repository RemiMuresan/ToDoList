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

namespace ToDoList
{
    [Activity(Label = "ToDoList", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private ToDoService _service;
        private Task _currentTask;
        private List<TaskItem> _currentItems;

        EditText taskName;
        EditText taskBody;
        Spinner savedList;
        Button delete;
        EventHandler<AdapterView.ItemSelectedEventArgs> handler;

        protected override void OnCreate(Android.OS.Bundle bundle)
        {
            base.OnCreate(bundle);
            
            SetContentView(Resource.Layout.Main);
            _service = new ToDoService(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "todo.db"));

            Button colorText = FindViewById<Button>(Resource.Id.colorText);
            Button save = FindViewById<Button>(Resource.Id.addList);
            delete = FindViewById<Button>(Resource.Id.deleteTask);
            delete.Enabled = false;
            taskBody = FindViewById<EditText>(Resource.Id.editText);
            taskName = FindViewById<EditText>(Resource.Id.editName);
            savedList = FindViewById<Spinner>(Resource.Id.savedList);
            LoadSavedTasks(0);

            colorText.Click += ColorText_Click;
            save.Click += Save_Click;
            delete.Click += Delete_Click;
        }

        private async void Save_Click(object sender, EventArgs e)
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

        private void Delete_Click(object sender, EventArgs e)
        {
            _service.DeleteTask(_currentTask);
            taskName.Text = string.Empty;
            taskBody.Text = string.Empty;
            _currentTask = new Task();
            delete.Enabled = false;
            LoadSavedTasks(0);
        }

        private async void SavedList_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
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
                foreach(var item in items)
                {
                    if(item.IsDone)
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

        private void ColorText_Click(object sender, EventArgs e)
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
        
        private async void LoadSavedTasks(int index)
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
    }
}

