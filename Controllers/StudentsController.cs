using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using data2410_api_v1.Models;

namespace data2410_api_v1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController(IConfiguration config) : ControllerBase
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection")!;

    private static string GetGrade(int marks) => marks switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 60 => "C",
        _ => "D"
    };

    [HttpGet]
    public async Task<ActionResult<List<Student>>> GetAll()
    {
        var students = new List<Student>();
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand("SELECT Id, Name, Course, Marks, Grade FROM Students", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            students.Add(new Student
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Course = reader.GetString(2),
                Marks = reader.GetInt32(3),
                Grade = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return students;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> GetById(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand("SELECT Id, Name, Course, Marks, Grade FROM Students WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return NotFound();

        return new Student
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Course = reader.GetString(2),
            Marks = reader.GetInt32(3),
            Grade = reader.IsDBNull(4) ? null : reader.GetString(4)
        };
    }

    [HttpPost]
    public async Task<ActionResult<Student>> Create(Student student)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(
            "INSERT INTO Students (Name, Course, Marks) OUTPUT INSERTED.Id VALUES (@Name, @Course, @Marks)", conn);
        cmd.Parameters.AddWithValue("@Name", student.Name);
        cmd.Parameters.AddWithValue("@Course", student.Course);
        cmd.Parameters.AddWithValue("@Marks", student.Marks);

        student.Id = (int)await cmd.ExecuteScalarAsync();
        return CreatedAtAction(nameof(GetById), new { id = student.Id }, student);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Student updated)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(
            "UPDATE Students SET Name = @Name, Course = @Course, Marks = @Marks WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Name", updated.Name);
        cmd.Parameters.AddWithValue("@Course", updated.Course);
        cmd.Parameters.AddWithValue("@Marks", updated.Marks);

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows == 0 ? NotFound() : NoContent();
    }

    [HttpPost("calculate-grades")]
    public async Task<ActionResult<List<Student>>> CalculateGrades()
    {
        var studentsWithGrade = new List<Student>();

        // Write code to calculate and update grades
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        // Read all students from the database
        using var selectCmd = new SqlCommand("SELECT Id, Name, Course, Marks, Grade FROM Students", conn);
        using var reader = await selectCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            studentsWithGrade.Add(new Student
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Course = reader.GetString(2),
                Marks = reader.GetInt32(3),
                Grade = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }
        reader.Close();

        // Calculate grade for each student and update the Grade column in the database
        foreach (var student in studentsWithGrade)
        {
            student.Grade = GetGrade(student.Marks);

            using var updateCmd = new SqlCommand(
                "UPDATE Students SET Grade = @Grade WHERE Id = @Id", conn);
            updateCmd.Parameters.AddWithValue("@Grade", student.Grade);
            updateCmd.Parameters.AddWithValue("@Id", student.Id);
            await updateCmd.ExecuteNonQueryAsync();
        }

        // Return the list of all students with their calculated grades
        return studentsWithGrade;
    }

    [HttpGet("report")]
    public async Task<IActionResult> Report()
    {
        // Write code for the report generation logic.
        var reports = new List<CourseReport>();
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        // Select the database and group students by course.
        // For each course, calculate total students, average marks,and count of each grade (A, B, C, D) using GROUP BY with aggregate functions.
        const string sql = """
            SELECT
                Course,
                COUNT(*) AS TotalStudents,
                AVG(CAST(Marks AS FLOAT)) AS AverageMarks,
                SUM(CASE WHEN Marks >= 90 THEN 1 ELSE 0 END) AS GradeA,
                SUM(CASE WHEN Marks >= 80 AND Marks < 90 THEN 1 ELSE 0 END) AS GradeB,
                SUM(CASE WHEN Marks >= 60 AND Marks < 80 THEN 1 ELSE 0 END) AS GradeC,
                SUM(CASE WHEN Marks < 60 THEN 1 ELSE 0 END) AS GradeD
            FROM Students
            GROUP BY Course
            ORDER BY Course
            """;

        using var cmd = new SqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            reports.Add(new CourseReport
            {
                CourseName = reader.GetString(0),
                TotalStudents = reader.GetInt32(1),
                AverageMarks = Math.Round(reader.GetDouble(2), 2),
                GradeDistribution = new GradeDistribution
                {
                    A = reader.GetInt32(3),
                    B = reader.GetInt32(4),
                    C = reader.GetInt32(5),
                    D = reader.GetInt32(6)
                }
            });
        }

        // Return the course-wise report
        return Ok(reports);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand("DELETE FROM Students WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows == 0 ? NotFound() : NoContent();
    }
}
