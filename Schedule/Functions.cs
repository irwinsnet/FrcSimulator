using System.Linq;
using static System.Console;
using System.Text.Json;

namespace frc;

/// <summary>
/// Randomly generates an FRC match schedule
/// </summary>
public class Schedule
{

    public readonly int numTeams;
    public readonly int matchesPerTeam;
    public readonly int numMatches;
    private int[,] schedule;
    private Random rand;

    /// <summary>
    /// Create a Schedule for a specific number of teams.
    /// </summary>
    /// <param name="num_teams">Number of teams at the competition</param>
    /// <param name="matches_per_team">Miniumum number of matches played by each team.</param>
    public Schedule(int num_teams, int matches_per_team = 12)
    {
        numTeams = num_teams;
        matchesPerTeam = matches_per_team;
        rand = new Random();

        double fractional_matches = (double)num_teams * matches_per_team / 6;
        numMatches = (int)Math.Ceiling(fractional_matches);
        schedule = new int[numMatches, 6];
        Build();
    }

    /// <summary>
    /// Number of elements in schedule array, should be numMatches * 6.
    /// </summary>
    public int ArrayCount { get { return schedule.Length; } }

    /// <summary>
    /// Array with number of matches played by each team.
    /// </summary>
    public int[] MatchesByTeam
    {
        get
        {
            var countsQuery =
                from int team in schedule
                group team by team into grp
                select grp.Count();
            return countsQuery.ToArray();
        }
    }

