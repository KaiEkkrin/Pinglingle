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

## Build for, and run on, a Raspberry Pi

[This page](https://pumpingco.de/blog/setup-your-raspberry-pi-for-docker-and-docker-compose/) has some instructions for installing docker and docker-compose on Pi.

I've made a Pi-specific docker-compose file for the project: docker-compose-pi.yml.

Then, clone the Git repository from the Pi and from its root, these commands should build and start up (with autostart!) the system:

```bash
docker-compose -f docker-compose-pi.yml build
docker-compose -f docker-compose-pi.yml up -d 
```
