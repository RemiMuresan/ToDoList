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
using ToDoData;
using SQLite;
using System.Linq;
using System.Collections.Generic;

namespace ToDoList
{
    [Activity(Label = "ToDoList", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private readonly string _databasePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "todo.db");
        private List<Task> _savedTasks;
        private Task _currentTask;
        private ToDoBL.TasksRepository _repo;

        EditText taskName;
        EditText taskBody;
        Spinner savedList;
        Button delete;
        EventHandler<AdapterView.ItemSelectedEventArgs> handler;

        protected override async void OnCreate(Android.OS.Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button colorText = FindViewById<Button>(Resource.Id.colorText);
            Button save = FindViewById<Button>(Resource.Id.addList);
            delete = FindViewById<Button>(Resource.Id.deleteTask);
            delete.Enabled = false;
            taskBody = FindViewById<EditText>(Resource.Id.editText);
            taskName = FindViewById<EditText>(Resource.Id.editName);
            savedList = FindViewById<Spinner>(Resource.Id.savedList);
            
            _repo = new ToDoBL.TasksRepository(_databasePath);

            _savedTasks = await _repo.GetTasks();
            if (_savedTasks == null)
            {
                _savedTasks = new List<Task>();
            }
            _savedTasks.Insert(0, new ToDoData.Task() { Name = "New" });
            LoadSavedTasks(0);

            colorText.Click += ColorText_Click;
            save.Click += Save_Click;
            delete.Click += Delete_Click;
        }

        private void LoadSavedTasks(int index)
        {
            var adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, _savedTasks);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            adapter.SetNotifyOnChange(true);
            if(handler != null)
                savedList.ItemSelected -= handler;
            handler = new EventHandler<AdapterView.ItemSelectedEventArgs>(SavedList_ItemSelected);
            savedList.ItemSelected += handler;
            savedList.Adapter = adapter;
            savedList.SetSelection(index, true);
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            _repo.DeleteTask(_currentTask);
            _savedTasks.Remove(_currentTask);
            taskName.Text = string.Empty;
            taskBody.Text = string.Empty;
            _currentTask = _savedTasks[0];
            delete.Enabled = false;
            LoadSavedTasks(0);
        }

        private void SavedList_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            _currentTask = _savedTasks.ElementAt(e.Position);
            if (_currentTask != null && _currentTask.Id > 0)
            {
                taskName.Text = _currentTask.Name;
                taskBody.TextFormatted = new SpannableString(_currentTask.Text);
                delete.Enabled = true;

            }
            else
            {
                taskName.Text = string.Empty;
                taskBody.Text = string.Empty;
                delete.Enabled = false;
            }
        }
        private async void Save_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(taskName.Text) && !string.IsNullOrEmpty(taskBody.Text))
            {
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
                    _savedTasks.Add(_currentTask);
                    delete.Enabled = true;
                }
                var id = await _repo.InsertUpdateTask(_currentTask);
                _currentTask.Id = id;
                var item = _savedTasks.Where(x => x.Id == _currentTask.Id).FirstOrDefault();
                item = _currentTask; ;
                LoadSavedTasks(_savedTasks.IndexOf(item));
            }
        }

        private void ColorText_Click(object sender, EventArgs e)
        {
            var text = taskBody.TextFormatted;
            SpannableString wordSpan = new SpannableString(text);
            wordSpan.SetSpan(new ForegroundColorSpan(Android.Graphics.Color.Red), taskBody.SelectionStart, taskBody.SelectionEnd, SpanTypes.ExclusiveExclusive);
            wordSpan.SetSpan(new StrikethroughSpan(), taskBody.SelectionStart, taskBody.SelectionEnd, SpanTypes.ExclusiveExclusive);
            taskBody.TextFormatted = wordSpan;
        }
    }
}

