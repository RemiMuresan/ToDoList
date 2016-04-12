using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace ToDoData
{
    [Table("TaskItems")]
    public class TaskItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string Text { get; set; }
        public DateTime? ChangedAt { get; set; }
        public bool IsDone { get; set; }
        
    }
}
