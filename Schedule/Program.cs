using static System.Console;
using System.Text.Json;

namespace frc;

class Program
{
    const int NUM_TEAMS = 50;
    const int MATCHES_PER_TEAM = 12;

    static void Main(string[] args)
    {
        WriteLine("Creating the Schedule");
        WriteLine($"Teams: {NUM_TEAMS}, Matches per Team: {MATCHES_PER_TEAM}");
        Schedule sched = new(NUM_TEAMS, MATCHES_PER_TEAM);
        sched.Shuffle();
        //// sched.Print();
        //WriteLine($"Initial Duplicates: {sched.GetMatchDuplicates().Sum()}");
        WriteLine($"Cost: {sched.GetCost()}");
        ////sched.Print();
        sched.Optimize(2000, 10000);
        sched.PrintAllies();
        //WriteLine($"Final Duplicates: {sched.GetMatchDuplicates().Sum()}");
        WriteLine($"Final Cost: {sched.GetCost()}");
        sched.Print();
        //Schedule.Write2DArray(sched.GetTurnarounds());
    }
}

