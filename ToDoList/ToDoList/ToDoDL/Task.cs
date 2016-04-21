using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace ToDoData
{
    [Table("Tasks")]
    public class Task
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime LastChange { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? Alert { get; set; }

        public long? EventId { get; set; }
        public long? ReminderId { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public Task()
        {
                
        }

        public Task(Task t)
        {
            Id = t.Id;
            Name = t.Name;
            Text = t.Text;
            DueDate = t.DueDate;
            Alert = t.Alert;
            DateCreated = t.DateCreated;
            LastChange = t.LastChange;
            EventId = t.EventId;
            ReminderId = t.ReminderId;

        }
    }
}