    /// <summary>
    /// Builds the base schedule.
    /// </summary>
    private void Build()
    {
        int current_team = 0;
        for (int i = 0; i < numMatches; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                schedule[i, j] = current_team;
                if (current_team >= numTeams - 1)
                {
                    current_team = 0;
                }
                else
                {
                    current_team++;
                }
            }
        }
    }

    /// <summary>
    /// Randomly shuffles the teams in the schedule.
    /// </summary>
    public void Shuffle()
    {
        for (int match_pos = 0; match_pos < ArrayCount; match_pos++)
        {
            (int cur_match, int cur_pos) = ToMatchAndPos(match_pos);
            int switch_index = rand.Next(match_pos, ArrayCount);
            (int switch_match, int switch_pos) = ToMatchAndPos(switch_index);

            (schedule[cur_match, cur_pos], schedule[switch_match, switch_pos]) =
                (schedule[switch_match, switch_pos], schedule[cur_match, cur_pos]);
        }
    }


    /// <summary>
    /// Converts match number and team position to a flat index
    /// </summary>
    /// <param name="match">Match number, starting at zero</param>
    /// <param name="pos">Team position, starting at zero (0-2: Blue, 3-5: Red)</param>
    /// <returns>Flattened index</returns>
    public static int ToFlatIndex(int match, int pos)
    {
        return 6 * match + pos;
    }

    /// <summary>
    /// Converts flattened index to match and position numbers.
    /// </summary>
    /// <param name="index"></param>
    /// <returns>Tuple of match and position</returns>
    public static (int, int) ToMatchAndPos(int index)
    {
        int match = index / 6;
        int pos = index % 6;
        return (match, pos);
    }

    /// <summary>
    /// Given two flat indices, swaps two teams on the match schedule.
    /// </summary>
    /// <param name="flat_index1"></param>
    /// <param name="flat_index2"></param>
    private void FlatSwap(int flat_index1, int flat_index2)
    {
        (int match1, int pos1) = ToMatchAndPos(flat_index1);
        (int match2, int pos2) = ToMatchAndPos(flat_index2);
        (schedule[match1, pos1], schedule[match2, pos2]) =
            (schedule[match2, pos2], schedule[match1, pos1]);
    }

    /// <summary>
    /// Randomly generates two flat indices, ensuring both indices are different.
    /// </summary>
    /// <returns>A Tuple of two flat indices.</returns>
    private (int, int) RandomSwapIndexes()
    {
        int index1 = rand.Next(ArrayCount);
        int index2;
        do
        {
            index2 = rand.Next(ArrayCount);
        } while (index1 == index2);

        return (index1, index2);
    }

    /// <summary>
    /// Count of times that the same team is assigned to a match more than once.
    /// </summary>
    /// <remarks>
    /// One duplicate occurs when two positions in a match, (i.e., red1, blue2)
    /// are filled by the same team. If the same team fills three positions,
    /// that counts as two duplicates, four positions counts as three
    /// duplicates, and so on.
    /// </remarks>
    /// <returns>1D array of duplicates for each match.</returns>
    public int[] GetMatchDuplicates()
    {
        int[] match_duplicates = new int[numMatches]; 
        for (int match = 0; match < numMatches; match++)
        {
            int[] match_teams = GetMatch(match);
            var match_set = new HashSet<int>(match_teams);
            match_duplicates[match] = 6 - match_set.Count;
        }
        return match_duplicates;
    }

    /// <summary>
    /// Number of matches until a team's next scheduled match.
    /// </summary>
    /// <remarks>
    /// If a team is assigned to play in match 4 and their next match is
    /// match 8, the team has a 4-match turnaround. If their next match was
    /// match 5, that would be a 1-match turnaround. A zero-match turnaround
    /// would indicate that the team was assigned more than once to the same
    /// match.
    /// 
    /// Depending on matchesPerTeam, teams may play different numbers of matches.
    /// If a team did not play the maximum possible number of matches, the turnaorund
    /// vector will be padded at the end with -1, i.e., a turnaround value of -1
    /// indicates NULL or no data. In other words, for schedules that use filler matches,
    /// teams that did not play any filler matches will have a -1 for their final
    /// turnaround.
    /// </remarks>
    /// <returns>
    /// All turnarounds for all teams and matches, as a two-dimensional array.
    /// Dimension is numTeams x (matchesPerTeam). First index is team number
    /// and second index is turnaround number. A Zero indicates no data because
    /// the team did no
    /// 
    /// </returns>
    public int[,] GetTurnarounds()
    {
        // List of assigned match numbers for each team.
        List<int>[] matches = new List<int>[numTeams];
        for (int i = 0; i < numTeams; i++)
        {
            matches[i] = new List<int>();
        }
        for (int match_number = 0; match_number < numMatches; match_number++)
        {
            for (int team_pos = 0; team_pos < 6; team_pos++)
            {
                int team = schedule[match_number, team_pos];
                matches[team].Add(match_number);
            }
        }
        int[,] turnarounds = new int[numTeams, matchesPerTeam];
        for (int team = 0; team < numTeams; team++)
        {
            for (int i = 0; i < matchesPerTeam; i++)
            {
                turnarounds[team, i] = -1;
            }
        }
        for (int team = 0;team < numTeams; team++)
        {
            for (int match = 0; match < (matches[team].Count - 1); match++)
            {
                turnarounds[team, match] = matches[team][match + 1] - matches[team][match];
            }
        }
        return turnarounds;
    }


    /// <summary>
    /// Retrieves allies and opponents for all teams from match schedule.
    /// </summary>
    /// <returns>Tuple of allies and opponents</returns>
    public (HashSet<int>[], HashSet<int>[]) GetAlliesAndOpponents()
    {
        // Results arrays
        HashSet<int>[] allies = new HashSet<int>[numTeams];
        HashSet<int>[] opponents = new HashSet<int>[numTeams];
        for (int i = 0; i < numTeams; i++)
        {
            allies[i] = new HashSet<int>();
            opponents[i] = new HashSet<int>();
        }
        // Table of team positions for a team's allies. The row index is a team's position
        // and the values are the positions of the team's allies.
        int[,] ally_pos_map =
        {
            {1, 2}, {0, 2}, {0, 1}, {4, 5}, {3, 5}, {3, 4}
        };
        // Table of team positions for a team's opponents. The row index is a team's
        // position and the values are positions of the team's opponents.
        int[,] opponent_pos_map =
        {
            {3, 4, 5}, {3, 4, 5 }, {3, 4, 5}, {0, 1, 2}, {0, 1, 2}, {0, 1, 2}
        };

        int current_team;
        int ally_pos;
        int opponent_pos;
        for (int match_number = 0; match_number < numMatches; match_number++)
        {
            for (int team_pos = 0; team_pos < 6; team_pos++)
            {
                current_team = schedule[match_number, team_pos];
                // Get team's allies
                for (int i = 0; i < 2; i++)
                {
                    ally_pos = ally_pos_map[team_pos, i];
                    allies[current_team].Add(schedule[match_number, ally_pos]);
                }
                // Get team's opponents
                for (int i = 0; i < 3; i++)
                {
                    opponent_pos = opponent_pos_map[team_pos, i];
                    opponents[current_team].Add(schedule[match_number, opponent_pos]);
                }
            }
        }
        return (allies, opponents);
    }

    public (int, int) GetAllyOpponentCost()
    {
        int ally_cost = 0;
        int opponent_cost = 0;
        int max_allies = Math.Min(2 * matchesPerTeam, numTeams);
        int max_opponents = Math.Min(3 * matchesPerTeam, numTeams);
        (HashSet<int>[] allies, HashSet<int>[] opponents) = GetAlliesAndOpponents();

        int num_allies;
        int num_opponents;
        for (int team  = 0; team < numTeams; team++)
        {
            num_allies = allies[team].Count;
            ally_cost += (int)Math.Pow(2, max_allies - num_allies);
            num_opponents = opponents[team].Count;
            opponent_cost = (int)Math.Pow(10, max_opponents - num_opponents);
        }
        return (ally_cost, opponent_cost);

    }

    public void PrintAllies()
    {
        (HashSet<int>[] allies, HashSet<int>[] opponents) = GetAlliesAndOpponents();

        for (int team = 0; team < numTeams; team++)
        {
            Write($"Team {team}: {allies[team].Count} Allies: ");
            foreach (int ally in allies[team])
            {
                Write($"{ally} ");
            }
            WriteLine();
        }
        for (int team = 0; team < numTeams; team++)
        {
            Write($"Team {team}: {opponents[team].Count} Opponents: ");
            foreach (int oppo in opponents[team])
            {
                Write($"{oppo} ");
            }
            WriteLine();
        }
    }

    /// <summary>
    /// Calculate total cost for all match turnarounds
    /// </summary>
    /// <remarks>
    /// Optimal turnaround is total number of matches divided number of matches per
    /// team, truncating to integer.
    /// 
    /// Turnarounds equal to the optimal turnaround or one less than optimal have zero cost.
    /// 
    /// Cost for other turnarounds is 10 to the power of (optimal-turnaround - actual turnaround - 2).
    /// For example, with 60 matches and 12 matches per team, optimal turnaround is 5.
    /// A one match turnaround would have cost Math.Pow(10, 5 - 1 - 2) = 100.
    /// 
    /// Turnarounds are summed for all teams and matches.
    /// </remarks>
    /// <returns></returns>
    public int GetTurnaroundCost()
    {
        int optimal_turnaround = numMatches / matchesPerTeam;
        int[] turnaround_costs = new int[optimal_turnaround + 1];
        int total_cost = 0;
        for (int i = 0; i <= optimal_turnaround; i++)
        {
            if (i < optimal_turnaround - 2)
            {
                turnaround_costs[i] = (int)Math.Pow(10, optimal_turnaround - i - 1);
            } else
            {
                turnaround_costs[i] = 0;
            }
            turnaround_costs[2] = turnaround_costs[1];
        }
        int[,] turnarounds = GetTurnarounds();
        int turnaround;
        for (int team = 0; team < turnarounds.GetLength(0); team++)
        {
            for (int i = 0; i < turnarounds.GetLength(1); i++)
            {
                turnaround = turnarounds[team, i];
                if (turnaround <= optimal_turnaround && turnaround >= 0)
                {
                    total_cost += turnaround_costs[turnaround];
                }
                // Avoid long gaps between matches
                else if(turnaround >= 2 * optimal_turnaround)
                {
                    total_cost += turnaround_costs[0];
                }
            }
        }
        return total_cost;
    }

    public int GetCost()
    {
        int turnaround_cost = GetTurnaroundCost();
        (int ally_cost, int opponent_cost) = GetAllyOpponentCost();
        int total_cost = 1 * turnaround_cost +  1 * opponent_cost + 1 * ally_cost;
        return total_cost;
    }

    public void Optimize(int initial_temp, int max_steps)
    {
        int initial_cost = GetCost();
        int current_cost = initial_cost;
        int new_cost;
        double current_temp = initial_temp;
        double criterion;
        double cost_diff;
        for (int i = 0; i < max_steps; i++)
        {
            (int idx1, int idx2) = RandomSwapIndexes();
            FlatSwap(idx1, idx2);
            new_cost = GetCost();
            cost_diff = new_cost - current_cost;
            if (cost_diff > 0)
            {
                cost_diff = new_cost - current_cost;
                criterion = Math.Exp(-cost_diff/current_temp);
                double rand_dbl = rand.NextDouble();
                if (rand_dbl > criterion)
                {
                    FlatSwap(idx1, idx2);
                } else
                {
                    current_cost = new_cost;
                }
            }
            else
            {
                current_cost = new_cost;
                // WriteLine("Swapped!");
            }
            current_temp = current_temp / (i + 1);
        }
    }


    public static void Write2DArray(int[,] array)
    {
        int[] team_turns = new int[array.GetLength(1)];
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                team_turns[j] = array[i, j];
            }
            WriteLine($"Row {i}: [{string.Join(", ", team_turns)}]");
        }
    }




    /// <summary>
    /// Get a six-element array for a specific match.
    /// </summary>
    /// <param name="match">Match number, starting at zero</param>
    /// <returns>int[6]</returns>
    private int[] GetMatch(int match)
    {
        int[] teams = new int[6];
        for (int i = 0;i < 6; i++)
        {
            teams[i] = schedule[match, i];
        }
        return teams;
    }
    public void Print()
    {
        for (int i = 0; i < numMatches; i++)
        {
            WriteLine(JsonSerializer.Serialize(
                GetMatch(i)));
        }
        WriteLine($"Number of Matches: { numMatches}");
    }

}
