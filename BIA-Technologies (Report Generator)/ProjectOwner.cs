using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Linq.Mapping;

namespace BIA_Technologies__Report_Generator_
{
    //[THINKABOUT] Красивые атрибуты. А зачем они, если используется ADO?
    [Table(Name = "ProjectOwners")]
    public class ProjectOwner
    {
        [Column(Name = "Guid", IsPrimaryKey = true, CanBeNull = false)]
        public Guid GUID { get; set; }

        [Column(Name = "Name")]
        public string Name { get; set; }
    }
}
