namespace Api.Tests;

[SetUpFixture]
public class TestSetup
{
    [OneTimeSetUp]
    public void DeleteTestDatabase()
    {
        var dbFile = Path.Combine(AppContext.BaseDirectory, "database", "contacthub_test.db");
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            if (File.Exists(dbFile + suffix))
            {
                File.Delete(dbFile + suffix);
            }
        }
    }
}
