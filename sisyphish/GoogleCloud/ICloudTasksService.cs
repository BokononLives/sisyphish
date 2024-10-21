namespace sisyphish.GoogleCloud;

public interface ICloudTasksService
{
    Task CreateHttpPostTask(string url, object body);
}