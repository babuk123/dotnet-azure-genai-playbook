namespace Interview;

// #8 LINQ on a student/employee list (group, aggregate, top N)  [Intermediate]
// Problem : Given a list of students (Name, Class, Marks): (1) average marks per class, (2) top 3 students overall, (3) students above their class average.
// Approach: GroupBy + Average; OrderByDescending + Take; join each student to their group average.
// Complexity: Grouping O(n); top 3 with sorting O(n log n).

public record Student(string Name, string Class, int Marks);

public static class StudentQueries
{
    public static Dictionary<string, double> AveragePerClass(IEnumerable<Student> s) =>
        s.GroupBy(x => x.Class)
         .ToDictionary(g => g.Key, g => g.Average(x => x.Marks));

    public static List<Student> Top3(IEnumerable<Student> s) =>
        s.OrderByDescending(x => x.Marks).ThenBy(x => x.Name).Take(3).ToList();

    public static List<Student> AboveClassAverage(IEnumerable<Student> s) =>
        s.GroupBy(x => x.Class)
         .SelectMany(g =>
         {
             double avg = g.Average(x => x.Marks);
             return g.Where(x => x.Marks > avg);
         })
         .ToList();
}
