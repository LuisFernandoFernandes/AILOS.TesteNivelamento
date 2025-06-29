using Newtonsoft.Json;

public class Program
{
    public static async Task Main()
    {
        var service = new FootballApiService();

        await PrintTeamGoals(service, "Paris Saint-Germain", 2013);
        await PrintTeamGoals(service, "Chelsea", 2014);
    }

    private static async Task PrintTeamGoals(FootballApiService service, string team, int year)
    {
        int totalGoals = await service.GetTotalScoredGoalsAsync(team, year);
        Console.WriteLine($"Team {team} scored {totalGoals} goals in {year}");

        // Output expected:
        // Team Paris Saint - Germain scored 109 goals in 2013
        // Team Chelsea scored 92 goals in 2014
    }
}

public class FootballApiService
{
    private readonly HttpClient _client = new HttpClient();
    private const string BaseUrl = "https://jsonmock.hackerrank.com/api/football_matches";

    public async Task<int> GetTotalScoredGoalsAsync(string team, int year)
    {
        int goalsAsTeam1 = await GetGoalsAsync(year, team, "team1");
        int goalsAsTeam2 = await GetGoalsAsync(year, team, "team2");
        return goalsAsTeam1 + goalsAsTeam2;
    }

    private async Task<int> GetGoalsAsync(int year, string team, string teamParam)
    {
        int page = 1, totalPages = 1, totalGoals = 0;

        do
        {
            string url = $"{BaseUrl}?year={year}&{teamParam}={team}&page={page}";

            try
            {
                var response = await _client.GetStringAsync(url);
                var data = JsonConvert.DeserializeObject<ApiResponse>(response);

                foreach (var match in data.data)
                {
                    totalGoals += int.Parse(teamParam == "team1" ? match.team1goals : match.team2goals);
                }

                totalPages = data.total_pages;
                page++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching data: {ex.Message}");
                break;
            }

        } while (page <= totalPages);

        return totalGoals;
    }
}

public class Match
{
    public string team1 { get; set; }
    public string team2 { get; set; }
    public string team1goals { get; set; }
    public string team2goals { get; set; }
}

public class ApiResponse
{
    public int total { get; set; }
    public int total_pages { get; set; }
    public Match[] data { get; set; }
}
