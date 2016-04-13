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

        public override string ToString()
        {
            return this.Name;
        }
    }
}
