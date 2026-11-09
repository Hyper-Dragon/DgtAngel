# DgtAngel native shim deployment

`DgtAngelNative.dll` is a drop-in replacement for DGT's `DGTEBDLL.dll`. It speaks
the same 42 exports but forwards every call over gRPC to DgtCherub, which owns
the real driver, so any DGT-aware client can share one physical board.

Build output is renamed to `DGTEBDLL.dll` when deployed.

## Where it has to live

DGT clients do **not** all load the driver the same way, so we cannot rely on a
per-application copy:

| Client | Resolves `DGTEBDLL.dll` from | Notes |
| --- | --- | --- |
| Arena 3.5.1 | its own folder | honours the normal search order |
| Fritz 14 / ChessBase | `System32` **only** | ignores its own folder even though the application directory is searched first |

Because of Fritz, the shim has to be installed machine-wide. Bitness splits the
path, because WOW64 redirects a 32-bit process's `System32`:

| Client bitness | Install to |
| --- | --- |
| 32-bit | `C:\Windows\SysWOW64\DGTEBDLL.dll` |
| 64-bit | `C:\Windows\System32\DGTEBDLL.dll` |

Note that DGT's own installer drops an **x86** copy in the real `System32`, which
no 64-bit process can load — so 64-bit DGT support is broken on a stock install
even before we get involved.

## Build

```
# x86 (32-bit clients)
MSBuild DgtAngelNative.vcxproj /p:Configuration=Release /p:Platform=Win32 `
        /p:PlatformToolset=v145 /p:VcpkgTriplet=x86-windows-static

# x64 (64-bit clients)
MSBuild DgtAngelNative.vcxproj /p:Configuration=Release /p:Platform=x64 `
        /p:PlatformToolset=v145 /p:VcpkgTriplet=x64-windows-static
```

Release links the **static** vcpkg triplets so each build is a single
self-contained DLL (system imports only). Two things trip people up:

- **`/p:VcpkgTriplet` is required.** vcpkg's MSBuild integration chooses the
  triplet; the project's own `LibraryPath` does not decide what gets linked.
  Without it you silently link the dynamic tree and get
  `LNK2038: mismatch detected for 'RuntimeLibrary'`.
- **vcpkg 2023-03-29 cannot see VS 2026.** It derives the toolset name from
  `VC\Auxiliary\Build\Microsoft.VCToolsVersion.default.txt`; anything newer than
  MSVC 14.3x is unrecognised and it reports "Unable to find a valid Visual
  Studio instance". Installing VS 2022 Build Tools is not enough on its own
  (17.14 ships 14.44). The working fix is a directory junction of the real
  toolset under an older number plus pointing that `.txt` at it — see
  `alias-vs-toolset-for-vcpkg.ps1` history. Do **not** upgrade vcpkg to fix
  this: it invalidates the existing `x86-windows`/`x64-windows` trees and moves
  gRPC/protobuf, which `dllmain.cpp` and the checked-in `.pb.cc` files are
  pinned to.

## Things that must stay true

**The ChessBase validation exports.** Fritz calls
`_DGTDLL_ChessBase_131e2d0711b4d50e` and will not run the board without it. In
the genuine DLL the two "validation" entry points are one-instruction constants:

```
_DGTDLL_ChessBase_131e2d0711b4d50e   mov eax,1B037FB4h ; ret 18h
_DGTDLL_WoodIn3_9c7b9ce70ec6a882     mov eax,2739B8EAh ; ret 14h
```

The `ret` immediates matter — the parameter counts in `dllmain.cpp` exist only to
produce the matching `@24` / `@20` stack cleanup.

**Callback order and completeness.** The real driver fires every callback
synchronously from one thread. The shim has one gRPC stream per callback, so a
per-channel "newest value wins" mailbox reorders callbacks against each other and
drops the ones the host has not pumped yet. That was the cause of Fritz showing
a piece lifted but never landing. Events are queued in arrival order and replayed
one at a time on the host's thread — keep it that way.

**Driver configuration is global.** `UseSAN`, `UseFEN`, `SetGameType` and
`SetAutoRotation` configure a single shared driver instance, so whatever Cherub
sets leaks to every proxied client. Cherub asks for SAN for its own display and
that made Fritz receive `e4` when it expects the driver default (`Pe2Pe4`) and
silently reject every move. Only the driver's defaults are safe.

## Fritz needs a `NewGame`

Fritz does not accept board moves until a game exists. The driver emits
`NewGame` when the board is restored to the starting position, so with Fritz sat
on its "New game" screen a lift shows but the placement is discarded. Restoring
the pieces (or pressing the DGT clock) starts the game; after that moves and
clock times both flow.

## Deploy

Run from an **elevated** PowerShell, with Cherub and any DGT client closed:

```powershell
.\install-dgt-shim.ps1
.\uninstall-dgt-shim.ps1
```

Both back up / restore whatever was in each location, and the installer removes
the runtime DLLs that only the old dynamic build needed.
