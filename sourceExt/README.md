# Dgt Angel Chrome Extension Root

[![CodeQL JS](https://github.com/Hyper-Dragon/DgtAngel/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/Hyper-Dragon/DgtAngel/actions/workflows/codeql-analysis.yml)

## Install

### Current Release

The current release will be ~~is~~ available from the Chrome Store.

### Development Install

- Open Chrome
- From the menu open More Tools-->Extensions
- Click 'Load Unpacked'
- Select the */sourceExt/DtgAngel* folder

## Adding Support For Pages

### The Page Reader

To support additional pages you need to create a .js file in the *src/js/inject/* folder and implement 2 methods:

- function IsUrlValid(urlToTest) {}
- function GetRemoteBoardState() {}

The first should return true or false if the supplied URL parameter is supported by the javascript.

The second returns the current board state. Call *getDefaultRemoteBoard()* and populate the values.  Supported constants can be found in *messages.js*.

To run the new code unpdate the manifest (see below), load the changes into Chrome and navigete to the supported URL.

### The Manifest

To enable additional page support add a new matches section and an associated *src/js/inject/**NAME**Board.js* file. You should include the other .js files in the order below.

```json
"content_scripts": [
        {
            "matches": [
                "https://www.chess.com/game/live/*",
                "https://www.chess.com/play/online/*"
            ],
            "js": [
                "src/js/shared/globals.js",
                "src/js/shared/chessHelpers.js",
                "src/js/shared/messages.js",
                "src/js/shared/globalHelpers.js",
                "src/js/inject/clientConnection.js",
                "src/js/inject/cdcPlayBoard.js",
                "src/js/inject/angelWrapper.js"
            ]
        },
        {
            "matches": ["https://www.chess.com/live/*"],
            "js": [
                "src/js/shared/globals.js",
                "src/js/shared/chessHelpers.js",
                "src/js/shared/messages.js",
                "src/js/shared/globalHelpers.js",
                "src/js/inject/clientConnection.js",
                "src/js/inject/cdcLiveBoard.js",
                "src/js/inject/angelWrapper.js"
            ]
        }
    ]
```

## Code QL Config

Default action template config changes (Run on extension changes only/Javascript only).

```yaml
on:
  push:
    branches: [ main ]
    paths:
      - 'DgtAngel/sourceExt/DgtAngel/'
  pull_request:
    # The branches below must be a subset of the branches above
    branches: [ main ]
```

```yaml
    strategy:
      fail-fast: false
      matrix:
        language: [ 'javascript' ]
```

