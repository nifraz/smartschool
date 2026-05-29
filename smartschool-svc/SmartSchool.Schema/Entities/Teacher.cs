using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartSchool.Schema.Resources;

namespace SmartSchool.Schema.Entities
{
    [Resource("teacher", Plural = "teachers", Module = "people", Icon = "mat:person", Label = "Teacher", SortOrder = 40)]
    public class Teacher : AbstractRecord
    {
        [Field(SortOrder = 1)]
        public string? RegistrationNo { get; set; }
        [Field(SortOrder = 2)]
        public string? ServiceGrade { get; set; }

        //one
        [ForeignKey(nameof(Person))]
        public long PersonId { get; set; }
        public Person Person { get; set; }

        //many
        [InverseProperty(nameof(SchoolTeacherEnrollment.Teacher))]
        public ICollection<SchoolTeacherEnrollment> SchoolTeacherEnrollments { get; set; } = [];
    }
}
