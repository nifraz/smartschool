using SmartSchool.Schema.Enums;
using SmartSchool.Schema.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchool.Schema.Entities
{
    [Resource("class", Plural = "classes", Module = "academic", Icon = "mat:groups", Label = "Class", SortOrder = 15)]
    public class Class : AbstractRecord
    {
        [Field(Required = true, SortOrder = 1)]
        public Grade Grade { get; set; }
        [Field(Required = true, SortOrder = 2)]
        public string Section { get; set; }
        [Field(SortOrder = 3)]
        public string? Location { get; set; }

        //one
        [ForeignKey(nameof(School))]
        public long SchoolId { get; set; }
        public School School { get; set; }

        [ForeignKey(nameof(Language))]
        public string LanguageCode { get; set; }
        public Language Language { get; set; }

        //many
        [InverseProperty(nameof(ClassStudentEnrollment.Class))]
        public ICollection<ClassStudentEnrollment> ClassStudentEnrollments { get; set; } = [];

        [InverseProperty(nameof(ClassTeacherEnrollment.Class))]
        public ICollection<ClassTeacherEnrollment> ClassTeacherEnrollments { get; set; } = [];

        public override string ToString()
        {
            return $"{Grade.ToString()} {Section}";
        }
    }
}
