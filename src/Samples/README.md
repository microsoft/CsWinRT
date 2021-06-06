# Note

The WinUIDesktopSample and WinUIDesktopSamplePackage contain repro scenarios for the memory leak we addressed around version 1.1.4.
These scenarios should be moved out of samples and into our unit test suite, so we can have automated verification.
Until we port ObjectLifetimeTests into our unit tests, these samples serve as a way to check we have not regressed any scenarios.