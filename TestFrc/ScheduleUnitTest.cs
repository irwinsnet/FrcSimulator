
using frc;
using Xunit.Abstractions;
using System.Text.Json;

namespace TestFrc;

public class ScheduleUnitTests
{
    private readonly ITestOutputHelper output;

    public ScheduleUnitTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    public void Write2DArray(int[,] array)
    {
        int[] team_turns = new int[array.GetLength(1)];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                team_turns[j] = array[i, j];
            }
            output.WriteLine($"Row {i}: [{string.Join(", ", team_turns)}]");
        }
    }


    [Fact]
    public void TestScheduleBuild()
    {
        // Act
        Schedule sched = new(30, 12);

        // Assert
        Assert.Equal(60, sched.numMatches);
    }

    [Fact]
    public void TestFlatIndex()
    {
        //Assert
        // tuple elements are (flat-index, match, pos)
        List<(int, int, int)> datalist = new List<(int, int, int)>
        {
            (0, 0, 0),
            (1, 0, 1),
            (5, 0, 5),
            (6, 1, 0),
            (11, 1, 5),
            (62, 10, 2)
        };
        foreach (var row in datalist)
        {
            Assert.Equal(row.Item1, Schedule.ToFlatIndex(row.Item2, row.Item3));
            Assert.Equal((row.Item2, row.Item3), Schedule.ToMatchAndPos(row.Item1));
        }
    }

    [Fact]
    public void TestCounts()
    {
        // Arrange
        Schedule sched = new(30, 12);
        //Act
        int[] counts = sched.MatchesByTeam;
        Assert.Equal(sched.numTeams, counts.Length);
        foreach (int match_count in counts)
        {
            Assert.Equal(12, match_count);
        }
    }

    [Fact]
    public void TestTurnarounds()
    {
        // Arrange
        Schedule sched = new(30, 12);
        // Act
        int[,] turnarounds = sched.GetTurnarounds();

        // Assert
        Assert.Equal(30, turnarounds.GetLength(0));
        Assert.Equal(12, turnarounds.GetLength(1));
        int[] expected_row = { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, -1 };
        int[] actual_row = new int[12];
        for (int i  = 0; i < 30; i++)
        {
            for (int j = 0; j < 12; j++)
            {
                actual_row[j] = turnarounds[i, j];
            }
            Assert.Equal(expected_row, actual_row);
        }

        sched.Shuffle();
        int row_sum;
        for (int i = 0; i < 30; i++)
        {
            row_sum = 0;
            for (int j = 0; j < 12; j++)
            {
                if (j == 11)
                {
                    Assert.Equal(-1, turnarounds[i, j]);
                } else
                {
                    row_sum += turnarounds[i, j];
                    Assert.InRange<int>(turnarounds[i, j], 0, 59);
                }
                Assert.InRange<int>(row_sum, 0, 59);
            }
        }
    }

    [Fact]
    public void TestTurnaroundCosts()
    {
        Schedule sched = new(30, 12);
        // Act
        int ta_cost = sched.GetTurnaroundCost();
        Assert.Equal(0, ta_cost);
    }
}