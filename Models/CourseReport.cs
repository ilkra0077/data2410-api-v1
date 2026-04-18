namespace data2410_api_v1.Models;

public class CourseReport
{
    public string CourseName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public double AverageMarks { get; set; }
    public GradeDistribution GradeDistribution { get; set; } = new();
}

public class GradeDistribution
{
    public int A { get; set; }
    public int B { get; set; }
    public int C { get; set; }
    public int D { get; set; }
}
