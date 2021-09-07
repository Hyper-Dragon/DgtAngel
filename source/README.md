# Dgt Angel Client (Cherub) Root

[![Cherub Build](https://github.com/Hyper-Dragon/DgtAngel/actions/workflows/BuildCherubOnMain.yml/badge.svg?branch=main)](https://github.com/Hyper-Dragon/DgtAngel/actions/workflows/BuildCherubOnMain.yml)

## Install

### Current Release

The current release is available from the [releases page](https://github.com/Hyper-Dragon/DgtAngel/releases).
  
## Github Build Config

Default action template modifications.

```yaml
name: Cherub Build

on:
  push:
    branches: [ main ]
    paths:
      - 'DgtAngel/source/'
  pull_request:
    branches: [ main ]

jobs:
  build:

    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '6.0.x'
        include-prerelease: true
    - name: Restore dependencies
      run: dotnet restore ./source/DgtCherub/DgtCherub.csproj
    - name: Build
      run: dotnet build --no-restore ./source/DgtCherub/DgtCherub.csproj
    - name: Test
      run: dotnet test --verbosity normal ./source/DynamicBoardTest/DynamicBoardTest.csproj
```

