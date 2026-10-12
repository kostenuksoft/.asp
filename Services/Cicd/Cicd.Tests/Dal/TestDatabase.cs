namespace Cicd.Tests.Dal;

public static class TestDatabase
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__CicdDb")
        ?? @"Server=(localdb)\MSSQLLocalDB;Database=DevHub_Cicd;Integrated Security=true;TrustServerCertificate=true";
}
