#!/usr/bin/env python3
"""Bluetooth SPP "board" probe, for DgtCherub / Chessconnect experiments.

Registers a Serial Port Profile server on the host's existing BlueZ and, when a
browser running Chessconnect pairs with this machine and opens the resulting COM
port, behaves like a Millennium e-board: answers the handshake and reports the
starting position. Everything received is logged, hex and ascii.

Why Millennium: of the board types Chessconnect supports over a serial port, it is
the one with a fully decoded, forgiving ASCII protocol (fixed-length commands with
a 2-hex XOR parity), and it supports LEDs, so it exercises the most of the client.

SAFETY - this container is a guest on an adapter that already runs a BLE workload
(the Neewer strip). It therefore:
  * never writes Adapter1.Powered
  * never restarts bluetoothd, never resets the controller, never runs btmgmt
  * only adds a Profile1 and an Agent1, and only toggles Pairable/Discoverable,
    recording the previous values and restoring them on exit
Nothing here touches the host filesystem or BlueZ configuration.

Environment:
  BLUEZ_ADAPTER     adapter object path        (default /org/bluez/hci0)
  BOARD_CHANNEL     RFCOMM channel 1-30        (default 3)
  BOARD_NAME        reported board name        (default "Cherub BT Board")
  BOARD_FEN         position to report         (default the start position)
  MAKE_DISCOVERABLE "0" to leave the adapter alone (default 1)
  LOG_HEX           "0" to log ascii only      (default 1)
"""

import os
import signal
import socket
import sys
import threading
import time

import dbus
import dbus.mainloop.glib
import dbus.service
from gi.repository import GLib

ADAPTER = os.environ.get("BLUEZ_ADAPTER", "/org/bluez/hci0")
CHANNEL = int(os.environ.get("BOARD_CHANNEL", "3"))
BOARD_NAME = os.environ.get("BOARD_NAME", "Cherub BT Board")
FEN = os.environ.get(
    "BOARD_FEN", "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
)
MAKE_DISCOVERABLE = os.environ.get("MAKE_DISCOVERABLE", "1") != "0"
LOG_HEX = os.environ.get("LOG_HEX", "1") != "0"

SPP_UUID = "00001101-0000-1000-8000-00805F9B34FB"
PROFILE_PATH = "/org/cherub/BtBoard/profile"
AGENT_PATH = "/org/cherub/BtBoard/agent"

BLUEZ = "org.bluez"
PROFILE_IFACE = "org.bluez.Profile1"
AGENT_IFACE = "org.bluez.Agent1"

# Inbound frame lengths, mirrored from Chessconnect's own parser. 'i' is
# variable (newline terminated), 'l' is the 165-char LED command, 'x' is the
# single character "all LEDs off".
INBOUND_LENGTHS = {"v": 7, "w": 7, "s": 67, "r": 7, "x": 1, "l": 165}

_print_lock = threading.Lock()


def log(tag, message):
    stamp = time.strftime("%H:%M:%S")
    with _print_lock:
        print(f"[{stamp}] {tag:<10} {message}", flush=True)


# ---------------------------------------------------------------- wire format


def parity(body):
    """XOR of every character, as two upper-case hex digits."""
    value = 0
    for char in body:
        value ^= ord(char)
    return f"{value & 0xFF:02X}"


def frame(body):
    return body + parity(body)


def version_frame():
    # 7 chars: 'v', four digits, parity. The client ignores input until it has
    # seen one of these, so it goes out unsolicited on connect.
    return frame("v0230")


def board_name_frame():
    # 'i' frames are newline terminated and must be (newline index + 3) long.
    return frame(f"i00{BOARD_NAME}\n")


def echo_frame(command):
    # Reply to W-commands with the same payload, lower-cased command letter.
    body = "w" + command[1:5].ljust(4, "0")
    return frame(body)


