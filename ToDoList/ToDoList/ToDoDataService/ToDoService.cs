using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToDoData;

namespace ToDoDataService
{
    public class ToDoService
    {
        private readonly string _databasePath;
        private List<Task> _savedTasks;
        private ToDoBL.TasksRepository _repo;
        private List<int> _checkedItems;

        public ToDoService(string path)
        {
            _databasePath = path;
            _checkedItems = new List<int>();
            _repo = new ToDoBL.TasksRepository(_databasePath);
        }

        private async System.Threading.Tasks.Task<List<Task>> PopulateList()
        {
            try
            {
                var savedTasks = await _repo.GetTasks();
                if (savedTasks == null)
                {
                    savedTasks = new List<Task>();
                }
                savedTasks.Insert(0, new ToDoData.Task() { Name = "New" });
                return savedTasks;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public IList<TaskItem> CreateCurrentItemsFromText(string text)
        {

            try
            {
                var textItems = text.Split(new string[] { "\n" }, StringSplitOptions.None);
                var currentItems = new List<TaskItem>();
                foreach (var textItem in textItems)
                {
                    if (!string.IsNullOrEmpty(textItem))
                        currentItems.Add(new TaskItem() { Text = textItem, ChangedAt = DateTime.Now, IsDone = false });
                }
                return currentItems;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async System.Threading.Tasks.Task<List<Task>> GetSavedTasks()
        {

            try
            {
                if (_savedTasks == null)
                {
                    _savedTasks = await PopulateList();
                }
                return _savedTasks;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async System.Threading.Tasks.Task<int> AddTask(Task task, List<TaskItem> currentItems)
        {

            try
            {
                if (task.Id == 0)
                    _savedTasks.Add(task);
                var id = await _repo.InsertUpdateTask(task, currentItems);
                task.Id = id;
                var item = _savedTasks.Where(x => x.Id == task.Id).FirstOrDefault();
                item = task;
                return _savedTasks.IndexOf(task);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async System.Threading.Tasks.Task<List<TaskItem>> GetTaskItems(int taskId)
        {

            try
            {
                return await _repo.GetTaskItems(taskId);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void DeleteTask(Task task)
        {

            try
            {
                _repo.DeleteTask(task);
                _savedTasks.Remove(task);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int GetCurrentLine(string text, int cursorAt)
        {

            try
            {
                if (!string.IsNullOrEmpty(text) && cursorAt >= 0 && cursorAt <= text.Length)
                {
                    if (cursorAt == text.Length)
                        cursorAt--;
                    var firstPart = text.Substring(0, cursorAt + 1);
                    var lines = firstPart.Split(new string[] { "\n" }, StringSplitOptions.None);
                    return lines.Count() - 1;
                }
                return -1;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool HasChanges(Task t1, Task t2)
        {
            if (!(string.IsNullOrEmpty(t1.Name)  && t2.Name == "New") && t1.Name != t2.Name)
                return true;
            if (t1.Text != t2.Text && !(string.IsNullOrEmpty(t1.Text) && string.IsNullOrEmpty(t2.Text)))
                return true;
            if (t1.DueDate != t2.DueDate)
                return true;
            if (t1.Alert != t2.Alert)
                return true;

            return false;
        }

        public string SerializeTask(TaskModel t)
        {
            return JsonConvert.SerializeObject(t);
        }
    }
}
