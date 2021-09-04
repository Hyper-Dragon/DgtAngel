# Dgt Angel Chrome Extension Root

## Install

### Current Release

The current release is available from the Chrome store [TODO].

### Development Install

- Open Chrome
- blah
- blah
  
## The Manifest

To enable additional page support add a new matches section and an associated *src/js/inject/**NAME**Board.js* file.

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

---
[![CodeQL JS](https://github.com/Hyper-Dragon/DgtAngel/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/Hyper-Dragon/DgtAngel/actions/workflows/codeql-analysis.yml)