def position_frame(fen):
    """64 squares, one char each: index = 7 - col + 8 * row, row 0 = rank 1."""
    payload = ["."] * 64
    ranks = fen.split(" ")[0].split("/")
    for row in range(8):
        col = 0
        for char in ranks[7 - row]:
            if char.isdigit():
                col += int(char)
                continue
            payload[7 - col + 8 * row] = char
            col += 1
    return frame("s" + "".join(payload))


# ---------------------------------------------------------------- the board


class MillenniumBoard:
    """Answers the client's handshake and then idles, reporting its position."""

    def __init__(self, sock, peer):
        self.sock = sock
        self.peer = peer
        self.buffer = ""
        self.ready = threading.Event()
        self.stopped = threading.Event()

    # -- outbound

    def send(self, text):
        if not text:
            return
        log("tx", f"{text!r}")
        try:
            self.sock.sendall(text.encode("ascii", "replace"))
        except OSError as error:
            log("tx", f"send failed: {error}")
            self.stopped.set()

    # -- inbound

    def run(self):
        log("connect", f"board session started with {self.peer}")
        self.send(version_frame())

        reporter = threading.Thread(target=self._report_positions, daemon=True)
        reporter.start()

        try:
            while not self.stopped.is_set():
                chunk = self.sock.recv(4096)
                if not chunk:
                    break
                self._receive(chunk)
        except OSError as error:
            log("connect", f"session error: {error}")
        finally:
            self.stopped.set()
            log("disconnect", f"board session ended with {self.peer}")
            try:
                self.sock.close()
            except OSError:
                pass

    def _receive(self, chunk):
        text = chunk.decode("ascii", "replace")
        if LOG_HEX:
            log("rx", f"{chunk.hex(' ')}")
        log("rx", f"{text!r}")
        self.buffer += text
        self._drain()

    def _drain(self):
        while self.buffer:
            command = self.buffer[0].lower()
            if command == "i":
                newline = self.buffer.find("\n")
                if newline < 0 or len(self.buffer) < newline + 3:
                    return
                length = newline + 3
            else:
                length = INBOUND_LENGTHS.get(command, 1)
            if len(self.buffer) < length:
                return
            raw = self.buffer[:length]
            self.buffer = self.buffer[length:]
            self._handle(raw.strip("\r\n"))

    def _handle(self, raw):
        body = raw[:-2] if len(raw) > 2 else ""
        letter = raw[0].lower() if raw else "?"

        if letter == "v":
            log("handshake", "client asked for the version")
            self.send(version_frame())
        elif letter == "i":
            log("handshake", "client asked for the board name")
            self.send(board_name_frame())
            self.ready.set()
        elif letter == "w":
            self.send(echo_frame(raw))
        else:
            # LED commands (l / x) and anything else: logged, not acted on.
            log("command", f"{letter!r} body={body!r}")

    def _report_positions(self):
        # Give the client a moment to finish its handshake, then keep reporting.
        if not self.ready.wait(timeout=10):
            log("handshake", "no board-name request within 10s - reporting anyway")
        while not self.stopped.wait(timeout=10):
            self.send(position_frame(FEN))


# ---------------------------------------------------------------- BlueZ glue


class Profile(dbus.service.Object):
    def __init__(self, bus):
        super().__init__(bus, PROFILE_PATH)

    @dbus.service.method(PROFILE_IFACE, in_signature="oha{sv}", out_signature="")
    def NewConnection(self, device, fd, properties):
        sock = socket.socket(fileno=fd.take())
        log("accept", f"incoming RFCOMM connection from {device}")
        threading.Thread(
            target=MillenniumBoard(sock, str(device)).run, daemon=True
        ).start()

    @dbus.service.method(PROFILE_IFACE, in_signature="o", out_signature="")
    def RequestDisconnection(self, device):
        log("accept", f"disconnect requested by {device}")

    @dbus.service.method(PROFILE_IFACE, in_signature="", out_signature="")
    def Release(self):
        log("accept", "profile released")


