# bt-board - a Bluetooth "board" for Chessconnect to pair with

A throwaway probe. It registers a **Serial Port Profile** server on the host's
existing BlueZ, so a Windows machine can pair with this box, get a
`Standard Serial over Bluetooth link (COMx)` port, and hand it to Chessconnect as
if it were a Millennium e-board.

The point is to find out what Chessconnect actually does once it believes a board
is attached: which handshake lines it sends, when it asks for positions, and what
it pushes back down the wire (LED patterns, clock commands, sound cues). Everything
is logged.

## It does not touch the Neewer container

This box's adapter is already carrying a BLE connection to the light strip. The
probe is deliberately additive:

- it never writes `Adapter1.Powered`
- it never restarts `bluetoothd`, resets the controller, or runs `btmgmt`
- it registers a `Profile1` and an `Agent1`, and toggles `Pairable` /
  `Discoverable` while recording **and restoring** their previous values on exit
- a second RFCOMM profile alongside a live BLE link is ordinary traffic; no
  existing connection is disturbed

Set `MAKE_DISCOVERABLE=0` if you want it to touch even less, and
`BE_DEFAULT_AGENT=0` if another container on this box owns the adapter's default
pairing agent — BlueZ cannot restore the previous holder, so the probe takes it
only because pairing needs *an* agent and `bleak`-style containers rarely register
one.

## Run it

```sh
docker run --rm --network host \
  -v /run/dbus/system_bus_socket:/run/dbus/system_bus_socket \
  -e DBUS_SYSTEM_BUS_ADDRESS=unix:path=/run/dbus/system_bus_socket \
  --name bt-board \
  ghcr.io/hyper-dragon/dgtangel/bt-board:latest
```

`--network host` is not required for the radio but keeps things simple; the mount
and the env var are what let the container drive the host's `bluetoothd`.

## Use it

1. On Windows: **Settings → Bluetooth & devices → Add device → Bluetooth**, pick
   this machine. Pairing is Just Works - no code to type.
2. If no COM port appears: **More Bluetooth options → COM ports → Add → Outgoing**,
   and choose the paired device. Windows does not always create it automatically.
3. In Chessconnect: **Chessboard = Millennium**, **Connection method = USB/serial**,
   then select the `Standard Serial over Bluetooth link` port.

## What you should see

```
[startup]   registered SPP profile on channel 3
[startup]   registered Just-Works pairing agent
[accept]    incoming RFCOMM connection from /org/bluez/hci0/dev_XX_XX_XX_XX_XX_XX
[connect]   board session started with ...
[tx]        'v023077'                     <- unlocks the client's parser
[rx]        '5634'                        <- its V request
[rx]        '493030...'                   <- its I00 request
[handshake] client asked for the board name
[tx]        'i00Cherub BT Board\n5B'
[rx]        '5730303030...'               <- the W settings burst
[tx]        'sRNBKQBNRPPPPPPPP...........' <- the board position
[command]   'l' body='L32...'             <- LED patterns it wants drawn
```

Anything it sends that we do not answer is logged rather than fatal, so a partial
handshake still tells us something.

## Wire format notes

Millennium commands are `<letter><payload><XOR parity>` with fixed lengths: `s` is
67 characters (64 squares), `v`/`w`/`r` are 7, `i` is newline-terminated. Squares
are one character each, upper case for white, `.` for empty, ordered
`index = 7 - col + 8 * row` with row 0 = rank 1. The client opens the port at
38400 7-odd-1, but over RFCOMM the line settings are nominal.

## Env vars

| Variable | Default | Purpose |
|---|---|---|
| `BLUEZ_ADAPTER` | `/org/bluez/hci0` | adapter object path |
| `BOARD_CHANNEL` | `3` | RFCOMM channel in the SDP record |
| `BOARD_NAME` | `Cherub BT Board` | name reported to the client |
| `BOARD_FEN` | start position | position reported every 10s |
| `MAKE_DISCOVERABLE` | `1` | set `0` to leave the adapter alone |
| `BE_DEFAULT_AGENT` | `1` | set `0` to not take the adapter's default agent |
| `LOG_HEX` | `1` | set `0` for ascii-only logging |

## Caveats

- Chrome's serial picker must show the Bluetooth COM port; if the device is paired
  but no port exists, that is a Windows-side pairing issue, not this container.
- This impersonates a board for testing on hardware you own. It is not something to
  ship, and it depends on a board protocol that could change in any release.

## Troubleshooting: "it has no org.bluez"

```
FATAL: connected to D-Bus, but it has no org.bluez
```

The socket mount worked - that is why we reached a bus at all - but the bus we
reached is not the one running BlueZ. In order of likelihood:

1. **BlueZ isn't running on the host.** `systemctl status bluetooth`.
2. **BlueZ lives inside another container** (common for BLE workloads that hold the
   dongle in a `--privileged` container), so the host system bus is bare. Check with
   `docker ps` and `docker inspect <name> --format '{{.HostConfig.Privileged}}'`.
   If so, either run the probe against that container's bus:

   ```sh
   PID=$(docker inspect -f '{{.State.Pid}}' <name>)
   docker run --rm --network host \
     -v /proc/$PID/root/run/dbus/system_bus_socket:/run/dbus/system_bus_socket \
     -e DBUS_SYSTEM_BUS_ADDRESS=unix:path=/run/dbus/system_bus_socket \
     ghcr.io/hyper-dragon/dgtangel/bt-board:latest
   ```

   ...or accept that the adapter belongs to that container and give the probe its
   own dongle instead.
3. **The socket you mounted isn't the host's system bus** - check the path in
   `DBUS_SYSTEM_BUS_ADDRESS`.

The preflight prints the bus address, whether the socket exists, and the names it
did see, so the answer is usually in the first three lines.

