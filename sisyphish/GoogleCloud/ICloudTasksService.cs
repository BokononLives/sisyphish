using sisyphish.Discord.Models;

namespace sisyphish.GoogleCloud;

public interface ICloudTasksService
{
    Task CreateHttpPostTask(string url, DiscordInteraction body);
}