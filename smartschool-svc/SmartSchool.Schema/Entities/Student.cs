using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartSchool.Schema.Resources;

namespace SmartSchool.Schema.Entities
{
    [Resource("student", Plural = "students", Module = "people", Icon = "mat:school", Label = "Student", SortOrder = 30)]
    public class Student : AbstractRecord
    {
        //one
        [ForeignKey(nameof(Person))]
        public long PersonId { get; set; }
        public Person Person { get; set; }

        //many
        [InverseProperty(nameof(SchoolStudentEnrollment.Student))]
        public ICollection<SchoolStudentEnrollment> SchoolStudentEnrollments { get; set; } = [];

    }

}
