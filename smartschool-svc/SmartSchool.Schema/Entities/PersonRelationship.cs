using SmartSchool.Schema.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSchool.Schema.Entities
{
    public class PersonRelationship : AbstractRecord
    {
        //one
        public long Person1Id { get; set; }
        [ForeignKey("Person1Id")]
        public Person Person1 { get; set; }

        [Required]
        public HumanRelationship Person1Relationship { get; set; }

        public long Person2Id { get; set; }
        [ForeignKey("Person2Id")]
        public Person Person2 { get; set; }

        [Required]
        public HumanRelationship Person2Relationship { get; set; }
    }
}
