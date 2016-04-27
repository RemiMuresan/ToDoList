using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoData;

namespace ToDoDataService
{
    public class TaskModel
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid TaskId { get; set; }
        public List<TaskItem> Items { get; set; }
    }
}
