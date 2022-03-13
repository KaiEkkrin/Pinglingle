# Pinglingle

A utility to help me track the quality of my network. (Well, it will be. Right now it's still mostly a Blazor project template.)

## Managing EF Core migrations

Pinglingle is set to use the in-memory database unless you configure the ConnectionString environment variable or command line setting, at which point it uses PostgreSQL.

To set up migrations you need the PostgreSQL driver to load, so:

* Start a Docker container containing a PostgreSQL

```powershell
docker run -d --name pingdev -p 5432:5432 -e POSTGRES_PASSWORD=38923673 -e POSTGRES_DB=pinglingle postgres
```

* Run a `dotnet ef` command with a matching connection string e.g.

```powershell
cd Server
dotnet ef migrations list -- --ConnectionString="Host=localhost;Username=postgres;Password=38923673;Port=5432;Database=pinglingle"
```
