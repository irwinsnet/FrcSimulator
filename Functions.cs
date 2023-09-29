using System;
using static System.Console;

partial class Program
{
    static void CreateSchedule(int num_teams, int matches_per_team = 12)
    {
        WriteLine("Creating the Schedule");
        WriteLine($"Teams: {num_teams}, Matches per Team: {matches_per_team}")
    }
}
