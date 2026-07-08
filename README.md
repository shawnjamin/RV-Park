# RV-Park

Team members:
- Shawn Allen
- Andrew Guerrero
- Brady Adams
- Joseph West
- Jin Starks
- Allen Abraham
- Dax Kelson

## Database setup

This project uses Entity Framework Core with SQLite. By default, the app stores
the local database at:

```text
App_Data/rvpark.db
```

`App_Data/rvpark.db` is already ignored by `.gitignore` through the `App_Data/*`
rule, so the generated SQLite database file should not be committed.

### Local setup

From the repo root, run:

```bash
dotnet restore
dotnet ef database update
dotnet run
```

If `dotnet ef` is not installed, install it with:

```bash
dotnet tool install --global dotnet-ef --version 10.0.9
```

If it is already installed but needs to match the project version, update it with:

```bash
dotnet tool update --global dotnet-ef --version 10.0.9
```

### Migrations

When EF models or `ApplicationDbContext` change, create a migration with:

```bash
dotnet ef migrations add DescriptiveMigrationName
```

Commit only the generated migration files:

```text
Migrations/<timestamp>_DescriptiveMigrationName.cs
Migrations/<timestamp>_DescriptiveMigrationName.Designer.cs
Migrations/ApplicationDbContextModelSnapshot.cs
```

Before committing, check the working tree:

```bash
git status --short
```

Do not commit generated local files such as:

```text
App_Data/rvpark.db
bin/
obj/
.env
```

### Hosted application

The hosted application uses SQLite in production and runs migrations on startup.
Set these environment variables for the hosted app:

```bash
ConnectionStrings__DefaultConnection='Data Source=/absolute/path/to/rvpark.db'
Database__MigrateOnStartup=true
```

For local development, `Database__MigrateOnStartup` does not need to be set if
developers are running `dotnet ef database update` manually.

### Test data seeding

The app includes an idempotent test data seeder for local development. It seeds
site types, sites, users, customers, employees, reservations, bills, and
payments. The seeder only creates missing seed records and does not overwrite
existing rows.

By default, local development runs the seeder on startup through
`appsettings.Development.json`:

```json
"Database": {
  "SeedOnStartup": true
}
```

You can also run the seeder once and exit with:

```bash
dotnet run -- --seed
```

The database schema must already be up to date before seeding. Run migrations
first with:

```bash
dotnet ef database update
```
