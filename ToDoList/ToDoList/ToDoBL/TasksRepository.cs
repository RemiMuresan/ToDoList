using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoData;

namespace ToDoBL
{
    public class TasksRepository
    {
        private readonly SQLite.SQLiteAsyncConnection _connection;

        public TasksRepository(string path)
        {
            try
            {
                _connection = new SQLiteAsyncConnection(path);
                _connection.CreateTableAsync<ToDoData.Task>();
                _connection.CreateTableAsync<ToDoData.TaskItem>();
            }
            catch (SQLite.SQLiteException ex)
            {

            }
        }
        public async Task<int> InsertUpdateTask(ToDoData.Task task, List<TaskItem> items)
        {
            try
            {
                if (task.Id > 0 && items != null && items.Count > 0)
                {
                    var oldItems = await _connection.Table<ToDoData.TaskItem>().Where(x => x.TaskId == task.Id).ToListAsync();
                    foreach (var item in oldItems)
                        await _connection.DeleteAsync(item);
                }
                if (task.Id == 0)
                {
                    await _connection.InsertAsync(task);
                }
                else
                    await _connection.UpdateAsync(task);
                foreach (var i in items)
                {
                    i.TaskId = task.Id;
                }
                await _connection.InsertAllAsync(items);
            }
            catch (SQLiteException ex)
            {
            }
            return task.Id;
        }

        public async void DeleteTask(ToDoData.Task task)
        {
            try
            {
                await _connection.DeleteAsync(task);
            }
            catch (SQLiteException ex)
            {
            }
        }

        public async Task<List<ToDoData.Task>> GetTasks()
        {
            return await _connection.Table<ToDoData.Task>().ToListAsync();
        }

        public async Task<List<ToDoData.TaskItem>> GetTaskItems(int taskId)
        {
            return await _connection.Table<ToDoData.TaskItem>().Where(x => x.TaskId == taskId).ToListAsync();
        }

        public async Task<ToDoData.Task> GetTask(int id)
        {
            return await (from t in _connection.Table<ToDoData.Task>()
                          where t.Id == id
                          select t).FirstOrDefaultAsync();
        }
    }
}