class Agent(dbus.service.Object):
    """Just Works: no PIN is ever shown to the user."""

    def __init__(self, bus):
        super().__init__(bus, AGENT_PATH)

    @dbus.service.method(AGENT_IFACE, in_signature="", out_signature="")
    def Release(self):
        pass

    @dbus.service.method(AGENT_IFACE, in_signature="os", out_signature="")
    def AuthorizeService(self, device, uuid):
        log("pairing", f"authorised service {uuid} for {device}")

    @dbus.service.method(AGENT_IFACE, in_signature="ou", out_signature="")
    def RequestConfirmation(self, device, passkey):
        log("pairing", f"confirming passkey {passkey:06d} for {device}")

    @dbus.service.method(AGENT_IFACE, in_signature="o", out_signature="")
    def RequestAuthorization(self, device):
        log("pairing", f"authorised {device}")

    @dbus.service.method(AGENT_IFACE, in_signature="", out_signature="")
    def Cancel(self):
        pass


def main():
    dbus.mainloop.glib.DBusGMainLoop(set_as_default=True)
    bus = dbus.SystemBus()

    adapter = dbus.Interface(
        bus.get_object(BLUEZ, ADAPTER), "org.bluez.Adapter1"
    )
    properties = dbus.Interface(
        bus.get_object(BLUEZ, ADAPTER), "org.freedesktop.DBus.Properties"
    )

    log("startup", f"adapter {ADAPTER} address={properties.Get('org.bluez.Adapter1', 'Address')}")

    profile = Profile(bus)
    agent = Agent(bus)

    manager = dbus.Interface(
        bus.get_object(BLUEZ, "/org/bluez"), "org.bluez.ProfileManager1"
    )
    manager.RegisterProfile(
        PROFILE_PATH,
        SPP_UUID,
        {
            "Name": dbus.String("Cherub Board (probe)"),
            "Role": dbus.String("server"),
            "Channel": dbus.Byte(CHANNEL),
            "RequireAuthentication": dbus.Boolean(False),
            "RequireAuthorization": dbus.Boolean(False),
            "AutoConnect": dbus.Boolean(True),
        },
    )
    log("startup", f"registered SPP profile on channel {CHANNEL}")

    agent_manager = dbus.Interface(
        bus.get_object(BLUEZ, "/org/bluez"), "org.bluez.AgentManager1"
    )
    agent_manager.RegisterAgent(AGENT_PATH, "NoInputNoOutput")
    agent_manager.RequestDefaultAgent(AGENT_PATH)
    log("startup", "registered Just-Works pairing agent")

    previous = {}
    if MAKE_DISCOVERABLE:
        for name in ("Discoverable", "Pairable"):
            previous[name] = bool(properties.Get("org.bluez.Adapter1", name))
            properties.Set("org.bluez.Adapter1", name, dbus.Boolean(True))
        log("startup", f"adapter discoverable/pairable (was {previous})")

    def shutdown(*_):
        log("shutdown", "restoring adapter state and unregistering")
        try:
            if MAKE_DISCOVERABLE:
                for name, value in previous.items():
                    properties.Set("org.bluez.Adapter1", name, dbus.Boolean(value))
        except Exception as error:  # noqa: BLE001 - never fail on the way out
            log("shutdown", f"could not restore adapter state: {error}")
        for unregister in (
            lambda: manager.UnregisterProfile(PROFILE_PATH),
            lambda: agent_manager.UnregisterAgent(AGENT_PATH),
        ):
            try:
                unregister()
            except Exception as error:  # noqa: BLE001
                log("shutdown", f"cleanup warning: {error}")
        GLib.MainLoop().quit()
        sys.exit(0)

    signal.signal(signal.SIGINT, shutdown)
    signal.signal(signal.SIGTERM, shutdown)

    log("startup", "ready - pair with this machine from Windows, then pick")
    log("startup", "  Chessboard: Millennium   Connection method: USB/serial")
    log("startup", "and select the 'Standard Serial over Bluetooth link' COM port")

    GLib.MainLoop().run()


if __name__ == "__main__":
    main()
