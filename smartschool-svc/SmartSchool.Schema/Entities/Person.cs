using Microsoft.EntityFrameworkCore;
using SmartSchool.Schema.Classes;
using SmartSchool.Schema.Enums;
using SmartSchool.Schema.Resources;
using SmartSchool.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchool.Schema.Entities
{
    //[Index(nameof(NicNo), IsUnique = true)]
    //[Index(nameof(Email), IsUnique = true)]
    [Resource("person", Plural = "persons", Module = "people", Icon = "mat:person", Label = "Person", SortOrder = 20)]
    public class Person : AbstractRecord
    {
        [Field(Required = true, SortOrder = 1)]
        public string FullName { get; set; }
        [Field(SortOrder = 2)]
        public string ShortName { get; set; }
        [Field(SortOrder = 3)]
        public string? Nickname { get; set; }
        [Field(SortOrder = 4)]
        public DateOnly? DateOfBirth { get; set; }
        [Field(SortOrder = 5)]
        public string? BcNo { get; set; }
        [Field(SortOrder = 6)]
        public Sex Sex { get; set; }
        [Field(SortOrder = 7)]
        public string? NicNo { get; set; }
        [Field(SortOrder = 8)]
        public string? PassportNo { get; set; }
        [Field(SortOrder = 9)]
        public string? MobileNo { get; set; }
        [Field(SortOrder = 10)]
        public string? Email { get; set; }
        [Field(SortOrder = 11)]
        public string? Address { get; set; }
        [Field(SortOrder = 12)]
        public string? Image { get; set; }

        //one
        [InverseProperty(nameof(User.Person))]
        public User? User { get; set; }

        [InverseProperty(nameof(Student.Person))]
        public Student? Student { get; set; }

        [InverseProperty(nameof(Teacher.Person))]
        public Teacher? Teacher { get; set; }

        [InverseProperty(nameof(Principal.Person))]
        public Principal? Principal { get; set; }

        //many
        [InverseProperty(nameof(SchoolStudentEnrollmentRequest.Person))]
        public ICollection<SchoolStudentEnrollmentRequest> SchoolStudentEnrollmentRequests { get; set; } = [];

        [InverseProperty(nameof(SchoolTeacherEnrollmentRequest.Person))]
        public ICollection<SchoolTeacherEnrollmentRequest> SchoolTeacherEnrollmentRequests { get; set; } = [];


        [InverseProperty(nameof(PersonRelationship.Person1))]
        public ICollection<PersonRelationship> Person1Relationships { get; set; } = [];

        [InverseProperty(nameof(PersonRelationship.Person2))]
        public ICollection<PersonRelationship> Person2Relationships { get; set; } = [];

        [NotMapped]
        public Age Age
        {
            get
            {
                return DateOfBirth != null
                    ? CalculateExactAge(DateOfBirth.Value)
                    : new Age { Years = 0, Months = 0, Days = 0 };
            }
        }

        private static Age CalculateExactAge(DateOnly dateOfBirth)
        {
            var (years, months, days) = dateOfBirth.GetAge(DateTime.Today);

            return new Age
            {
                Years = years,
                Months = months,
                Days = days,
            };
        }
    }
}
