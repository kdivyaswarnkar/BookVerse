global using Xunit;

// The database tests share one test database, so we run test classes one after another.
// (Inside a single test, the race tests still start two requests at the same moment on purpose.)
[assembly: CollectionBehavior(DisableTestParallelization = true)]
