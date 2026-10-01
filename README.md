# Sudoku-IObservable

Sudoku on a grid that propagates its own constraints with `IObservable<T>` (Rx.NET). See [GLOSSARY.md](GLOSSARY.md) for the domain language and [docs/adr/](docs/adr/) for the decisions behind it.

## Structure

| Project | Purpose |
| --- | --- |
| `src/SudokuObservable.AppHost` | Aspire AppHost: orchestrates Api and Web |
| `src/SudokuObservable.ServiceDefaults` | Aspire service defaults (telemetry, health, service discovery, resilience) |
| `src/SudokuObservable.Core` | The domain: games, grids and reactive constraint propagation |
| `src/SudokuObservable.Api` | REST API (Minimal APIs, OpenAPI, Scalar) |
| `src/SudokuObservable.Web` | Blazor Web App (Interactive Server), talks to the Api |
| `tests/SudokuObservable.Core.Tests` | xUnit v3 tests for Core |
| `tests/SudokuObservable.Api.Tests` | xUnit v3 tests for the Api, over HTTP and on its game store |
| `tests/SudokuObservable.Web.Tests` | xUnit v3 tests for the Web's API client and display mapping, and that its enums match Core's |

## Running

Requires the .NET 10 SDK and the Aspire CLI (`dotnet tool install -g Aspire.Cli`).

```sh
aspire run
```

The Aspire dashboard links to Api (Scalar UI at `/scalar`) and Web.

## Testing

```sh
dotnet test
```
