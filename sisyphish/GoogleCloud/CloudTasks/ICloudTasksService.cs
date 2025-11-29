using sisyphish.Discord.Models;

namespace sisyphish.GoogleCloud.CloudTasks;

public interface ICloudTasksService
{
    Task CreateHttpPostTask(string url, DiscordInteraction body);
}
