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

### Prerequisites

[This page](https://pumpingco.de/blog/setup-your-raspberry-pi-for-docker-and-docker-compose/) has some instructions for installing docker and docker-compose on Pi.

I've made a Pi-specific docker-compose file for the project: docker-compose-pi.yml.

There is currently an issue with running the dotnet-sdk image on Raspbian buster due to an old version of libseccomp2. To fix this, I've pulled libseccomp2 from testing by doing the following things:

* Add this entry to `/etc/apt/sources.list`

```sources.list
deb http://raspbian.raspberrypi.org/raspbian/ testing main
```

* To stop the whole system from trying to update itself to testing, create a suitable `/etc/apt/preferences` file. You can figure out the exact thing to pin by running `sudo apt update; apt-cache policy` after adding the `sources.list` entry. Mine looks like this:

```preferences
Package: *
Pin: release o=Raspbian,a=testing,n=bookworm,l=Raspbian,c=main,b=armhf
Pin-Priority: 101
```

* Verify those preferences have taken effect by running `apt-cache policy` again. The `testing` row should show priority 101, not the default 500.
* Install the testing libseccomp2 (but not anything else from testing!) by running

```bash
sudo apt install libseccomp2/testing
```

* Verify that the dotnet-sdk image works properly now with

```bash
docker run -it --rm mcr.microsoft.com/dotnet/sdk:6.0
dotnet --version
```

* The version number should be printed. Ctrl-D to exit the container.

### Building and running

Clone the Git repository from the Pi and from its root, these commands should build and start up (with autostart!) the system:

```bash
docker-compose -f docker-compose-pi.yml build
docker-compose -f docker-compose-pi.yml up -d 
```

The docker-compose-pi.yml is set up to autostart, etc.

## Other notes

Windows Firewall blocks ICMPv4 ping by default! It's quite easily enabled however, e.g. via these instructions: [How to allow Pings](https://www.thewindowsclub.com/how-to-allow-pings-icmp-echo-requests-through-windows-firewall)
