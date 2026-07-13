# MSW Unauthorized Recovery Integration Test

This test runs an end-to-end flow for watchdog token recovery:

1. Starts a fake game API server (`IsOnline` + `RequestToken`).
2. Starts `MSW` in a temporary working directory with a custom `MSW_config.json`.
3. Calls `MSW` REST endpoint `UpdateState` to start a fake simulation process.
4. Fake simulation receives an initial (invalid) token over pipe and calls `IsOnline`.
5. Fake API returns `401`, simulation notifies `MSW` via `ReportUnauthorized`.
6. `MSW` renews token immediately and sends updated token over pipe.
7. Fake simulation retries `IsOnline` and succeeds.

The test fails on timeout if any critical step does not happen.

## Run

From repository root:

```powershell
dotnet run --project tests\MswUnauthorizedRecoveryTest\MswUnauthorizedRecoveryTest.csproj
```

Artifacts are written to:

- `tests/_artifacts/MswUnauthorizedRecovery/<timestamp>/`

Useful files:

- `msw-workdir/MSWdata/MSW_config.json` (generated config used by MSW)
- `fake-sim-out/tokens.log` (tokens received by fake simulation)
- `fake-sim-out/api-calls.log` (`IsOnline` success/401 records)

