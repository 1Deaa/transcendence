# Disaster Recovery Runbook

This runbook covers the automated SQL Server backup pipeline of HrmSystem and the
restore procedure used for disaster recovery drills.

## 1. What runs automatically

| Piece | Detail |
| --- | --- |
| Job | `DatabaseBackupJob` (Quartz.NET), scheduled by `BackupCron` — default `0 0 2 * * ?` (02:00 UTC, daily) |
| Command | `BACKUP DATABASE [HrmSystem] TO DISK = '/var/backups/<name>.bak' WITH COMPRESSION, CHECKSUM, INIT` — executed **server-side** by SQL Server |
| Output | Compressed, checksummed `.bak` in the shared Docker volume `sqlserver_backups` (mounted at `/var/backups` in both the SQL Server and API containers) |
| History | Every run is recorded in the `BackupHistory` table (`GET /api/backups`, newest first) |
| Retention | `DatabaseBackupJob` retention job (`RetentionCron`, default `0 30 3 * * ?`): 7 daily + 4 weekly backups, older files and history rows are pruned |
| On-demand | `POST /api/backups/run` (permission `backups:manage` — platform admin) triggers an immediate run |
| Monitoring | The public status page (`/status`) exposes a `backup-freshness` component: Degraded after 26 h without a successful backup, Unhealthy after 50 h |

> **Volume ownership:** a fresh named volume is root-owned; the one-shot
> `hrmsystem.backup-perms` service in `docker-compose.yml` chowns it to uid 10001
> (the SQL Server user) before SQL Server starts. On a fresh machine, run one backup
> after startup (`POST /api/backups/run` as a platform admin) so the status page
> reports Healthy immediately.

## 2. Restore procedure (disaster recovery)

All commands run from the repository root (Docker Compose deployment).

1. **Freeze the API** (avoids writes and connection noise during the restore):
   `docker compose stop hrmsystem.api`
2. **List available backups** (newest first):
   `docker exec hrmsystem.sqlserver bash -c "ls -lt /var/backups"`
   Alternatively inspect the history API/table for the exact file that succeeded.
3. **Inspect the backup's file list** (paths inside the backup):
   ```bash
   docker exec -it hrmsystem.sqlserver /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
     -Q "RESTORE FILELISTONLY FROM DISK = '/var/backups/<file>.bak'"
   ```
4. **Restore the database over the current one** (same instance, so original paths are kept):
   ```bash
   docker exec -it hrmsystem.sqlserver /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
     -Q "RESTORE DATABASE [HrmSystem] FROM DISK = '/var/backups/<file>.bak' WITH REPLACE, RECOVERY"
   ```
   For a side-by-side drill restore instead, use `WITH MOVE` clauses and a different
   database name (e.g. `HrmSystem_Drill`) — recommended for quarterly drills so
   production data is never overwritten.
5. **Verify the restore:**
   - `curl http://localhost:5000/health/ready` → Healthy
   - Log in with a seeded account and spot-check the employee directory
   - `GET /api/backups` → history loads (the restored DB contains its own history)
6. **Unfreeze the API:** `docker compose start hrmsystem.api`

## 3. Drill checklist (quarterly, or before evaluation)

- [ ] Run one on-demand backup, confirm `status = Succeeded` in `GET /api/backups`
- [ ] Confirm a `.bak` file appears in the volume (`docker exec hrmsystem.api ls -la /var/backups`)
- [ ] Perform a **drill restore** into a scratch database (`RESTORE ... WITH MOVE, RECOVERY`)
- [ ] Sanity-check row counts in the scratch database (`SELECT COUNT(*) FROM Employees`)
- [ ] Drop the scratch database
- [ ] Confirm `/api/status` still reports the `backup-freshness` component as Healthy
- [ ] Record the drill date + outcome in this file

## 4. Known limitations

- Backups live in a Docker volume on the same host — for production, replicate the
  volume to off-host storage (out of scope for the school deployment).
- The checksum option (`WITH CHECKSUM`) makes silent page corruption fail the backup
  rather than the restore; a failed history row is the signal to investigate before it
  matters.
