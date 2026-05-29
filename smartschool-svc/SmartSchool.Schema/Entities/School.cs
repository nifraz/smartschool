using Microsoft.EntityFrameworkCore;
using SmartSchool.Schema.Enums;
using SmartSchool.Schema.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchool.Schema.Entities
{
    [Index(nameof(CensusNo), IsUnique = true)]
    [Resource("school", Plural = "schools", Module = "academic", Icon = "mat:school", Label = "School", SortOrder = 10)]
    public class School : AbstractRecord
    {
        [Field(Label = "Census No", Required = true, Unique = true, SortOrder = 1)]
        public string CensusNo { get; set; }

        [Field(Label = "Name", Required = true, SortOrder = 2)]
        public string Name { get; set; }

        [Field(SortOrder = 3)]
        public string Location { get; set; }

        [Field(SortOrder = 4)]
        public string? Address { get; set; }

        [Field(SortOrder = 5)]
        public string? Email { get; set; }

        [Field(SortOrder = 6)]
        public string? PhoneNo { get; set; }

        [Field(SortOrder = 7)]
        public SchoolType Type { get; set; }

        //one
        [ForeignKey(nameof(Division))]
        public int DivisionId { get; set; }
        public Division Division { get; set; }

        //many
        [InverseProperty(nameof(SchoolStudentEnrollmentRequest.School))]
        public ICollection<SchoolStudentEnrollmentRequest> SchoolStudentEnrollmentRequests { get; set; } = [];

        [InverseProperty(nameof(SchoolStudentEnrollment.School))]
        public ICollection<SchoolStudentEnrollment> SchoolStudentEnrollments { get; set; } = [];

        [InverseProperty(nameof(SchoolTeacherEnrollmentRequest.School))]
        public ICollection<SchoolTeacherEnrollmentRequest> SchoolTeacherEnrollmentRequests { get; set; } = [];

        [InverseProperty(nameof(SchoolTeacherEnrollment.School))]
        public ICollection<SchoolTeacherEnrollment> SchoolTeacherEnrollments { get; set; } = [];

        [InverseProperty(nameof(SchoolPrincipalEnrollment.School))]
        public ICollection<SchoolPrincipalEnrollment> SchoolPrincipalEnrollments { get; set; } = [];

        [InverseProperty(nameof(Class.School))]
        public ICollection<Class> Classes { get; set; } = [];
    }
}
