using AutoMapper;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using SmartSchool.Graphql.Inputs;
using SmartSchool.Graphql.Models;
using SmartSchool.Schema;
using SmartSchool.Schema.Entities;
using SmartSchool.Schema.Enums;

namespace SmartSchool.Graphql.Mutations
{
    [ExtendObjectType(typeof(Mutation))]
    public class StudentMutation
    {
        //private readonly IDbContextFactory<SmartSchoolDbContext> dbContextFactory;
        private readonly IMapper mapper;

        public StudentMutation(
            //IDbContextFactory<SmartSchoolDbContext> dbContextFactory,
            IMapper mapper
        )
        {
            //this.dbContextFactory = dbContextFactory;
            this.mapper = mapper;
        }

        public async Task<StudentModel> CreateStudentAsync(AppDbContext dbContext, StudentInput input)
        {
            //using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var newRecord = mapper.Map<Student>(input);
            //newRecord.DateOfBirth = DateTime.SpecifyKind(newRecord.DateOfBirth, DateTimeKind.Utc); //need fix

            dbContext.Students.Add(newRecord);
            await dbContext.SaveChangesAsync();

            return mapper.Map<StudentModel>(newRecord);
        }

        public async Task<StudentModel> UpdateStudentAsync(AppDbContext dbContext, StudentInput input)
        {
            ArgumentNullException.ThrowIfNull(input.Id);

            var existingRecord = await dbContext.Students.FindAsync(input.Id.Value) ?? throw new KeyNotFoundException($"No matching record found (id={input.Id.Value})");
            mapper.Map(input, existingRecord);

            await dbContext.SaveChangesAsync();

            return mapper.Map<StudentModel>(existingRecord);
        }

    }
}
