# Dgt Angel Chrome Extension Root

## Install

### Current Release

The current release is available from the Chrome store [TODO].

### Development Install

- Open Chrome
- blah
- blah
  
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