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
            var savedTasks = await _repo.GetTasks();
            if (savedTasks == null)
            {
                savedTasks = new List<Task>();
            }
            savedTasks.Insert(0, new ToDoData.Task() { Name = "New" });
            return savedTasks;
        }
        
        public IList<TaskItem> CreateCurrentItemsFromText(string text)
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

        public async System.Threading.Tasks.Task<List<Task>> GetSavedTasks()
        {
            if(_savedTasks == null)
            {
                _savedTasks = await PopulateList();
            }
            return _savedTasks;
        }
        public async System.Threading.Tasks.Task<int> AddTask(Task task, List<TaskItem> currentItems)
        {
            if (task.Id == 0)
                _savedTasks.Add(task);
            var id = await _repo.InsertUpdateTask(task, currentItems);
            task.Id = id;
            var item = _savedTasks.Where(x => x.Id == task.Id).FirstOrDefault();
            item = task;
            return _savedTasks.IndexOf(task);
        }

        public async System.Threading.Tasks.Task<List<TaskItem>> GetTaskItems(int taskId)
        {
            return await _repo.GetTaskItems(taskId);
        }

        public void DeleteTask(Task task)
        {
            _repo.DeleteTask(task);
            _savedTasks.Remove(task);
        }

        public int GetCurrentLine(string text, int cursorAt)
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
    }
}
